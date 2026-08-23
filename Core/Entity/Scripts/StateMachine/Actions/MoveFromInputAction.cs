using Godot;

namespace ethra.V1.Actions
{
    public sealed class MoveFromInputAction : IStateAction
    {
        private readonly float _speed;

        public MoveFromInputAction(float speed)
        {
            _speed = speed;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (owner is not Player player)
            {
                return;
            }

            Vector2 direction = player.MoveInput;
            if (direction.LengthSquared() > 0.0001f)
            {
                direction = direction.Normalized();
            }

            owner.DesiredVelocity = direction * _speed;
        }

        public void Exit(Entity owner)
        {
        }
    }
}
