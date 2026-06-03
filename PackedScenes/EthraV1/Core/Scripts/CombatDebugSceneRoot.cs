using Godot;
using CombatGameManager = ethra.V1.GameManager;
using CombatInventoryStats = ethra.V1.IStats;
using CombatPlayer = ethra.V1.Player;
using CombatTestEnemyNode = ethra.V1.TestEnemyNode;

public partial class CombatDebugSceneRoot : Node2D
{
    private const int DebugWeaponId = 2001;

    [Export] public PackedScene PlayerScene { get; set; }
    [Export] public PackedScene TestEnemyScene { get; set; }
    [Export] public NodePath GameManagerPath { get; set; } = "GameManager";
    [Export] public NodePath WorldPath { get; set; } = "World";

    public override void _Ready()
    {
        CallDeferred(nameof(InitializeManualDebugScene));
    }

    private void InitializeManualDebugScene()
    {
        CombatGameManager gameManager = GetNodeOrNull<CombatGameManager>(GameManagerPath) ?? CombatGameManager.Instance;
        WorldSceneRoot world = GetNodeOrNull<WorldSceneRoot>(WorldPath);

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

        CombatPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
        ConfigurePlayerStats(player);
        gameManager.SetPlayer(player);

        SpawnPlayer(gameManager, world, player);
        EquipDebugWeapon(gameManager);

        CombatTestEnemyNode testEnemy = SpawnTestEnemy(world);

        GD.Print("[CombatDebug] Manual combat debug scene ready.");
        if (testEnemy != null)
        {
            GD.Print($"[CombatDebug] TestEnemy HP: {testEnemy.CurrentHealth}");
        }
    }

    private void SpawnPlayer(CombatGameManager gameManager, WorldSceneRoot world, CombatPlayer player)
    {
        PackedScene playerScene = PlayerScene ?? gameManager.PlayerScene;
        if (playerScene == null)
        {
            GD.PushError("[CombatDebug] Player scene is missing.");
            return;
        }

        Marker2D spawn = world.GetSpawn("NewGameSpawn");
        Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
        gameManager.Scene.SpawnPlayerNode(playerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);
    }

    private CombatTestEnemyNode SpawnTestEnemy(WorldSceneRoot world)
    {
        Node parent = world.Entities.GetNodeOrNull<Node>("Enemies") ?? world.Entities;
        if (TestEnemyScene == null)
        {
            GD.PushError("[CombatDebug] TestEnemy scene is missing.");
            return null;
        }

        CombatTestEnemyNode enemy = TestEnemyScene.Instantiate<CombatTestEnemyNode>();
        enemy.Name = "TestEnemy";
        enemy.GlobalPosition = new Vector2(72f, 0f);
        parent.AddChild(enemy);
        return enemy;
    }

    private static void EquipDebugWeapon(CombatGameManager gameManager)
    {
        if (gameManager.Inventory == null)
        {
            GD.PushWarning("[CombatDebug] Inventory manager is missing; debug weapon was not equipped.");
            return;
        }

        gameManager.Inventory.AddItem(DebugWeaponId);
        gameManager.Inventory.UseItem(DebugWeaponId);
    }

    private static void ConfigurePlayerStats(CombatInventoryStats stats)
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
