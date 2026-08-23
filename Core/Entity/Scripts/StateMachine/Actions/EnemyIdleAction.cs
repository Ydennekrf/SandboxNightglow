using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemyIdleAction : IStateAction
    {
        private readonly float _durationSeconds;
        private readonly string _animationKey;

        public EnemyIdleAction(float durationSeconds, string animationKey = "Idle")
        {
            _durationSeconds = Mathf.Max(0f, durationSeconds);
            _animationKey = animationKey;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Idle");
            context.PlayEnemyAnimation(_animationKey);
            context.StartStateTimer(_durationSeconds);
            context.SetDesiredVelocity(Vector2.Zero);
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Idle");
            context.TickStateTimer(delta);
            context.SetDesiredVelocity(Vector2.Zero);
        }

        public void Exit(Entity owner)
        {
        }

        private static bool TryGetContext(Entity owner, out IEnemyBehaviorContext context)
        {
            context = owner is Enemy enemy ? enemy.BehaviorContext : null;
            return context != null;
        }
    }
}
