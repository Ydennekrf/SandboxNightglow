using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemyHurtAction : IStateAction
    {
        private readonly float _durationSeconds;
        private readonly string _animationKey;

        public EnemyHurtAction(float durationSeconds, string animationKey = "Hurt")
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

            context.SetStateText("Hurt");
            context.PlayEnemyAnimation(_animationKey);
            context.ClearHurtRequest();
            context.MarkHurtComplete(false);
            context.StartStateTimer(_durationSeconds);
            context.SetDesiredVelocity(Vector2.Zero);
            context.EndAttack();
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Hurt");
            context.SetDesiredVelocity(Vector2.Zero);
            context.TickStateTimer(delta);
            context.MarkHurtComplete(context.IsStateTimerComplete);
        }

        public void Exit(Entity owner)
        {
            if (TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                context.MarkHurtComplete(false);
            }
        }

        private static bool TryGetContext(Entity owner, out IEnemyBehaviorContext context)
        {
            context = owner is Enemy enemy ? enemy.BehaviorContext : null;
            return context != null;
        }
    }
}
