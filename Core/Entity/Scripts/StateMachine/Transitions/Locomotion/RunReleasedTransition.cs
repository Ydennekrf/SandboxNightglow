namespace ethra.V1.Transitions
{
    /// <summary>
    /// Transition when Run input is not currently held.
    /// ONLY WORKS FOR PLAYER.
    /// </summary>
    public sealed class RunReleasedTransition : IStateTransition
    {
        public BaseState Target { get; }

        public RunReleasedTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            if (owner is Player player)
            {
                return !player.RunPressed;
            }

            return false;
        }
    }
}
