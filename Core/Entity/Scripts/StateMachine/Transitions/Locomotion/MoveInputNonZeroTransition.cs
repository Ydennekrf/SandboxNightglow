using Godot;

namespace ethra.V1.Transitions
{
    /// <summary>
    /// Detect when there is move input 
    /// ONLY WORKS FOR Player
    /// </summary>
    public sealed class MoveInputNonZeroTransition : IStateTransition
    {
        public BaseState Target { get; }

        private readonly float _deadZone;

        public MoveInputNonZeroTransition(BaseState target, float deadZone = 0.05f)
        {
            Target = target;
            _deadZone = deadZone;
        }

        public bool ShouldTransition(Entity owner)
        {
            // Using LengthSquared avoids a sqrt.
            if(owner is Player player)
            {
                return player.MoveInput.LengthSquared() > (_deadZone * _deadZone);
            }
            return false;
        }
    }
}
