using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemyBasicAttackAction : IStateAction
    {
        private readonly float _damage;
        private readonly float _durationSeconds;
        private readonly float _activeDurationSeconds;
        private readonly string _damageType;
        private readonly string _abilityId;
        private readonly string _animationKey;
        private float _remainingSeconds;

        public EnemyBasicAttackAction(
            float damage,
            float durationSeconds,
            float activeDurationSeconds,
            string damageType = "Physical",
            string abilityId = "EnemyBasicAttack",
            string animationKey = "Attack")
        {
            _damage = Mathf.Max(1f, damage);
            _durationSeconds = Mathf.Max(0.05f, durationSeconds);
            _activeDurationSeconds = Mathf.Clamp(activeDurationSeconds, 0.01f, _durationSeconds);
            _damageType = string.IsNullOrWhiteSpace(damageType) ? "Physical" : damageType;
            _abilityId = string.IsNullOrWhiteSpace(abilityId) ? "EnemyBasicAttack" : abilityId;
            _animationKey = animationKey;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            _remainingSeconds = _durationSeconds;
            context.SetStateText("Attack");
            context.PlayEnemyAnimation(_animationKey);
            context.MarkAttackComplete(false);
            context.SetDesiredVelocity(Vector2.Zero);
            context.BeginAttack(_damage, _damageType, _abilityId, _activeDurationSeconds);
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetStateText("Attack");
            context.SetDesiredVelocity(Vector2.Zero);
            _remainingSeconds = Mathf.Max(0f, _remainingSeconds - delta);

            if (_remainingSeconds <= 0f)
            {
                context.MarkAttackComplete(true);
            }
        }

        public void Exit(Entity owner)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.EndAttack();
            context.MarkAttackComplete(false);
        }

        private static bool TryGetContext(Entity owner, out IEnemyBehaviorContext context)
        {
            context = owner is Enemy enemy ? enemy.BehaviorContext : null;
            return context != null;
        }
    }
}
