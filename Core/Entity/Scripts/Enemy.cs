

namespace ethra.V1
{
    public partial class Enemy : CombatEntity, IEnemy
    {
        public IEnemyBehaviorContext BehaviorContext { get; set; }
        public int Level { get; private set; } = 1;
        public int ExperienceReward { get; private set; } = 25;

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
            player?.GainExperience(ExperienceReward);
        }

        public void ConfigureProgression(int level, int experienceReward)
        {
            Level = Godot.Mathf.Max(1, level);
            ExperienceReward = Godot.Mathf.Max(0, experienceReward);
        }
    }
}
