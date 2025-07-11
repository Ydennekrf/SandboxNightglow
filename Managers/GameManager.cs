using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node {
	public static GameManager Instance {get; private set;}

	public override void _EnterTree() 
	{
		if(Instance != null && Instance != this){
			QueueFree();
			return;
		}
		Instance = this;
		// validate all scenes in the registry to avoid scene manager errors down the road.
		SceneRegistry.ValidateAll();
		// load the main menu on game load up.
		CallDeferred(nameof(ShowMainMenu));
	}

	public void ShowMainMenu()
	{
		var scene = SceneRegistry.Load("MainMenu");
		if (scene == null)
		{
			GD.PushError("GameManager: MainMenu scene not found in SceneRegistry.");
			return;
		}

		var menu = scene.Instantiate<Control>();

		var root = GetTree().Root;
		var masters = root.GetNode("Masters");
		var currentScene = masters.GetNode<Node>("CurrentScene");

		if (currentScene == null)
		{
			GD.PushError("GameManager: CurrentScene node not found.");
			return;
		}

		currentScene.CallDeferred("add_child", menu);
	}

	public void StartNewGame()
	{
		
		GD.Print("Load main scene now");
		SceneManager.Instance.LoadSceneWithPlayerByKey("TestZone", "Start");
	}

	public void StartLoadGame(int slot)
	{
		PlayerSaveData save = SaveData.Load(slot); // load slot 1
		
		if (save == null)
		{
			GD.Print("No save file found.");
			return;
		}
		LoadRequest load = new LoadRequest();

		load.Data = save;

		WorldStateManager.I.LoadFromDto(save.WorldState);

		EventManager.I.Publish<LoadRequest?>(GameEvent.LoadRequested, load);
		// convert saved data to EntityData
		EntityData data = new EntityData();

		// 1)  Gather everything from the save file into a temporary dictionary
		var raw = new Dictionary<StatType, int>();
		foreach (var kv in save.CurrentStats)
			if (Enum.TryParse(kv.Key, out StatType t))
				raw[t] = kv.Value;

		// 2)  Create Stat objects with the correct max
		foreach (var (type, value) in raw)
		{
			switch (type)
			{
				case StatType.CurrentHealth:
					int maxHp = raw.GetValueOrDefault(StatType.MaxHealth, value);
					data.EntityStats[type] = new Stat(type, value, maxHp);
					break;

				case StatType.CurrentMana:
					int maxMp = raw.GetValueOrDefault(StatType.MaxMana, value);
					data.EntityStats[type] = new Stat(type, value, maxMp);
					break;

				case StatType.CurrentStamina:
					int maxSta = raw.GetValueOrDefault(StatType.MaxStamina, value);
					data.EntityStats[type] = new Stat(type, value, maxSta);
					break;
					
				case StatType.Experience:
					data.EntityStats[type] = new Stat(type, value, 999999999);
					break;

				default:
					data.EntityStats[type] = new Stat(type, value,999);
					break;
			}
		}

		int Pid = PlayerManager.Instance.CreatePlayer();
		Player loadedPlayer = SceneRegistry.Load("Player")?.Instantiate<Player>();
		loadedPlayer.SetPlayerId(Pid);
		// set stats here
		loadedPlayer.SaveSlotId = save.SaveSlot;
		PlayerManager.Instance.SetLoadedPlayerData(save, data, Pid);

		SceneManager.Instance.LoadSceneWithPlayerByKey(save.SceneID, save.SpawnID, loadedPlayer, Pid);
	}

	public PackedScene GetPlayerScene()
	{
		return SceneRegistry.Load("Player");
	}
}
