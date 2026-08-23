namespace ethra.V1.Transitions
{
    public sealed class HarvestRequestedTransition : IStateTransition
    {
        public BaseState Target { get; }

        public HarvestRequestedTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            return owner is Player player && player.HasPendingHarvestRequest && !player.DialogActive;
        }
    }
}
