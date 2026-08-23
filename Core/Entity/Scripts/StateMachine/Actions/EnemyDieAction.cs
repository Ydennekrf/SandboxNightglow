using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemyDieAction : IStateAction
    {
        private readonly string _animationKey;

        public EnemyDieAction(string animationKey = "Die")
        {
            _animationKey = animationKey;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Dead");
            context.PlayEnemyAnimation(_animationKey);
            context.SetDesiredVelocity(Vector2.Zero);
            context.EndAttack();
            context.EnterDeadState();
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Dead");
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
