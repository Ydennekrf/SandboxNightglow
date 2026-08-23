using Godot;
using System.Collections.Generic;
using System.Linq;
using DebugGameManager = ethra.V1.GameManager;
using DebugEnemySpawnMarker = ethra.V1.EnemySpawnMarker;
using DebugInventoryStats = ethra.V1.IStats;
using DebugPlayer = ethra.V1.Player;
using DebugTestEnemyNode = ethra.V1.TestEnemyNode;

/// <summary>
/// Debug-only harness for interactables, prompts, chest/button behavior, crafting seeds, and combat overlays.
/// </summary>
/// <remarks>
/// This scene root should place and seed test objects only. Prompt selection, interactable behavior,
/// inventory changes, and combat effects should remain in reusable components/managers.
/// </remarks>
public partial class InteractionDebugSceneRoot : Node2D
{
    private const int DefaultDebugWeaponItemId = 2001;
    private const int DefaultDebugSocketWeaponItemId = 2003;
    private const int DefaultDebugCraftingMaterialItemId = 3001;
    private const int DefaultDebugCraftingMaterialQuantity = 20;
    private const int DefaultDebugArmorItemId = 4301;
    private const int DefaultDebugTrinketItemId = 4421;
    private static readonly int[] DefaultDebugRuneItemIds = { 5001, 5002, 5003, 5101, 5111, 5121, 5131, 5141 };

    private int _initializeAttempts;
    private DebugGameManager _gameManager;
    private DebugPlayer _player;
    private PanelContainer _statusTogglePanel;
    private readonly List<DebugTestEnemyNode> _testEnemies = new();
    private readonly HashSet<string> _enabledStatusEffectIds = new();
    private readonly Dictionary<string, CheckBox> _statusToggleButtons = new();

    [Export] public PackedScene PlayerScene { get; set; }
    [Export] public PackedScene TestEnemyScene { get; set; }
    [Export] public NodePath GameManagerPath { get; set; } = "GameManager";
    [Export] public NodePath WorldPath { get; set; } = "World";
    [Export] public string InitialSpawnName { get; set; } = "NewGameSpawn";
    [Export] public NodePath EnemySpawnerParentPath { get; set; } = "World/Entities/SpawnPoints";
    [Export] public bool SpawnEnemiesOnReady { get; set; }
    [Export] public int DebugWeaponItemId { get; set; } = 2001;
    [Export] public bool EnableDebugEnemyDamageKeys { get; set; } = true;
    [Export] public int DebugEnemyDamageAmount { get; set; } = 25;
    [Export] public bool EnableDebugExperienceKey { get; set; } = true;
    [Export] public int DebugExperienceGrantAmount { get; set; } = 25;
    [Export] public NodePath DebugHudParentPath { get; set; } = "UI/Hud";
    [Export] public bool ShowStatusPanelOnReady { get; set; }
    [ExportGroup("Weapon Upgrade Debug Seeds")]
    [Export] public int DebugSocketWeaponItemId { get; set; } = 2003;
    [Export] public int DebugCraftingMaterialItemId { get; set; } = 3001;
    [Export] public int DebugCraftingMaterialQuantity { get; set; } = 20;
    [Export] public int[] DebugRuneItemIds { get; set; } = { 5001, 5002, 5003, 5101, 5111, 5121, 5131, 5141 };
    [ExportGroup("Equipment Debug Seeds")]
    [Export] public int DebugArmorItemId { get; set; } = 4301;
    [Export] public int DebugTrinketItemId { get; set; } = 4421;
    [ExportGroup("Debug Player Stats")]
    [Export] public int DebugPlayerMaxHealth { get; set; } = 100;
    [Export] public int DebugPlayerMaxMana { get; set; } = 100;
    [Export] public int DebugPlayerStrength { get; set; } = 10;
    [Export] public int DebugPlayerDexterity { get; set; } = 10;
    [Export] public int DebugPlayerIntelligence { get; set; } = 10;
    [Export] public int DebugPlayerSpirit { get; set; } = 10;
    [Export] public int DebugPlayerVitality { get; set; } = 10;
    [Export] public int DebugPlayerLuck { get; set; } = 10;

    public override void _Ready()
    {
        ethra.V1.CombatFeedbackBus.PayloadQueued += OnPayloadQueued;
        CallDeferred(nameof(InitializeManualDebugScene));
    }

