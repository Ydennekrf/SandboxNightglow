

using System.Collections.Generic;

namespace ethra.V1
{
    public partial class Boss : CombatEntity, IBoss
    {
        public Boss(IEntityManager entity, ICombat combat, StateMachine fsm) : base(entity, combat, fsm)
        {
        }

        public Dictionary<int, BossPhase> phaseDict { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public void ExecutePhase(int phaseID)
        {
            throw new System.NotImplementedException();
        }
    }
}