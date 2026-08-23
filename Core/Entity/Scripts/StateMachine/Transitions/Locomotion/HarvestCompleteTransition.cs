namespace ethra.V1.Transitions
{
    public sealed class HarvestCompleteTransition : IStateTransition
    {
        public BaseState Target { get; }

        public HarvestCompleteTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            return owner is Player player && player.HarvestComplete;
        }
    }
}
