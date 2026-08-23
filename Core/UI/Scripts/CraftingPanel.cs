using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
	public partial class CraftingPanel : Control
	{
		[Export] public string DefaultStationType { get; set; } = "Workbench";
		[Export] public NodePath TitleLabelPath { get; set; } = "Panel/Margin/Root/Header/TitleLabel";
		[Export] public NodePath CloseButtonPath { get; set; } = "Panel/Margin/Root/Header/CloseButton";
		[Export] public NodePath RecipeListPath { get; set; } = "Panel/Margin/Root/Body/RecipeColumn/RecipeScroll/RecipeList";
		[Export] public NodePath ResultIconPath { get; set; } = "Panel/Margin/Root/Body/DetailsColumn/ResultRow/IconFrame/ResultIcon";
		[Export] public NodePath ResultLabelPath { get; set; } = "Panel/Margin/Root/Body/DetailsColumn/ResultRow/ResultLabel";
		[Export] public NodePath DescriptionLabelPath { get; set; } = "Panel/Margin/Root/Body/DetailsColumn/DescriptionLabel";
		[Export] public NodePath MaterialsListPath { get; set; } = "Panel/Margin/Root/Body/DetailsColumn/MaterialsScroll/MaterialsList";
		[Export] public NodePath MessageLabelPath { get; set; } = "Panel/Margin/Root/Body/DetailsColumn/MessageLabel";
		[Export] public NodePath CraftButtonPath { get; set; } = "Panel/Margin/Root/Body/DetailsColumn/Actions/CraftButton";
		[Export] public NodePath BodyPath { get; set; } = "Panel/Margin/Root/Body";

		private const float RowHeight = 34f;

		private string _stationType;
		private CraftingRecipe _selectedRecipe;
		private VBoxContainer _recipeList;
		private Label _titleLabel;
		private Label _resultLabel;
		private Label _descriptionLabel;
		private TextureRect _resultIcon;
		private VBoxContainer _materialsList;
		private Button _craftButton;
		private Button _closeButton;
		private Label _messageLabel;
		private Control _craftingBody;
		private VBoxContainer _upgradePanel;
		private VBoxContainer _weaponList;
		private VBoxContainer _upgradeRuneList;
		private VBoxContainer _elementalRuneList;
		private Label _weaponDetailsLabel;
		private Label _upgradeMessageLabel;
		private Button _craftingTabButton;
		private Button _upgradeTabButton;
		private Button _previewButton;
		private Button _confirmSocketButton;
		private Button _cancelSocketButton;
		private bool _showingUpgradeTab;
		private string _selectedWeaponInstanceId;
		private string _pendingSocketWeaponInstanceId;
		private int _pendingSocketRuneItemId;
		private PendingSocketKind _pendingSocketKind = PendingSocketKind.None;
		private readonly List<CraftingRecipe> _currentRecipes = new();

		private enum PendingSocketKind
		{
			None,
			Upgrade,
			Elemental
		}

		public override void _Ready()
		{
			MouseFilter = MouseFilterEnum.Stop;
			ResolveNodes();
			BuildUpgradeTabUi();
			Visible = false;

			if (_closeButton != null)
			{
				_closeButton.Pressed += Close;
			}

			if (_craftButton != null)
			{
				_craftButton.Pressed += OnCraftPressed;
			}

			if (GameManager.Instance?.Inventory != null)
			{
				GameManager.Instance.Inventory.Changed += OnInventoryChanged;
			}
		}

		public override void _ExitTree()
		{
			if (_closeButton != null)
			{
				_closeButton.Pressed -= Close;
			}

			if (_craftButton != null)
			{
				_craftButton.Pressed -= OnCraftPressed;
			}

			if (_confirmSocketButton != null)
			{
				_confirmSocketButton.Pressed -= ConfirmPendingSocket;
			}

			if (_cancelSocketButton != null)
			{
				_cancelSocketButton.Pressed -= CancelPendingSocket;
			}

			if (GameManager.Instance?.Inventory != null)
			{
				GameManager.Instance.Inventory.Changed -= OnInventoryChanged;
			}
		}

		public void Open(string stationType)
		{
			_stationType = string.IsNullOrWhiteSpace(stationType) ? DefaultStationType : stationType;
			Visible = true;
			ShowCraftingTab();
			SetMessage(string.Empty);
			RefreshRecipes();
			RefreshWeaponUpgradePanel();
		}

		public void Close()
		{
			Visible = false;
			_selectedRecipe = null;
			ClearPendingSocket();
			SetMessage(string.Empty);
		}

		private void ResolveNodes()
		{
			_titleLabel = GetNodeOrNull<Label>(TitleLabelPath);
			_closeButton = GetNodeOrNull<Button>(CloseButtonPath);
			_recipeList = GetNodeOrNull<VBoxContainer>(RecipeListPath);
			_resultIcon = GetNodeOrNull<TextureRect>(ResultIconPath);
			_resultLabel = GetNodeOrNull<Label>(ResultLabelPath);
			_descriptionLabel = GetNodeOrNull<Label>(DescriptionLabelPath);
			_materialsList = GetNodeOrNull<VBoxContainer>(MaterialsListPath);
			_messageLabel = GetNodeOrNull<Label>(MessageLabelPath);
			_craftButton = GetNodeOrNull<Button>(CraftButtonPath);
			_craftingBody = GetNodeOrNull<Control>(BodyPath);

			if (_resultIcon != null)
			{
				_resultIcon.CustomMinimumSize = new Vector2(48f, 48f);
				_resultIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
				_resultIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			}

			if (_recipeList == null || _resultLabel == null || _descriptionLabel == null || _materialsList == null || _craftButton == null)
			{
				GD.PushError("CraftingPanel: one or more exported node paths are invalid. Check CraftingPanel.tscn wiring.");
			}
		}

		private void BuildUpgradeTabUi()
		{
			VBoxContainer root = GetNodeOrNull<VBoxContainer>("Panel/Margin/Root");
			if (root == null || _craftingBody == null || root.GetNodeOrNull("CraftingTabs") != null)
			{
				return;
			}

			HBoxContainer tabs = new()
			{
				Name = "CraftingTabs",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_craftingTabButton = new Button { Text = "Crafting", ToggleMode = true, ButtonPressed = true };
			_upgradeTabButton = new Button { Text = "Weapon Upgrade", ToggleMode = true };
			_craftingTabButton.Pressed += ShowCraftingTab;
			_upgradeTabButton.Pressed += ShowUpgradeTab;
			tabs.AddChild(_craftingTabButton);
			tabs.AddChild(_upgradeTabButton);
			root.AddChild(tabs);
			root.MoveChild(tabs, Mathf.Min(1, root.GetChildCount() - 1));

			_upgradePanel = new VBoxContainer
			{
				Name = "WeaponUpgradePanel",
				Visible = false,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			root.AddChild(_upgradePanel);

			HBoxContainer body = new()
			{
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			_upgradePanel.AddChild(body);

			_weaponList = BuildColumn(body, "Weapons", 220f);
			VBoxContainer details = BuildColumn(body, "Selected Weapon", 260f);
			_upgradeRuneList = BuildColumn(body, "Upgrade Runes", 200f);
			_elementalRuneList = BuildColumn(body, "Elemental Runes", 200f);

			_weaponDetailsLabel = new Label
			{
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			details.AddChild(_weaponDetailsLabel);

			_previewButton = new Button
			{
				Text = "Preview Magic Shape",
				CustomMinimumSize = new Vector2(170f, RowHeight)
			};
			_previewButton.Pressed += PreviewSelectedWeaponMagic;
			details.AddChild(_previewButton);

			HBoxContainer socketActions = new()
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			details.AddChild(socketActions);

			_confirmSocketButton = new Button
			{
				Text = "Socket Rune",
				Disabled = true,
				CustomMinimumSize = new Vector2(120f, RowHeight)
			};
			_confirmSocketButton.Pressed += ConfirmPendingSocket;
			socketActions.AddChild(_confirmSocketButton);

			_cancelSocketButton = new Button
			{
				Text = "Cancel",
				Disabled = true,
				CustomMinimumSize = new Vector2(90f, RowHeight)
			};
			_cancelSocketButton.Pressed += CancelPendingSocket;
			socketActions.AddChild(_cancelSocketButton);

			_upgradeMessageLabel = new Label
			{
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				CustomMinimumSize = new Vector2(0, 34f)
			};
			_upgradePanel.AddChild(_upgradeMessageLabel);
		}

		private static VBoxContainer BuildColumn(HBoxContainer parent, string title, float width)
		{
			VBoxContainer column = new()
			{
				CustomMinimumSize = new Vector2(width, 0),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			parent.AddChild(column);
			column.AddChild(new Label { Text = title });

			ScrollContainer scroll = new()
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			column.AddChild(scroll);

			VBoxContainer list = new()
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			scroll.AddChild(list);
			return list;
		}

		private void ShowCraftingTab()
		{
			_showingUpgradeTab = false;
			if (_craftingBody != null)
			{
				_craftingBody.Visible = true;
			}

			if (_upgradePanel != null)
			{
				_upgradePanel.Visible = false;
			}

			if (_craftingTabButton != null)
			{
				_craftingTabButton.SetPressedNoSignal(true);
			}

			if (_upgradeTabButton != null)
			{
				_upgradeTabButton.SetPressedNoSignal(false);
			}
		}

		private void ShowUpgradeTab()
		{
			_showingUpgradeTab = true;
			if (_craftingBody != null)
			{
				_craftingBody.Visible = false;
			}

			if (_upgradePanel != null)
			{
				_upgradePanel.Visible = true;
			}

			if (_craftingTabButton != null)
			{
				_craftingTabButton.SetPressedNoSignal(false);
			}

			if (_upgradeTabButton != null)
			{
				_upgradeTabButton.SetPressedNoSignal(true);
			}

			RefreshWeaponUpgradePanel();
		}

		private bool HasRequiredNodes()
		{
			return _recipeList != null
				&& _resultLabel != null
				&& _descriptionLabel != null
				&& _materialsList != null
				&& _craftButton != null;
		}

		private void RefreshRecipes()
		{
			if (!HasRequiredNodes())
			{
				return;
			}

			foreach (Node child in _recipeList.GetChildren())
			{
				child.QueueFree();
			}

			_currentRecipes.Clear();
			CraftingManager crafting = GameManager.Instance?.Crafting;
			if (crafting == null)
			{
				_resultLabel.Text = "Crafting is unavailable.";
				_descriptionLabel.Text = string.Empty;
				_craftButton.Disabled = true;
				return;
			}

			if (_titleLabel != null)
			{
				_titleLabel.Text = $"Crafting - {_stationType}";
			}

			IReadOnlyList<CraftingRecipe> recipes = crafting.GetRecipes(_stationType);
			foreach (CraftingRecipe recipe in recipes)
			{
				_currentRecipes.Add(recipe);
				_recipeList.AddChild(BuildRecipeButton(recipe, crafting.CanCraft(recipe)));
			}

			if (_selectedRecipe == null || !_currentRecipes.Contains(_selectedRecipe))
			{
				_selectedRecipe = _currentRecipes.Count > 0 ? _currentRecipes[0] : null;
			}

			RefreshDetails();
		}

		private Button BuildRecipeButton(CraftingRecipe recipe, bool canCraft)
		{
			string marker = canCraft ? "[Can Craft]" : "[Missing Materials]";
			Button row = new()
			{
				Text = $"{marker} {recipe.ResolveDisplayName(GameManager.Instance?.DB)}",
				CustomMinimumSize = new Vector2(250f, RowHeight),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Disabled = false,
				Modulate = canCraft ? new Color(0.72f, 1f, 0.72f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f)
			};
			row.Pressed += () => SelectRecipe(recipe);
			return row;
		}

		private void SelectRecipe(CraftingRecipe recipe)
		{
			if (recipe == null)
			{
				return;
			}

			_selectedRecipe = recipe;
			SetMessage(string.Empty);
			RefreshDetails();
		}

		private void RefreshDetails()
		{
			if (!HasRequiredNodes())
			{
				return;
			}

			ClearMaterials();

			CraftingManager crafting = GameManager.Instance?.Crafting;
			MasterRepository db = GameManager.Instance?.DB;
			if (_selectedRecipe == null || crafting == null || db == null)
			{
				if (_resultIcon != null)
				{
					_resultIcon.Texture = null;
				}
				_resultLabel.Text = "No recipes available.";
				_descriptionLabel.Text = string.Empty;
				_craftButton.Disabled = true;
				return;
			}

			string resultName = db.GetItemDisplayName(_selectedRecipe.ResultItemId);
			if (_resultIcon != null)
			{
				_resultIcon.Texture = db.GetItemIcon(_selectedRecipe.ResultItemId);
			}
			_resultLabel.Text = $"{resultName} x{GetSafeQuantity(_selectedRecipe.ResultQuantity)}";
			_descriptionLabel.Text = string.IsNullOrWhiteSpace(_selectedRecipe.Description)
				? "No recipe description authored yet."
				: _selectedRecipe.Description;

			foreach (CraftingIngredient ingredient in _selectedRecipe.RequiredMaterials)
			{
				if (ingredient == null)
				{
					continue;
				}

				int owned = crafting.GetOwnedQuantity(ingredient.ItemId);
				bool hasEnough = owned >= ingredient.Quantity;
				Label materialLabel = new()
				{
					Text = $"{db.GetItemDisplayName(ingredient.ItemId)}: {owned}/{ingredient.Quantity}",
					Modulate = hasEnough ? new Color(0.72f, 1f, 0.72f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f),
					AutowrapMode = TextServer.AutowrapMode.WordSmart
				};
				_materialsList.AddChild(materialLabel);
			}

			string blockReason = crafting.GetCraftBlockReason(_selectedRecipe);
			_craftButton.Disabled = blockReason != null;
			SetMessage(blockReason ?? "Ready to craft.");
		}

		private void OnCraftPressed()
		{
			CraftingManager crafting = GameManager.Instance?.Crafting;
			if (_selectedRecipe == null || crafting == null)
			{
				return;
			}

			CraftingAttemptResult result = crafting.Craft(_selectedRecipe);
			AudioManager.PlayUi(result.Success ? "sound.crafting.craft_success" : "sound.ui.cancel");
			SetMessage(result.Message);
			GameManager.Instance?.Publish(
				GameEvent.NotificationRequested,
				new NotificationRequest(result.Message, result.Success ? NotificationType.Loot : NotificationType.Error));

			RefreshRecipes();
			RefreshWeaponUpgradePanel();
		}

		private void OnInventoryChanged()
		{
			if (!Visible)
			{
				return;
			}

			RefreshRecipes();
		}

		private void ClearMaterials()
		{
			if (_materialsList == null)
			{
				return;
			}

			foreach (Node child in _materialsList.GetChildren())
			{
				child.QueueFree();
			}
		}

		private void RefreshWeaponUpgradePanel()
		{
			if (_upgradePanel == null)
			{
				return;
			}

			ClearList(_weaponList);
			ClearList(_upgradeRuneList);
			ClearList(_elementalRuneList);

			WeaponUpgradeManager upgrades = GameManager.Instance?.WeaponUpgrades;
			MasterRepository db = GameManager.Instance?.DB;
			if (upgrades == null || db == null)
			{
				SetUpgradeMessage("Weapon upgrading is unavailable.");
				return;
			}

			IReadOnlyList<WeaponInstanceState> weapons = upgrades.GetWeapons();
			if (weapons.Count == 0)
			{
				AddInfo(_weaponList, "No weapons available.");
				_selectedWeaponInstanceId = null;
			}
			else
			{
				if (string.IsNullOrWhiteSpace(_selectedWeaponInstanceId) || weapons.All(w => w.InstanceId != _selectedWeaponInstanceId))
				{
					_selectedWeaponInstanceId = weapons[0].InstanceId;
				}

				foreach (WeaponInstanceState weapon in weapons)
				{
					WeaponInstanceState captured = weapon;
					Button row = new()
					{
						Text = $"{db.GetItemDisplayName(weapon.ItemId)} #{ShortId(weapon.InstanceId)}",
						CustomMinimumSize = new Vector2(0, RowHeight),
						SizeFlagsHorizontal = SizeFlags.ExpandFill,
						Modulate = weapon.InstanceId == _selectedWeaponInstanceId ? new Color(0.75f, 0.9f, 1f, 1f) : Colors.White
					};
					row.Pressed += () =>
					{
						_selectedWeaponInstanceId = captured.InstanceId;
						ClearPendingSocket();
						RefreshWeaponUpgradePanel();
					};
					_weaponList.AddChild(row);
				}
			}

			WeaponInstanceState selected = GameManager.Instance?.Inventory?.GetWeaponInstance(_selectedWeaponInstanceId);
			RefreshWeaponDetails(selected, upgrades, db);
			RefreshRuneLists(selected, upgrades);
		}

		private void RefreshWeaponDetails(WeaponInstanceState selected, WeaponUpgradeManager upgrades, MasterRepository db)
		{
			if (_weaponDetailsLabel == null)
			{
				return;
			}

			if (selected == null || db.GetItemFromRepo(selected.ItemId) is not WeaponItem weapon)
			{
				_weaponDetailsLabel.Text = "Select a weapon.";
				if (_previewButton != null)
				{
					_previewButton.Disabled = true;
				}
				return;
			}

			List<string> upgradeSlots = new();
			for (int i = 0; i < weapon.UpgradeRuneSlotCount; i++)
			{
				string runeName = i < selected.UpgradeRuneItemIds.Count
					? db.GetItemDisplayName(selected.UpgradeRuneItemIds[i])
					: "Empty";
				upgradeSlots.Add($"Slot {i + 1}: {runeName}");
			}

			string elemental = !weapon.HasElementalRuneSlot
				? "This weapon has no elemental slot."
				: selected.ElementalRuneItemId.HasValue
					? db.GetItemDisplayName(selected.ElementalRuneItemId.Value)
					: "Empty";

			_weaponDetailsLabel.Text =
				$"{weapon.Name}\n" +
				$"Upgrade slots: {(weapon.UpgradeRuneSlotCount > 0 ? string.Join("\n", upgradeSlots) : "This weapon has no rune slots.")}\n" +
				$"Elemental slot: {elemental}\n\n" +
				upgrades.BuildModifierSummary(selected);

			if (_previewButton != null)
			{
				_previewButton.Disabled = !selected.ElementalRuneItemId.HasValue;
			}
		}

		private void RefreshRuneLists(WeaponInstanceState selected, WeaponUpgradeManager upgrades)
		{
			IReadOnlyList<RuneItem> upgradeRunes = upgrades.GetAvailableUpgradeRunes();
			IReadOnlyList<RuneItem> elementalRunes = upgrades.GetAvailableElementalRunes();

			if (upgradeRunes.Count == 0)
			{
				AddInfo(_upgradeRuneList, "No runes available.");
			}
			else
			{
				foreach (RuneItem rune in upgradeRunes)
				{
					AddRuneButton(_upgradeRuneList, rune, () => QueueSocketSelection(selected, rune, PendingSocketKind.Upgrade));
				}
			}

			if (elementalRunes.Count == 0)
			{
				AddInfo(_elementalRuneList, "No runes available.");
			}
			else
			{
				foreach (RuneItem rune in elementalRunes)
				{
					AddRuneButton(_elementalRuneList, rune, () => QueueSocketSelection(selected, rune, PendingSocketKind.Elemental));
				}
			}
		}

		private void AddRuneButton(VBoxContainer list, RuneItem rune, System.Action pressed)
		{
			Button row = new()
			{
				Text = $"{rune.Name} ({GameManager.Instance?.Inventory?.GetItemCount(rune.Id) ?? 0})",
				TooltipText = rune.Description,
				CustomMinimumSize = new Vector2(0, RowHeight),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			row.Pressed += pressed;
			list.AddChild(row);
		}

		private void QueueSocketSelection(WeaponInstanceState weapon, RuneItem rune, PendingSocketKind socketKind)
		{
			MasterRepository db = GameManager.Instance?.DB;
			if (weapon == null || rune == null || db == null)
			{
				SetUpgradeMessage("Select a weapon and rune first.");
				SetSocketConfirmationEnabled(false);
				return;
			}

			_pendingSocketWeaponInstanceId = weapon.InstanceId;
			_pendingSocketRuneItemId = rune.Id;
			_pendingSocketKind = socketKind;
			SetSocketConfirmationEnabled(true);
			SetUpgradeMessage($"Confirm socketing {rune.Name} into {db.GetItemDisplayName(weapon.ItemId)} #{ShortId(weapon.InstanceId)}.");
		}

		private void ConfirmPendingSocket()
		{
			WeaponUpgradeManager upgrades = GameManager.Instance?.WeaponUpgrades;
			if (upgrades == null || _pendingSocketKind == PendingSocketKind.None)
			{
				SetUpgradeMessage("Choose a rune to socket first.");
				return;
			}

			WeaponSocketingResult result = _pendingSocketKind == PendingSocketKind.Elemental
				? upgrades.SocketElementalRune(_pendingSocketWeaponInstanceId, _pendingSocketRuneItemId)
				: upgrades.SocketUpgradeRune(_pendingSocketWeaponInstanceId, _pendingSocketRuneItemId);

			ClearPendingSocket();
			ApplySocketResult(result);
		}

		private void CancelPendingSocket()
		{
			ClearPendingSocket();
			SetUpgradeMessage("Socketing cancelled.");
		}

		private void ClearPendingSocket()
		{
			_pendingSocketWeaponInstanceId = null;
			_pendingSocketRuneItemId = 0;
			_pendingSocketKind = PendingSocketKind.None;
			SetSocketConfirmationEnabled(false);
		}

		private void SetSocketConfirmationEnabled(bool enabled)
		{
			if (_confirmSocketButton != null)
			{
				_confirmSocketButton.Disabled = !enabled;
			}

			if (_cancelSocketButton != null)
			{
				_cancelSocketButton.Disabled = !enabled;
			}
		}

		private void ApplySocketResult(WeaponSocketingResult result)
		{
			SetUpgradeMessage(result?.Message ?? "Socketing failed.");
			GameManager.Instance?.Publish(
				GameEvent.NotificationRequested,
				new NotificationRequest(result?.Message ?? "Socketing failed.", result?.Success == true ? NotificationType.Info : NotificationType.Error));
			RefreshWeaponUpgradePanel();
		}

		private void PreviewSelectedWeaponMagic()
		{
			WeaponUpgradeManager upgrades = GameManager.Instance?.WeaponUpgrades;
			WeaponInstanceState selected = GameManager.Instance?.Inventory?.GetWeaponInstance(_selectedWeaponInstanceId);
			RuneItem rune = upgrades?.GetElementalRune(selected);
			if (rune == null)
			{
				SetUpgradeMessage("Socket an elemental rune before previewing.");
				return;
			}

			Node currentScene = GetTree()?.CurrentScene;
			if (currentScene == null)
			{
				return;
			}

			MagicAreaHighlighter highlighter = currentScene.GetNodeOrNull<MagicAreaHighlighter>("MagicAreaHighlighter");
			if (highlighter == null)
			{
				highlighter = new MagicAreaHighlighter { Name = "MagicAreaHighlighter" };
				currentScene.AddChild(highlighter);
			}

			Vector2 origin = GetTree().GetFirstNodeInGroup("Player") is Node2D playerNode
				? playerNode.GlobalPosition
				: Vector2.Zero;
			highlighter.ShowPreview(rune, origin, Vector2I.Right);
			SetUpgradeMessage($"Previewing {rune.Element} {rune.Shape}.");
		}

		private static void AddInfo(VBoxContainer list, string text)
		{
			list?.AddChild(new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart });
		}

		private static void ClearList(VBoxContainer list)
		{
			if (list == null)
			{
				return;
			}

			foreach (Node child in list.GetChildren())
			{
				child.QueueFree();
			}
		}

		private void SetUpgradeMessage(string text)
		{
			if (_upgradeMessageLabel != null)
			{
				_upgradeMessageLabel.Text = text;
			}
		}

		private static string ShortId(string instanceId)
		{
			if (string.IsNullOrWhiteSpace(instanceId))
			{
				return "????";
			}

			return instanceId.Length <= 4 ? instanceId : instanceId[^4..];
		}

		private void SetMessage(string text)
		{
			if (_messageLabel != null)
			{
				_messageLabel.Text = text;
			}
		}

		private static int GetSafeQuantity(int quantity)
		{
			return quantity > 0 ? quantity : 1;
		}
	}
}
