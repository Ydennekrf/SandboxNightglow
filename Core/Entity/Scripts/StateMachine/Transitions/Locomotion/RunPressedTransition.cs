namespace ethra.V1.Transitions
{
    /// <summary>
    /// Transition when Run input is currently held.
    /// ONLY WORKS FOR PLAYER.
    /// </summary>
    public sealed class RunPressedTransition : IStateTransition
    {
        public BaseState Target { get; }
        private readonly float _deadZone;

        public RunPressedTransition(BaseState target, float deadZone = 0.05f)
        {
            Target = target;
            _deadZone = deadZone;
        }

        public bool ShouldTransition(Entity owner)
        {
            if (owner is Player player)
            {
                return player.RunPressed &&
                       player.MoveInput.LengthSquared() > (_deadZone * _deadZone);
            }

            return false;
        }
    }
}
