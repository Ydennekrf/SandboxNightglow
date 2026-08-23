using Godot;
using DebugGameManager = ethra.V1.GameManager;
using DebugInventoryStats = ethra.V1.IStats;
using DebugPlayer = ethra.V1.Player;

/// <summary>
/// Debug-only harness for testing map layer states, triggers, collision swaps, and file-backed debug logs.
/// </summary>
/// <remarks>
/// The reusable map-layer behavior lives in Core/World/MapLayers. This root only spawns a debug player
/// and clears/logs the manual test session.
/// </remarks>
public partial class MapLayerDebugSceneRoot : Node2D
{
    [Export] public PackedScene PlayerScene { get; set; }
    [Export] public NodePath GameManagerPath { get; set; } = "GameManager";
    [Export] public NodePath WorldPath { get; set; } = "World";
    [Export] public string InitialSpawnName { get; set; } = "NewGameSpawn";
    [Export] public bool ClearDebugLogOnReady { get; set; } = true;
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
        if (ClearDebugLogOnReady)
        {
            ethra.V1.DebugLog.Clear();
        }

        ethra.V1.DebugLog.Info("MapLayer", $"MapLayerDebugSceneRoot ready. log='{ethra.V1.DebugLog.ResolvedLogFilePath}'", this);
        GD.Print($"[MapLayerDebug] File log: {ethra.V1.DebugLog.ResolvedLogFilePath}");
        CallDeferred(nameof(InitializeManualDebugScene));
    }

    private void InitializeManualDebugScene()
    {
        DebugGameManager gameManager = DebugGameManager.Instance ?? GetNodeOrFallback<DebugGameManager>(GameManagerPath, "GameManager");
        WorldSceneRoot world = GetNodeOrFallback<WorldSceneRoot>(WorldPath, "World");

        if (gameManager == null)
        {
            GD.PushError("[MapLayerDebug] GameManager is missing.");
            return;
        }

        if (world == null)
        {
            GD.PushError("[MapLayerDebug] WorldSceneRoot is missing.");
            return;
        }

        DebugPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);
        gameManager.InitializeCurrentSceneUi();

        SpawnPlayer(gameManager, world, player);
        ethra.V1.DebugLog.Info("MapLayer", $"Spawned debug player at spawn='{InitialSpawnName}'.", this);

        GD.Print("[MapLayerDebug] Map layer helper debug scene ready.");
        GD.Print("[MapLayerDebug] Walk into the blue house trigger to fade the roof.");
        GD.Print("[MapLayerDebug] Walk into the yellow trigger to use the bridge top route.");
        GD.Print("[MapLayerDebug] Walk into the purple trigger to use the under-bridge route.");
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
            GD.PushError("[MapLayerDebug] Player scene is missing.");
            return;
        }

        Marker2D spawn = world.GetSpawn(string.IsNullOrWhiteSpace(InitialSpawnName) ? "NewGameSpawn" : InitialSpawnName);
        Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
        gameManager.Scene.SpawnPlayerNode(playerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);
    }

    private void ConfigurePlayerStats(DebugInventoryStats stats)
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
