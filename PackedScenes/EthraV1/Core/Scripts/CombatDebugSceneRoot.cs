using Godot;
using System.Collections.Generic;
using System.Linq;
using CombatGameManager = ethra.V1.GameManager;
using CombatEntity = ethra.V1.Entity;
using CombatEnemySpawnMarker = ethra.V1.EnemySpawnMarker;
using CombatInventoryStats = ethra.V1.IStats;
using CombatPlayer = ethra.V1.Player;
using CombatTestEnemyNode = ethra.V1.TestEnemyNode;

/// <summary>
/// Debug-only harness for combat, status effects, enemy spawning, and player progression smoke checks.
/// </summary>
/// <remarks>
/// This root may arrange debug controls and seed test state, but reusable combat behavior belongs in
/// CombatManager, TestEnemyNode, HitBox/HurtBox, and FSM actions.
/// </remarks>
public partial class CombatDebugSceneRoot : Node2D
{
    private int _initializeAttempts;
    private CombatGameManager _gameManager;
    private CombatPlayer _player;
    private readonly List<CombatTestEnemyNode> _testEnemies = new();
    private readonly HashSet<string> _enabledStatusEffectIds = new();
    private readonly Dictionary<string, CheckBox> _statusToggleButtons = new();
    private Label _comboDebugLabel;
    private string _currentWeaponName = "<none>";
    private string _currentComboId = "<none>";
    private string _currentComboStep = "<none>";
    private string _currentHitboxProfile = "<none>";
    private string _currentWindowState = "idle";
    private string _lastHitTarget = "<none>";

    [Export] public PackedScene PlayerScene;
    [Export] public PackedScene TestEnemyScene;
    [Export] public NodePath GameManagerPath = "GameManager";
    [Export] public NodePath WorldPath = "World";
    [Export] public string InitialSpawnName = "NewGameSpawn";
    [Export] public NodePath EnemySpawnerPath = "World/Entities/SpawnPoints/TimedEnemySpawn";
    [Export] public NodePath EnemySpawnerParentPath = "World/Entities/SpawnPoints";
    [Export] public int DebugWeaponItemId = 2001;
    [Export] public bool EnableDebugEnemyDamageKeys = true;
    [Export] public int DebugEnemyDamageAmount = 25;
    [Export] public bool EnableDebugExperienceKey = true;
    [Export] public int DebugExperienceGrantAmount = 25;
    [Export] public NodePath DebugHudParentPath = "UI/Hud";
    [ExportGroup("Debug Player Stats")]
    [Export] public int DebugPlayerMaxHealth = 100;
    [Export] public int DebugPlayerMaxMana = 100;
    [Export] public int DebugPlayerStrength = 10;
    [Export] public int DebugPlayerDexterity = 10;
    [Export] public int DebugPlayerIntelligence = 10;
    [Export] public int DebugPlayerSpirit = 10;
    [Export] public int DebugPlayerVitality = 10;
    [Export] public int DebugPlayerLuck = 10;

    public override void _Ready()
    {
        ethra.V1.CombatFeedbackBus.PayloadQueued += OnPayloadQueued;
        ethra.V1.CombatFeedbackBus.ComboStepStarted += OnComboStepStarted;
        ethra.V1.CombatFeedbackBus.ActiveWindowChanged += OnActiveWindowChanged;
        ethra.V1.CombatFeedbackBus.BufferWindowChanged += OnBufferWindowChanged;
        ethra.V1.CombatFeedbackBus.HitResolved += OnHitResolved;
        CallDeferred(nameof(InitializeManualDebugScene));
    }

