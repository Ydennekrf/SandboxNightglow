using Godot;
using System;
using System.Runtime.CompilerServices;

namespace ethra.V1
{
    public partial class SceneManager : ISceneManager
	{
		//======== fields and properties ==========//
		private PackedScene _currentScene;

		private Player _playerRef;

		private PackedScene _sceneToGoTo;

		private Node _gameManager;

		private IGameStateManager _gsm;
		private MasterRepository _db;

		public void Initialize(Node host, IGameStateManager gsm, MasterRepository db)
        {
            _gameManager = host ?? throw new ArgumentNullException(nameof(host));
            _gsm = gsm ?? throw new ArgumentNullException(nameof(gsm));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

		//========= Interface Public Methods ============//
		/// <summary>
        /// takes a string value representing the scene name you are trying to go to and loads in the new scene.
        /// </summary>
        /// <param name="sceneToGoTo"></param>
        /// <exception cref="NotImplementedException"></exception>
		public void GoToScene(string sceneToGoTo)
		{

			if (_gameManager == null) throw new InvalidOperationException("SceneManager not initialized. Call Initialize(...) first.");

            _sceneToGoTo = _db.GetSceneFromRepo(sceneToGoTo);

            if (_sceneToGoTo == null)
                throw new InvalidOperationException($"Scene '{sceneToGoTo}' was not found in the repository.");

            // Change scene
            var root = _gameManager.GetTree().CurrentScene;
            var world = root.GetNodeOrNull<Node2D>("World");

            if (world == null) throw new InvalidOperationException("MasterNode is missing node named 'World'.");

            // clear the old scene (keep UI Intact)
            foreach (var child in world.GetChildren()) child.QueueFree();

            var worldSceneInstance = _sceneToGoTo.Instantiate<Node2D>();
            world.AddChild(worldSceneInstance);

            _currentScene = _sceneToGoTo;
			
		}

		// =========== Internal Methods ============//

		
		/// <summary>
        /// pulls the player refrence from the current scene and saves it for persistance
        /// </summary>
        /// <param name="id">the player's unique integer based ID</param>
        /// <returns>the full player refrence</returns>
        /// <exception cref="NotImplementedException"></exception>
		private Player GetPlayerFromCurrentScene(int id)
        {
			throw new NotImplementedException();
        }

        public PlayerNode SpawnPlayerNode(PackedScene playerPacked, Vector2 position, Player model, Node parent = null)
        {
            if (_gameManager == null) throw new InvalidOperationException("SceneManager not initialized. Call Initialize(...) first.");
            if (playerPacked == null) throw new ArgumentNullException(nameof(playerPacked));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var node = playerPacked.Instantiate<PlayerNode>();
            node.GlobalPosition = position;

            // Prefer an explicit parent; otherwise use the current scene root
            Node attachParent = parent ?? _gameManager.GetTree().CurrentScene;
            attachParent.AddChild(node);

            node.Bind(model);
            return node;
        }

    }
}

