
using Godot;

namespace ethra.V1{
    public interface ISceneManager
    {
       void Initialize(Node host, IGameStateManager gsm, MasterRepository db);

        void GoToScene(string sceneToGoTo);

        // Spawn into current scene root (after scene load)
        PlayerNode SpawnPlayerNode(PackedScene playerPacked, Vector2 position, Player model, Node parent = null);

        
    }
}