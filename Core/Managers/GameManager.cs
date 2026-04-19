using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ethra.V1
{
	public partial class GameManager : Node, IGameManager, ISaveRegistry, IGameStateManager, IResolveable, IEvent , IEntityManager, ICombat, IInventory
	{

	//======= fields and properties ==============//

	
	// ===== managers
	#region Manager refs
	
	private CombatManager _combat;
	private DialogManager _dialog;
	private EntityManager _entity;
	private EventManager _event;
	private GameStateManager _gameState;
	private InventoryManager _inventory;
	private SceneManager _scene;
	private UIManager _ui;
	private MasterRepository _db;
	private ISaveLoadService _saves;
	private int _resolveOrder;


	public static GameManager Instance { get; private set; }
	public CombatManager Combat { get { return _combat; } }
	public DialogManager Dialog { get { return _dialog; } }
	public EntityManager Entity { get { return _entity; } }
	public EventManager Events { get { return _event; } }
	public GameStateManager GameState { get { return _gameState; } }
	public InventoryManager Inventory { get { return _inventory; } }
	public SceneManager Scene { get { return _scene; } }
	public UIManager UI { get { return _ui; } }
	public MasterRepository DB { get { return _db; } }
	#endregion


		// ==== Game Manager specific fields =====//

	private int _saveslot;

	private List<IResolveable> _resolveList;
	public int ResolveOrder => _resolveOrder;
	
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
	[Export] public string SceneFolderLocation;

	[ExportGroup("Repository Data Sources")]

	[ExportSubgroup("Dialog CSV")]
	[Export] public string DialogCsvPath = string.Empty;
	[ExportSubgroup("Item CSV")]
	[Export] public string ItemCsvPath = "res://Core/Inventory/Data/items_seed.csv";
	[ExportSubgroup("Item Effects CSV")]
	[Export] public string ItemEffectsCsvPath = "res://Core/Inventory/Data/item_effects_seed.csv";

	[ExportGroup("Player Exports")]
		[ExportSubgroup("StateMachine")]

			// any variables required to build the player StateMachine
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

			#endregion

	[ExportGroup("Game State Exports")]
		[ExportSubgroup("world time config")]
			[Export] public float tickPerMinute;
		
	
	

	#endregion

	#region Godot methods

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			Instance = this;
			Initialize();
		}



		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			Resolve();
			Resolve(delta);
		}

		public override void _ExitTree()
		{
			base._ExitTree();

			unregisterManagers();
		}

	#endregion

	//========== Game Manager Interface methods ===================//
		public void Initialize()
		{

			_list = new List<ISaveable>();
			_resolveList = new List<IResolveable>();

			// when we initialize all the manager classes check if they are ISavable if so add them to the registry on save
			_combat = new CombatManager();
			_dialog = new DialogManager();
			_entity = new EntityManager();
			_event = new EventManager();
			_gameState = new GameStateManager();
			
			_scene = new SceneManager();
			_ui = new UIManager();
			_db = new MasterRepository();
			_saves = new SaveLoadService(this);
			_inventory = new InventoryManager(_db);

			_scene.Initialize(this, _gameState, _db);

			registerManager(_combat);
			registerManager(_dialog);
			registerManager(_entity);
			registerManager(_event);
			registerManager(_gameState);
			registerManager(_inventory);
			registerManager(_scene);
			registerManager(_ui);

			GetAllItems();
			GetAllItemEffects();
			GetAllDialog();
			GetAllScenes();


			var uiRoot = GetTree().CurrentScene.GetNodeOrNull<UIRoot>("UI");
			if (uiRoot == null)
				GD.PushError("UIRoot not found at path 'UI' under current scene.");
			else
				_ui.Initialize(uiRoot);

		}



	#region Fill Master Repo
	public void GetAllItems()
		{
		// this will get called after all the manager's have been initialized and start loading all the game item refrences into the master repository
		if (string.IsNullOrWhiteSpace(ItemCsvPath))
		{
			GD.PushWarning("GetAllItems: ItemCsvPath is empty. Skipping item csv load.");
			return;
		}

				DB.FillCsvRepo(ItemCsvPath, MasterRepository.RepoLoadType.Items, new[]
						{
							"id", "name", "category", "description", "rarity", "sell_value", "subtype", "max_stack"
						});
					}

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

			DB.FillCsvRepo(ItemCsvPath, MasterRepository.RepoLoadType.Items, new[]
				{
					"id", "name", "category", "description", "rarity", "sell_value", "subtype", "max_stack"
				});
			}

	public void GetAllScenes()
		{
		// this will get called after all the manager's have been initialized and start loading all the scene object refrences into the master repository
		DB.FillSceneRepo(SceneFolderLocation);
		}

		public void GetAllDialog()
		{
			// loads all the dialog trees into the master repository
			if (string.IsNullOrWhiteSpace(DialogCsvPath))
			{
				GD.PushWarning("GetAllDialog: DialogCsvPath is empty. Skipping dialog csv load.");
				return;
			}

			DB.FillCsvRepo(DialogCsvPath, MasterRepository.RepoLoadType.Dialog);
		}
		#endregion

	#region Save load workflow
		/// <summary>
		/// takes the ID from the UI to load the save file
		/// </summary>
		/// <param name="id">the save slot id</param>
	public async void LoadSavedGame(int id)
		{
			await _saves.LoadGameAsync(id);
			_saveslot = id;
		}
	
	public async void SaveCurrentGame()
		{
			await _saves.SaveGameAsync(_saveslot);
		}

	public void StartNewGame()
	{
		  // 1) Create player model via EntityManager
	Player player = CreatePlayerModel(); // your existing pipeline, but should return fully initialized player

	_gameState.SetPlayer(player);

	// 2) Load game scene
	_scene.GoToScene("LabScene");

	// 3) Spawn player next frame (scene root exists then)
	CallDeferred(nameof(SpawnPlayerAfterSceneLoad));

			_ui.ShowOnlyHud();
	}

	public Player CreatePlayerModel()
{
	// Create the FSM here so the flow is explicit
	IStateMachine fsm = new StateMachine();

	// Delegate actual construction to EntityManager
	Player player = _entity.CreatePlayer(
		combat: _combat,
		inventory: _inventory,
		fsm: fsm
	);
			

	return player;
}

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

			_scene.SpawnPlayerNode(PlayerScene, worldRoot.GetSpawn("NewGameSpawn").GlobalPosition, player, worldRoot.Entities);
	}

		public void SpawnPlayerAtMarker(string markerName)
		{
			var player = _gameState.GetPlayer();
			if (player == null)
			{
				GD.PushError("SpawnPlayerAtMarker: No player in GameState.");
				return;
			}

			var world = GetTree().CurrentScene.GetNodeOrNull<WorldSceneRoot>("World");
			if (world == null)
			{
				GD.PushError("SpawnPlayerAtMarker: WorldSceneRoot not found at 'World'.");
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
			// scene manager loads the main menu scene back in
			_saveslot = 0;
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
			foreach (ISaveable s in _list)
			{
				Unregister(s);
			}
			
			foreach(IResolveable r in _resolveList)
			{
				_resolveList.Remove(r);
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
			return _gameState.GetPlayer();
		}

	public NPC GetNPC(string name)
		{
			return _gameState.GetNPC(name);
		}
	public bool SetPlayer(Player player)
		{
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