    public override void _ExitTree()
    {
        ethra.V1.CombatFeedbackBus.PayloadQueued -= OnPayloadQueued;
        ethra.V1.CombatFeedbackBus.ComboStepStarted -= OnComboStepStarted;
        ethra.V1.CombatFeedbackBus.ActiveWindowChanged -= OnActiveWindowChanged;
        ethra.V1.CombatFeedbackBus.BufferWindowChanged -= OnBufferWindowChanged;
        ethra.V1.CombatFeedbackBus.HitResolved -= OnHitResolved;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (EnableDebugEnemyDamageKeys && key.Keycode == Key.H)
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
        CombatGameManager gameManager = CombatGameManager.Instance ?? GetNodeOrFallback<CombatGameManager>(GameManagerPath, "GameManager");
        WorldSceneRoot world = GetNodeOrFallback<WorldSceneRoot>(WorldPath, "World");

        if (gameManager == null)
        {
            GD.PushError("[CombatDebug] GameManager is missing.");
            return;
        }

        if (world == null)
        {
            GD.PushError("[CombatDebug] WorldSceneRoot is missing.");
            return;
        }

        if (!IsGameManagerReady(gameManager))
        {
            _initializeAttempts++;
            if (_initializeAttempts > 30)
            {
                GD.PushError("[CombatDebug] GameManager managers did not initialize.");
                return;
            }

            CallDeferred(nameof(InitializeManualDebugScene));
            return;
        }

        CombatPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);
        _gameManager = gameManager;
        _player = player;

        SpawnPlayer(gameManager, world, player);
        gameManager.InitializeCurrentSceneUi();
        gameManager.UI?.BindPlayerHud(player);
        EquipDebugWeapon(gameManager);

        SpawnTestEnemies();
        BuildComboDebugPanel();
        BuildStatusTogglePanel();

        GD.Print("[CombatDebug] Enemy behavior test ready.");
        GD.Print("[CombatDebug] Press H to damage the nearest test enemy. Press K to defeat it. Press X to grant player XP.");
        GD.Print($"[CombatDebug] Spawned test enemies: {_testEnemies.Count}");
        foreach (CombatTestEnemyNode testEnemy in _testEnemies)
        {
            GD.Print($"[CombatDebug] {testEnemy.Name} HP: {testEnemy.CurrentHealth}");
        }
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

    private void SpawnPlayer(CombatGameManager gameManager, WorldSceneRoot world, CombatPlayer player)
    {
        PackedScene playerScene = PlayerScene ?? gameManager.PlayerScene;
        if (playerScene == null)
        {
            GD.PushError("[CombatDebug] Player scene is missing.");
            return;
        }

        Marker2D spawn = world.GetSpawn(string.IsNullOrWhiteSpace(InitialSpawnName) ? "NewGameSpawn" : InitialSpawnName);
        Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
        gameManager.Scene.SpawnPlayerNode(playerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);
    }

    private void SpawnTestEnemies()
    {
        _testEnemies.Clear();

        Node parent = GetNodeOrFallback<Node>(EnemySpawnerParentPath, "World/Entities/SpawnPoints");
        if (parent != null)
        {
            foreach (Node child in parent.GetChildren())
            {
                if (child is CombatEnemySpawnMarker marker)
                {
                    TrySpawnEnemy(marker);
                }
            }
        }

        if (_testEnemies.Count > 0)
        {
            return;
        }

        CombatEnemySpawnMarker fallback = GetNodeOrFallback<CombatEnemySpawnMarker>(EnemySpawnerPath, "World/Entities/SpawnPoints/TimedEnemySpawn");
        if (fallback == null)
        {
            GD.PushError($"[CombatDebug] No enemy spawners found under '{EnemySpawnerParentPath}'.");
            return;
        }

        TrySpawnEnemy(fallback);
    }

    private void TrySpawnEnemy(CombatEnemySpawnMarker spawner)
    {
        if (spawner == null)
        {
            return;
        }

        if (spawner.EnemyScene == null)
        {
            spawner.EnemyScene = TestEnemyScene;
        }

        Node2D spawned = spawner.SpawnEnemy();
        if (spawned is CombatTestEnemyNode testEnemy)
        {
            _testEnemies.Add(testEnemy);
            return;
        }

        GD.PushWarning($"[CombatDebug] Spawned enemy from '{spawner.Name}' is not TestEnemyNode; debug H/K damage shortcuts are unavailable for it.");
    }

