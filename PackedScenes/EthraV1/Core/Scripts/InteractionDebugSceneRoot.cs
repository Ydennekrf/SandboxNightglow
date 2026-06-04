using Godot;
using DebugGameManager = ethra.V1.GameManager;
using DebugInventoryStats = ethra.V1.IStats;
using DebugPlayer = ethra.V1.Player;

public partial class InteractionDebugSceneRoot : Node2D
{
    [Export] public PackedScene PlayerScene { get; set; }
    [Export] public NodePath GameManagerPath { get; set; } = "GameManager";
    [Export] public NodePath WorldPath { get; set; } = "World";

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeManualDebugScene));
    }

    private void InitializeManualDebugScene()
    {
        DebugGameManager gameManager = DebugGameManager.Instance ?? GetNodeOrNull<DebugGameManager>(GameManagerPath);
        WorldSceneRoot world = GetNodeOrNull<WorldSceneRoot>(WorldPath);

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

        DebugPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);
        gameManager.InitializeCurrentSceneUi();

        SpawnPlayer(gameManager, world, player);

        GD.Print("[InteractionDebug] Manual interaction debug scene ready.");
    }

    private void SpawnPlayer(DebugGameManager gameManager, WorldSceneRoot world, DebugPlayer player)
    {
        PackedScene playerScene = PlayerScene ?? gameManager.PlayerScene;
        if (playerScene == null)
        {
            GD.PushError("[InteractionDebug] Player scene is missing.");
            return;
        }

        Marker2D spawn = world.GetSpawn("NewGameSpawn");
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
}
