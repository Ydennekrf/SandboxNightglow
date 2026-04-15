

namespace ethra.V1
{
    public partial class InteractableObject : NonCombatEntity, IInteractableObject
    {
        public InteractableObject(IEntityManager entity, StateMachine fsm) : base(entity, fsm)
        {
        }
    }
}