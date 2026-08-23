using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemyPursueAction : IStateAction
    {
        private readonly float _speed;
        private readonly string _animationKey;

        public EnemyPursueAction(float speed, string animationKey = "Pursue")
        {
            _speed = Mathf.Max(0f, speed);
            _animationKey = animationKey;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Pursue");
            context.PlayEnemyAnimation(_animationKey);
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Pursue");
            Vector2 direction = context.PlayerGlobalPosition - context.GlobalPosition;
            if (direction.LengthSquared() <= 0.001f)
            {
                context.SetDesiredVelocity(Vector2.Zero);
                return;
            }

            direction = direction.Normalized();
            context.FaceMovement(direction);
            context.SetDesiredVelocity(direction * _speed);
        }

        public void Exit(Entity owner)
        {
            if (TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                context.SetDesiredVelocity(Vector2.Zero);
            }
        }

        private static bool TryGetContext(Entity owner, out IEnemyBehaviorContext context)
        {
            context = owner is Enemy enemy ? enemy.BehaviorContext : null;
            return context != null;
        }
    }
}
