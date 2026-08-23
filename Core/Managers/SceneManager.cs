using Godot;
using System;
using System.Runtime.CompilerServices;

namespace ethra.V1
{
    /// <summary>
    /// Coordinates world scene loading and player node instancing under the active MasterNode.
    /// </summary>
    /// <remarks>
    /// SceneManager owns scene transitions only. Player model creation and persistent state remain in
    /// GameManager, EntityManager, and GameStateManager.
    /// </remarks>
    public partial class SceneManager : ISceneManager
	{
		//======== fields and properties ==========//
		private PackedScene _currentScene;
		private string _currentSceneKey = string.Empty;

		private Player _playerRef;

		private PackedScene _sceneToGoTo;

		private Node _gameManager;

		private IGameStateManager _gsm;
		private MasterRepository _db;

        /// <summary>
        /// Supplies the scene manager with the host node, persistent game state, and scene repository.
        /// </summary>
		public void Initialize(Node host, IGameStateManager gsm, MasterRepository db)
        {
            _gameManager = host ?? throw new ArgumentNullException(nameof(host));
            _gsm = gsm ?? throw new ArgumentNullException(nameof(gsm));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

		//========= Interface Public Methods ============//
		/// <summary>
        /// Replaces the current world scene with a repository scene key while keeping the MasterNode UI intact.
        /// </summary>
        /// <param name="sceneToGoTo">Scene repository key, usually the .tscn file name without extension.</param>
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

            // World content is replaced while UI remains owned by the current MasterNode.
            foreach (var child in world.GetChildren()) child.QueueFree();

            var worldSceneInstance = _sceneToGoTo.Instantiate<Node2D>();
            world.AddChild(worldSceneInstance);

            _currentScene = _sceneToGoTo;
            _currentSceneKey = sceneToGoTo;
            if (_gsm is GameStateManager gameState)
            {
                gameState.SetCurrentLocationWithDisplayName(sceneToGoTo, sceneToGoTo);
            }
			
		}

		// =========== Internal Methods ============//

		
		/// <summary>
        /// Reserved for future scene-owned player lookup if multiple player nodes are supported.
        /// </summary>
		private Player GetPlayerFromCurrentScene(int id)
        {
			throw new NotImplementedException();
        }

        /// <summary>
        /// Instances the authored PlayerNode scene, attaches it to the target parent, and binds it to the player model.
        /// </summary>
        public PlayerNode SpawnPlayerNode(PackedScene playerPacked, Vector2 position, Player model, Node parent = null)
        {
            if (_gameManager == null) throw new InvalidOperationException("SceneManager not initialized. Call Initialize(...) first.");
            if (playerPacked == null) throw new ArgumentNullException(nameof(playerPacked));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var node = playerPacked.Instantiate<PlayerNode>();
            node.GlobalPosition = position;

            // Prefer world-provided containers so debug and production scenes share the same spawn path.
            Node attachParent = parent ?? _gameManager.GetTree().CurrentScene;
            attachParent.AddChild(node);

            node.Bind(model);
            node.SetSurfaceResolver(FindWorldSurfaceResolver(attachParent));
            return node;
        }

        /// <summary>
        /// Returns the last repository scene key loaded through GoToScene.
        /// </summary>
        public string GetCurrentSceneKey()
        {
            return _currentSceneKey;
        }

        private static TileSurfaceResolver FindWorldSurfaceResolver(Node start)
        {
            Node current = start;
            while (current != null)
            {
                if (current is WorldSceneRoot worldRoot)
                {
                    return worldRoot.SurfaceResolver;
                }

                current = current.GetParent();
            }

            return null;
        }

    }
}
