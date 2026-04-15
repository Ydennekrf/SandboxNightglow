

using System.Collections.Generic;

namespace ethra.V1
{
    public interface IEntityManager
    {
        
        List<Player> registeredPlayers { get; set; }
        List<Enemy> registeredEnemies { get; set; }
        List<NPC> registeredNPCs { get; set; }

        
        void SpawnPlayer(Player player);

        void SpawnEnemy(Enemy enemy);

        void SpawnNPC(NPC npc);

        void SpawnBoss(Boss boss);

        void DespawnEntity(Entity entity);

        Player CreatePlayer(ICombat combat, IInventory inventory, IStateMachine fsm);

        Enemy CreateEnemy(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm);

        void CreateBoss(string name,int level, IEntityManager entity, ICombat combat, IStateMachine fsm);

    }
}