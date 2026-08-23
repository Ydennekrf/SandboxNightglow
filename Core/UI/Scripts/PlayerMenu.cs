using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ethra.V1
{
    public partial class PlayerMenu : Control
    {
        [Export] private GridContainer InventoryGrid;
        [Export] private InventorySlotView EquippedWeaponSlot;
        [Export] private InventorySlotView EquippedArmorSlot;
        [Export] private InventorySlotView EquippedTrinketSlot1;
        [Export] private InventorySlotView EquippedTrinketSlot2;
        [Export] private RichTextLabel ToolTip;
        [Export] private RichTextLabel StatsStatus;
        [Export] private NodePath AbilityPathAuthorGraphPath;

        private readonly List<InventorySlotView> _inventorySlots = new();
        private InventoryManager _inventory;
        private MasterRepository _repo;
        private InventorySlotView _selectedSlot;
        private Player _player;
        private Button _levelUpButton;
        private PanelContainer _levelUpPanel;
        private Label _progressionSummary;
        private AbilityPathGraphView _abilityGraphView;
        private RichTextLabel _playerStatsReadout;
        private RichTextLabel _abilityDetails;
        private Button _equipButton;
        private Button _unequipButton;
        private Label _equipmentHint;
        private Button _unlockButton;
        private Button _closeLevelUpButton;
        private string _selectedAbilityNodeId = string.Empty;
        private Player _subscribedProgressionPlayer;

        public override void _Ready()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                GD.PushWarning("PlayerMenu: GameManager.Instance is null.");
                return;
            }

            _inventory = gm.Inventory;
            _repo = gm.DB;
            _player = gm.GameState?.GetPlayer();

            CacheInventorySlots();
            BindEquipmentSlots();
            ApplySceneAuthoredAbilityPath();
            BuildLevelUpUi();
            BuildEquipmentActionUi();

            if (_inventory != null)
            {
                _inventory.Changed += RefreshAll;
            }

            SubscribeProgressionEvents(_player);

            RefreshAll();
        }

        public override void _ExitTree()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= RefreshAll;
            }

            foreach (InventorySlotView slot in _inventorySlots)
            {
                slot.Clicked -= OnSlotClicked;
            }

            if (EquippedWeaponSlot != null)
            {
                EquippedWeaponSlot.Clicked -= OnSlotClicked;
            }

            if (EquippedArmorSlot != null)
            {
                EquippedArmorSlot.Clicked -= OnSlotClicked;
            }

            if (EquippedTrinketSlot1 != null)
            {
                EquippedTrinketSlot1.Clicked -= OnSlotClicked;
            }

            if (EquippedTrinketSlot2 != null)
            {
                EquippedTrinketSlot2.Clicked -= OnSlotClicked;
            }

            if (_equipButton != null)
            {
                _equipButton.Pressed -= OnEquipPressed;
            }

            if (_unequipButton != null)
            {
                _unequipButton.Pressed -= OnUnequipPressed;
            }

            SubscribeProgressionEvents(null);

            if (_levelUpButton != null)
            {
                _levelUpButton.Pressed -= OnLevelUpButtonPressed;
            }

            if (_abilityGraphView != null)
            {
                _abilityGraphView.FocusedNodeChanged -= OnAbilityNodeFocused;
            }

            if (_unlockButton != null)
            {
                _unlockButton.Pressed -= OnUnlockAbilityNodePressed;
            }

            if (_closeLevelUpButton != null)
            {
                _closeLevelUpButton.Pressed -= OnCloseLevelUpPressed;
            }
        }

        private void CacheInventorySlots()
        {
            _inventorySlots.Clear();

        if (InventoryGrid == null)
        {
            GD.PushWarning("PlayerMenu: InventoryGrid export is not assigned.");
            return;
        }

        foreach (Node child in InventoryGrid.GetChildren())
        {
            if (child is InventorySlotView slot)
            {
                slot.Clicked += OnSlotClicked;
                _inventorySlots.Add(slot);
            }
        }
        }

        private void BindEquipmentSlots()
        {
            if (EquippedWeaponSlot != null)
            {
                EquippedWeaponSlot.Clicked += OnSlotClicked;
            }

        if (EquippedArmorSlot != null)
        {
            EquippedArmorSlot.Clicked += OnSlotClicked;
        }

        if (EquippedTrinketSlot1 != null)
        {
            EquippedTrinketSlot1.Clicked += OnSlotClicked;
        }

        if (EquippedTrinketSlot2 != null)
        {
            EquippedTrinketSlot2.Clicked += OnSlotClicked;
        }
        }

        private void RefreshAll()
        {
            RefreshInventoryGrid();
            RefreshEquipmentSlots();
            RefreshDetails(_selectedSlot);
            RefreshLevelUpPanel();
        }

        private void BuildEquipmentActionUi()
        {
            if (_equipButton != null)
            {
                return;
            }

            VBoxContainer playerView = GetNodeOrNull<VBoxContainer>("Inventory/VBoxContainer/PlayerView");
            if (playerView == null)
            {
                GD.PushWarning("PlayerMenu: unable to build equipment action UI because PlayerView was not found.");
                return;
            }

            HBoxContainer actionRow = new()
            {
                Name = "EquipmentActions"
            };
            playerView.AddChild(actionRow);

            _equipButton = new Button
            {
                Text = "Equip",
                Disabled = true,
                CustomMinimumSize = new Vector2(72f, 28f)
            };
            _equipButton.Pressed += OnEquipPressed;
            actionRow.AddChild(_equipButton);

            _unequipButton = new Button
            {
                Text = "Unequip",
                Disabled = true,
                CustomMinimumSize = new Vector2(86f, 28f)
            };
            _unequipButton.Pressed += OnUnequipPressed;
            actionRow.AddChild(_unequipButton);

            _equipmentHint = new Label
            {
                Text = string.Empty,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            playerView.AddChild(_equipmentHint);
        }

        private void BuildLevelUpUi()
        {
            if (_levelUpButton != null)
            {
                return;
            }

            VBoxContainer playerView = GetNodeOrNull<VBoxContainer>("Inventory/VBoxContainer/PlayerView");
            HBoxContainer inventoryRoot = GetNodeOrNull<HBoxContainer>("Inventory");
            if (playerView == null || inventoryRoot == null)
            {
                GD.PushWarning("PlayerMenu: unable to build Level Up UI because expected menu containers were not found.");
                return;
            }

            _levelUpButton = new Button
            {
                Text = "Level Up",
                CustomMinimumSize = new Vector2(120f, 28f)
            };
            _levelUpButton.Pressed += OnLevelUpButtonPressed;
            playerView.AddChild(_levelUpButton);

            _levelUpPanel = new PanelContainer
            {
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop
            };
            AddChild(_levelUpPanel);
            _levelUpPanel.SetAnchorsPreset(LayoutPreset.FullRect);
            _levelUpPanel.OffsetLeft = 0f;
            _levelUpPanel.OffsetTop = 0f;
            _levelUpPanel.OffsetRight = 0f;
            _levelUpPanel.OffsetBottom = 0f;

            HBoxContainer layout = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _levelUpPanel.AddChild(layout);

            _abilityGraphView = new AbilityPathGraphView
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(560f, 420f)
            };
            _abilityGraphView.FocusedNodeChanged += OnAbilityNodeFocused;
            layout.AddChild(_abilityGraphView);

            VBoxContainer detailsStack = new()
            {
                CustomMinimumSize = new Vector2(300f, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            layout.AddChild(detailsStack);

            HBoxContainer titleRow = new();
            detailsStack.AddChild(titleRow);

            Label title = new()
            {
                Text = "Ability Path",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            titleRow.AddChild(title);

            _closeLevelUpButton = new Button
            {
                Text = "Close"
            };
            _closeLevelUpButton.Pressed += OnCloseLevelUpPressed;
            titleRow.AddChild(_closeLevelUpButton);

            _progressionSummary = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            detailsStack.AddChild(_progressionSummary);

            _playerStatsReadout = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                CustomMinimumSize = new Vector2(0f, 170f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            detailsStack.AddChild(_playerStatsReadout);

            _abilityDetails = new RichTextLabel
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                BbcodeEnabled = true,
                FitContent = false
            };
            detailsStack.AddChild(_abilityDetails);

            _unlockButton = new Button
            {
                Text = "Unlock",
                Disabled = true
            };
            _unlockButton.Pressed += OnUnlockAbilityNodePressed;
            detailsStack.AddChild(_unlockButton);
        }

        private void RefreshInventoryGrid()
        {
        if (_inventory == null || _repo == null)
        {
            return;
        }

        IReadOnlyList<InventoryManager.InventoryDisplayEntry> items = _inventory.GetInventoryDisplayEntries();

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            if (i >= items.Count)
            {
                _inventorySlots[i].SetEmpty();
                continue;
            }

            InventoryManager.InventoryDisplayEntry entry = items[i];
            InventoryItem item = _repo.GetItemFromRepo(entry.ItemId);

            if (item == null)
            {
                _inventorySlots[i].SetEmpty();
                continue;
            }

            _inventorySlots[i].SetItem(item, entry.Quantity, entry.WeaponInstanceId);
        }
        }

        private void RefreshEquipmentSlots()
        {
        if (_inventory == null || _repo == null)
        {
            return;
        }

        SetEquipmentSlot(EquippedWeaponSlot, _inventory.GetEquippedWeapons(), "MainHand", _inventory.GetEquippedWeaponInstances());
        SetEquipmentSlot(EquippedArmorSlot, _inventory.GetEquippedArmor(), "Armor");
        SetEquipmentSlot(EquippedTrinketSlot1, _inventory.GetEquippedTrinkets(), "Trinket1");
        SetEquipmentSlot(EquippedTrinketSlot2, _inventory.GetEquippedTrinkets(), "Trinket2");
        }

        private void SetEquipmentSlot(
            InventorySlotView slotView,
            IReadOnlyDictionary<string, int> equipped,
            string slotKey,
            IReadOnlyDictionary<string, string> equippedWeaponInstances = null)
        {
        if (slotView == null)
        {
            return;
        }

        if (equipped == null || !equipped.TryGetValue(slotKey, out int itemId))
        {
            slotView.SetEmpty();
            return;
        }

        InventoryItem item = _repo.GetItemFromRepo(itemId);
        if (item == null)
        {
            slotView.SetEmpty();
            return;
        }

        string weaponInstanceId = equippedWeaponInstances != null && equippedWeaponInstances.TryGetValue(slotKey, out string instanceId)
            ? instanceId
            : string.Empty;
        slotView.SetItem(item, 1, weaponInstanceId, slotKey);
        }

        private void OnSlotClicked(InventorySlotView slot)
        {
        SelectSlot(slot);

        if (_inventory == null || slot?.ItemId == null)
        {
            return;
        }

        if (slot.ItemData is WeaponItem)
        {
            _inventory.UseItem(slot.ItemId.Value, slot.WeaponInstanceId);
            RefreshAll();
        }
        }

        private void SelectSlot(InventorySlotView slot)
        {
        _selectedSlot = slot;

        foreach (InventorySlotView inventorySlot in _inventorySlots)
        {
            inventorySlot.SetSelected(inventorySlot == slot);
        }

        if (EquippedWeaponSlot != null)
        {
            EquippedWeaponSlot.SetSelected(EquippedWeaponSlot == slot);
        }

        if (EquippedArmorSlot != null)
        {
            EquippedArmorSlot.SetSelected(EquippedArmorSlot == slot);
        }

        if (EquippedTrinketSlot1 != null)
        {
            EquippedTrinketSlot1.SetSelected(EquippedTrinketSlot1 == slot);
        }

        if (EquippedTrinketSlot2 != null)
        {
            EquippedTrinketSlot2.SetSelected(EquippedTrinketSlot2 == slot);
        }

        RefreshDetails(slot);
        RefreshEquipmentActions(slot);
        }

        private void RefreshDetails(InventorySlotView slot)
        {
        if (ToolTip == null || StatsStatus == null)
        {
            return;
        }

        if (slot?.ItemData == null)
        {
            ToolTip.Text = "Select an item to view its description.";
            StatsStatus.Text = string.Empty;
            RefreshEquipmentActions(slot);
            return;
        }

        InventoryItem item = slot.ItemData;

        ToolTip.Text = item.Description;
        if (item is WeaponItem && !string.IsNullOrWhiteSpace(slot.WeaponInstanceId))
        {
            string socketDescription = BuildWeaponSocketDescription(slot.WeaponInstanceId);
            if (!string.IsNullOrWhiteSpace(socketDescription))
            {
                ToolTip.Text = $"{item.Description}\n\n{socketDescription}";
            }
        }

        var details = new StringBuilder();
        details.AppendLine($"Name: {item.Name}");
        details.AppendLine($"Category: {item.Category}");
        details.AppendLine($"Subtype: {item.Subtype}");
        details.AppendLine($"Rarity: {item.Rarity}");
        details.AppendLine($"Count: {slot.Count}");
        if (!string.IsNullOrWhiteSpace(slot.EquipmentSlotKey))
        {
            details.AppendLine($"Equipped Slot: {slot.EquipmentSlotKey}");
        }

        if (item.Effects != null && item.Effects.Count > 0)
        {
            details.AppendLine("Effects:");
            foreach (ItemEffects effect in item.Effects)
            {
                details.AppendLine($"- {effect.EffectName}");
            }
        }

        if (item is WeaponItem && !string.IsNullOrWhiteSpace(slot.WeaponInstanceId))
        {
            AppendWeaponSocketStats(details, slot.WeaponInstanceId);
        }

        _player = GameManager.Instance?.GameState?.GetPlayer();
        if (_player != null)
        {
            details.AppendLine($"Magic Range Bonus: {_player.MagicRangeBonus}");
        }

        StatsStatus.Text = details.ToString();
        RefreshEquipmentActions(slot);
        }

        private void RefreshEquipmentActions(InventorySlotView slot)
        {
        if (_equipButton == null || _unequipButton == null)
        {
            return;
        }

        bool inventoryEquippable = slot?.ItemData is ArmorItem or TrinketItem
            && string.IsNullOrWhiteSpace(slot.EquipmentSlotKey);
        bool equippedArmorOrTrinket = slot?.ItemData is ArmorItem or TrinketItem
            && !string.IsNullOrWhiteSpace(slot.EquipmentSlotKey);

        _equipButton.Disabled = !inventoryEquippable;
        _unequipButton.Disabled = !equippedArmorOrTrinket;

        if (_equipmentHint == null)
        {
            return;
        }

        if (inventoryEquippable)
        {
            _equipmentHint.Text = slot.ItemData is TrinketItem
                ? "Equip to the first open trinket slot, or replace Trinket1 when both are occupied."
                : "Equip to the armor slot.";
        }
        else if (equippedArmorOrTrinket)
        {
            _equipmentHint.Text = $"Unequip from {slot.EquipmentSlotKey}.";
        }
        else
        {
            _equipmentHint.Text = string.Empty;
        }
        }

        private void OnEquipPressed()
        {
        if (_inventory == null || _selectedSlot?.ItemId == null)
        {
            return;
        }

        _inventory.EquipItem(_selectedSlot.ItemId.Value);
        RefreshAll();
        }

        private void OnUnequipPressed()
        {
        if (_inventory == null || string.IsNullOrWhiteSpace(_selectedSlot?.EquipmentSlotKey))
        {
            return;
        }

        _inventory.UnequipEquipmentSlot(_selectedSlot.EquipmentSlotKey);
        _selectedSlot = null;
        RefreshAll();
        }

        private string BuildWeaponSocketDescription(string weaponInstanceId)
        {
            WeaponInstanceState instance = _inventory?.GetWeaponInstance(weaponInstanceId);
            if (instance == null)
            {
                return string.Empty;
            }

            WeaponUpgradeManager upgrades = GameManager.Instance?.WeaponUpgrades;
            string summary = upgrades?.BuildModifierSummary(instance);
            return string.IsNullOrWhiteSpace(summary) ? string.Empty : $"Socketed runes:\n{summary}";
        }

        private void AppendWeaponSocketStats(StringBuilder details, string weaponInstanceId)
        {
            WeaponInstanceState instance = _inventory?.GetWeaponInstance(weaponInstanceId);
            if (instance == null)
            {
                return;
            }

            details.AppendLine("Socketed Runes:");
            if (instance.UpgradeRuneItemIds.Count == 0 && !instance.ElementalRuneItemId.HasValue)
            {
                details.AppendLine("- None");
                return;
            }

            foreach (int runeId in instance.UpgradeRuneItemIds)
            {
                InventoryItem rune = _repo.GetItemFromRepo(runeId);
                if (rune == null)
                {
                    continue;
                }

                details.AppendLine($"- {rune.Name}");
                foreach (ItemEffects effect in rune.Effects ?? new List<ItemEffects>())
                {
                    details.AppendLine($"  {effect.EffectName}");
                }
            }

            if (instance.ElementalRuneItemId.HasValue && _repo.GetItemFromRepo(instance.ElementalRuneItemId.Value) is RuneItem elemental)
            {
                details.AppendLine($"- {elemental.Name}");
                details.AppendLine($"  Element: {elemental.Element}");
                details.AppendLine($"  Shape: {elemental.Shape}");
            }
        }

        private void RefreshLevelUpPanel()
        {
            if (_levelUpPanel == null || !_levelUpPanel.Visible)
            {
                return;
            }

            _player = GameManager.Instance?.GameState?.GetPlayer();
            SubscribeProgressionEvents(_player);
            ApplySceneAuthoredAbilityPath();
            if (_player == null)
            {
                if (_progressionSummary != null)
                {
                    _progressionSummary.Text = "Player progression is unavailable.";
                }
                RefreshPlayerStatsReadout();
                return;
            }

            if (_progressionSummary != null)
            {
                _progressionSummary.Text =
                    $"LV {_player.Progression.Level}  XP {_player.Progression.CurrentExperience}/{_player.Progression.ExperienceToNextLevel}  AP {_player.Progression.AbilityPoints}\n"
                    + $"Current: {SafeNodeLabel(_player.AbilityPath.CurrentNodeId)}  Unlocked nodes: {_player.AbilityPath.UnlockedNodeIds.Count}";
            }

            RefreshPlayerStatsReadout();
            _abilityGraphView?.Bind(_player);
            RefreshAbilityDetails();
        }

        private void RefreshPlayerStatsReadout()
        {
            if (_playerStatsReadout == null)
            {
                return;
            }

            if (_player == null)
            {
                _playerStatsReadout.Text = "[b]Stats[/b]\nUnavailable";
                return;
            }

            var stats = new StringBuilder();
            stats.AppendLine("[b]Current Stats[/b]");
            stats.AppendLine($"HP: {_player.CurHP}/{_player.MaxHP}");
            stats.AppendLine($"MP: {_player.CurMana}/{_player.MaxMana}");
            stats.AppendLine($"Strength: {_player.Strength}");
            stats.AppendLine($"Dexterity: {_player.Dexterity}");
            stats.AppendLine($"Intelligence: {_player.Intelligence}");
            stats.AppendLine($"Spirit: {_player.Spirit}");
            stats.AppendLine($"Vitality: {_player.Vitality}");
            stats.AppendLine($"Luck: {_player.Luck}");

            if (_player.AbilityPath.UnlockedActiveAbilityIds.Count > 0)
            {
                stats.AppendLine($"Active: {string.Join(", ", _player.AbilityPath.UnlockedActiveAbilityIds)}");
            }

            if (_player.AbilityPath.UnlockedPassiveAbilityIds.Count > 0)
            {
                stats.AppendLine($"Passive: {string.Join(", ", _player.AbilityPath.UnlockedPassiveAbilityIds)}");
            }

            _playerStatsReadout.Text = stats.ToString();
        }

        private void RefreshAbilityDetails()
        {
            if (_abilityDetails == null || _unlockButton == null)
            {
                return;
            }

            if (_player == null || string.IsNullOrWhiteSpace(_selectedAbilityNodeId) || !_player.AbilityPath.TryGetNode(_selectedAbilityNodeId, out AbilityPathNodeDefinition node))
            {
                _abilityDetails.Text = "Select an available connected node.";
                _unlockButton.Disabled = true;
                return;
            }

            var details = new StringBuilder();
            details.AppendLine($"[b]{node.DisplayName}[/b]");
            details.AppendLine(node.Description);
            details.AppendLine($"Cost: {node.Cost} AP");
            details.AppendLine($"Type: {node.NodeType}");
            if (node.Effects != null && node.Effects.Count > 0)
            {
                details.AppendLine("Effects:");
                foreach (AbilityEffectDefinition effect in node.Effects)
                {
                    details.AppendLine($"- {AbilityEffectApplier.Describe(effect)}");
                }
            }

            _abilityDetails.Text = details.ToString();
            bool canUnlock = _player.AbilityPath.IsUnlockable(node.NodeId) && _player.Progression.AbilityPoints >= node.Cost;
            _unlockButton.Disabled = !canUnlock;
            _unlockButton.Text = _player.AbilityPath.IsUnlocked(node.NodeId) ? "Unlocked" : "Unlock";
        }

        private void OnLevelUpButtonPressed()
        {
            if (_levelUpPanel == null)
            {
                return;
            }

            _levelUpPanel.Visible = !_levelUpPanel.Visible;
            RefreshLevelUpPanel();
        }

        private void OnCloseLevelUpPressed()
        {
            if (_levelUpPanel != null)
            {
                _levelUpPanel.Visible = false;
            }
        }

        private void OnAbilityNodeFocused(string nodeId)
        {
            _selectedAbilityNodeId = nodeId ?? string.Empty;
            RefreshAbilityDetails();
        }

        private void OnUnlockAbilityNodePressed()
        {
            _player = GameManager.Instance?.GameState?.GetPlayer();
            if (_player == null || string.IsNullOrWhiteSpace(_selectedAbilityNodeId))
            {
                return;
            }

            if (_player.AbilityPath.TryUnlockNode(_selectedAbilityNodeId, _player, out string message))
            {
                GameManager.Instance?.Publish(
                    GameEvent.NotificationRequested,
                    new NotificationRequest(message, NotificationType.Info));
                _selectedAbilityNodeId = string.Empty;
            }
            else
            {
                GameManager.Instance?.Publish(
                GameEvent.NotificationRequested,
                    new NotificationRequest(message, NotificationType.Error));
            }

            RefreshAll();
            _abilityGraphView?.Refresh();
            GameManager.Instance?.UI?.BindPlayerHud(_player);
        }

        private void OnProgressionChanged(PlayerExperienceChangedEvent evt) => RefreshLevelUpPanel();
        private void OnProgressionChanged(PlayerLevelChangedEvent evt) => RefreshLevelUpPanel();
        private void OnProgressionChanged(PlayerAbilityPointsChangedEvent evt) => RefreshLevelUpPanel();

        private static string SafeNodeLabel(string nodeId)
        {
            return string.IsNullOrWhiteSpace(nodeId) ? "None" : nodeId;
        }

        private void ApplySceneAuthoredAbilityPath()
        {
            _player = GameManager.Instance?.GameState?.GetPlayer();
            AbilityPathAuthorGraph graph = ResolveAbilityPathAuthorGraph();
            if (_player == null || graph == null)
            {
                return;
            }

            AbilityPathDefinition definition = graph.BuildDefinition();
            if (definition.Nodes.Count == 0)
            {
                GD.PushWarning("PlayerMenu: scene-authored ability path graph has no nodes.");
                return;
            }

            _player.AbilityPath.SetDefinitionPreservingState(definition);
        }

        private void SubscribeProgressionEvents(Player player)
        {
            if (_subscribedProgressionPlayer == player)
            {
                return;
            }

            if (_subscribedProgressionPlayer?.Progression != null)
            {
                _subscribedProgressionPlayer.Progression.ExperienceChanged -= OnProgressionChanged;
                _subscribedProgressionPlayer.Progression.LevelChanged -= OnProgressionChanged;
                _subscribedProgressionPlayer.Progression.AbilityPointsChanged -= OnProgressionChanged;
            }

            _subscribedProgressionPlayer = player;

            if (_subscribedProgressionPlayer?.Progression != null)
            {
                _subscribedProgressionPlayer.Progression.ExperienceChanged += OnProgressionChanged;
                _subscribedProgressionPlayer.Progression.LevelChanged += OnProgressionChanged;
                _subscribedProgressionPlayer.Progression.AbilityPointsChanged += OnProgressionChanged;
            }
        }

        private AbilityPathAuthorGraph ResolveAbilityPathAuthorGraph()
        {
            if (!string.IsNullOrWhiteSpace(AbilityPathAuthorGraphPath?.ToString()))
            {
                AbilityPathAuthorGraph configured = GetNodeOrNull<AbilityPathAuthorGraph>(AbilityPathAuthorGraphPath);
                if (configured != null)
                {
                    return configured;
                }
            }

            return GetNodeOrNull<AbilityPathAuthorGraph>("AbilityPathAuthorGraph");
        }
    }
}
