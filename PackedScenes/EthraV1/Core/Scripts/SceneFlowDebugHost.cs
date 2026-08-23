using Godot;
using DebugGameManager = ethra.V1.GameManager;
using DebugInventoryStats = ethra.V1.IStats;
using DebugPlayer = ethra.V1.Player;

public partial class SceneFlowDebugHost : Node2D
{
    private int _initializeAttempts;

    [Export] public string InitialSceneKey { get; set; } = "SceneFlow_A";
    [Export] public string InitialSpawnName { get; set; } = "NewGameSpawn";

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeSceneFlowDebug));
    }

    private void InitializeSceneFlowDebug()
    {
        DebugGameManager gameManager = DebugGameManager.Instance;
        if (gameManager == null || !IsGameManagerReady(gameManager))
        {
            RetryOrFail("[SceneFlowDebug] GameManager managers did not initialize.");
            return;
        }

        DebugPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);
        gameManager.InitializeCurrentSceneUi();

        gameManager.Scene.GoToScene(InitialSceneKey);
        gameManager.CallDeferred(nameof(DebugGameManager.SpawnPlayerAtMarker), InitialSpawnName);
    }

    private void RetryOrFail(string message)
    {
        _initializeAttempts++;
        if (_initializeAttempts > 30)
        {
            GD.PushError(message);
            return;
        }

        CallDeferred(nameof(InitializeSceneFlowDebug));
    }

    private static bool IsGameManagerReady(DebugGameManager gameManager)
    {
        return gameManager.Combat != null
            && gameManager.Entity != null
            && gameManager.GameState != null
            && gameManager.Scene != null
            && gameManager.Inventory != null;
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
}
