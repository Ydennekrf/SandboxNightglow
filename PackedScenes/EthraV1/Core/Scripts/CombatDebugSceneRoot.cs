using Godot;
using CombatEnemy = ethra.V1.Enemy;
using CombatGameManager = ethra.V1.GameManager;
using CombatPlayer = ethra.V1.Player;
using CombatStateMachine = ethra.V1.StateMachine;
using CombatStats = ethra.V1.IStats;

public partial class CombatDebugSceneRoot : Node2D
{
	private const int DebugWeaponId = 2001;

	[Export] public PackedScene TrainingDummyScene { get; set; }
	[Export] public NodePath GameManagerPath { get; set; } = "GameManager";
	[Export] public NodePath WorldPath { get; set; } = "World";

	public override void _Ready()
	{
		CallDeferred(nameof(InitializeDebugScene));
	}

	private void InitializeDebugScene()
	{
		CombatGameManager gameManager = GetNodeOrNull<CombatGameManager>(GameManagerPath) ?? CombatGameManager.Instance;
		WorldSceneRoot world = GetNodeOrNull<WorldSceneRoot>(WorldPath);

		if (gameManager == null)
		{
			GD.PushError("CombatDebugSceneRoot: GameManager is missing.");
			return;
		}

		if (world == null)
		{
			GD.PushError("CombatDebugSceneRoot: WorldSceneRoot is missing.");
			return;
		}

		CombatPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
		ConfigureStats(player);
		gameManager.SetPlayer(player);

		Marker2D spawn = world.GetSpawn("NewGameSpawn");
		Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
		gameManager.Scene.SpawnPlayerNode(gameManager.PlayerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);

		gameManager.Inventory.AddItem(DebugWeaponId);
		gameManager.Inventory.UseItem(DebugWeaponId);

		CombatEnemy target = gameManager.CreateEnemy("DebugTrainingTarget", 1, gameManager, gameManager, new CombatStateMachine());
		ConfigureStats(target);
		target.Facing = ethra.V1.FacingDirection.Left;

		if (!gameManager.registeredEnemies.Contains(target))
		{
			gameManager.registeredEnemies.Add(target);
		}

		SpawnTrainingDummyVisual(world);
		GD.Print("CombatDebugSceneRoot: combat debug scene initialized.");
	}

	private static void ConfigureStats(CombatStats stats)
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

	private void SpawnTrainingDummyVisual(WorldSceneRoot world)
	{
		if (TrainingDummyScene == null)
		{
			GD.PushWarning("CombatDebugSceneRoot: TrainingDummyScene is not set.");
			return;
		}

		Node container = world.GetEnemyByName("DebugTrainingDummy") ?? world.Entities.GetNodeOrNull<Node>("Enemies") ?? world.Entities;
		Node2D dummy = TrainingDummyScene.Instantiate<Node2D>();
		dummy.Name = "DebugTrainingDummy";
		dummy.GlobalPosition = new Vector2(96, 0);
		container.AddChild(dummy);
	}
}