    private void ApplyDebugEnemyDamage(int requestedDamage)
    {
        CombatTestEnemyNode targetEnemy = FindDebugDamageTarget();
        if (_gameManager?.Combat == null || targetEnemy?.EnemyModel == null)
        {
            GD.PushWarning("[CombatDebug] Debug enemy damage skipped; test enemy is not ready.");
            return;
        }

        if (targetEnemy.EnemyModel is not CombatInventoryStats stats || stats.CurHP <= 0)
        {
            return;
        }

        int damage = requestedDamage == int.MaxValue
            ? stats.CurHP
            : Mathf.Clamp(requestedDamage, 1, stats.CurHP);

        _gameManager.Combat.DealDamage(targetEnemy.EnemyModel, damage, "Debug", _player);
        ethra.V1.CombatFeedbackBus.EmitHitResolved(_player, targetEnemy.EnemyModel, damage, false, "Debug", string.Empty);
        if (targetEnemy.EnemyModel is CombatInventoryStats updatedStats && updatedStats.CurHP <= 0)
        {
            _gameManager.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest($"{targetEnemy.Name} defeated.", NotificationType.Info));
        }
        GD.Print($"[CombatDebug] Applied {damage} debug damage to {targetEnemy.Name}. HP now {targetEnemy.CurrentHealth}.");
    }

    private void GrantDebugExperience()
    {
        if (_player == null)
        {
            GD.PushWarning("[ProgressionDebug] Cannot grant XP; player is not ready.");
            return;
        }

        int amount = Mathf.Max(1, DebugExperienceGrantAmount);
        GD.Print($"[ProgressionDebug] Granting {amount} XP.");
        _player.GainExperience(amount);
    }

    private void BuildComboDebugPanel()
    {
        Node parent = GetNodeOrFallback<Node>(DebugHudParentPath, "UI/Hud");
        if (parent == null)
        {
            GD.PushWarning("[ComboDebug] Debug HUD parent missing; combo status label was not created.");
            return;
        }

        if (parent.GetNodeOrNull("ComboDebugPanel") != null)
        {
            return;
        }

        PanelContainer panel = new()
        {
            Name = "ComboDebugPanel",
            OffsetLeft = 16f,
            OffsetTop = 16f,
            OffsetRight = 360f,
            OffsetBottom = 100f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        MarginContainer margin = new()
        {
            Name = "Margin"
        };
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        _comboDebugLabel = new Label
        {
            Name = "ComboDebugLabel",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        margin.AddChild(_comboDebugLabel);
        parent.AddChild(panel);
        RefreshComboDebugLabel();
    }

    private void BuildStatusTogglePanel()
    {
        Node parent = GetNodeOrFallback<Node>(DebugHudParentPath, "UI/Hud");
        if (parent == null)
        {
            GD.PushWarning("[StatusDebug] Debug HUD parent missing; status toggles were not created.");
            return;
        }

        if (parent.GetNodeOrNull("StatusEffectToggles") != null)
        {
            return;
        }

        PanelContainer panel = new()
        {
            Name = "StatusEffectToggles",
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
        if (packet?.Source != _player)
        {
            return;
        }

        _currentComboId = string.IsNullOrWhiteSpace(packet.ComboId) ? "<none>" : packet.ComboId;
        string chargedLabel = packet.IsCharged ? $" charged {packet.ChargeSeconds:0.00}s" : string.Empty;
        _currentComboStep = $"{packet.ComboPhase}: {(string.IsNullOrWhiteSpace(packet.ComboStepLabel) ? packet.AnimationName : packet.ComboStepLabel)}{chargedLabel}";
        _currentHitboxProfile = string.IsNullOrWhiteSpace(packet.HitboxProfile?.ProfileId) ? "<fallback>" : packet.HitboxProfile.ProfileId;
        RefreshComboDebugLabel();

        if (_enabledStatusEffectIds.Count > 0)
        {
            foreach (string statusId in _enabledStatusEffectIds)
            {
                if (!packet.AdditionalEffectIds.Contains(statusId))
                {
                    packet.AdditionalEffectIds.Add(statusId);
                }
            }

            GD.Print($"[StatusDebug] Player attack will apply: {string.Join(", ", packet.AdditionalEffectIds)}");
        }
    }

    private void OnComboStepStarted(ethra.V1.Player player, string comboId, int phase, string stepLabel, string hitboxProfileId)
    {
        if (player != _player)
        {
            return;
        }

        _currentComboId = string.IsNullOrWhiteSpace(comboId) ? "<none>" : comboId;
        _currentComboStep = $"{phase}: {(string.IsNullOrWhiteSpace(stepLabel) ? "<unnamed>" : stepLabel)}";
        _currentHitboxProfile = string.IsNullOrWhiteSpace(hitboxProfileId) ? "<fallback>" : hitboxProfileId;
        _currentWindowState = "startup/recovery";
        RefreshComboDebugLabel();
    }

    private void OnActiveWindowChanged(ethra.V1.Player player, bool isOpen, float elapsed, float duration)
    {
        if (player != _player)
        {
            return;
        }

        _currentWindowState = isOpen ? $"active {elapsed:0.00}/{duration:0.00}" : $"inactive {elapsed:0.00}/{duration:0.00}";
        RefreshComboDebugLabel();
    }

    private void OnBufferWindowChanged(ethra.V1.Player player, bool isOpen, float elapsed, float duration)
    {
        if (player != _player)
        {
            return;
        }

        _currentWindowState = isOpen ? $"combo window {elapsed:0.00}/{duration:0.00}" : $"window closed {elapsed:0.00}/{duration:0.00}";
        RefreshComboDebugLabel();
    }

    private void OnHitResolved(CombatEntity source, CombatEntity target, float damage, bool crit, string damageType, string element)
    {
        if (!ReferenceEquals(source, _player) || target == null)
        {
            return;
        }

        _lastHitTarget = $"{target.Name} ({damage:0})";
        RefreshComboDebugLabel();
    }

    private void RefreshComboDebugLabel()
    {
        if (_comboDebugLabel == null)
        {
            return;
        }

        _comboDebugLabel.Text =
            $"Weapon: {_currentWeaponName}\n"
            + $"Combo: {_currentComboId}\n"
            + $"Step: {_currentComboStep} | Window: {_currentWindowState}\n"
            + $"Hitbox: {_currentHitboxProfile} | Last hit: {_lastHitTarget}";
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

    private CombatTestEnemyNode FindDebugDamageTarget()
    {
        CombatTestEnemyNode best = null;
        float bestDistanceSquared = float.PositiveInfinity;
        Vector2 origin = GetTree()?.GetFirstNodeInGroup("Player") is Node2D playerNode
            ? playerNode.GlobalPosition
            : Vector2.Zero;

        foreach (CombatTestEnemyNode enemy in _testEnemies)
        {
            if (enemy == null || enemy.EnemyModel is not CombatInventoryStats stats || stats.CurHP <= 0)
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

    private static bool IsGameManagerReady(CombatGameManager gameManager)
    {
        return gameManager.Combat != null
            && gameManager.Entity != null
            && gameManager.GameState != null
            && gameManager.Scene != null
            && gameManager.Inventory != null;
    }

    private void EquipDebugWeapon(CombatGameManager gameManager)
    {
        if (gameManager.Inventory == null)
        {
            GD.PushWarning("[CombatDebug] Inventory manager is missing; debug weapon was not equipped.");
            return;
        }

        if (DebugWeaponItemId <= 0)
        {
            GD.PushWarning("[CombatDebug] DebugWeaponItemId is not configured; debug weapon was not equipped.");
            return;
        }

        gameManager.Inventory.AddItem(DebugWeaponItemId);
        gameManager.Inventory.UseItem(DebugWeaponItemId);
        _currentWeaponName = gameManager.DB?.GetItemFromRepo(DebugWeaponItemId)?.Name ?? DebugWeaponItemId.ToString();
        RefreshComboDebugLabel();
    }

    private void ConfigurePlayerStats(CombatInventoryStats stats)
    {
        if (stats == null)
        {
            return;
        }

        stats.MaxHP = Mathf.Max(1, DebugPlayerMaxHealth);
        stats.CurHP = stats.MaxHP;
        stats.MaxMana = Mathf.Max(0, DebugPlayerMaxMana);
        stats.CurMana = stats.MaxMana;
        stats.Strength = DebugPlayerStrength;
        stats.Dexterity = DebugPlayerDexterity;
        stats.Intelligence = DebugPlayerIntelligence;
        stats.Spirit = DebugPlayerSpirit;
        stats.Vitality = DebugPlayerVitality;
        stats.Luck = DebugPlayerLuck;
    }

    private static bool IsEmptyNodePath(NodePath path)
    {
        return path == null || string.IsNullOrWhiteSpace(path.ToString());
    }
}
