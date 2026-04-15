using Godot;

namespace ethra.V1.Transitions
{
    /// <summary>
    /// ONLY WORKS FOR PLAYER
    /// </summary>
    public sealed class MoveInputZeroTransition : IStateTransition
    {
        public BaseState Target { get; }

        private readonly float _deadZone;

        public MoveInputZeroTransition(BaseState target, float deadZone = 0.05f)
        {
            Target = target;
            _deadZone = deadZone;
        }

        public bool ShouldTransition(Entity owner)
        {
            if(owner is Player player)
            {
                return player.MoveInput.LengthSquared() <= (_deadZone * _deadZone);
            }
            return false;
        }
    }
}
