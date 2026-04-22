namespace ethra.V1.Transitions
{
    /// <summary>
    /// Transition when Attack input was pressed this frame.
    /// ONLY WORKS FOR PLAYER.
    /// </summary>
    public sealed class AttackPressedTransition : IStateTransition
    {
        public BaseState Target { get; }

        public AttackPressedTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            if (owner is Player player)
            {
                return player.AttackPressed;
            }

            return false;
        }
    }
}
