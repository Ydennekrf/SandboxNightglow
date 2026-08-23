using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
    /// <summary>
    /// Owns runtime entity model registries and entity save snapshot capture.
    /// </summary>
    /// <remarks>
    /// EntityManager creates player/enemy models. SceneManager and scene roots own node instancing, so this
    /// class should not become a scene placement or visual orchestration layer.
    /// </remarks>
    public partial class EntityManager : ISaveable, IEntityManager
	{
		private string _saveKey = "Player";
		public string SaveKey => _saveKey;

        public List<Player> registeredPlayers { get; set; }
        public List<Enemy> registeredEnemies { get; set; }
        public List<NPC> registeredNPCs { get; set; }

		public EntityManager()
		{
			registeredPlayers = new List<Player>();
			
			registeredEnemies = new List<Enemy>();
			registeredNPCs = new List<NPC>();
		}

        /// <summary>
        /// Captures the current player model snapshot for save/load.
        /// </summary>
		public object CaptureSnapshot()
        {
            Player player = registeredPlayers != null && registeredPlayers.Count > 0
                ? registeredPlayers[0]
                : GameManager.Instance?.GameState?.GetPlayer();

            return new EntitySave
            {
                Player = SaveLoadService.CreatePlayerSnapshot(player)
            };
        }

        public void RestoreSnapshot(object snapshot)
        {
            // Player scene/node restoration is coordinated by GameManager after the target scene is loaded.
        }

        public void SpawnPlayer(Player player)
        {
            
        }

        /// <summary>
        /// Registers an enemy model so combat/debug systems can find active targets.
        /// </summary>
        public void SpawnEnemy(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            registeredEnemies ??= new List<Enemy>();
            if (!registeredEnemies.Contains(enemy))
            {
                registeredEnemies.Add(enemy);
            }
        }

        public void SpawnNPC(NPC npc)
        {
            throw new NotImplementedException();
        }

        public void SpawnBoss(Boss boss)
        {
            throw new NotImplementedException();
        }

        public void DespawnEntity(Entity entity)
        {
            throw new NotImplementedException();
        }

       
        /// <summary>
        /// Creates a simple enemy model for debug and early gameplay use.
        /// </summary>
        public Enemy CreateEnemy(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm)
        {
            registeredEnemies ??= new List<Enemy>();

            Enemy enemy = new Enemy(entity, combat, fsm);
            enemy.SetName(string.IsNullOrWhiteSpace(name) ? "Enemy" : name);
            enemy.ConfigureProgression(level, 25 + Math.Max(0, level - 1) * 10);
            enemy.InitializeStats(
                maxHp: 50,
                maxMana: 0,
                strength: 5,
                dexterity: 5,
                intelligence: 0,
                spirit: 0,
                vitality: 5,
                luck: 0);

            registeredEnemies.Add(enemy);
            return enemy;
        }

        public void CreateBoss(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm)
        {
            throw new NotImplementedException();
        }

       /// <summary>
       /// Creates and initializes the player model. Node spawning is handled later by SceneManager.
       /// </summary>
       public Player CreatePlayer( ICombat combat, IInventory inventory, IStateMachine fsm)
        {
            registeredPlayers ??= new List<Player>();

            var player = new Player(this, combat, inventory, fsm);

            player.Initialize();

            registeredPlayers.Add(player);
            return player;
        }
    }
}
