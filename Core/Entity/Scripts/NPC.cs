

using System.Diagnostics.Tracing;

namespace ethra.V1
{
    public partial class NPC : NonCombatEntity, INPC
    {
        public NPC(IEntityManager entity, StateMachine fsm) : base(entity, fsm)
        {
        }
    }
}