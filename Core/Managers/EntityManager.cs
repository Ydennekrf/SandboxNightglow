using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class EntityManager : ISaveable, IEntityManager
	{
       // this needs a Game manager reference maybe we need a ISprite interface 
		private string _saveKey = "Player";
		public string SaveKey => _saveKey;

        public List<Player> registeredPlayers { get; set; }
        public List<Enemy> registeredEnemies { get; set; }
        public List<NPC> registeredNPCs { get; set; }

		// Called when the node enters the scene tree for the first time.


		public EntityManager()
		{
			registeredPlayers = new List<Player>();
			
			registeredEnemies = new List<Enemy>();
			registeredNPCs = new List<NPC>();
		}

		public object CaptureSnapshot()
        {
            EntitySave saveFile = new EntitySave();

            foreach (Player p in registeredPlayers)
            {
                saveFile.players.Add(p);
            }

            foreach (NPC n in registeredNPCs)
            {
                saveFile.npcs.Add(n);
            }

            return saveFile;
        }

        public void RestoreSnapshot(object snapshot)
        {
            throw new NotImplementedException();
        }

        public void SpawnPlayer(Player player)
        {
            
        }

        public void SpawnEnemy(Enemy enemy)
        {
            throw new NotImplementedException();
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

       
        public Enemy CreateEnemy(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm)
        {
            Enemy enemy = new Enemy(entity, combat, fsm);
            
            return enemy;
        }

        public void CreateBoss(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm)
        {
            throw new NotImplementedException();
        }

       public Player CreatePlayer( ICombat combat, IInventory inventory, IStateMachine fsm)
        {
            // Ensure registries exist
            registeredPlayers ??= new List<Player>();

            var player = new Player(this, combat, inventory, fsm);

            // Leaf initializes itself (FSM build)
            player.Initialize();

            registeredPlayers.Add(player);
            return player;
        }
    }
}

