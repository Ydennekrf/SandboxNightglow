using Godot;
using Godot.Collections;

namespace ethra.V1
{
	/// <summary>
	/// Static authored recipe data loaded from .tres/.res resources by CraftingManager.
	/// </summary>
	/// <remarks>
	/// RecipeId must remain stable across saves and UI references. Runtime craftability belongs in
	/// CraftingManager, not in the resource.
	/// </remarks>
	[GlobalClass]
	public partial class CraftingRecipe : Resource
	{
		/// <summary>Stable recipe ID used for lookup and UI selection.</summary>
		[Export] public string RecipeId { get; set; } = string.Empty;
		/// <summary>Optional authored name; falls back to the result item name when empty.</summary>
		[Export] public string DisplayName { get; set; } = string.Empty;
		/// <summary>Category used to group/filter recipes in crafting UI.</summary>
		[Export] public CraftingCategory Category { get; set; } = CraftingCategory.Misc;
		/// <summary>Static item ID produced by this recipe.</summary>
		[Export] public int ResultItemId { get; set; }
		/// <summary>Quantity of the result item added on a successful craft.</summary>
		[Export] public int ResultQuantity { get; set; } = 1;
		/// <summary>Required item IDs and quantities consumed by the crafting transaction.</summary>
		[Export] public Array<CraftingIngredient> RequiredMaterials { get; set; } = new();
		/// <summary>Optional station type gate; empty means any station can show the recipe.</summary>
		[Export] public string RequiredStationType { get; set; } = string.Empty;
		/// <summary>Whether the recipe should be visible/craftable without additional unlock logic.</summary>
		[Export] public bool IsUnlocked { get; set; } = true;
		/// <summary>Primary sort value for crafting UI lists.</summary>
		[Export] public int SortOrder { get; set; }
		/// <summary>Optional multiline flavor or instruction text shown by crafting UI.</summary>
		[Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;

		/// <summary>
		/// Returns the authored name, result item name, or RecipeId as a display-safe fallback chain.
		/// </summary>
		public string ResolveDisplayName(MasterRepository db)
		{
			if (!string.IsNullOrWhiteSpace(DisplayName))
			{
				return DisplayName;
			}

			return db?.GetItemFromRepo(ResultItemId)?.Name ?? RecipeId;
		}
	}
}
