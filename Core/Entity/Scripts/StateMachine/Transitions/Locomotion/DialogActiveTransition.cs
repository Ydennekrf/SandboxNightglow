namespace ethra.V1.Transitions
{
    public sealed class DialogActiveTransition : IStateTransition
    {
        public BaseState Target { get; }

        public DialogActiveTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            return owner is Player player && player.DialogActive;
        }
    }
}
