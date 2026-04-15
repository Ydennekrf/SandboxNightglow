


namespace ethra.V1
{
    public partial class NonCombatEntity : Entity, INonCombatEntity
    {
        public NonCombatEntity(IEntityManager entity, IStateMachine fsm) : base(entity, fsm)
        {
        }

        public override void Initialize()
        {
            
            throw new System.NotImplementedException();
        }
    }
}