    public override void _ExitTree()
    {
        ethra.V1.CombatFeedbackBus.PayloadQueued -= OnPayloadQueued;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.P)
        {
            ToggleStatusPanel();
            GetViewport().SetInputAsHandled();
        }
        else if (EnableDebugEnemyDamageKeys && key.Keycode == Key.H)
        {
            ApplyDebugEnemyDamage(DebugEnemyDamageAmount);
            GetViewport().SetInputAsHandled();
        }
        else if (EnableDebugEnemyDamageKeys && key.Keycode == Key.K)
        {
            ApplyDebugEnemyDamage(int.MaxValue);
            GetViewport().SetInputAsHandled();
        }
        else if (EnableDebugExperienceKey && key.Keycode == Key.X)
        {
            GrantDebugExperience();
            GetViewport().SetInputAsHandled();
        }
        else if (TryToggleStatusEffectByKey(key.Keycode))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void InitializeManualDebugScene()
    {
        DebugGameManager gameManager = DebugGameManager.Instance ?? GetNodeOrFallback<DebugGameManager>(GameManagerPath, "GameManager");
        WorldSceneRoot world = GetNodeOrFallback<WorldSceneRoot>(WorldPath, "World");

        if (gameManager == null)
        {
            GD.PushError("[InteractionDebug] GameManager is missing.");
            return;
        }

        if (world == null)
        {
            GD.PushError("[InteractionDebug] WorldSceneRoot is missing.");
            return;
        }

        if (!IsGameManagerReady(gameManager))
        {
            _initializeAttempts++;
            if (_initializeAttempts > 30)
            {
                GD.PushError("[InteractionDebug] GameManager managers did not initialize.");
                return;
            }

            CallDeferred(nameof(InitializeManualDebugScene));
            return;
        }

        DebugPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);
        _gameManager = gameManager;
        _player = player;

        SpawnPlayer(gameManager, world, player);
        gameManager.InitializeCurrentSceneUi();
        gameManager.UI?.BindPlayerHud(player);
        EquipDebugWeapon(gameManager);
        SeedWeaponUpgradeDebugInventory(gameManager);
        SeedEquipmentDebugInventory(gameManager);
        if (SpawnEnemiesOnReady)
        {
            SpawnEnemyMarkers();
        }
        BuildStatusTogglePanel();
        RunWorldStateDebugSmoke(gameManager);

