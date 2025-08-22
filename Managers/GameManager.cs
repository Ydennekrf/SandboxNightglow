using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	public ImageTexture LastDeathSnapshot { get; private set; }
	private bool _gameoverHandled;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
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
		EventManager.I.Subscribe<Entity>(GameEvent.GameOverRequested, OnGameOver);
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
	public override void _ExitTree()
	{
		EventManager.I.Unsubscribe<Entity>(GameEvent.GameOverRequested, OnGameOver);
	}


	public void StartNewGame()
	{

		GD.Print("Load main scene now");
		SceneManager.Instance.LoadSceneWithPlayerByKey("TestZone", "Start");
	}

	public void StartLoadGame(int slot)
	{
		_gameoverHandled = false;
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

		int Pid = PlayerManager.Instance.CreatePlayer();
		Player loadedPlayer = SceneRegistry.Load("Player")?.Instantiate<Player>();
		loadedPlayer.SetPlayerId(Pid);

		// 2)  Create Stat objects with the correct max
		foreach (var (type, value) in raw)
		{
			switch (type)
			{
				case StatType.CurrentHealth:
					int maxHp = raw.GetValueOrDefault(StatType.MaxHealth, value);
					data.EntityStats[type] = new Stat(type, value, maxHp, loadedPlayer);
					break;

				case StatType.CurrentMana:
					int maxMp = raw.GetValueOrDefault(StatType.MaxMana, value);
					data.EntityStats[type] = new Stat(type, value, maxMp, loadedPlayer);
					break;

				case StatType.CurrentStamina:
					int maxSta = raw.GetValueOrDefault(StatType.MaxStamina, value);
					data.EntityStats[type] = new Stat(type, value, maxSta, loadedPlayer);
					break;

				case StatType.Experience:
					data.EntityStats[type] = new Stat(type, value, 999999999, loadedPlayer);
					break;

				default:
					data.EntityStats[type] = new Stat(type, value, 999, loadedPlayer);
					break;
			}
		}


		// set stats here
		loadedPlayer.SaveSlotId = save.SaveSlot;
		PlayerManager.Instance.SetLoadedPlayerData(save, data, Pid);

		SceneManager.Instance.LoadSceneWithPlayerByKey(save.SceneID, save.SpawnID, loadedPlayer, Pid);
	}

	public PackedScene GetPlayerScene()
	{
		return SceneRegistry.Load("Player");
	}

	public void CaptureDeathSnapshot()
	{
		var img = GetViewport().GetTexture().GetImage();
		LastDeathSnapshot = ImageTexture.CreateFromImage(img);
	}


	private void OnGameOver(Entity player)
	{
		if (_gameoverHandled) return; // guard against duplicates
		_gameoverHandled = true;
		

				// 1) Capture snapshot before pausing or swapping scenes
		CaptureDeathSnapshot();

		PackedScene scene = SceneRegistry.Load("GameOver");

		Node gameoverscene = scene.Instantiate();
		// 2) Now fade out and replace scene
		SceneManager.Instance.ReplaceCurrentScene(gameoverscene);

    }
}
