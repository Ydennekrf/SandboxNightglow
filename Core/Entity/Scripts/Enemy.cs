

namespace ethra.V1
{
    public partial class Enemy : CombatEntity, IEnemy
    {
        public Enemy(IEntityManager entity, ICombat combat, IStateMachine fsm) : base(entity, combat, fsm)
        {
        }

        public void Despawn(int time)
        {
            throw new System.NotImplementedException();
        }

        public void DropLoot(int tableID)
        {
            throw new System.NotImplementedException();
        }

        public void GiveExperience(Player player)
        {
            throw new System.NotImplementedException();
        }
    }
}