        GD.Print("[InteractionDebug] Manual interaction debug scene ready.");
        GD.Print("[InteractionDebug] Press P to show or hide attack effect toggles. H damages nearest enemy, K defeats it, X grants XP.");
    }

    private T GetNodeOrFallback<T>(NodePath configuredPath, string fallbackPath) where T : Node
    {
        T node = null;
        if (!IsEmptyNodePath(configuredPath))
        {
            node = GetNodeOrNull<T>(configuredPath);
        }

        return node ?? GetNodeOrNull<T>(fallbackPath);
    }

    private void SpawnPlayer(DebugGameManager gameManager, WorldSceneRoot world, DebugPlayer player)
    {
        PackedScene playerScene = PlayerScene ?? gameManager.PlayerScene;
        if (playerScene == null)
        {
            GD.PushError("[InteractionDebug] Player scene is missing.");
            return;
        }

        Marker2D spawn = world.GetSpawn(string.IsNullOrWhiteSpace(InitialSpawnName) ? "NewGameSpawn" : InitialSpawnName);
        Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
        gameManager.Scene.SpawnPlayerNode(playerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);
    }

    private void SpawnEnemyMarkers()
    {
        _testEnemies.Clear();

        Node parent = GetNodeOrFallback<Node>(EnemySpawnerParentPath, "World/Entities/SpawnPoints");
        if (parent == null)
        {
            GD.PushWarning($"[InteractionDebug] Enemy spawner parent '{EnemySpawnerParentPath}' was not found.");
            return;
        }

        int spawnedCount = 0;
        foreach (Node child in parent.GetChildren())
        {
            if (child is not DebugEnemySpawnMarker marker)
            {
                continue;
            }

            if (TrySpawnEnemy(marker))
            {
                spawnedCount++;
            }
        }

        GD.Print($"[InteractionDebug] Spawned debug enemies: {spawnedCount}");
    }

    private bool TrySpawnEnemy(DebugEnemySpawnMarker marker)
    {
        if (marker == null)
        {
            return false;
        }

        if (marker.EnemyScene == null)
        {
            marker.EnemyScene = TestEnemyScene;
        }

        Node2D spawned = marker.SpawnEnemy();
        if (spawned is DebugTestEnemyNode testEnemy)
        {
            _testEnemies.Add(testEnemy);
            return true;
        }

        if (spawned != null)
        {
            GD.PushWarning($"[InteractionDebug] Spawned enemy from '{marker.Name}' is not TestEnemyNode; debug H/K damage shortcuts are unavailable for it.");
        }

        return spawned != null;
    }

    private void ApplyDebugEnemyDamage(int requestedDamage)
    {
        DebugTestEnemyNode targetEnemy = FindDebugDamageTarget();
        if (_gameManager?.Combat == null || targetEnemy?.EnemyModel == null)
        {
            GD.PushWarning("[InteractionDebug] Debug enemy damage skipped; test enemy is not ready.");
            return;
        }

        if (targetEnemy.EnemyModel is not DebugInventoryStats stats || stats.CurHP <= 0)
        {
            return;
        }

        int damage = requestedDamage == int.MaxValue
            ? stats.CurHP
            : Mathf.Clamp(requestedDamage, 1, stats.CurHP);

        _gameManager.Combat.DealDamage(targetEnemy.EnemyModel, damage, "Debug", _player);
        ethra.V1.CombatFeedbackBus.EmitHitResolved(_player, targetEnemy.EnemyModel, damage, false, "Debug", string.Empty);
        if (targetEnemy.EnemyModel is DebugInventoryStats updatedStats && updatedStats.CurHP <= 0)
        {
            _gameManager.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest($"{targetEnemy.Name} defeated.", NotificationType.Info));
        }

        GD.Print($"[InteractionDebug] Applied {damage} debug damage to {targetEnemy.Name}. HP now {targetEnemy.CurrentHealth}.");
    }

    private void GrantDebugExperience()
    {
        if (_player == null)
        {
            GD.PushWarning("[InteractionDebug] Cannot grant XP; player is not ready.");
            return;
        }

        int amount = Mathf.Max(1, DebugExperienceGrantAmount);
        GD.Print($"[InteractionDebug] Granting {amount} XP.");
        _player.GainExperience(amount);
    }

    private void BuildStatusTogglePanel()
    {
        Node parent = GetNodeOrFallback<Node>(DebugHudParentPath, "UI/Hud");
        if (parent == null)
        {
            GD.PushWarning("[StatusDebug] Debug HUD parent missing; status toggles were not created.");
            return;
        }

        if (parent.GetNodeOrNull("StatusEffectToggles") is PanelContainer existingPanel)
        {
            _statusTogglePanel = existingPanel;
            _statusTogglePanel.Visible = ShowStatusPanelOnReady;
            return;
        }

        PanelContainer panel = new()
        {
            Name = "StatusEffectToggles",
            Visible = ShowStatusPanelOnReady,
            OffsetLeft = 16f,
            OffsetTop = 112f,
            OffsetRight = 216f,
            OffsetBottom = 430f,
            MouseFilter = Control.MouseFilterEnum.Stop
        };

        VBoxContainer list = new()
        {
            Name = "ToggleList"
        };
        panel.AddChild(list);

        Label title = new()
        {
            Text = "Attack Effects (1-0)"
        };
        list.AddChild(title);

        foreach ((string label, string statusId, string keyLabel) in GetDebugStatusOptions())
        {
            CheckBox toggle = new()
            {
                Text = $"{keyLabel} {label}",
                TooltipText = statusId,
                FocusMode = Control.FocusModeEnum.None
            };
            toggle.Toggled += enabled => SetStatusToggle(statusId, enabled);
            _statusToggleButtons[statusId] = toggle;
            list.AddChild(toggle);
        }

        parent.AddChild(panel);
        _statusTogglePanel = panel;
    }

    private void ToggleStatusPanel()
    {
        if (_statusTogglePanel == null)
        {
            BuildStatusTogglePanel();
        }

        if (_statusTogglePanel == null)
        {
            return;
        }

        _statusTogglePanel.Visible = !_statusTogglePanel.Visible;
        GD.Print($"[StatusDebug] Attack effect panel visible: {_statusTogglePanel.Visible}");
    }

    private void SetStatusToggle(string statusId, bool enabled)
    {
        if (enabled)
        {
            _enabledStatusEffectIds.Add(statusId);
            GD.Print($"[StatusDebug] Enabled effect: {statusId}");
        }
        else
        {
            _enabledStatusEffectIds.Remove(statusId);
            GD.Print($"[StatusDebug] Disabled effect: {statusId}");
        }

        if (_statusToggleButtons.TryGetValue(statusId, out CheckBox toggle) && toggle.ButtonPressed != enabled)
        {
            toggle.SetPressedNoSignal(enabled);
        }
    }

    private bool TryToggleStatusEffectByKey(Key key)
    {
        int optionIndex = key switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            Key.Key6 => 5,
            Key.Key7 => 6,
            Key.Key8 => 7,
            Key.Key9 => 8,
            Key.Key0 => 9,
            _ => -1
        };

        IReadOnlyList<(string Label, string StatusId, string KeyLabel)> options = GetDebugStatusOptions();
        if (optionIndex < 0 || optionIndex >= options.Count)
        {
            return false;
        }

        string statusId = options[optionIndex].StatusId;
        SetStatusToggle(statusId, !_enabledStatusEffectIds.Contains(statusId));
        return true;
    }

    private void OnPayloadQueued(ethra.V1.AttackPayloadPacket packet)
    {
        if (packet?.Source != _player || _enabledStatusEffectIds.Count == 0)
        {
            return;
        }

        foreach (string statusId in _enabledStatusEffectIds)
        {
            if (!packet.AdditionalEffectIds.Contains(statusId))
            {
                packet.AdditionalEffectIds.Add(statusId);
            }
        }

        GD.Print($"[StatusDebug] Player attack will apply: {string.Join(", ", packet.AdditionalEffectIds)}");
    }

    private static IReadOnlyList<(string Label, string StatusId, string KeyLabel)> GetDebugStatusOptions()
    {
        return new List<(string Label, string StatusId, string KeyLabel)>
        {
            ("Knockback", ethra.V1.StatusEffectCatalog.Knockback, "1"),
            ("Stun", ethra.V1.StatusEffectCatalog.Stun, "2"),
            ("Armor Break", ethra.V1.StatusEffectCatalog.ArmorBreak, "3"),
            ("Cold", ethra.V1.StatusEffectCatalog.Cold, "4"),
            ("Burn", ethra.V1.StatusEffectCatalog.Burn, "5"),
            ("Poison", ethra.V1.StatusEffectCatalog.Poison, "6"),
            ("Mana Burn", ethra.V1.StatusEffectCatalog.ManaBurn, "7"),
            ("Silence", ethra.V1.StatusEffectCatalog.Silence, "8"),
            ("Blind", ethra.V1.StatusEffectCatalog.Blind, "9"),
            ("Thorns (target)", ethra.V1.StatusEffectCatalog.Thorns, "0")
        };
    }

    private DebugTestEnemyNode FindDebugDamageTarget()
    {
        DebugTestEnemyNode best = null;
        float bestDistanceSquared = float.PositiveInfinity;
        Vector2 origin = GetTree()?.GetFirstNodeInGroup("Player") is Node2D playerNode
            ? playerNode.GlobalPosition
            : Vector2.Zero;

        foreach (DebugTestEnemyNode enemy in _testEnemies)
        {
            if (enemy == null || enemy.EnemyModel is not DebugInventoryStats stats || stats.CurHP <= 0)
            {
                continue;
            }

            float distanceSquared = origin.DistanceSquaredTo(enemy.GlobalPosition);
            if (distanceSquared < bestDistanceSquared)
            {
                best = enemy;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private static void RunWorldStateDebugSmoke(DebugGameManager gameManager)
    {
        if (gameManager?.GameState == null)
        {
            GD.PushWarning("[InteractionDebug] GameStateManager is unavailable; skipped world state debug smoke.");
            return;
        }

        var gameState = gameManager.GameState;
        gameState.SetFlag("debug.test_flag");
        gameState.AddFriendship("test_npc", 5);
        gameState.MarkAreaExplored("debug_area");
        gameState.SetCurrentLocation("InteractionDebugScene", "Start", "debug_area");
        gameState.SetTimeOfDay(1, "Evening");

        WorldStateDto snapshot = gameState.CaptureWorldStateSnapshot();
        gameState.RestoreWorldStateSnapshot(snapshot);
    }

    private void ConfigurePlayerStats(DebugInventoryStats stats)
    {
        if (stats == null)
        {
            return;
        }

        stats.MaxHP = DebugPlayerMaxHealth > 0 ? DebugPlayerMaxHealth : 100;
        stats.CurHP = stats.MaxHP;
        stats.MaxMana = DebugPlayerMaxMana > 0 ? DebugPlayerMaxMana : 100;
        stats.CurMana = stats.MaxMana;
        stats.Strength = DebugPlayerStrength > 0 ? DebugPlayerStrength : 10;
        stats.Dexterity = DebugPlayerDexterity > 0 ? DebugPlayerDexterity : 10;
        stats.Intelligence = DebugPlayerIntelligence > 0 ? DebugPlayerIntelligence : 10;
        stats.Spirit = DebugPlayerSpirit > 0 ? DebugPlayerSpirit : 10;
        stats.Vitality = DebugPlayerVitality > 0 ? DebugPlayerVitality : 10;
        stats.Luck = DebugPlayerLuck > 0 ? DebugPlayerLuck : 10;
    }

    private static bool IsGameManagerReady(DebugGameManager gameManager)
    {
        return gameManager.Combat != null
            && gameManager.Entity != null
            && gameManager.GameState != null
            && gameManager.Scene != null
            && gameManager.Inventory != null;
    }

    private void EquipDebugWeapon(DebugGameManager gameManager)
    {
        if (gameManager.Inventory == null)
        {
            GD.PushWarning("[InteractionDebug] Inventory manager is missing; debug weapon was not equipped.");
            return;
        }

        int weaponItemId = DebugWeaponItemId > 0 ? DebugWeaponItemId : DefaultDebugWeaponItemId;
        if (weaponItemId <= 0)
        {
            GD.PushWarning("[InteractionDebug] DebugWeaponItemId is not configured; debug weapon was not equipped.");
            return;
        }

        gameManager.Inventory.AddItem(weaponItemId);
        gameManager.Inventory.UseItem(weaponItemId);
    }

    private void SeedWeaponUpgradeDebugInventory(DebugGameManager gameManager)
    {
        if (gameManager?.Inventory == null)
        {
            return;
        }

        int craftingMaterialItemId = DebugCraftingMaterialItemId > 0 ? DebugCraftingMaterialItemId : DefaultDebugCraftingMaterialItemId;
        int craftingMaterialQuantity = DebugCraftingMaterialQuantity > 0 ? DebugCraftingMaterialQuantity : DefaultDebugCraftingMaterialQuantity;
        if (craftingMaterialItemId > 0 && craftingMaterialQuantity > 0)
        {
            gameManager.Inventory.AddItemQuantity(craftingMaterialItemId, craftingMaterialQuantity);
        }

        int socketWeaponItemId = DebugSocketWeaponItemId > 0 ? DebugSocketWeaponItemId : DefaultDebugSocketWeaponItemId;
        if (socketWeaponItemId > 0)
        {
            gameManager.Inventory.AddItem(socketWeaponItemId);
        }

        foreach (int runeId in DebugRuneItemIds is { Length: > 0 } ? DebugRuneItemIds : DefaultDebugRuneItemIds)
        {
            if (runeId > 0)
            {
                gameManager.Inventory.AddItemQuantity(runeId, 1);
            }
        }

        GD.Print("[InteractionDebug] Seeded crafting materials and weapon upgrade runes.");
    }

    private void SeedEquipmentDebugInventory(DebugGameManager gameManager)
    {
        if (gameManager?.Inventory == null)
        {
            return;
        }

        int armorItemId = DebugArmorItemId > 0 ? DebugArmorItemId : DefaultDebugArmorItemId;
        if (armorItemId > 0)
        {
            gameManager.Inventory.AddItem(armorItemId);
        }

        int trinketItemId = DebugTrinketItemId > 0 ? DebugTrinketItemId : DefaultDebugTrinketItemId;
        if (trinketItemId > 0)
        {
            gameManager.Inventory.AddItem(trinketItemId);
        }

        GD.Print("[InteractionDebug] Seeded armor/trinket equipment debug items.");
    }

    private static bool IsEmptyNodePath(NodePath path)
    {
        return path == null || string.IsNullOrWhiteSpace(path.ToString());
    }
}
