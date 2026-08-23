namespace ethra.V1.Transitions
{
    public sealed class DialogEndedTransition : IStateTransition
    {
        public BaseState Target { get; }

        public DialogEndedTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            return owner is Player player && !player.DialogActive;
        }
    }
}
