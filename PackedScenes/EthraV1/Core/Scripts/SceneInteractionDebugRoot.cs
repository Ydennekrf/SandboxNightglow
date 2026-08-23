using Godot;
using DebugGameManager = ethra.V1.GameManager;
using DebugInventoryStats = ethra.V1.IStats;
using DebugPlayer = ethra.V1.Player;

public partial class SceneInteractionDebugRoot : Node2D
{
    private int _initializeAttempts;

    [Export] public PackedScene PlayerScene { get; set; }
    [Export] public NodePath GameManagerPath { get; set; } = "GameManager";
    [Export] public NodePath WorldPath { get; set; } = "World";
    [Export] public string SpawnName { get; set; } = "NewGameSpawn";
    [Export] public string ReadyMessage { get; set; } = string.Empty;

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeManualDebugScene));
    }

    private void InitializeManualDebugScene()
    {
        DebugGameManager gameManager = DebugGameManager.Instance ?? GetNodeOrFallback<DebugGameManager>(GameManagerPath, "GameManager");
        WorldSceneRoot world = GetNodeOrFallback<WorldSceneRoot>(WorldPath, "World");

        if (gameManager == null)
        {
            RetryOrFail("[SceneInteractionDebug] GameManager is missing.");
            return;
        }

        if (!IsGameManagerReady(gameManager))
        {
            RetryOrFail("[SceneInteractionDebug] GameManager managers did not initialize.");
            return;
        }

        if (world == null)
        {
            GD.PushError("[SceneInteractionDebug] WorldSceneRoot is missing.");
            return;
        }

        DebugPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);
        gameManager.InitializeCurrentSceneUi();

        SpawnPlayer(gameManager, world, player);

        if (!string.IsNullOrWhiteSpace(ReadyMessage))
        {
            GD.Print(ReadyMessage);
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

    private void RetryOrFail(string failureMessage)
    {
        _initializeAttempts++;
        if (_initializeAttempts > 30)
        {
            GD.PushError(failureMessage);
            return;
        }

        CallDeferred(nameof(InitializeManualDebugScene));
    }

    private void SpawnPlayer(DebugGameManager gameManager, WorldSceneRoot world, DebugPlayer player)
    {
        PackedScene playerScene = PlayerScene ?? gameManager.PlayerScene;
        if (playerScene == null)
        {
            GD.PushError("[SceneInteractionDebug] Player scene is missing.");
            return;
        }

        Marker2D spawn = world.GetSpawn(SpawnName);
        Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
        gameManager.Scene.SpawnPlayerNode(playerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);
    }

    private static void ConfigurePlayerStats(DebugInventoryStats stats)
    {
        if (stats == null)
        {
            return;
        }

        stats.MaxHP = 100;
        stats.CurHP = 100;
        stats.MaxMana = 100;
        stats.CurMana = 100;
        stats.Strength = 10;
        stats.Dexterity = 10;
        stats.Intelligence = 10;
        stats.Spirit = 10;
        stats.Vitality = 10;
        stats.Luck = 10;
    }

    private static bool IsGameManagerReady(DebugGameManager gameManager)
    {
        return gameManager.Combat != null
            && gameManager.Entity != null
            && gameManager.GameState != null
            && gameManager.Scene != null
            && gameManager.Inventory != null;
    }

    private static bool IsEmptyNodePath(NodePath path)
    {
        return path == null || string.IsNullOrWhiteSpace(path.ToString());
    }
}
