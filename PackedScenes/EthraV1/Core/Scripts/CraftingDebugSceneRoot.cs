using Godot;
using DebugGameManager = ethra.V1.GameManager;
using DebugInventoryStats = ethra.V1.IStats;
using DebugPlayer = ethra.V1.Player;

/// <summary>
/// Debug-only harness that spawns a player and seeds materials for manual crafting UI checks.
/// </summary>
/// <remarks>
/// Crafting rules and inventory transactions belong in CraftingManager and InventoryManager; this root only
/// prepares an isolated scene for testing them.
/// </remarks>
public partial class CraftingDebugSceneRoot : Node2D
{
	[Export] public PackedScene PlayerScene { get; set; }
	[Export] public NodePath WorldPath { get; set; } = "World";
	[Export] public string InitialSpawnName { get; set; } = "NewGameSpawn";
	[Export] public int SeedMaterialItemId { get; set; } = 3001;
	[Export] public int SeedMaterialQuantity { get; set; } = 5;
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
		CallDeferred(nameof(InitializeManualDebugScene));
	}

	private void InitializeManualDebugScene()
	{
		DebugGameManager gameManager = DebugGameManager.Instance;
		WorldSceneRoot world = GetNodeOrNull<WorldSceneRoot>(WorldPath);

		if (gameManager == null)
		{
			GD.PushError("[CraftingDebug] GameManager is missing.");
			return;
		}

		if (world == null)
		{
			GD.PushError("[CraftingDebug] WorldSceneRoot is missing.");
			return;
		}

		DebugPlayer player = gameManager.GetPlayer() ?? gameManager.CreatePlayerModel();
		ConfigurePlayerStats(player);
		gameManager.SetPlayer(player);
		gameManager.InitializeCurrentSceneUi();

		SpawnPlayer(gameManager, world, player);
		SeedInventory(gameManager);

		GD.Print("[CraftingDebug] Crafting debug scene ready.");
	}

	private void SpawnPlayer(DebugGameManager gameManager, WorldSceneRoot world, DebugPlayer player)
	{
		PackedScene playerScene = PlayerScene ?? gameManager.PlayerScene;
		if (playerScene == null)
		{
			GD.PushError("[CraftingDebug] Player scene is missing.");
			return;
		}

		Marker2D spawn = world.GetSpawn(string.IsNullOrWhiteSpace(InitialSpawnName) ? "NewGameSpawn" : InitialSpawnName);
		Vector2 spawnPosition = spawn?.GlobalPosition ?? Vector2.Zero;
		gameManager.Scene.SpawnPlayerNode(playerScene, spawnPosition, player, world.GetPlayerContainer() ?? world.Entities);
	}

	private void SeedInventory(DebugGameManager gameManager)
	{
		if (gameManager.Inventory == null || SeedMaterialItemId <= 0 || SeedMaterialQuantity <= 0)
		{
			return;
		}

		int currentCount = gameManager.Inventory.GetItemCount(SeedMaterialItemId);
		int missing = SeedMaterialQuantity - currentCount;
		if (missing <= 0)
		{
			GD.Print($"[CraftingDebug] Inventory already has item {SeedMaterialItemId} x{currentCount}.");
			return;
		}

		if (gameManager.Inventory.AddItemQuantity(SeedMaterialItemId, missing))
		{
			GD.Print($"[CraftingDebug] Seeded item {SeedMaterialItemId} x{missing}.");
		}
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
}
