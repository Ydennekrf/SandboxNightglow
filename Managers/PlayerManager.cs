using Godot;
using System.Collections.Generic;
using System.Linq;
using System;

public partial class PlayerManager : Node
{

	
	[Export] public  BaseStats PlayerBaseStats;
	public static PlayerManager Instance { get; private set; }

	private List<Player> _players = new();
	public IReadOnlyList<Player> Players => _players.AsReadOnly();

	//this is the DTO for writting to file
	private Dictionary<int, PlayerSaveData> _storedData = new();

	// this is the node attached to the player.
	private Dictionary<int, EntityData> _playerSaveDataStore = new();
	private int _nextPlayerId = 1;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{

			QueueFree();
			return;
		}

		Instance = this;
	}

	public void RegisterPlayer(Player player)
	{
		
		if (!_players.Contains(player))
			_players.Add(player);
	}

	public void DeregisterPlayer(Player player)
	{
		if (_players.Contains(player))
			_players.Remove(player);
	}

	public Player GetFirstPlayer()
	{
		return _players.FirstOrDefault();
	}

	public Player GetPlayerById(int id)
	{
		return _players.FirstOrDefault(p => p.PlayerId == id);
	}

	public int CreatePlayer()
	{
		int id = _nextPlayerId++;
		
		return id;
	}


	public Player SpawnPlayer(Vector2 position, Player player)
	{
		if (player == null)
		{
			GD.PushError("PlayerManager: Could not instantiate player.");
			return null;
		}
		
		player.GlobalPosition = position;

		// checks if the player has Stored Data if so inits those stats to the player.
		if (_playerSaveDataStore.TryGetValue(player.PlayerId, out var savedData))
		{
			GD.Print("SpawnPlayer: Loading from saved data");
			
			player.SetInitialData(savedData);
		}


		
		RegisterPlayer(player);
		return player;
	}

	public void StorePlayerData(Player player)
	{

		if (player.SaveSlotId == 0)                       // never saved before
        player.SaveSlotId = SaveData.GetNextAvailableSlot();
		// any time we set the storedData we also are saving the game, for now this is simple and sets up auto save.
		// might back this out later to allow for save checkpoints around the map, this will also easily enable spawn points.
		SaveRequest save = new SaveRequest();
		PlayerSaveData saveData = new PlayerSaveData
		{

			        CurrentStats = new Dictionary<string, int>(),
					Inventory = new List<ItemStack?>(), // or whatever your default type is
					SceneID = SceneManager.Instance.GetCurrentScene().Name,
					SpawnID = player.currentSpawnId,
					SaveSlot = player.SaveSlotId,
					SavedAt = DateTime.UtcNow
		};

		
		saveData.WorldState = WorldStateManager.I.ToDto();
		save.Data = saveData;
				foreach (var kv in player.Data.EntityStats)
		{
			save.Data.CurrentStats[kv.Key.ToString()] = kv.Value.Value;

		}
    	EventManager.I.Publish(GameEvent.SaveRequested, save);

		// foreach (var kvp in inv.EquippedItems)
		//     saveData.EquippedItems[kvp.Key] = kvp.Value.ItemId;

		_storedData[player.PlayerId] = save.Data;
		
		SaveData.SaveGame(save.Data);
	}

	public void SetLoadedPlayerData(PlayerSaveData DTO, EntityData node, int id)
	{
		if (!_storedData.ContainsKey(id))
		{
			_storedData.Add(id, DTO);
		}
		else
		{
			_storedData[id] = DTO;
		}

		if (!_playerSaveDataStore.ContainsKey(id))
		{
			_playerSaveDataStore.Add(id, node);
		}
		else
		{
			_playerSaveDataStore[id] = node;
		}
	}

	public void SetBaseStats(Player player)
	{
		EntityData baseData = new EntityData(PlayerBaseStats);

		player.Data = baseData;
		GD.Print("player base stats set.");
	}
}
