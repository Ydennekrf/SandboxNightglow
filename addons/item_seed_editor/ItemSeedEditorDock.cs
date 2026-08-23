#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ethra.V1
{
	[Tool]
	public partial class ItemSeedEditorDock : Control
	{
		private readonly ItemSeedCsvService _csvService = new();
		private readonly ItemSeedValidationService _validationService = new();
		private readonly Dictionary<Control, ItemSeedCsvRow> _rowByField = new();

		private List<ItemSeedCsvFile> _files = new();
		private List<ItemSeedCsvRow> _visibleRows = new();
		private List<ItemSeedValidationResult> _validationResults = new();
		private ItemSeedCsvRow _selectedRow;

		private OptionButton _targetFileOption;
		private LineEdit _searchEdit;
		private Tree _itemTree;
		private TabContainer _mainTabs;
		private VBoxContainer _detailsBox;
		private VBoxContainer _helperBox;
		private VBoxContainer _createBox;
		private VBoxContainer _createTypeFieldsBox;
		private VBoxContainer _createEffectsBox;
		private Tree _validationTree;
		private Label _statusLabel;
		private TextureRect _iconPreview;
		private OptionButton _createTypeOption;
		private LineEdit _createIdEdit;
		private LineEdit _createNameEdit;
		private TextEdit _createDescriptionEdit;
		private OptionButton _createRarityOption;
		private SpinBox _createSellValueSpin;
		private LineEdit _createSubtypeEdit;
		private SpinBox _createMaxStackSpin;
		private LineEdit _createIconPathEdit;
		private readonly Dictionary<string, Control> _createTypeEditors = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<EffectDraft> _createEffectDrafts = new();
		private ConfirmationDialog _deleteDialog;
		private ConfirmationDialog _reloadDialog;
		private ConfirmationDialog _saveDialog;
		private FileDialog _resourceDialog;
		private ItemSeedCsvRow _pendingPathRow;
		private string _pendingPathHeader = string.Empty;
		private LineEdit _pendingPathEdit;
		private bool _saveAllRequested;
		private ItemSeedCsvFile _pendingSaveFile;

		private sealed class EffectDraft
		{
			public string EffectType { get; set; } = "plus";
			public string EffectStat { get; set; } = "str";
			public int EffectPower { get; set; } = 1;
			public string StatusId { get; set; } = "status.burn";
			public string StatusTrigger { get; set; } = "on_hit";
			public float StatusChance { get; set; } = 1f;
			public float StatusDuration { get; set; }
			public int StatusStacks { get; set; } = 1;
		}

		public override void _Ready()
		{
			BuildUi();
			LoadData();
		}

		private void BuildUi()
		{
			SetAnchorsPreset(LayoutPreset.FullRect);
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			SizeFlagsVertical = SizeFlags.ExpandFill;
			CustomMinimumSize = new Vector2(420, 620);

			VBoxContainer root = new()
			{
				CustomMinimumSize = new Vector2(420, 620),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			root.SetAnchorsPreset(LayoutPreset.FullRect);
			AddChild(root);

			HBoxContainer toolbar = new();
			root.AddChild(toolbar);

			Button reloadButton = MakeButton("Reload");
			reloadButton.Pressed += ConfirmReload;
			toolbar.AddChild(reloadButton);

			Button validateButton = MakeButton("Validate");
			validateButton.Pressed += RunValidation;
			toolbar.AddChild(validateButton);

			Button saveCurrentButton = MakeButton("Save Current File");
			saveCurrentButton.Pressed += SaveCurrentFile;
			toolbar.AddChild(saveCurrentButton);

			Button saveAllButton = MakeButton("Save All");
			saveAllButton.Pressed += ConfirmSaveAll;
			toolbar.AddChild(saveAllButton);

			toolbar.AddChild(new VSeparator());

			_targetFileOption = new OptionButton { CustomMinimumSize = new Vector2(150, 0) };
			toolbar.AddChild(_targetFileOption);

			Button addButton = MakeButton("Add Raw Row");
			addButton.Pressed += AddItem;
			toolbar.AddChild(addButton);

			Button duplicateButton = MakeButton("Duplicate");
			duplicateButton.Pressed += DuplicateItem;
			toolbar.AddChild(duplicateButton);

			Button deleteButton = MakeButton("Delete");
			deleteButton.Pressed += ConfirmDelete;
			toolbar.AddChild(deleteButton);

			_mainTabs = new TabContainer
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			root.AddChild(_mainTabs);

			VBoxContainer itemsTab = new()
			{
				Name = "Items",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			_mainTabs.AddChild(itemsTab);

			_searchEdit = new LineEdit
			{
				PlaceholderText = "Search id, name, category, source...",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_searchEdit.TextChanged += _ => RefreshList();
			itemsTab.AddChild(_searchEdit);

			_itemTree = new Tree
			{
				Columns = 4,
				HideRoot = true,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			_itemTree.SetColumnTitle(0, "Id");
			_itemTree.SetColumnTitle(1, "Name");
			_itemTree.SetColumnTitle(2, "Type");
			_itemTree.SetColumnTitle(3, "Source");
			_itemTree.ColumnTitlesVisible = true;
			_itemTree.ItemSelected += OnTreeItemSelected;
			itemsTab.AddChild(_itemTree);

			ScrollContainer createScroll = new()
			{
				Name = "New Item",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			_createBox = new VBoxContainer
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			createScroll.AddChild(_createBox);
			_mainTabs.AddChild(createScroll);
			BuildCreateTab();

			ScrollContainer detailScroll = new()
			{
				Name = "Selected Item",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			VBoxContainer selectedBox = new()
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			detailScroll.AddChild(selectedBox);
			_detailsBox = new VBoxContainer
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			selectedBox.AddChild(_detailsBox);

			_helperBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			selectedBox.AddChild(_helperBox);
			_mainTabs.AddChild(detailScroll);

			VBoxContainer validationBox = new()
			{
				Name = "Validation",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			_mainTabs.AddChild(validationBox);
			_validationTree = new Tree
			{
				Columns = 4,
				HideRoot = true,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			_validationTree.SetColumnTitle(0, "Level");
			_validationTree.SetColumnTitle(1, "Source");
			_validationTree.SetColumnTitle(2, "Id");
			_validationTree.SetColumnTitle(3, "Message");
			_validationTree.ColumnTitlesVisible = true;
			_validationTree.ItemSelected += OnValidationItemSelected;
			validationBox.AddChild(_validationTree);

			_statusLabel = new Label { Text = "Ready." };
			root.AddChild(_statusLabel);

			_deleteDialog = new ConfirmationDialog
			{
				Title = "Delete Item",
				DialogText = "Delete this row from the in-memory CSV data? This can break recipes, inventories, equipment, saves, or scene references. Related data will not be deleted."
			};
			_deleteDialog.Confirmed += DeleteSelectedItem;
			AddChild(_deleteDialog);

			_reloadDialog = new ConfirmationDialog
			{
				Title = "Reload CSV Data",
				DialogText = "Reload item CSV files from disk? Unsaved editor changes will be discarded."
			};
			_reloadDialog.Confirmed += LoadData;
			AddChild(_reloadDialog);

			_saveDialog = new ConfirmationDialog
			{
				Title = "Save Item CSV Data",
				DialogText = "Save dirty item CSV files? Backup copies will be written first when possible."
			};
			_saveDialog.Confirmed += ConfirmedSave;
			AddChild(_saveDialog);

			_resourceDialog = new FileDialog
			{
				Access = FileDialog.AccessEnum.Resources,
				FileMode = FileDialog.FileModeEnum.OpenFile,
				Title = "Select Resource Path"
			};
			_resourceDialog.FileSelected += OnResourcePathSelected;
			AddChild(_resourceDialog);
		}

		private void BuildCreateTab()
		{
			ClearChildren(_createBox);
			_createTypeEditors.Clear();

			_createBox.AddChild(new Label
			{
				Text = "Create Item",
				ThemeTypeVariation = "HeaderSmall"
			});
			_createBox.AddChild(new Label
			{
				Text = "Choose an item type, fill the required fields, add effects if needed, then create the item row. Use Save All to write CSV changes to disk.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			});

			GridContainer basics = new()
			{
				Columns = 2,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_createBox.AddChild(basics);

			_createTypeOption = BuildOptionButton(GetCreateItemTypes(), "Weapon");
			_createTypeOption.ItemSelected += _ => RefreshCreateFormForType();
			AddCreateControl(basics, "Item Type", _createTypeOption);

			_createIdEdit = new LineEdit { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			AddCreateControl(basics, "Id", _createIdEdit);

			Button suggestIdButton = MakeButton("Suggest Next Id");
			suggestIdButton.Pressed += SuggestCreateId;
			basics.AddChild(new Label { Text = string.Empty });
			basics.AddChild(suggestIdButton);

			_createNameEdit = new LineEdit { PlaceholderText = "Display name", SizeFlagsHorizontal = SizeFlags.ExpandFill };
			AddCreateControl(basics, "Name", _createNameEdit);

			_createRarityOption = BuildOptionButton(new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" }, "Common");
			AddCreateControl(basics, "Rarity", _createRarityOption);

			_createSellValueSpin = new SpinBox { MinValue = 0, MaxValue = 999999, Step = 1, Value = 1 };
			AddCreateControl(basics, "Sell Value", _createSellValueSpin);

			_createSubtypeEdit = new LineEdit { PlaceholderText = "Sword, Head, Potion, Ore...", SizeFlagsHorizontal = SizeFlags.ExpandFill };
			AddCreateControl(basics, "Subtype", _createSubtypeEdit);

			_createMaxStackSpin = new SpinBox { MinValue = 1, MaxValue = 999, Step = 1, Value = 1 };
			AddCreateControl(basics, "Max Stack", _createMaxStackSpin);

			HBoxContainer iconRow = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			_createIconPathEdit = new LineEdit { PlaceholderText = "res://...", SizeFlagsHorizontal = SizeFlags.ExpandFill };
			iconRow.AddChild(_createIconPathEdit);
			Button browseIcon = MakeButton("Browse");
			browseIcon.Pressed += () => OpenCreateResourcePicker(_createIconPathEdit);
			iconRow.AddChild(browseIcon);
			AddCreateControl(basics, "Icon Path", iconRow);

			_createDescriptionEdit = new TextEdit
			{
				CustomMinimumSize = new Vector2(0, 80),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			AddCreateControl(basics, "Description", _createDescriptionEdit);

			_createBox.AddChild(new HSeparator());
			_createBox.AddChild(new Label { Text = "Type Fields", ThemeTypeVariation = "HeaderSmall" });
			_createTypeFieldsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			_createBox.AddChild(_createTypeFieldsBox);

			_createBox.AddChild(new HSeparator());
			_createBox.AddChild(new Label { Text = "Effects", ThemeTypeVariation = "HeaderSmall" });
			_createBox.AddChild(new Label
			{
				Text = "Effects are optional. Weapons usually use on-hit statuses or stat bonuses. Armor usually uses on-equip statuses or stat bonuses. Consumables usually use on-use statuses.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			});
			_createEffectsBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			_createBox.AddChild(_createEffectsBox);

			HBoxContainer effectButtons = new();
			Button addStatEffect = MakeButton("Add Stat Effect");
			addStatEffect.Pressed += () => AddCreateEffect("plus");
			effectButtons.AddChild(addStatEffect);
			Button addStatusEffect = MakeButton("Add Status Effect");
			addStatusEffect.Pressed += () => AddCreateEffect("status");
			effectButtons.AddChild(addStatusEffect);
			_createBox.AddChild(effectButtons);

			_createBox.AddChild(new HSeparator());
			Button createButton = MakeButton("Save Item To List");
			createButton.Pressed += CreateItemFromForm;
			_createBox.AddChild(createButton);

			RefreshCreateFormForType();
		}

		private static void AddCreateControl(GridContainer grid, string label, Control control)
		{
			grid.AddChild(new Label { Text = label });
			grid.AddChild(control);
		}

		private void RefreshCreateFormForType()
		{
			if (_createTypeFieldsBox == null)
			{
				return;
			}

			ClearChildren(_createTypeFieldsBox);
			_createTypeEditors.Clear();
			string type = GetSelectedCreateType();
			_createMaxStackSpin.Value = GetDefaultMaxStack(type);
			_createSubtypeEdit.Text = GetDefaultSubtype(type);
			SuggestCreateId();

			GridContainer typeGrid = new()
			{
				Columns = 2,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_createTypeFieldsBox.AddChild(typeGrid);

			if (type == "Weapon")
			{
				AddCreatePathField(typeGrid, "weapon_up_draw_path", "Weapon Up Draw");
				AddCreatePathField(typeGrid, "weapon_down_draw_path", "Weapon Down Draw");
				AddCreatePathField(typeGrid, "weapon_up_stow_path", "Weapon Up Stow");
				AddCreatePathField(typeGrid, "weapon_down_stow_path", "Weapon Down Stow");
				AddCreatePathField(typeGrid, "combo_profile_path", "Combo Profile");
				AddCreateSpinField(typeGrid, "upgrade_rune_slots", "Upgrade Rune Slots", 0, 12, 0);
				AddCreateBoolField(typeGrid, "has_elemental_rune_slot", "Has Elemental Rune Slot", false);
			}
			else if (type == "Rune" || type == "Elemental Rune")
			{
				OptionButton kind = BuildOptionButton(new[] { "Upgrade", "Elemental" }, type == "Elemental Rune" ? "Elemental" : "Upgrade");
				AddCreateTypedField(typeGrid, "rune_kind", "Rune Kind", kind);

				OptionButton element = BuildOptionButton(new[] { "Electricity", "Ice", "Fire", "Acid", "Darkness" }, "Fire");
				AddCreateTypedField(typeGrid, "rune_element", "Element", element);

				OptionButton shape = BuildOptionButton(new[] { "ProjectileBolt", "Linear", "Cone", "CircleWaveAwayFromPlayer" }, "ProjectileBolt");
				AddCreateTypedField(typeGrid, "magic_shape", "Magic Shape", shape);
			}
			else
			{
				_createTypeFieldsBox.AddChild(new Label
				{
					Text = "No extra type-specific fields are required by the current item CSV schema.",
					AutowrapMode = TextServer.AutowrapMode.WordSmart
				});
			}

			RefreshCreateEffects();
		}

		private void AddCreatePathField(GridContainer grid, string header, string label)
		{
			HBoxContainer row = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			LineEdit edit = new() { PlaceholderText = "res://...", SizeFlagsHorizontal = SizeFlags.ExpandFill };
			row.AddChild(edit);
			Button browse = MakeButton("Browse");
			browse.Pressed += () => OpenCreateResourcePicker(edit);
			row.AddChild(browse);
			AddCreateTypedField(grid, header, label, row);
			_createTypeEditors[header] = edit;
		}

		private void AddCreateSpinField(GridContainer grid, string header, string label, double min, double max, double value)
		{
			SpinBox spin = new() { MinValue = min, MaxValue = max, Step = 1, Value = value };
			AddCreateTypedField(grid, header, label, spin);
		}

		private void AddCreateBoolField(GridContainer grid, string header, string label, bool value)
		{
			CheckBox check = new() { ButtonPressed = value };
			AddCreateTypedField(grid, header, label, check);
		}

		private void AddCreateTypedField(GridContainer grid, string header, string label, Control control)
		{
			grid.AddChild(new Label { Text = label });
			grid.AddChild(control);
			_createTypeEditors[header] = control;
		}

		private void OpenCreateResourcePicker(LineEdit edit)
		{
			_pendingPathRow = null;
			_pendingPathHeader = string.Empty;
			_pendingPathEdit = edit;
			if (!string.IsNullOrWhiteSpace(edit.Text))
			{
				_resourceDialog.CurrentPath = edit.Text;
			}
			_resourceDialog.PopupCentered(new Vector2I(900, 600));
		}

		private void AddCreateEffect(string effectType)
		{
			_createEffectDrafts.Add(new EffectDraft
			{
				EffectType = effectType,
				StatusTrigger = GetDefaultStatusTrigger(GetSelectedCreateType())
			});
			RefreshCreateEffects();
		}

		private void RefreshCreateEffects()
		{
			if (_createEffectsBox == null)
			{
				return;
			}

			ClearChildren(_createEffectsBox);
			if (_createEffectDrafts.Count == 0)
			{
				_createEffectsBox.AddChild(new Label { Text = "No effects added yet." });
				return;
			}

			for (int i = 0; i < _createEffectDrafts.Count; i++)
			{
				AddCreateEffectEditor(i, _createEffectDrafts[i]);
			}
		}

		private void AddCreateEffectEditor(int index, EffectDraft draft)
		{
			VBoxContainer panel = new()
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_createEffectsBox.AddChild(panel);

			Label summary = new()
			{
				Text = DescribeEffectDraft(draft),
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			panel.AddChild(summary);

			HBoxContainer row = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			panel.AddChild(row);

			OptionButton type = BuildOptionButton(new[] { "plus", "minus", "status" }, draft.EffectType);
			type.ItemSelected += selected =>
			{
				draft.EffectType = type.GetItemText((int)selected);
				RefreshCreateEffects();
			};
			row.AddChild(type);

			if (draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase))
			{
				OptionButton status = BuildOptionButton(GetKnownStatusIds(), draft.StatusId);
				status.ItemSelected += selected =>
				{
					draft.StatusId = status.GetItemText((int)selected);
					RefreshCreateEffects();
				};
				row.AddChild(status);

				OptionButton trigger = BuildOptionButton(new[] { "on_hit", "on_equip", "on_use" }, draft.StatusTrigger);
				trigger.ItemSelected += selected =>
				{
					draft.StatusTrigger = trigger.GetItemText((int)selected);
					RefreshCreateEffects();
				};
				row.AddChild(trigger);

				SpinBox chance = new() { MinValue = 0, MaxValue = 1, Step = 0.05, Value = draft.StatusChance, CustomMinimumSize = new Vector2(75, 0) };
				chance.ValueChanged += value =>
				{
					draft.StatusChance = (float)value;
					summary.Text = DescribeEffectDraft(draft);
				};
				row.AddChild(chance);

				SpinBox duration = new() { MinValue = 0, MaxValue = 999, Step = 0.25, Value = draft.StatusDuration, CustomMinimumSize = new Vector2(75, 0) };
				duration.ValueChanged += value =>
				{
					draft.StatusDuration = (float)value;
					summary.Text = DescribeEffectDraft(draft);
				};
				row.AddChild(duration);

				SpinBox stacks = new() { MinValue = 1, MaxValue = 99, Step = 1, Value = draft.StatusStacks, CustomMinimumSize = new Vector2(60, 0) };
				stacks.ValueChanged += value =>
				{
					draft.StatusStacks = (int)value;
					summary.Text = DescribeEffectDraft(draft);
				};
				row.AddChild(stacks);
			}
			else
			{
				OptionButton stat = BuildOptionButton(GetKnownStatIds(), draft.EffectStat);
				stat.ItemSelected += selected =>
				{
					draft.EffectStat = stat.GetItemText((int)selected);
					RefreshCreateEffects();
				};
				row.AddChild(stat);

				SpinBox power = new() { MinValue = -999, MaxValue = 999, Step = 1, Value = draft.EffectPower, CustomMinimumSize = new Vector2(75, 0) };
				power.ValueChanged += value =>
				{
					draft.EffectPower = (int)value;
					summary.Text = DescribeEffectDraft(draft);
				};
				row.AddChild(power);
			}

			Button remove = MakeButton("Remove");
			remove.Pressed += () =>
			{
				if (index >= 0 && index < _createEffectDrafts.Count)
				{
					_createEffectDrafts.RemoveAt(index);
					RefreshCreateEffects();
				}
			};
			row.AddChild(remove);
		}

		private void CreateItemFromForm()
		{
			ItemSeedCsvFile itemFile = GetItemFile();
			if (itemFile == null)
			{
				SetStatus("Cannot create item: items_seed.csv is not loaded.");
				return;
			}

			string id = _createIdEdit.Text.Trim();
			if (string.IsNullOrWhiteSpace(id))
			{
				SuggestCreateId();
				id = _createIdEdit.Text.Trim();
			}

			if (string.IsNullOrWhiteSpace(id) || _files.SelectMany(file => file.Rows).Any(row => !row.IsDeleted && GetId(row).Equals(id, StringComparison.OrdinalIgnoreCase)))
			{
				SetStatus($"Cannot create item: id '{id}' is empty or already exists.");
				return;
			}

			string name = _createNameEdit.Text.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				SetStatus("Cannot create item: name is required.");
				return;
			}

			ItemSeedCsvRow row = new()
			{
				SourceFile = itemFile,
				IsDirty = true,
				IsNew = true
			};
			foreach (string header in itemFile.Headers)
			{
				row.Values[header] = BuildCreateValue(header);
			}
			itemFile.Rows.Add(row);

			int effectsAdded = AddCreateEffectsToItem(id);
			_selectedRow = row;
			RefreshList();
			RefreshDetails();
			SelectTab(2);
			ClearCreateFormAfterSave();
			SetStatus($"Created {name} ({id}) with {effectsAdded} effect row(s). Use Save All to write CSV files.");
		}

		private string BuildCreateValue(string header)
		{
			string type = GetSelectedCreateType();
			string category = GetCategoryForCreateType(type);
			if (header.Equals("id", StringComparison.OrdinalIgnoreCase))
			{
				return _createIdEdit.Text.Trim();
			}
			if (header.Equals("name", StringComparison.OrdinalIgnoreCase))
			{
				return _createNameEdit.Text.Trim();
			}
			if (header.Equals("description", StringComparison.OrdinalIgnoreCase))
			{
				return _createDescriptionEdit.Text.Trim();
			}
			if (header.Equals("rarity", StringComparison.OrdinalIgnoreCase))
			{
				return _createRarityOption.GetItemText(_createRarityOption.Selected);
			}
			if (header.Equals("sell_value", StringComparison.OrdinalIgnoreCase))
			{
				return ((int)_createSellValueSpin.Value).ToString();
			}
			if (header.Equals("category", StringComparison.OrdinalIgnoreCase))
			{
				return category;
			}
			if (header.Equals("subtype", StringComparison.OrdinalIgnoreCase))
			{
				return _createSubtypeEdit.Text.Trim();
			}
			if (header.Equals("max_stack", StringComparison.OrdinalIgnoreCase))
			{
				return ((int)_createMaxStackSpin.Value).ToString();
			}
			if (header.Equals("icon_path", StringComparison.OrdinalIgnoreCase))
			{
				return _createIconPathEdit.Text.Trim();
			}
			if (_createTypeEditors.TryGetValue(header, out Control editor))
			{
				return ReadEditorValue(editor);
			}
			return string.Empty;
		}

		private int AddCreateEffectsToItem(string itemId)
		{
			ItemSeedCsvFile effectFile = GetEffectFile();
			if (effectFile == null || _createEffectDrafts.Count == 0)
			{
				return 0;
			}

			foreach (EffectDraft draft in _createEffectDrafts)
			{
				ItemSeedCsvRow effectRow = new()
				{
					SourceFile = effectFile,
					IsDirty = true,
					IsNew = true
				};
				foreach (string header in effectFile.Headers)
				{
					effectRow.Values[header] = BuildCreateEffectValue(header, itemId, draft);
				}
				effectFile.Rows.Add(effectRow);
			}
			return _createEffectDrafts.Count;
		}

		private static string BuildCreateEffectValue(string header, string itemId, EffectDraft draft)
		{
			if (header.Equals("item_id", StringComparison.OrdinalIgnoreCase))
			{
				return itemId;
			}
			if (header.Equals("effect_type", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType;
			}
			if (header.Equals("effect_stat", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? string.Empty : draft.EffectStat;
			}
			if (header.Equals("effect_power", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? string.Empty : draft.EffectPower.ToString();
			}
			if (header.Equals("status_id", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? draft.StatusId : string.Empty;
			}
			if (header.Equals("status_trigger", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? draft.StatusTrigger : string.Empty;
			}
			if (header.Equals("status_chance", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? draft.StatusChance.ToString("0.###") : string.Empty;
			}
			if (header.Equals("status_duration", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? draft.StatusDuration.ToString("0.###") : string.Empty;
			}
			if (header.Equals("status_stacks", StringComparison.OrdinalIgnoreCase))
			{
				return draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase) ? draft.StatusStacks.ToString() : string.Empty;
			}
			return string.Empty;
		}

		private void ClearCreateFormAfterSave()
		{
			_createNameEdit.Text = string.Empty;
			_createDescriptionEdit.Text = string.Empty;
			_createIconPathEdit.Text = string.Empty;
			_createSellValueSpin.Value = 1;
			_createEffectDrafts.Clear();
			RefreshCreateFormForType();
		}

		private void LoadData()
		{
			_files = _csvService.LoadAll();
			_selectedRow = null;
			RefreshTargetFiles();
			RefreshCreateFormForType();
			RefreshList();
			RefreshDetails();
			_validationTree.Clear();
			SetStatus($"Loaded {_files.Sum(file => file.Rows.Count)} rows from {_files.Count} CSV files.");
		}

		private void RefreshTargetFiles()
		{
			_targetFileOption.Clear();
			for (int i = 0; i < _files.Count; i++)
			{
				_targetFileOption.AddItem(_files[i].Path.GetFile(), i);
			}
		}

		private void RefreshList()
		{
			string filter = _searchEdit?.Text?.Trim() ?? string.Empty;
			_visibleRows = _files
				.SelectMany(file => file.Rows)
				.Where(row => !row.IsDeleted)
				.Where(row => MatchesFilter(row, filter))
				.ToList();

			_itemTree.Clear();
			TreeItem root = _itemTree.CreateItem();
			foreach (ItemSeedCsvRow row in _visibleRows)
			{
				TreeItem item = _itemTree.CreateItem(root);
				item.SetMetadata(0, _visibleRows.IndexOf(row));
				item.SetText(0, DirtyPrefix(row) + GetId(row));
				item.SetText(1, GetName(row));
				item.SetText(2, GetCategory(row));
				item.SetText(3, row.SourceFile?.Path.GetFile() ?? string.Empty);
				if (row == _selectedRow)
				{
					item.Select(0);
				}
			}
		}

		private void RefreshDetails()
		{
			ClearChildren(_detailsBox);
			ClearChildren(_helperBox);
			_rowByField.Clear();

			if (_selectedRow == null)
			{
				_detailsBox.AddChild(new Label { Text = "Select an item row." });
				return;
			}

			_detailsBox.AddChild(new Label
			{
				Text = $"{GetId(_selectedRow)} - {_selectedRow.SourceFile.Path.GetFile()}"
			});

			foreach (string header in _selectedRow.SourceFile.Headers)
			{
				AddField(header, _selectedRow.GetValue(header));
			}

			BuildHelperPanel(_selectedRow);
		}

		private void AddField(string header, string value)
		{
			Label label = new() { Text = header };
			_detailsBox.AddChild(label);

			Control editor;
			if (header.Contains("description", StringComparison.OrdinalIgnoreCase))
			{
				TextEdit textEdit = new()
				{
					Text = value,
					CustomMinimumSize = new Vector2(0, 70),
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				textEdit.TextChanged += () => UpdateField(textEdit, header, textEdit.Text);
				editor = textEdit;
			}
			else if (IsBoolHeader(header))
			{
				CheckBox checkBox = new() { Text = "True", ButtonPressed = IsTruthy(value) };
				checkBox.Toggled += pressed => UpdateField(checkBox, header, pressed ? "true" : "false");
				editor = checkBox;
			}
			else if (IsOptionHeader(header, out string[] options))
			{
				OptionButton option = new();
				option.AddItem(string.Empty);
				foreach (string entry in options)
				{
					option.AddItem(entry);
				}
				int selected = Array.FindIndex(options, optionValue => optionValue.Equals(value, StringComparison.OrdinalIgnoreCase));
				option.Selected = selected >= 0 ? selected + 1 : 0;
				option.ItemSelected += index => UpdateField(option, header, option.GetItemText((int)index));
				editor = option;
			}
			else
			{
				LineEdit lineEdit = new()
				{
					Text = value,
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				lineEdit.TextChanged += text => UpdateField(lineEdit, header, text);
				editor = lineEdit;

				if (IsPathHeader(header))
				{
					HBoxContainer pathRow = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					pathRow.AddChild(lineEdit);
					Button browseButton = MakeButton("Browse");
					browseButton.Pressed += () => OpenResourcePicker(_selectedRow, header);
					pathRow.AddChild(browseButton);
					_rowByField[editor] = _selectedRow;
					_detailsBox.AddChild(pathRow);
					return;
				}
			}

			_rowByField[editor] = _selectedRow;
			_detailsBox.AddChild(editor);
		}

		private void BuildHelperPanel(ItemSeedCsvRow row)
		{
			_helperBox.AddChild(new HSeparator());
			_helperBox.AddChild(new Label { Text = "Type Helper" });

			string category = GetCategory(row);
			string subtype = row.GetValue("subtype");
			AddHelperLine($"Category: {category}");
			if (!string.IsNullOrWhiteSpace(subtype))
			{
				AddHelperLine($"Subtype: {subtype}");
			}

			AddIconPreview(row);

			if (category.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
			{
				AddHelperFields(row, "icon_path", "weapon_up_draw_path", "weapon_down_draw_path", "weapon_up_stow_path", "weapon_down_stow_path", "combo_profile_path", "upgrade_rune_slots", "has_elemental_rune_slot");
			}
			else if (category.Equals("Armor", StringComparison.OrdinalIgnoreCase))
			{
				AddHelperFields(row, "icon_path", "subtype");
			}
			else if (category.Equals("Consumable", StringComparison.OrdinalIgnoreCase))
			{
				AddHelperFields(row, "icon_path", "subtype");
			}
			else if (category.Equals("Crafting", StringComparison.OrdinalIgnoreCase) || category.Equals("Material", StringComparison.OrdinalIgnoreCase))
			{
				AddHelperFields(row, "icon_path", "subtype", "rarity", "max_stack");
			}
			else if (category.Equals("Rune", StringComparison.OrdinalIgnoreCase))
			{
				AddHelperFields(row, "icon_path", "rune_kind", "rune_element", "magic_shape");
				if (row.GetValue("rune_kind").Equals("Elemental", StringComparison.OrdinalIgnoreCase))
				{
					AddElementColorPreview(row.GetValue("rune_element"));
				}
			}
			else
			{
				AddHelperLine("No type-specific helper is available for this category.");
			}

			BuildEffectPanel(row);
		}

		private void AddHelperFields(ItemSeedCsvRow row, params string[] headers)
		{
			foreach (string header in headers)
			{
				if (!row.SourceFile.Headers.Contains(header, StringComparer.OrdinalIgnoreCase))
				{
					continue;
				}

				string value = row.GetValue(header);
				string suffix = IsPathHeader(header) && !string.IsNullOrWhiteSpace(value) && !ResourceLoader.Exists(value) && !FileAccess.FileExists(value)
					? " [missing]"
					: string.Empty;
				AddHelperLine($"{header}: {value}{suffix}");
			}
		}

		private void AddIconPreview(ItemSeedCsvRow row)
		{
			if (!row.SourceFile.Headers.Contains("icon_path", StringComparer.OrdinalIgnoreCase))
			{
				return;
			}

			_iconPreview = new TextureRect
			{
				CustomMinimumSize = new Vector2(48, 48),
				ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
			};
			Texture2D texture = ResourceLoader.Load<Texture2D>(row.GetValue("icon_path"));
			if (texture != null)
			{
				_iconPreview.Texture = texture;
			}
			_helperBox.AddChild(_iconPreview);
		}

		private void AddElementColorPreview(string element)
		{
			ColorRect rect = new()
			{
				Color = element switch
				{
					"Electricity" => new Color(0.35f, 0.85f, 1f),
					"Ice" => new Color(0.58f, 0.9f, 1f),
					"Fire" => new Color(1f, 0.28f, 0.08f),
					"Acid" => new Color(0.35f, 1f, 0.24f),
					"Darkness" => new Color(0.45f, 0.18f, 0.72f),
					_ => Colors.DimGray
				},
				CustomMinimumSize = new Vector2(80, 18)
			};
			_helperBox.AddChild(rect);
		}

		private void BuildEffectPanel(ItemSeedCsvRow itemRow)
		{
			if (!IsItemDefinitionRow(itemRow))
			{
				return;
			}

			ItemSeedCsvFile effectFile = GetEffectFile();
			if (effectFile == null)
			{
				AddHelperLine("No item_effects_seed.csv file loaded.");
				return;
			}

			string itemId = GetId(itemRow);
			_helperBox.AddChild(new HSeparator());
			_helperBox.AddChild(new Label { Text = "Item Effects" });

			List<ItemSeedCsvRow> effects = GetEffectRows(itemId).ToList();
			if (effects.Count == 0)
			{
				AddHelperLine("No effects for this item. Use Add Effect to add a stat modifier or status behavior.");
			}

			foreach (ItemSeedCsvRow effectRow in effects)
			{
				AddEffectEditorRow(effectRow);
			}

			Button addEffect = MakeButton("Add Effect");
			addEffect.Pressed += () => AddEffectRow(itemId);
			_helperBox.AddChild(addEffect);
		}

		private void AddEffectEditorRow(ItemSeedCsvRow effectRow)
		{
			VBoxContainer panel = new()
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_helperBox.AddChild(panel);

			panel.AddChild(new Label
			{
				Text = DescribeEffectRow(effectRow),
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			});

			GridContainer row = new()
			{
				Columns = 2,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			panel.AddChild(row);

			OptionButton type = BuildOptionButton(new[] { "plus", "minus", "status" }, effectRow.GetValue("effect_type"));
			type.ItemSelected += index => UpdateEffectField(effectRow, "effect_type", type.GetItemText((int)index));
			AddCreateControl(row, "Effect Type", type);

			if (effectRow.GetValue("effect_type").Equals("status", StringComparison.OrdinalIgnoreCase))
			{
				OptionButton status = BuildOptionButton(GetKnownStatusIds(), effectRow.GetValue("status_id"));
				status.ItemSelected += index => UpdateEffectField(effectRow, "status_id", status.GetItemText((int)index));
				AddCreateControl(row, "Status", status);

				OptionButton trigger = BuildOptionButton(new[] { "on_hit", "on_equip", "on_use" }, effectRow.GetValue("status_trigger"));
				trigger.ItemSelected += index => UpdateEffectField(effectRow, "status_trigger", trigger.GetItemText((int)index));
				AddCreateControl(row, "Trigger", trigger);

				SpinBox chance = new()
				{
					MinValue = 0,
					MaxValue = 1,
					Step = 0.05,
					Value = double.TryParse(effectRow.GetValue("status_chance"), out double parsedChance) ? parsedChance : 1,
					CustomMinimumSize = new Vector2(70, 0)
				};
				chance.ValueChanged += value => UpdateEffectField(effectRow, "status_chance", value.ToString("0.###"));
				AddCreateControl(row, "Chance", chance);

				SpinBox duration = new()
				{
					MinValue = 0,
					MaxValue = 999,
					Step = 0.25,
					Value = double.TryParse(effectRow.GetValue("status_duration"), out double parsedDuration) ? parsedDuration : 0,
					CustomMinimumSize = new Vector2(70, 0)
				};
				duration.ValueChanged += value => UpdateEffectField(effectRow, "status_duration", value.ToString("0.###"));
				AddCreateControl(row, "Duration", duration);

				SpinBox stacks = new()
				{
					MinValue = 1,
					MaxValue = 99,
					Step = 1,
					Value = int.TryParse(effectRow.GetValue("status_stacks"), out int parsedStacks) ? parsedStacks : 1,
					CustomMinimumSize = new Vector2(55, 0)
				};
				stacks.ValueChanged += value => UpdateEffectField(effectRow, "status_stacks", ((int)value).ToString());
				AddCreateControl(row, "Stacks", stacks);
			}
			else
			{
				OptionButton stat = BuildOptionButton(GetKnownStatIds(), effectRow.GetValue("effect_stat"));
				stat.ItemSelected += index => UpdateEffectField(effectRow, "effect_stat", stat.GetItemText((int)index));
				AddCreateControl(row, "Stat", stat);

				SpinBox power = new()
				{
					MinValue = -999,
					MaxValue = 999,
					Step = 1,
					Value = int.TryParse(effectRow.GetValue("effect_power"), out int parsed) ? parsed : 0,
					CustomMinimumSize = new Vector2(70, 0)
				};
				power.ValueChanged += value => UpdateEffectField(effectRow, "effect_power", ((int)value).ToString());
				AddCreateControl(row, "Power", power);
			}

			HBoxContainer actions = new();
			Button duplicate = MakeButton("Duplicate");
			duplicate.Pressed += () => DuplicateEffectRow(effectRow);
			actions.AddChild(duplicate);

			Button delete = MakeButton("Delete");
			delete.Pressed += () => DeleteEffectRow(effectRow);
			actions.AddChild(delete);
			panel.AddChild(actions);
		}

		private static OptionButton BuildOptionButton(string[] options, string selectedValue)
		{
			OptionButton option = new() { CustomMinimumSize = new Vector2(95, 0) };
			foreach (string entry in options)
			{
				option.AddItem(entry);
			}

			int selected = Array.FindIndex(options, entry => entry.Equals(selectedValue, StringComparison.OrdinalIgnoreCase));
			option.Selected = selected >= 0 ? selected : 0;
			return option;
		}

		private void AddHelperLine(string text)
		{
			_helperBox.AddChild(new Label
			{
				Text = text,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			});
		}

		private static string DescribeEffectRow(ItemSeedCsvRow row)
		{
			string type = row.GetValue("effect_type");
			if (type.Equals("status", StringComparison.OrdinalIgnoreCase))
			{
				string chance = string.IsNullOrWhiteSpace(row.GetValue("status_chance")) ? "100%" : $"{ParsePercent(row.GetValue("status_chance"))}%";
				string duration = string.IsNullOrWhiteSpace(row.GetValue("status_duration")) || row.GetValue("status_duration") == "0"
					? "catalog/default duration"
					: $"{row.GetValue("status_duration")}s";
				string stacks = string.IsNullOrWhiteSpace(row.GetValue("status_stacks")) ? "1" : row.GetValue("status_stacks");
				return $"Apply {row.GetValue("status_id")} {FormatTrigger(row.GetValue("status_trigger"))}, {chance} chance, {stacks} stack(s), {duration}.";
			}

			string sign = type.Equals("minus", StringComparison.OrdinalIgnoreCase) ? "-" : "+";
			return $"{sign}{row.GetValue("effect_power")} {row.GetValue("effect_stat")} while this item effect is active.";
		}

		private static string DescribeEffectDraft(EffectDraft draft)
		{
			if (draft.EffectType.Equals("status", StringComparison.OrdinalIgnoreCase))
			{
				return $"Apply {draft.StatusId} {FormatTrigger(draft.StatusTrigger)}, {Mathf.RoundToInt(draft.StatusChance * 100f)}% chance, {draft.StatusStacks} stack(s), {(draft.StatusDuration <= 0f ? "catalog/default duration" : $"{draft.StatusDuration:0.###}s")}.";
			}

			string sign = draft.EffectType.Equals("minus", StringComparison.OrdinalIgnoreCase) ? "-" : "+";
			return $"{sign}{draft.EffectPower} {draft.EffectStat} while this item effect is active.";
		}

		private static int ParsePercent(string value)
		{
			return float.TryParse(value, out float chance) ? Mathf.RoundToInt(chance * 100f) : 100;
		}

		private static string FormatTrigger(string trigger)
		{
			return trigger switch
			{
				"on_hit" => "on hit",
				"on_equip" => "when equipped",
				"on_use" => "when used",
				_ => "with unknown trigger"
			};
		}

		private void UpdateField(Control editor, string header, string value)
		{
			if (!_rowByField.TryGetValue(editor, out ItemSeedCsvRow row))
			{
				return;
			}

			row.SetValue(header, value);
			if (row == _selectedRow)
			{
				BuildHelperOnly();
			}
			RefreshList();
			SetStatus($"Edited {GetId(row)} in {row.SourceFile.Path.GetFile()}.");
		}

		private void UpdateEffectField(ItemSeedCsvRow row, string header, string value)
		{
			row.SetValue(header, value);
			EnsureEffectDefaults(row);
			BuildHelperOnly();
			RefreshList();
			SetStatus($"Edited effect for item {row.GetValue("item_id")}.");
		}

		private static void EnsureEffectDefaults(ItemSeedCsvRow row)
		{
			if (row == null)
			{
				return;
			}

			if (row.GetValue("effect_type").Equals("status", StringComparison.OrdinalIgnoreCase))
			{
				SetDefaultIfEmpty(row, "status_id", "status.burn");
				SetDefaultIfEmpty(row, "status_trigger", "on_hit");
				SetDefaultIfEmpty(row, "status_chance", "1");
				SetDefaultIfEmpty(row, "status_duration", "0");
				SetDefaultIfEmpty(row, "status_stacks", "1");
				return;
			}

			SetDefaultIfEmpty(row, "effect_stat", "str");
			SetDefaultIfEmpty(row, "effect_power", "1");
		}

		private static void SetDefaultIfEmpty(ItemSeedCsvRow row, string header, string value)
		{
			if (row.SourceFile.Headers.Contains(header, StringComparer.OrdinalIgnoreCase)
				&& string.IsNullOrWhiteSpace(row.GetValue(header)))
			{
				row.SetValue(header, value);
			}
		}

		private void AddEffectRow(string itemId)
		{
			ItemSeedCsvFile effectFile = GetEffectFile();
			if (effectFile == null || string.IsNullOrWhiteSpace(itemId))
			{
				SetStatus("Cannot add effect without an item id and item effects CSV.");
				return;
			}

			ItemSeedCsvRow row = new()
			{
				SourceFile = effectFile,
				IsDirty = true,
				IsNew = true
			};
			foreach (string header in effectFile.Headers)
			{
				row.Values[header] = header switch
				{
					"item_id" => itemId,
					"effect_type" => "plus",
					"effect_stat" => "str",
					"effect_power" => "1",
					"status_chance" => "1",
					"status_duration" => "0",
					"status_stacks" => "1",
					_ => string.Empty
				};
			}
			effectFile.Rows.Add(row);
			BuildHelperOnly();
			RefreshList();
			SetStatus($"Added effect row for item {itemId}.");
		}

		private void DuplicateEffectRow(ItemSeedCsvRow source)
		{
			ItemSeedCsvRow copy = _csvService.DuplicateRow(source);
			if (copy == null)
			{
				return;
			}

			copy.SetValue("item_id", source.GetValue("item_id"));
			BuildHelperOnly();
			RefreshList();
			SetStatus($"Duplicated effect row for item {source.GetValue("item_id")}.");
		}

		private void DeleteEffectRow(ItemSeedCsvRow row)
		{
			row.IsDeleted = true;
			row.IsDirty = true;
			BuildHelperOnly();
			RefreshList();
			SetStatus($"Marked effect row for item {row.GetValue("item_id")} for deletion.");
		}

		private void OpenResourcePicker(ItemSeedCsvRow row, string header)
		{
			if (row == null || string.IsNullOrWhiteSpace(header))
			{
				return;
			}

			_pendingPathRow = row;
			_pendingPathHeader = header;
			string currentPath = row.GetValue(header);
			if (!string.IsNullOrWhiteSpace(currentPath))
			{
				_resourceDialog.CurrentPath = currentPath;
			}
			_resourceDialog.PopupCentered(new Vector2I(900, 600));
		}

		private void OnResourcePathSelected(string path)
		{
			if (_pendingPathEdit != null)
			{
				_pendingPathEdit.Text = path;
				SetStatus($"Selected resource path {path}.");
				_pendingPathEdit = null;
				return;
			}

			if (_pendingPathRow == null || string.IsNullOrWhiteSpace(_pendingPathHeader))
			{
				return;
			}

			_pendingPathRow.SetValue(_pendingPathHeader, path);
			RefreshDetails();
			RefreshList();
			SetStatus($"Set {_pendingPathHeader} to {path}.");
			_pendingPathRow = null;
			_pendingPathHeader = string.Empty;
			_pendingPathEdit = null;
		}

		private void BuildHelperOnly()
		{
			ClearChildren(_helperBox);
			if (_selectedRow != null)
			{
				BuildHelperPanel(_selectedRow);
			}
		}

		private void OnTreeItemSelected()
		{
			TreeItem selected = _itemTree.GetSelected();
			if (selected == null)
			{
				return;
			}

			int index = (int)selected.GetMetadata(0);
			if (index < 0 || index >= _visibleRows.Count)
			{
				return;
			}

			_selectedRow = _visibleRows[index];
			RefreshDetails();
			SelectTab(2);
		}

		private void AddItem()
		{
			ItemSeedCsvFile file = GetTargetFile();
			if (file == null)
			{
				SetStatus("No target CSV file selected.");
				return;
			}

			_selectedRow = _csvService.AddRow(file, GetSuggestedNewId(file));
			RefreshList();
			RefreshDetails();
			SetStatus($"Added item row to {file.Path.GetFile()}.");
		}

		private void DuplicateItem()
		{
			if (_selectedRow == null)
			{
				SetStatus("Select an item before duplicating.");
				return;
			}

			_selectedRow = _csvService.DuplicateRow(_selectedRow);
			if (_selectedRow != null)
			{
				string idHeader = ItemSeedCsvService.FindIdHeader(_selectedRow.SourceFile.Headers);
				if (!string.IsNullOrWhiteSpace(idHeader))
				{
					_selectedRow.SetValue(idHeader, GetSuggestedNewId(_selectedRow.SourceFile));
				}
			}
			RefreshList();
			RefreshDetails();
			SetStatus("Duplicated selected item row.");
		}

		private void ConfirmDelete()
		{
			if (_selectedRow == null)
			{
				SetStatus("Select an item before deleting.");
				return;
			}

			_deleteDialog.PopupCentered();
		}

		private void DeleteSelectedItem()
		{
			if (_selectedRow == null)
			{
				return;
			}

			string id = GetId(_selectedRow);
			_selectedRow.IsDeleted = true;
			_selectedRow.IsDirty = true;
			_selectedRow = null;
			RefreshList();
			RefreshDetails();
			SetStatus($"Marked item '{id}' for deletion. Save to write changes.");
		}

		private void ConfirmReload()
		{
			if (HasDirtyData())
			{
				_reloadDialog.PopupCentered();
				return;
			}

			LoadData();
		}

		private void SaveCurrentFile()
		{
			ItemSeedCsvFile file = _selectedRow?.SourceFile ?? GetTargetFile();
			if (file == null)
			{
				SetStatus("No current CSV file selected.");
				return;
			}

			ConfirmSaveFile(file);
		}

		private void ConfirmSaveAll()
		{
			_saveAllRequested = true;
			_pendingSaveFile = null;
			_saveDialog.DialogText = BuildSaveSummary(_files.Where(file => file.IsDirty));
			_saveDialog.PopupCentered();
		}

		private void ConfirmSaveFile(ItemSeedCsvFile file)
		{
			_saveAllRequested = false;
			_pendingSaveFile = file;
			_saveDialog.DialogText = BuildSaveSummary(new[] { file });
			_saveDialog.PopupCentered();
		}

		private void ConfirmedSave()
		{
			if (_saveAllRequested)
			{
				SaveAll();
				return;
			}

			if (_pendingSaveFile != null)
			{
				SaveFile(_pendingSaveFile);
				RefreshList();
				RefreshDetails();
			}
		}

		private void SaveAll()
		{
			int saved = 0;
			foreach (ItemSeedCsvFile file in _files.Where(file => file.IsDirty))
			{
				if (SaveFile(file))
				{
					saved++;
				}
			}

			RefreshList();
			RefreshDetails();
			SetStatus($"Saved {saved} dirty CSV files.");
		}

		private bool SaveFile(ItemSeedCsvFile file)
		{
			Error error = _csvService.SaveFile(file, createBackup: true, out string backupPath);
			if (error != Error.Ok)
			{
				SetStatus($"Failed to save {file.Path.GetFile()}: {error}.");
				return false;
			}

			SetStatus(string.IsNullOrWhiteSpace(backupPath)
				? $"Saved {file.Path.GetFile()}."
				: $"Saved {file.Path.GetFile()} with backup {backupPath.GetFile()}.");
			return true;
		}

		private void RunValidation()
		{
			List<ItemSeedValidationResult> results = _validationService.Validate(_files);
			int errors = results.Count(result => result.Severity == ItemSeedValidationSeverity.Error);
			int warnings = results.Count(result => result.Severity == ItemSeedValidationSeverity.Warning);
			PopulateValidationTree(results);
			SelectTab(3);
			SetStatus($"Validation complete: {errors} errors, {warnings} warnings.");
		}

		private void PopulateValidationTree(List<ItemSeedValidationResult> results)
		{
			_validationResults = results;
			_validationTree.Clear();
			TreeItem root = _validationTree.CreateItem();
			if (results.Count == 0)
			{
				TreeItem item = _validationTree.CreateItem(root);
				item.SetText(0, "OK");
				item.SetText(3, "Validation passed.");
				return;
			}

			for (int index = 0; index < results.Count; index++)
			{
				ItemSeedValidationResult result = results[index];
				TreeItem item = _validationTree.CreateItem(root);
				item.SetText(0, result.Severity.ToString());
				item.SetText(1, result.SourcePath.GetFile());
				item.SetText(2, result.ItemId);
				item.SetText(3, result.Message);
				item.SetMetadata(0, index);
			}
		}

		private void OnValidationItemSelected()
		{
			TreeItem item = _validationTree.GetSelected();
			if (item == null)
			{
				return;
			}

			int index = (int)item.GetMetadata(0);
			if (index < 0 || index >= _validationResults.Count)
			{
				return;
			}

			ItemSeedCsvRow row = _validationResults[index].Row;
			if (row == null)
			{
				return;
			}

			_selectedRow = row;
			_searchEdit.Text = string.Empty;
			RefreshList();
			RefreshDetails();
			SelectTab(2);
			SetStatus($"Selected validation row {GetId(row)} in {row.SourceFile.Path.GetFile()}.");
		}

		private ItemSeedCsvFile GetTargetFile()
		{
			int selected = _targetFileOption.Selected;
			return selected >= 0 && selected < _files.Count ? _files[selected] : _files.FirstOrDefault();
		}

		private ItemSeedCsvFile GetItemFile()
		{
			return _files.FirstOrDefault(file =>
				file.Headers.Contains("id", StringComparer.OrdinalIgnoreCase)
				&& file.Headers.Contains("category", StringComparer.OrdinalIgnoreCase)
				&& file.Headers.Contains("name", StringComparer.OrdinalIgnoreCase));
		}

		private void SuggestCreateId()
		{
			if (_createIdEdit == null)
			{
				return;
			}

			ItemSeedCsvFile itemFile = GetItemFile();
			_createIdEdit.Text = itemFile == null ? "item.new_item" : GetSuggestedNewIdForType(itemFile, GetSelectedCreateType());
		}

		private string GetSuggestedNewIdForType(ItemSeedCsvFile file, string createType)
		{
			string idHeader = ItemSeedCsvService.FindIdHeader(file.Headers);
			if (string.IsNullOrWhiteSpace(idHeader))
			{
				return "item.new_item";
			}

			string category = GetCategoryForCreateType(createType);
			int rangeStart = GetIdRangeStart(createType);
			int max = rangeStart - 1;
			foreach (ItemSeedCsvRow row in file.Rows.Where(row => !row.IsDeleted))
			{
				if (!int.TryParse(row.GetValue(idHeader), out int id))
				{
					continue;
				}

				bool sameCategory = row.GetValue("category").Equals(category, StringComparison.OrdinalIgnoreCase);
				bool inRange = id >= rangeStart && id < rangeStart + 1000;
				if (sameCategory || inRange)
				{
					max = Math.Max(max, id);
				}
			}

			return (max + 1).ToString();
		}

		private static int GetIdRangeStart(string createType)
		{
			return createType switch
			{
				"Consumable" => 1001,
				"Weapon" => 2001,
				"Material" => 3001,
				"Armor" => 4001,
				"Rune" => 5001,
				"Elemental Rune" => 5101,
				_ => 9001
			};
		}

		private string GetSelectedCreateType()
		{
			if (_createTypeOption == null || _createTypeOption.Selected < 0)
			{
				return "Weapon";
			}

			return _createTypeOption.GetItemText(_createTypeOption.Selected);
		}

		private static string GetCategoryForCreateType(string createType)
		{
			return createType switch
			{
				"Material" => "Crafting",
				"Elemental Rune" => "Rune",
				_ => createType
			};
		}

		private static string GetDefaultSubtype(string createType)
		{
			return createType switch
			{
				"Weapon" => "Sword",
				"Armor" => "Head",
				"Consumable" => "Potion",
				"Material" => "Material",
				"Rune" => "Upgrade",
				"Elemental Rune" => "Elemental",
				_ => string.Empty
			};
		}

		private static int GetDefaultMaxStack(string createType)
		{
			return createType is "Consumable" or "Material" ? 99 : 1;
		}

		private static string GetDefaultStatusTrigger(string createType)
		{
			return createType switch
			{
				"Consumable" => "on_use",
				"Armor" => "on_equip",
				_ => "on_hit"
			};
		}

		private static string ReadEditorValue(Control editor)
		{
			return editor switch
			{
				LineEdit lineEdit => lineEdit.Text.Trim(),
				SpinBox spinBox => ((int)spinBox.Value).ToString(),
				CheckBox checkBox => checkBox.ButtonPressed ? "true" : "false",
				OptionButton optionButton => optionButton.Selected >= 0 ? optionButton.GetItemText(optionButton.Selected) : string.Empty,
				_ => string.Empty
			};
		}

		private bool HasDirtyData()
		{
			return _files.Any(file => file.IsDirty);
		}

		private ItemSeedCsvFile GetEffectFile()
		{
			return _files.FirstOrDefault(file =>
				file.Headers.Contains("item_id", StringComparer.OrdinalIgnoreCase)
				&& file.Headers.Contains("effect_type", StringComparer.OrdinalIgnoreCase)
				&& file.Headers.Contains("effect_stat", StringComparer.OrdinalIgnoreCase)
				&& file.Headers.Contains("effect_power", StringComparer.OrdinalIgnoreCase));
		}

		private IEnumerable<ItemSeedCsvRow> GetEffectRows(string itemId)
		{
			ItemSeedCsvFile effectFile = GetEffectFile();
			if (effectFile == null || string.IsNullOrWhiteSpace(itemId))
			{
				return Enumerable.Empty<ItemSeedCsvRow>();
			}

			return effectFile.Rows
				.Where(row => !row.IsDeleted)
				.Where(row => row.GetValue("item_id").Equals(itemId, StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsItemDefinitionRow(ItemSeedCsvRow row)
		{
			return row?.SourceFile?.Headers.Contains("id", StringComparer.OrdinalIgnoreCase) == true
				&& row.SourceFile.Headers.Contains("category", StringComparer.OrdinalIgnoreCase);
		}

		private string GetSuggestedNewId(ItemSeedCsvFile file)
		{
			string idHeader = ItemSeedCsvService.FindIdHeader(file.Headers);
			if (string.IsNullOrWhiteSpace(idHeader))
			{
				return "item.new_item";
			}

			int max = 0;
			foreach (ItemSeedCsvRow row in file.Rows.Where(row => !row.IsDeleted))
			{
				if (int.TryParse(row.GetValue(idHeader), out int id))
				{
					max = Math.Max(max, id);
				}
			}

			return max > 0 ? (max + 1).ToString() : "item.new_item";
		}

		private static string BuildSaveSummary(IEnumerable<ItemSeedCsvFile> files)
		{
			List<string> lines = new() { "Save CSV changes? Backup copies will be written first when possible.", string.Empty };
			foreach (ItemSeedCsvFile file in files.Where(file => file != null))
			{
				int added = file.Rows.Count(row => row.IsNew && !row.IsDeleted);
				int edited = file.Rows.Count(row => row.IsDirty && !row.IsNew && !row.IsDeleted);
				int deleted = file.Rows.Count(row => row.IsDeleted);
				lines.Add($"{file.Path.GetFile()}: {added} new, {edited} edited, {deleted} deleted");
			}

			if (lines.Count == 2)
			{
				lines.Add("No dirty CSV files selected.");
			}

			return string.Join("\n", lines);
		}

		private bool MatchesFilter(ItemSeedCsvRow row, string filter)
		{
			if (string.IsNullOrWhiteSpace(filter))
			{
				return true;
			}

			return GetId(row).Contains(filter, StringComparison.OrdinalIgnoreCase)
				|| GetName(row).Contains(filter, StringComparison.OrdinalIgnoreCase)
				|| GetCategory(row).Contains(filter, StringComparison.OrdinalIgnoreCase)
				|| (row.SourceFile?.Path.GetFile() ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase);
		}

		private static string GetId(ItemSeedCsvRow row)
		{
			IReadOnlyList<string> headers = row.SourceFile != null ? row.SourceFile.Headers : Array.Empty<string>();
			string header = ItemSeedCsvService.FindIdHeader(headers);
			return string.IsNullOrWhiteSpace(header) ? string.Empty : row.GetValue(header);
		}

		private static string GetName(ItemSeedCsvRow row)
		{
			IReadOnlyList<string> headers = row.SourceFile != null ? row.SourceFile.Headers : Array.Empty<string>();
			string header = ItemSeedCsvService.FindFirstHeader(headers, "name", "display_name", "displayName");
			return string.IsNullOrWhiteSpace(header) ? string.Empty : row.GetValue(header);
		}

		private static string GetCategory(ItemSeedCsvRow row)
		{
			IReadOnlyList<string> headers = row.SourceFile != null ? row.SourceFile.Headers : Array.Empty<string>();
			string header = ItemSeedCsvService.FindFirstHeader(headers, "category", "type", "item_type", "effect_type");
			return string.IsNullOrWhiteSpace(header) ? string.Empty : row.GetValue(header);
		}

		private static string DirtyPrefix(ItemSeedCsvRow row)
		{
			return row.IsDirty || row.IsNew ? "* " : string.Empty;
		}

		private static bool IsBoolHeader(string header)
		{
			return header.StartsWith("has_", StringComparison.OrdinalIgnoreCase)
				|| header.StartsWith("is_", StringComparison.OrdinalIgnoreCase);
		}

		private bool IsOptionHeader(string header, out string[] options)
		{
			if (header.Equals("category", StringComparison.OrdinalIgnoreCase))
			{
				options = _files.SelectMany(file => file.Rows).Select(row => row.GetValue("category")).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
				return options.Length > 0;
			}

			if (header.Equals("rarity", StringComparison.OrdinalIgnoreCase))
			{
				options = _files.SelectMany(file => file.Rows).Select(row => row.GetValue("rarity")).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
				return options.Length > 0;
			}

			if (header.Equals("rune_kind", StringComparison.OrdinalIgnoreCase))
			{
				options = new[] { "Upgrade", "Elemental" };
				return true;
			}

			if (header.Equals("effect_type", StringComparison.OrdinalIgnoreCase))
			{
				options = new[] { "plus", "minus", "status" };
				return true;
			}

			if (header.Equals("effect_stat", StringComparison.OrdinalIgnoreCase))
			{
				options = new[] { "maxhp", "maxmana", "str", "dex", "int", "spi", "vit", "luk", "knockback" };
				return true;
			}

			if (header.Equals("status_id", StringComparison.OrdinalIgnoreCase))
			{
				options = GetKnownStatusIds();
				return true;
			}

			if (header.Equals("status_trigger", StringComparison.OrdinalIgnoreCase))
			{
				options = new[] { "on_hit", "on_equip", "on_use" };
				return true;
			}

			if (header.Equals("rune_element", StringComparison.OrdinalIgnoreCase))
			{
				options = new[] { "Electricity", "Ice", "Fire", "Acid", "Darkness" };
				return true;
			}

			if (header.Equals("magic_shape", StringComparison.OrdinalIgnoreCase))
			{
				options = new[] { "ProjectileBolt", "Linear", "Cone", "CircleWaveAwayFromPlayer" };
				return true;
			}

			options = Array.Empty<string>();
			return false;
		}

		private static bool IsTruthy(string value)
		{
			return value.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("1", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("yes", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("y", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsPathHeader(string header)
		{
			return header.EndsWith("_path", StringComparison.OrdinalIgnoreCase);
		}

		private static string[] GetCreateItemTypes()
		{
			return new[] { "Weapon", "Armor", "Consumable", "Material", "Rune", "Elemental Rune" };
		}

		private static string[] GetKnownStatIds()
		{
			return new[] { "maxhp", "maxmana", "str", "dex", "int", "spi", "vit", "luk", "knockback" };
		}

		private static string[] GetKnownStatusIds()
		{
			return new[]
			{
				"status.knockback",
				"status.stun",
				"status.armor_break",
				"status.cold",
				"status.burn",
				"status.poison",
				"status.mana_burn",
				"status.silence",
				"status.blind",
				"status.thorns"
			};
		}

		private static Button MakeButton(string text)
		{
			return new Button { Text = text };
		}

		private static void ClearChildren(Node node)
		{
			foreach (Node child in node.GetChildren())
			{
				node.RemoveChild(child);
				child.QueueFree();
			}
		}

		private void SelectTab(int index)
		{
			if (_mainTabs != null && index >= 0 && index < _mainTabs.GetTabCount())
			{
				_mainTabs.CurrentTab = index;
			}
		}

		private void SetStatus(string text)
		{
			if (_statusLabel != null)
			{
				_statusLabel.Text = text;
			}
		}
	}
}
#endif
