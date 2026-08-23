using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
	/// <summary>
	/// Loads crafting recipes and performs inventory-backed crafting transactions.
	/// </summary>
	/// <remarks>
	/// CraftingManager owns recipe validation and material/result exchange. Recipe authoring data lives in
	/// CraftingRecipe resources; UI should ask this manager for craftability and result messages.
	/// </remarks>
	public partial class CraftingManager
	{
		private readonly InventoryManager _inventory;
		private readonly MasterRepository _db;
		private readonly List<CraftingRecipe> _recipes = new();

		public CraftingManager(InventoryManager inventory, MasterRepository db)
		{
			_inventory = inventory;
			_db = db;
		}

		/// <summary>
		/// Loaded recipe definitions sorted for predictable UI display.
		/// </summary>
		public IReadOnlyList<CraftingRecipe> Recipes => _recipes;

		/// <summary>
		/// Recursively loads .tres/.res recipe resources from the configured recipe folder.
		/// </summary>
		public void LoadRecipes(string rootPath)
		{
			_recipes.Clear();

			if (string.IsNullOrWhiteSpace(rootPath))
			{
				GD.PushWarning("CraftingManager.LoadRecipes: recipe folder path is empty.");
				return;
			}

			if (!rootPath.EndsWith("/"))
			{
				rootPath += "/";
			}

			if (!DirAccess.DirExistsAbsolute(rootPath))
			{
				GD.PushWarning($"CraftingManager.LoadRecipes: recipe folder does not exist: {rootPath}");
				return;
			}

			int loaded = 0;
			int skipped = 0;
			LoadRecipesRecursive(rootPath, ref loaded, ref skipped);
			_recipes.Sort(CompareRecipes);
			GD.Print($"CraftingManager.LoadRecipes: loaded={loaded} skipped={skipped} source={rootPath}");
		}

		/// <summary>
		/// Returns unlocked recipes visible at the requested station and optional category.
		/// </summary>
		public IReadOnlyList<CraftingRecipe> GetRecipes(string stationType, CraftingCategory? category = null)
		{
			return _recipes
				.Where(recipe => IsRecipeVisibleAtStation(recipe, stationType))
				.Where(recipe => category == null || recipe.Category == category.Value)
				.OrderBy(recipe => recipe.SortOrder)
				.ThenBy(recipe => recipe.ResolveDisplayName(_db), StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		/// <summary>
		/// Looks up a loaded recipe by stable RecipeId.
		/// </summary>
		public CraftingRecipe GetRecipe(string recipeId)
		{
			if (string.IsNullOrWhiteSpace(recipeId))
			{
				return null;
			}

			return _recipes.FirstOrDefault(recipe => string.Equals(recipe.RecipeId, recipeId, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns the current owned quantity for an ingredient item ID.
		/// </summary>
		public int GetOwnedQuantity(int itemId)
		{
			return _inventory?.GetItemCount(itemId) ?? 0;
		}

		/// <summary>
		/// Checks whether a recipe can currently be crafted.
		/// </summary>
		public bool CanCraft(CraftingRecipe recipe)
		{
			return GetCraftBlockReason(recipe) == null;
		}

		/// <summary>
		/// Returns a user-facing reason a recipe cannot be crafted, or null when it can be crafted.
		/// </summary>
		public string GetCraftBlockReason(CraftingRecipe recipe)
		{
			if (recipe == null)
			{
				return "Recipe is missing.";
			}

			if (!recipe.IsUnlocked)
			{
				return "Recipe is locked.";
			}

			if (_inventory == null)
			{
				return "Inventory is unavailable.";
			}

			if (_db?.GetItemFromRepo(recipe.ResultItemId) == null)
			{
				return $"Result item id {recipe.ResultItemId} is missing from MasterRepository.";
			}

			int resultQuantity = Math.Max(1, recipe.ResultQuantity);
			if (!_inventory.CanAddItem(recipe.ResultItemId, resultQuantity))
			{
				return "Inventory cannot hold the crafted item.";
			}

			foreach (CraftingIngredient ingredient in recipe.RequiredMaterials)
			{
				if (ingredient == null || ingredient.ItemId <= 0 || ingredient.Quantity <= 0)
				{
					return "Recipe has an invalid ingredient.";
				}

				if (_db.GetItemFromRepo(ingredient.ItemId) == null)
				{
					return $"Ingredient item id {ingredient.ItemId} is missing from MasterRepository.";
				}

				if (!_inventory.HasItemQuantity(ingredient.ItemId, ingredient.Quantity))
				{
					string itemName = _db.GetItemDisplayName(ingredient.ItemId);
					return $"Missing {itemName}: {_inventory.GetItemCount(ingredient.ItemId)}/{ingredient.Quantity}.";
				}
			}

			return null;
		}

		/// <summary>
		/// Removes required materials and adds the result item as one gameplay transaction.
		/// </summary>
		public CraftingAttemptResult Craft(CraftingRecipe recipe)
		{
			string blockReason = GetCraftBlockReason(recipe);
			if (blockReason != null)
			{
				return new CraftingAttemptResult(false, blockReason);
			}

			foreach (CraftingIngredient ingredient in recipe.RequiredMaterials)
			{
				if (!_inventory.RemoveItemQuantity(ingredient.ItemId, ingredient.Quantity))
				{
					return new CraftingAttemptResult(false, $"Could not remove required item {ingredient.ItemId}.");
				}
			}

			if (!_inventory.AddItemQuantity(recipe.ResultItemId, Math.Max(1, recipe.ResultQuantity)))
			{
				GD.PushError($"CraftingManager.Craft: materials were removed but result item {recipe.ResultItemId} could not be added.");
				return new CraftingAttemptResult(false, "Crafting failed while adding result item.");
			}

			string resultName = _db.GetItemDisplayName(recipe.ResultItemId);
			return new CraftingAttemptResult(true, $"Crafted: {resultName}");
		}

		private void LoadRecipesRecursive(string dirPath, ref int loaded, ref int skipped)
		{
			using var dir = DirAccess.Open(dirPath);
			if (dir == null)
			{
				GD.PushWarning($"CraftingManager.LoadRecipesRecursive: failed to open directory: {dirPath}");
				return;
			}

			dir.ListDirBegin();
			while (true)
			{
				string entry = dir.GetNext();
				if (string.IsNullOrEmpty(entry))
				{
					break;
				}

				if (entry is "." or "..")
				{
					continue;
				}

				string path = dirPath + entry;
				if (dir.CurrentIsDir())
				{
					LoadRecipesRecursive(path + "/", ref loaded, ref skipped);
					continue;
				}

				if (!entry.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
					&& !entry.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				CraftingRecipe recipe = ResourceLoader.Load<CraftingRecipe>(path);
				if (!ValidateRecipe(recipe, path))
				{
					skipped++;
					continue;
				}

				_recipes.Add(recipe);
				loaded++;
			}
			dir.ListDirEnd();
		}

		private bool ValidateRecipe(CraftingRecipe recipe, string sourcePath)
		{
			if (recipe == null)
			{
				GD.PushWarning($"CraftingManager: skipped non-recipe resource: {sourcePath}");
				return false;
			}

			if (string.IsNullOrWhiteSpace(recipe.RecipeId))
			{
				GD.PushWarning($"CraftingManager: recipe in {sourcePath} is missing RecipeId.");
				return false;
			}

			if (_recipes.Any(existing => string.Equals(existing.RecipeId, recipe.RecipeId, StringComparison.OrdinalIgnoreCase)))
			{
				GD.PushWarning($"CraftingManager: duplicate RecipeId '{recipe.RecipeId}' in {sourcePath}.");
				return false;
			}

			if (recipe.ResultItemId <= 0 || _db.GetItemFromRepo(recipe.ResultItemId) == null)
			{
				GD.PushWarning($"CraftingManager: recipe '{recipe.RecipeId}' result item id {recipe.ResultItemId} was not found.");
				return false;
			}

			if (recipe.RequiredMaterials == null || recipe.RequiredMaterials.Count == 0)
			{
				GD.PushWarning($"CraftingManager: recipe '{recipe.RecipeId}' has no required materials.");
				return false;
			}

			foreach (CraftingIngredient ingredient in recipe.RequiredMaterials)
			{
				if (ingredient == null || ingredient.ItemId <= 0 || ingredient.Quantity <= 0)
				{
					GD.PushWarning($"CraftingManager: recipe '{recipe.RecipeId}' has an invalid ingredient.");
					return false;
				}

				if (_db.GetItemFromRepo(ingredient.ItemId) == null)
				{
					GD.PushWarning($"CraftingManager: recipe '{recipe.RecipeId}' ingredient item id {ingredient.ItemId} was not found.");
					return false;
				}
			}

			return true;
		}

		private static bool IsRecipeVisibleAtStation(CraftingRecipe recipe, string stationType)
		{
			if (recipe == null || !recipe.IsUnlocked)
			{
				return false;
			}

			if (string.IsNullOrWhiteSpace(recipe.RequiredStationType))
			{
				return true;
			}

			return string.Equals(recipe.RequiredStationType, stationType, StringComparison.OrdinalIgnoreCase);
		}

		private static int CompareRecipes(CraftingRecipe left, CraftingRecipe right)
		{
			int sort = left.SortOrder.CompareTo(right.SortOrder);
			if (sort != 0)
			{
				return sort;
			}

			return string.Compare(left.RecipeId, right.RecipeId, StringComparison.OrdinalIgnoreCase);
		}
	}
}
