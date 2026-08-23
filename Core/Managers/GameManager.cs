using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ethra.V1
{
	/// <summary>
	/// Autoload orchestration root for the Ethra V1 runtime.
	/// </summary>
	/// <remarks>
	/// GameManager wires managers together, loads repository data, coordinates scene/player startup,
	/// and forwards legacy interface calls. Domain rules should stay in dedicated managers such as
	/// CombatManager, InventoryManager, QuestManager, CraftingManager, and GameStateManager.
	/// </remarks>
	public partial class GameManager : Node, IGameManager, ISaveRegistry, IGameStateManager, IResolveable, IEvent , IEntityManager, ICombat, IInventory
	{

	//======= fields and properties ==============//

	
	// ===== managers
	#region Manager refs
	
	private CombatManager _combat;
	private AudioManager _audio;
	private DialogManager _dialog;
	private EntityManager _entity;
	private EventManager _event;
	private GameStateManager _gameState;
	private InventoryManager _inventory;
	private CraftingManager _crafting;
	private WeaponUpgradeManager _weaponUpgrades;
	private QuestManager _quest;
	private SceneManager _scene;
	private UIManager _ui;
	private MasterRepository _db;
	private ISaveLoadService _saves;
	private int _resolveOrder;


	public static GameManager Instance { get; private set; }
	/// <summary>Owns damage, hit resolution, status effects, and combat payload delivery.</summary>
	public CombatManager Combat { get { return _combat; } }
	/// <summary>Owns stable-ID sound playback, music, ambience, and audio categories.</summary>
	public AudioManager Audio { get { return _audio; } }
	/// <summary>Owns active dialog tree playback and dialog panel coordination.</summary>
	public DialogManager Dialog { get { return _dialog; } }
	/// <summary>Owns runtime entity models and entity save snapshots.</summary>
	public EntityManager Entity { get { return _entity; } }
	/// <summary>Central event bus used by managers, UI, debug scenes, and gameplay objects.</summary>
	public EventManager Events { get { return _event; } }
	/// <summary>Persistent world/player state manager for saves and gameplay flags.</summary>
	public GameStateManager GameState { get { return _gameState; } }
	/// <summary>Owns item stacks, equipment state, and weapon instance state.</summary>
	public InventoryManager Inventory { get { return _inventory; } }
	/// <summary>Owns recipe lookup and crafting transactions.</summary>
	public CraftingManager Crafting { get { return _crafting; } }
	/// <summary>Owns rune socketing rules for weapon instances.</summary>
	public WeaponUpgradeManager WeaponUpgrades { get { return _weaponUpgrades; } }
	/// <summary>Owns quest runtime state and quest event subscriptions.</summary>
	public QuestManager Quest { get { return _quest; } }
	/// <summary>Owns world scene transitions and player node spawning.</summary>
	public SceneManager Scene { get { return _scene; } }
	/// <summary>Owns HUD/menu panel coordination for the current scene's UIRoot.</summary>
	public UIManager UI { get { return _ui; } }
	/// <summary>Repository of loaded scenes, static item data, dialog trees, quests, and ability paths.</summary>
	public MasterRepository DB { get { return _db; } }
	#endregion


		// ==== Game Manager specific fields =====//

	private int _saveslot;
	private const int StarterWeaponItemId = 2001;
	private const bool DebugValidateItemStatusEffectsOnStartup = true; // Temporary: remove after manual item status testing.
	private SaveGame _pendingLoadedSave;
	private bool _debugSaveKeyHeld;
	private bool _returnToMenuKeyHeld;

	private List<IResolveable> _resolveList;
	public int ResolveOrder => _resolveOrder;
	public int ActiveSaveSlot => _saveslot;
	
	//====SaveRegistry=====//

	#region SaveRegistry
	private List<ISaveable> _list;
	public IReadOnlyList<ISaveable> All => _list;

	public List<Player> registeredPlayers { get {return _entity.registeredPlayers;} set{_entity.registeredPlayers = value;} }
	public List<Enemy> registeredEnemies { get => _entity.registeredEnemies; set => _entity.registeredEnemies = value; }
	public List<NPC> registeredNPCs { get => _entity.registeredNPCs; set => _entity.registeredNPCs = value; }
	#endregion

	//============ EXPORTS ===============//
	#region Exports

	[ExportGroup("Scene folder Path")]
	/// <summary>Root folder scanned for loadable world/debug scenes by file name key.</summary>
	[Export] public string SceneFolderLocation;
	/// <summary>Scene repository key loaded when starting a new game.</summary>
	[Export] public string NewGameSceneKey { get; set; } = "PhosphorForest";
	/// <summary>Spawn marker used when starting a new game.</summary>
	[Export] public string NewGameSpawnName { get; set; } = "NewGameSpawn";

	[ExportGroup("Repository Data Sources")]

	[ExportSubgroup("Dialog Data")]
	/// <summary>Folder containing dialog tree JSON files loaded into MasterRepository.</summary>
	[Export] public string DialogTreeDataFolderPath = "res://Core/Dialog/Data";
	[Export] public string DialogCsvPath = string.Empty;
	[ExportSubgroup("Quest Data")]
	/// <summary>Folder containing quest definition JSON files loaded into MasterRepository.</summary>
	[Export] public string QuestDefinitionDataFolderPath = "res://Core/Quest/Data";
	[ExportSubgroup("Ability Path Data")]
	/// <summary>Folder containing ability path JSON definitions used by player progression.</summary>
	[Export] public string AbilityPathDefinitionDataFolderPath = "res://Core/Progression/Data";
	/// <summary>Stable ability path ID applied to newly created players and used as save fallback.</summary>
	[Export] public string PlayerAbilityPathId = "abilitypath.player.debug";
	[ExportSubgroup("Item CSV")]
	/// <summary>CSV source for static item definitions, including weapons, runes, crafting items, and consumables.</summary>
	[Export] public string ItemCsvPath = "res://Core/Inventory/Data/items_seed.csv";
	[ExportSubgroup("Item Effects CSV")]
	/// <summary>CSV source for item effect rows attached after item definitions load.</summary>
	[Export] public string ItemEffectsCsvPath = "res://Core/Inventory/Data/item_effects_seed.csv";
	[ExportSubgroup("Crafting Data")]
	/// <summary>Folder containing CraftingRecipe resources loaded by CraftingManager.</summary>
	[Export] public string CraftingRecipeDataFolderPath = "res://Core/Crafting/Data";
	[ExportGroup("Player Exports")]
		[ExportSubgroup("StateMachine")]

			/// <summary>Base movement speed copied into the player model/state machine setup.</summary>
			[Export] public float MoveSpeed;

		[ExportSubgroup("Player Inventory")]
			[Export] public string[] InitialItemList;
		[ExportSubgroup("Player Statgain Config")]
			[Export] public double hpGain;
			[Export] public double mpGain;
			[Export] public double strGain;
			[Export] public double dexGain;
			[Export] public double intGain;
			[Export] public double spiGain;
			[Export] public double vitGain;
			[Export] public double lukGain;

			[Export] public double expGain;

			[ExportSubgroup("Player Base Stats")]

			[Export] public double hpBase;
			[Export] public double mpBase;
			[Export] public double strBase;
			[Export] public double dexBase;
			[Export] public double intBase;
			[Export] public double spiBase;
			[Export] public double vitBase;
			[Export] public double lukBase;
			[ExportSubgroup("Player Scene")]
			[Export] public PackedScene PlayerScene { get; set; }
			[ExportSubgroup("Player Sprites")]
			#region Sprites

			[Export] public Texture2D WepUpDraw;
			[Export] public Texture2D WepDownDraw;
			[Export] public Texture2D WepUpStow;
			[Export] public Texture2D WepDownStow;
			[Export] public Texture2D Hair;
			[Export] public Texture2D Clothes;
			[Export] public Texture2D Body;
			[Export] public Texture2D Notify;
			[Export] public Texture2D PlayerOverlay;
			[Export] public Texture2D OneHandBody;
			[Export] public Texture2D TwoHandBody;
			[Export] public Texture2D BowBody;
			[Export] public Texture2D HarvestPickaxeSprite;
			[Export] public Texture2D HarvestAxeSprite;

			#endregion

	[ExportGroup("Game State Exports")]
		[ExportSubgroup("world time config")]
			[Export] public float tickPerMinute;
		
	
	

	#endregion

	#region Godot methods

		/// <summary>
		/// Establishes the singleton autoload instance and builds manager dependencies.
		/// </summary>
		public override void _Ready()
		{
			if (Instance != null && Instance != this)
			{
				GD.PushWarning($"Duplicate GameManager detected at '{GetPath()}'. Disabling duplicate instance; active instance is '{Instance.GetPath()}'.");
				SetProcess(false);
				QueueFree();
				return;
			}

			Instance = this;
			Initialize();
		}



		/// <summary>
		/// Runs manager resolve ticks and debug save/load shortcuts after initialization.
		/// </summary>
	public override void _Process(double delta)
		{
			if (_resolveList == null)
			{
				return;
			}

			Resolve();
			Resolve(delta);
			HandleDebugSaveLoadInput();
		}

		public override void _ExitTree()
		{
			base._ExitTree();

			_quest?.Shutdown();
			unregisterManagers();
			if (Instance == this)
			{
				Instance = null;
			}
		}

	#endregion

	//========== Game Manager Interface methods ===================//
		/// <summary>
		/// Constructs managers, registers save/resolve participants, loads static repositories, and defers UI binding.
		/// </summary>
		public void Initialize()
		{

			_list = new List<ISaveable>();
			_resolveList = new List<IResolveable>();

			// when we initialize all the manager classes check if they are ISavable if so add them to the registry on save
			_combat = new CombatManager();
			_audio = GetNodeOrNull<AudioManager>("AudioManager");
			if (_audio == null)
			{
				GD.PushWarning("GameManager: AudioManager child node is missing. Creating a runtime fallback with unassigned audio exports.");
				_audio = new AudioManager { Name = "AudioManager" };
				AddChild(_audio);
			}
			_dialog = new DialogManager();
			_entity = new EntityManager();
			_event = new EventManager();
			_gameState = new GameStateManager();
			
			_scene = new SceneManager();
			_ui = new UIManager();
			_db = new MasterRepository();
			_saves = new SaveLoadService(this);
			_inventory = new InventoryManager(_db);
			_crafting = new CraftingManager(_inventory, _db);
			_weaponUpgrades = new WeaponUpgradeManager(_inventory, _db);
			_quest = new QuestManager(_db);

			_audio.Initialize();
			_scene.Initialize(this, _gameState, _db);

			registerManager(_combat);
			registerManager(_dialog);
			registerManager(_entity);
			registerManager(_event);
			registerManager(_gameState);
			registerManager(_inventory);
			registerManager(_quest);
			registerManager(_scene);
			registerManager(_ui);

			GetAllItems();
			GetAllItemEffects();
			if (DebugValidateItemStatusEffectsOnStartup)
			{
				DB.DebugValidateItemStatusEffects();
			}
			GetAllCraftingRecipes();
			GetAllDialog();
			GetAllQuests();
			GetAllAbilityPaths();
			GetAllScenes();
			_quest.Initialize();

			CallDeferred(nameof(InitializeCurrentSceneUi));

		}

		/// <summary>
		/// Binds UIManager to the current scene's UIRoot after the scene tree has settled.
		/// </summary>
		public void InitializeCurrentSceneUi()
		{
			var currentScene = GetTree()?.CurrentScene;
			if (currentScene == null)
			{
				GD.PushWarning("InitializeCurrentSceneUi: CurrentScene is null. UI initialization deferred.");
				CallDeferred(nameof(InitializeCurrentSceneUi));
				return;
			}

			var uiRoot = currentScene.GetNodeOrNull<UIRoot>("UI");
			if (uiRoot == null)
			{
				GD.PushWarning($"InitializeCurrentSceneUi: UIRoot not found at path 'UI' under scene '{currentScene.Name}'.");
				return;
			}

			_ui.Initialize(uiRoot);
		}



	#region Fill Master Repo
	/// <summary>
	/// Loads static item definitions from the configured item CSV into MasterRepository.
	/// </summary>
	public void GetAllItems()
		{
		if (string.IsNullOrWhiteSpace(ItemCsvPath))
		{
			GD.PushWarning("GetAllItems: ItemCsvPath is empty. Skipping item csv load.");
			return;
		}

				DB.FillCsvRepo(ItemCsvPath, MasterRepository.RepoLoadType.Items, new[]
						{
							"id", "name", "category", "description", "rarity", "sell_value", "subtype", "max_stack", "icon_path", "weapon_up_draw_path", "weapon_down_draw_path", "weapon_up_stow_path", "weapon_down_stow_path", "combo_profile_path"
						});
					}

		/// <summary>
		/// Loads item effect rows and attaches them to already-loaded inventory items.
		/// </summary>
		public void GetAllItemEffects()
		{
			if (string.IsNullOrWhiteSpace(ItemEffectsCsvPath))
			{
				GD.PushWarning("GetAllItemEffects: ItemEffectsCsvPath is empty. Skipping item effects csv load.");
				return;
			}

			DB.FillCsvRepo(ItemEffectsCsvPath, MasterRepository.RepoLoadType.ItemEffects, new[]
				{
					"item_id", "effect_type", "effect_stat", "effect_power"
				});
		}

		/// <summary>
		/// Loads authored crafting recipes through CraftingManager.
		/// </summary>
		public void GetAllCraftingRecipes()
		{
			_crafting?.LoadRecipes(CraftingRecipeDataFolderPath);
		}

	/// <summary>
	/// Scans the configured scene folder and registers scenes by file name key.
	/// </summary>
	public void GetAllScenes()
		{
		DB.FillSceneRepo(SceneFolderLocation);
		}

		/// <summary>
		/// Loads dialog tree definitions used by DialogManager.
		/// </summary>
		public void GetAllDialog()
		{
			if (string.IsNullOrWhiteSpace(DialogTreeDataFolderPath))
			{
				GD.PushWarning("GetAllDialog: DialogTreeDataFolderPath is empty. Skipping dialog tree load.");
				return;
			}

			DB.FillDialogTreeRepo(DialogTreeDataFolderPath);
		}

		/// <summary>
		/// Loads quest definitions used by QuestManager and the quest log UI.
		/// </summary>
		public void GetAllQuests()
		{
			if (string.IsNullOrWhiteSpace(QuestDefinitionDataFolderPath))
			{
				GD.PushWarning("GetAllQuests: QuestDefinitionDataFolderPath is empty. Skipping quest definition load.");
				return;
			}

			DB.FillQuestDefinitionRepo(QuestDefinitionDataFolderPath);
		}

		/// <summary>
		/// Loads ability path definitions used by player progression.
		/// </summary>
		public void GetAllAbilityPaths()
		{
			if (string.IsNullOrWhiteSpace(AbilityPathDefinitionDataFolderPath))
			{
				GD.PushWarning("GetAllAbilityPaths: AbilityPathDefinitionDataFolderPath is empty. Skipping ability path definition load.");
				return;
			}

			DB.FillAbilityPathDefinitionRepo(AbilityPathDefinitionDataFolderPath);
		}
		#endregion

	#region Save load workflow
		/// <summary>
		/// Loads a save slot selected by UI or debug input, then records it as the active slot.
		/// </summary>
		/// <param name="id">Save slot number in the configured save range.</param>
	public async void LoadSavedGame(int id)
		{
			await _saves.LoadGameAsync(id);
			_saveslot = id;
		}
	
	/// <summary>
	/// Saves the current run into the active save slot when one has been selected.
	/// </summary>
	public async void SaveCurrentGame()
		{
			if (_saveslot <= 0)
			{
				GD.Print("[SaveLoad] No active save slot. Start a new game from a slot or load an existing save first.");
				return;
			}

			await SaveGameSlotAsync(_saveslot);
		}

	public async void SaveGameSlot(int slot)
		{
			await SaveGameSlotAsync(slot);
		}

	public IReadOnlyList<SaveSlotInfo> GetSaveSlotInfos()
		{
			return _saves.GetSaveSlotInfos();
		}

	public void RestoreLoadedSave(SaveGame save)
		{
			if (save == null)
			{
				GD.PushError("[SaveLoad] Cannot restore null save.");
				return;
			}

			_saveslot = save.SlotNumber;
			_pendingLoadedSave = save;

			Player player = CreatePlayerModel();
			ApplyPlayerSnapshot(player, save.Player?.Player);
			_gameState.RestoreSnapshot(save.GameState);
			_gameState.SetPlayer(player);

			PlayerSnapshot playerSnapshot = save.Player?.Player ?? new PlayerSnapshot();
			string sceneId = !string.IsNullOrWhiteSpace(playerSnapshot.SceneId)
				? playerSnapshot.SceneId
				: _gameState.CurrentSceneId;
			sceneId = string.IsNullOrWhiteSpace(sceneId) ? ResolveDefaultSceneKey() : sceneId;

			try
			{
				_scene.GoToScene(sceneId);
			}
			catch (Exception ex)
			{
				GD.PushError($"[SaveLoad] Failed to load saved scene '{sceneId}': {ex.Message}");
				_pendingLoadedSave = null;
				return;
			}

			CallDeferred(nameof(SpawnLoadedPlayerAfterSceneLoad));
			_ui.ShowOnlyHud();
			GD.Print($"[SaveLoad] Loaded scene: {sceneId}");
		}

	/// <summary>
	/// Starts a new run using the default slot and player name.
	/// </summary>
	public void StartNewGame()
	{
		StartNewGameInSlot(1, "Player");
	}

	/// <summary>
	/// Creates a fresh player model, sets initial persistent state, loads the first world scene, and defers node spawning.
	/// </summary>
	public void StartNewGameInSlot(int slot, string playerName)
	{
		if (slot < 1 || slot > SaveLoadService.MaxSaveSlots)
		{
			GD.PushError($"[SaveLoad] Cannot start new game in slot {slot}: valid slots are 1-{SaveLoadService.MaxSaveSlots}.");
			return;
		}

		_saveslot = slot;

	Player player = CreatePlayerModel();

	string safePlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
	player.SetName(safePlayerName);
	_gameState.SetPlayerName(safePlayerName);

	_gameState.SetPlayer(player);

	_scene.GoToScene(ResolveDefaultSceneKey());

	CallDeferred(nameof(SpawnPlayerAfterSceneLoad));

			_ui.ShowOnlyHud();
			GD.Print($"[SaveLoad] Started new game in slot {slot} for player '{safePlayerName}'.");
	}

	/// <summary>
	/// Creates the runtime player model and initializes stats, progression, starter equipment, and training enemy data.
	/// </summary>
	public Player CreatePlayerModel()
	{
		IStateMachine fsm = new StateMachine();

		Player player = _entity.CreatePlayer(
			combat: _combat,
			inventory: _inventory,
			fsm: fsm
		);

		InitializeNewGamePlayer(player);
		EquipStarterWeapon(player);
		EnsureStarterEnemyRegistered();

		return player;
	}

	private void InitializeNewGamePlayer(Player player)
	{
		if (player == null)
		{
			return;
		}

		player.SetName("Player");
		player.InitializeStats(
			maxHp: ToStat(hpBase),
			maxMana: ToStat(mpBase),
			strength: ToStat(strBase),
			dexterity: ToStat(dexBase),
			intelligence: ToStat(intBase),
			spirit: ToStat(spiBase),
			vitality: ToStat(vitBase),
			luck: ToStat(lukBase));
		InitializePlayerAbilityPath(player);
	}

	private void EquipStarterWeapon(Player player)
	{
		if (player == null)
		{
			return;
		}

		if (_db.GetItemFromRepo(StarterWeaponItemId) is not InventoryItem item)
		{
			GD.PushWarning($"Starter weapon id {StarterWeaponItemId} was not found in the item repository.");
			return;
		}

		item.SetOwner(player);
		_inventory.AddItem(StarterWeaponItemId);
		_inventory.UseItem(StarterWeaponItemId);
	}

	private void EnsureStarterEnemyRegistered()
	{
		if (_entity.registeredEnemies.Count > 0)
		{
			return;
		}

		_entity.CreateEnemy(
			name: "Training Dummy",
			level: 1,
			entity: _entity,
			combat: _combat,
			fsm: new StateMachine());
	}

	private static int ToStat(double value)
	{
		return Math.Max(0, Mathf.RoundToInt((float)value));
	}

	/// <summary>
	/// Spawns the current GameState player at the new-game marker after a world scene is loaded.
	/// </summary>
	public void SpawnPlayerAfterSceneLoad()
	{
		var player = _gameState.GetPlayer();
		
			if(player == null)
			{
				GD.PushError("No player model in GameState when spawning.");
				return;
			}

			var root = GetTree().CurrentScene;
			var world = root.GetNode<Node2D>("World");
			if(world.GetChildCount() == 0) { GD.PushError("World has no loaded scene instance");  return; }

			var worldScene = world.GetChild(0) as Node;
			var worldRoot = worldScene as WorldSceneRoot;

			if(worldRoot == null)
			{
				GD.PushError("Loaded world scene root does not have WorldSceneRoot.cs attached.");
				return;
			}

			if(PlayerScene == null)
			{
				GD.PushError("GameManager.PlayerScene export is not set.");
				return;
			}

			string spawnName = string.IsNullOrWhiteSpace(NewGameSpawnName) ? "NewGameSpawn" : NewGameSpawnName;
			Marker2D spawn = worldRoot.GetSpawn(spawnName) ?? worldRoot.GetSpawn("NewGameSpawn");
			if (spawn == null)
			{
				GD.PushError($"SpawnPlayerAfterSceneLoad: Spawn '{spawnName}' was not found and fallback 'NewGameSpawn' is missing.");
				return;
			}

			_scene.SpawnPlayerNode(PlayerScene, spawn.GlobalPosition, player, worldRoot.Entities);
			_ui.BindPlayerHud(player);
	}

	/// <summary>
	/// Spawns the restored GameState player after loading the saved world scene and applies inventory state.
	/// </summary>
	public void SpawnLoadedPlayerAfterSceneLoad()
	{
		if (_pendingLoadedSave == null)
		{
			return;
		}

		var player = _gameState.GetPlayer();
		if (player == null)
		{
			GD.PushError("[SaveLoad] No player model available while restoring save.");
			_pendingLoadedSave = null;
			return;
		}

		var root = GetTree().CurrentScene;
		var world = root.GetNodeOrNull<Node2D>("World");
		if (world == null || world.GetChildCount() == 0)
		{
			GD.PushError("[SaveLoad] Cannot restore player: current world scene is missing.");
			_pendingLoadedSave = null;
			return;
		}

		var worldRoot = world.GetChild(0) as WorldSceneRoot;
		if (worldRoot == null)
		{
			GD.PushError("[SaveLoad] Cannot restore player: loaded world scene has no WorldSceneRoot.");
			_pendingLoadedSave = null;
			return;
		}

		PlayerSnapshot snapshot = _pendingLoadedSave.Player?.Player ?? new PlayerSnapshot();
		Vector2 fallbackPosition = ResolveSpawnPosition(worldRoot, snapshot.SpawnId);
		Vector2 savedPosition = new Vector2(snapshot.Position?.X ?? fallbackPosition.X, snapshot.Position?.Y ?? fallbackPosition.Y);
		Vector2 restorePosition = savedPosition == Vector2.Zero ? fallbackPosition : savedPosition;

		PlayerNode node = _scene.SpawnPlayerNode(PlayerScene, restorePosition, player, worldRoot.GetPlayerContainer() ?? worldRoot.Entities);
		node.GlobalPosition = restorePosition;
		_ui.BindPlayerHud(player);

		if (_pendingLoadedSave.Inventory != null)
		{
			_inventory.RestoreSnapshot(_pendingLoadedSave.Inventory);
			GD.Print("[SaveLoad] Restored inventory snapshot.");
		}

		GD.Print($"[SaveLoad] Restored player position: {restorePosition.X:0.##},{restorePosition.Y:0.##}");
		GD.Print("[SaveLoad] Restored game state snapshot.");
		_pendingLoadedSave = null;
		InitializeCurrentSceneUi();
	}

		/// <summary>
		/// Spawns the current player model at a named marker in the active WorldSceneRoot.
		/// </summary>
		public void SpawnPlayerAtMarker(string markerName)
		{
			var player = _gameState.GetPlayer();
			if (player == null)
			{
				GD.PushError("SpawnPlayerAtMarker: No player in GameState.");
				return;
			}

			var world = ResolveCurrentWorldSceneRoot();
			if (world == null)
			{
				GD.PushError("SpawnPlayerAtMarker: WorldSceneRoot not found in the current scene.");
				return;
			}

			var marker = world.GetSpawn(markerName);
			if (marker == null)
			{
				GD.PushError($"SpawnPlayerAtMarker: Spawn '{markerName}' not found.");
				return;
			}

			// Spawn a fresh node each scene (simple + reliable)
			var packed = PlayerScene; 
			if (packed == null)
			{
				GD.PushError("SpawnPlayerAtMarker: PlayerScene export is null.");
				return;
			}

			_scene.SpawnPlayerNode(packed, marker.GlobalPosition, player, parent: world.GetPlayerContainer() ?? GetTree().CurrentScene);
			_ui.BindPlayerHud(player);
		}

		private WorldSceneRoot ResolveCurrentWorldSceneRoot()
		{
			var currentScene = GetTree()?.CurrentScene;
			if (currentScene == null)
			{
				return null;
			}

			if (currentScene is WorldSceneRoot currentWorldRoot)
			{
				return currentWorldRoot;
			}

			if (currentScene.GetNodeOrNull<WorldSceneRoot>("World") is WorldSceneRoot directWorld)
			{
				return directWorld;
			}

			var worldContainer = currentScene.GetNodeOrNull<Node>("World");
			if (worldContainer == null)
			{
				return null;
			}

			foreach (Node child in worldContainer.GetChildren())
			{
				if (child is WorldSceneRoot worldSceneRoot)
				{
					return worldSceneRoot;
				}
			}

			return null;
		}

	private string ResolveDefaultSceneKey()
		{
			return string.IsNullOrWhiteSpace(NewGameSceneKey) ? "PhosphorForest" : NewGameSceneKey;
		}

	private async Task SaveGameSlotAsync(int slot)
		{
			if (slot < 1 || slot > SaveLoadService.MaxSaveSlots)
			{
				GD.PushError($"[SaveLoad] Cannot save slot {slot}: valid slots are 1-{SaveLoadService.MaxSaveSlots}.");
				return;
			}

			_saveslot = slot;
			CaptureCurrentLocationForSave();
			await _saves.SaveGameAsync(slot);
		}

	private void CaptureCurrentLocationForSave()
		{
			string sceneId = _scene?.GetCurrentSceneKey();
			if (string.IsNullOrWhiteSpace(sceneId))
			{
				sceneId = _gameState.CurrentSceneId;
			}

			sceneId = string.IsNullOrWhiteSpace(sceneId) ? ResolveDefaultSceneKey() : sceneId;
			string spawnId = string.IsNullOrWhiteSpace(_gameState.CurrentSpawnId)
				? "NewGameSpawn"
				: _gameState.CurrentSpawnId;

			_gameState.SetCurrentLocationWithDisplayName(sceneId, sceneId, spawnId, _gameState.CurrentAreaId);
		}

	private static Vector2 ResolveSpawnPosition(WorldSceneRoot worldRoot, string spawnId)
		{
			string safeSpawnId = string.IsNullOrWhiteSpace(spawnId) ? "NewGameSpawn" : spawnId;
			Marker2D marker = worldRoot.GetSpawn(safeSpawnId) ?? worldRoot.GetSpawn("NewGameSpawn");
			return marker?.GlobalPosition ?? Vector2.Zero;
		}

	private void ApplyPlayerSnapshot(Player player, PlayerSnapshot snapshot)
		{
			if (player == null || snapshot == null)
			{
				return;
			}

			player.SetName(string.IsNullOrWhiteSpace(snapshot.PlayerName) ? "Player" : snapshot.PlayerName);
			player.Progression.RestoreSnapshot(
				snapshot.Level,
				snapshot.CurrentExperience,
				snapshot.TotalExperience,
				snapshot.SkillPoints);

			if (snapshot.Stats == null || snapshot.Stats.Count == 0)
			{
				RestorePlayerAbilityPath(player, snapshot.AbilityPath);
				return;
			}

			int maxHp = GetSnapshotStat(snapshot, "MaxHP", player.MaxHP);
			int curHp = GetSnapshotStat(snapshot, "CurHP", maxHp);
			int maxMana = GetSnapshotStat(snapshot, "MaxMana", player.MaxMana);
			int curMana = GetSnapshotStat(snapshot, "CurMana", maxMana);

			player.InitializeStats(
				maxHp,
				maxMana,
				GetSnapshotStat(snapshot, "Strength", player.Strength),
				GetSnapshotStat(snapshot, "Dexterity", player.Dexterity),
				GetSnapshotStat(snapshot, "Intelligence", player.Intelligence),
				GetSnapshotStat(snapshot, "Spirit", player.Spirit),
				GetSnapshotStat(snapshot, "Vitality", player.Vitality),
				GetSnapshotStat(snapshot, "Luck", player.Luck));

			player.CurHP = curHp - player.CurHP;
			player.CurMana = curMana - player.CurMana;

			RestorePlayerAbilityPath(player, snapshot.AbilityPath);
		}

	private void InitializePlayerAbilityPath(Player player)
		{
			AbilityPathDefinition definition = _db?.GetAbilityPathDefinitionFromRepo(PlayerAbilityPathId);
			if (definition == null)
			{
				GD.PushWarning($"[Progression] Player ability path '{PlayerAbilityPathId}' was not found.");
				return;
			}

			player?.AbilityPath.Initialize(definition);
		}

	private void RestorePlayerAbilityPath(Player player, AbilityPathSave save)
		{
			if (player == null)
			{
				return;
			}

			string pathId = !string.IsNullOrWhiteSpace(save?.PathId) ? save.PathId : PlayerAbilityPathId;
			AbilityPathDefinition definition = _db?.GetAbilityPathDefinitionFromRepo(pathId);
			if (definition == null)
			{
				GD.PushWarning($"[Progression] Saved ability path '{pathId}' was not found. Falling back to '{PlayerAbilityPathId}'.");
				definition = _db?.GetAbilityPathDefinitionFromRepo(PlayerAbilityPathId);
			}

			if (definition == null)
			{
				return;
			}

			player.AbilityPath.Restore(definition, save);
		}

	private static int GetSnapshotStat(PlayerSnapshot snapshot, string key, int fallback)
		{
			return snapshot.Stats != null && snapshot.Stats.TryGetValue(key, out int value)
				? value
				: fallback;
		}

	private void ClearWorldScene()
		{
			var root = GetTree()?.CurrentScene;
			var world = root?.GetNodeOrNull<Node2D>("World");
			if (world == null)
			{
				return;
			}

			foreach (Node child in world.GetChildren())
			{
				child.QueueFree();
			}
		}

	private void HandleDebugSaveLoadInput()
		{
			if (_ui?.HasRoot != true)
			{
				return;
			}

			int saveSlot = 0;
			if (Input.IsKeyPressed(Key.F5))
			{
				saveSlot = 1;
			}
			else if (Input.IsKeyPressed(Key.F6))
			{
				saveSlot = 2;
			}
			else if (Input.IsKeyPressed(Key.F7))
			{
				saveSlot = 3;
			}
			else if (Input.IsKeyPressed(Key.F8))
			{
				saveSlot = 4;
			}

			if (saveSlot > 0)
			{
				if (!_debugSaveKeyHeld)
				{
					_debugSaveKeyHeld = true;
					SaveGameSlot(saveSlot);
				}
			}
			else
			{
				_debugSaveKeyHeld = false;
			}

			if (Input.IsKeyPressed(Key.Escape))
			{
				if (!_returnToMenuKeyHeld)
				{
					_returnToMenuKeyHeld = true;
					if (_ui?.CloseTopGameplayPanel() == true)
					{
						return;
					}

					if (IsCurrentSceneDebugScene())
					{
						GD.Print("[GameManager] Escape pressed in a debug scene; no main-menu transition was performed.");
						return;
					}

					ReturnToMainMenu();
				}
			}
			else
			{
				_returnToMenuKeyHeld = false;
			}
		}

	private bool IsCurrentSceneDebugScene()
		{
			string sceneName = GetTree()?.CurrentScene?.Name;
			return !string.IsNullOrWhiteSpace(sceneName)
				&& sceneName.Contains("Debug", StringComparison.OrdinalIgnoreCase);
		}

		public void SaveGameSettings()
	{
			
	}

	public void QuitGame()
	{
		// closes the game completely
	}

	public void ReturnToMainMenu()
		{
			_saveslot = 0;
			ClearWorldScene();
			_ui.ShowOnlyMainMenu();
		}
		#endregion

	#region initialization workflows
		private void registerManager(object manager)
		{
			if (manager is ISaveable saveable)
			{
				Register(saveable);
			}

			if (manager is IResolveable resolvable)
			{
				// add this manager to the resolve loop
				_resolveList.Add(resolvable);
			}
		}
	private void unregisterManagers()
		{
			if (_list != null)
			{
				foreach (ISaveable s in _list.ToArray())
				{
					Unregister(s);
				}
			}

			if (_resolveList != null)
			{
				foreach(IResolveable r in _resolveList.ToArray())
				{
					_resolveList.Remove(r);
				}
			}
		}
	
	//======= ISaveRegistry =====//
	public void Register(ISaveable s)
		{
				if (!_list.Contains(s)) _list.Add(s);
		}

		public void Unregister(ISaveable s)
		{
			_list.Remove(s);
		}


	#endregion
	//=======Game State ======//
	#region IGameState
	public Player GetPlayer()
		{
			if (_gameState == null)
			{
				return null;
			}

			return _gameState.GetPlayer();
		}

	public NPC GetNPC(string name)
		{
			return _gameState.GetNPC(name);
		}
	public bool SetPlayer(Player player)
		{
			if (_gameState == null)
			{
				return false;
			}

			return _gameState.SetPlayer(player);
		}
	#endregion
	
	//======= IResolveable ====//
	#region IResolveable
	public void Resolve()
		{
			foreach(IResolveable r in _resolveList)
			{
				r.Resolve();
			}
		}

		public void Resolve(object obj)
		{
			foreach (IResolveable r in _resolveList)
			{
				r.Resolve(obj);
			}
		}
	#endregion

	
	// ===== EVENT MANAGER ======//
	#region IEventManager
	public void Subscribe<T>(GameEvent evt, Action<T> handler)
		{
			_event.Subscribe(evt, handler);
		}

	public void Unsubscribe<T>(GameEvent evt, Action<T> handler)
		{
			_event.Unsubscribe(evt, handler);
		}

	public void Publish<T>(GameEvent evt, T payload)
		{
			_event.Publish<T>(evt, payload);
		}

	public void Subscribe(GameEvent evt, Action handler)
		{
			_event.Subscribe(evt, handler);
		}

	public void Unsubscribe(GameEvent evt, Action handler)
		{
			_event.Unsubscribe(evt, handler);
		}

		public void Publish(GameEvent evt)
		{
			_event.Publish(evt);
		}
		
	#endregion
	
	//====== Entity Manager ========//
	#region IEntityManager
	public void SpawnPlayer(Player player)
		{
			_entity.SpawnPlayer(player);
		}

	public void SpawnEnemy(Enemy enemy)
		{
			_entity.SpawnEnemy(enemy);
		}

	public void SpawnNPC(NPC npc)
		{
			_entity.SpawnNPC(npc);
		}

	public void SpawnBoss(Boss boss)
		{
			_entity.SpawnBoss(boss);
		}

	public void DespawnEntity(Entity entity)
		{
			_entity.DespawnEntity(entity);
		}

	public Player CreatePlayer(ICombat combat, IInventory inventory, IStateMachine fsm)
		{

			return null;
		}

	public Enemy CreateEnemy(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm)
		{
			Enemy newEnemy = _entity.CreateEnemy(name, level, entity, combat, fsm);
			// set the enemy in the GameState Manager
			return newEnemy;
		}

		public void CreateBoss(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm)
		{
			_entity.CreateBoss(name, level, entity, combat, fsm);
		}
	#endregion
	
	// =========ICombat=======//
	#region ICombat

	public bool TryResolveAttack(Entity attacker, Entity target, string abilityId, out float finalDamage, out bool isCritical)
	{
			return _combat.TryResolveAttack(attacker, target, abilityId, out finalDamage, out isCritical);
	}

	public void DealDamage(Entity target, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null)
	{
			_combat.DealDamage(target, amount, damageType, source, tags);
	}

	public void Heal(Entity target, float amount, Entity source = null, IEnumerable<string> tags = null)
	{
			_combat.Heal(target, amount, source, tags);
	}

	public void ApplyStatus(Entity target, string statusId, int stacks = 1, float? durationSeconds = null, Entity source = null)
	{
			_combat.ApplyStatus(target, statusId, stacks, durationSeconds, source);
	}

	public void RemoveStatus(Entity target, string statusId, int stacks = int.MaxValue)
	{
			_combat.RemoveStatus(target, statusId, stacks);
	}

	public bool CanHit(Entity attacker, Entity target, string abilityId)
	{
			return _combat.CanHit(attacker, target, abilityId);
	}

	public float PreviewDamage(Entity attacker, Entity target, string abilityId)
	{
			return _combat.PreviewDamage(attacker, target, abilityId);
	}

		public void DealAreaDamage(IEnumerable<Entity> targets, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null, IEnumerable<string> statusIds = null)
		{
			_combat.DealAreaDamage(targets, amount, damageType, source, tags, statusIds);
		}

	   

		#endregion

	// ========IInventory======= //
		#region IInventory
			 public void UseItem(int id)
		{
		   _inventory.UseItem(id);
		}

		public bool AddItem(int id)
		{
			return _inventory.AddItem(id);
		}

		public void DropItem(int id)
		{
			_inventory.DropItem(id);
		}

		#endregion
	}

}
