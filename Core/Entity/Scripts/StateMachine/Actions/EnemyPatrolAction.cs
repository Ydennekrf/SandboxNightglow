using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemyPatrolAction : IStateAction
    {
        private readonly float _speed;
        private readonly float _durationSeconds;
        private readonly float _arrivalDistance;
        private readonly string _animationKey;

        public EnemyPatrolAction(float speed, float durationSeconds, float arrivalDistance = 6f, string animationKey = "Patrol")
        {
            _speed = Mathf.Max(0f, speed);
            _durationSeconds = Mathf.Max(0f, durationSeconds);
            _arrivalDistance = Mathf.Max(1f, arrivalDistance);
            _animationKey = animationKey;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Patrol");
            context.PlayEnemyAnimation(_animationKey);
            context.StartStateTimer(_durationSeconds);
            context.ChooseRandomPatrolDestination();
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Patrol");
            context.TickStateTimer(delta);

            if (!context.HasPatrolDestination || context.GlobalPosition.DistanceTo(context.CurrentPatrolDestination) <= _arrivalDistance)
            {
                context.ChooseRandomPatrolDestination();
            }

            Vector2 direction = context.CurrentPatrolDestination - context.GlobalPosition;
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
                context.ClearPatrolDestination();
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
