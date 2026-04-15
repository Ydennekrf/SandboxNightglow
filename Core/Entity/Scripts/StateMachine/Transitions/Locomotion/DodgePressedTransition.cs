namespace ethra.V1.Transitions
{
    /// <summary>
    /// Transition when Dodge input was pressed this frame.
    /// ONLY WORKS FOR PLAYER.
    /// </summary>
    public sealed class DodgePressedTransition : IStateTransition
    {
        public BaseState Target { get; }

        public DodgePressedTransition(BaseState target)
        {
            Target = target;
        }

        public bool ShouldTransition(Entity owner)
        {
            if (owner is Player player)
            {
                return player.DodgePressed;
            }

            return false;
        }
    }
}
