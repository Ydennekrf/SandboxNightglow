using Godot;

public partial class SceneManager : Node
{
	public static SceneManager Instance;
	private string _pendingSpawnId;
	private Player _pendingPlayer;
	private int _pendingPlayerId;
	private Node _currentSceneInstance;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			GD.PushWarning("Duplicate SceneManager detected. Removing extra.");
			QueueFree();
			return;
		}

		Instance = this;
	}

	// Entry point using scene key from SceneRegistry
	public void LoadSceneWithPlayerByKey(string sceneKey, string spawnId, Player playerInstance = null, int playerId = 0)
	{
		if (!SceneRegistry.ScenePaths.TryGetValue(sceneKey, out var path))
		{
			GD.PushError($"SceneManager: Scene key '{sceneKey}' not found in SceneRegistry.");
			return;
		}
		GD.Print($"SceneManager: Loading {sceneKey} at spawn point {spawnId}");
		_pendingPlayer = playerInstance;
		_pendingPlayerId = playerId;
		LoadSceneWithPlayer(path, spawnId);
	}

	// Main loading logic
	public async void LoadSceneWithPlayer(string scenePath, string spawnId)
	{
		
		_pendingSpawnId = spawnId;

		// Load the scene manually
		var packedScene = GD.Load<PackedScene>(scenePath);
		if (packedScene == null)
		{
			GD.PushError($"SceneManager: Failed to load PackedScene at path {scenePath}");
			return;
		}

		_currentSceneInstance = packedScene.Instantiate();
		if (_currentSceneInstance == null)
		{
			GD.PushError($"SceneManager: Failed to instantiate scene at path {scenePath}");
			return;
		}

		// Replace contents of Masters/CurrentScene
		var root = GetTree().Root;
		var masters = root.GetNode("Masters");
		var currentScene = masters.GetNode<Node>("CurrentScene");

		foreach (var child in currentScene.GetChildren())
		{
			child.QueueFree();
		}

		currentScene.AddChild(_currentSceneInstance);

		
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		DeferredSceneInit();
	}

	private void DeferredSceneInit()
	{
		if (_currentSceneInstance == null)
		{
			GD.PushError("SceneManager: Current scene instance is null.");
			return;
		}

		var spawn = _currentSceneInstance.GetNodeOrNull<Marker2D>($"SpawnPoints/{_pendingSpawnId}");
		var playerLayer = _currentSceneInstance.GetNodeOrNull<Node2D>("Map/PlayerLayer");
		

		// get player by ID here before and set pending player.

		if (_pendingPlayer == null)
		{
			_pendingPlayerId = PlayerManager.Instance.CreatePlayer();
			_pendingPlayer = SceneRegistry.Load("Player")?.Instantiate<Player>();
			_pendingPlayer.SetPlayerId(_pendingPlayerId);
			PlayerManager.Instance.SetBaseStats(_pendingPlayer);
		}

		_pendingPlayer = PlayerManager.Instance.SpawnPlayer(spawn?.GlobalPosition ?? Vector2.Zero, _pendingPlayer);
		

		playerLayer?.AddChild(_pendingPlayer);

		_pendingSpawnId = null;
		_pendingPlayerId = 0;
		
	}

	public Node GetCurrentScene()
	{
		return GetTree().Root.GetNode<Node>("Masters/CurrentScene").GetChild(0);
	}

	public void ReplaceCurrentScene(Node newScene)
	{
		var holder = GetTree().Root.GetNode<Node>("Masters/CurrentScene");

		foreach (var child in holder.GetChildren())
			child.QueueFree();

		holder.AddChild(newScene);
	}
}
