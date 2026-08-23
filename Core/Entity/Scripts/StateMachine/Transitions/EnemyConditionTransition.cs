namespace ethra.V1.Transitions
{
    public sealed class EnemyConditionTransition : IStateTransition
    {
        private readonly EnemyTransitionCondition _condition;

        public BaseState Target { get; }

        public EnemyConditionTransition(BaseState target, EnemyTransitionCondition condition)
        {
            Target = target;
            _condition = condition;
        }

        public bool ShouldTransition(Entity owner)
        {
            if (owner is not Enemy enemy || enemy.BehaviorContext == null)
            {
                return false;
            }

            IEnemyBehaviorContext context = enemy.BehaviorContext;
            return _condition switch
            {
                EnemyTransitionCondition.SpawnComplete => context.IsSpawned && context.IsStateTimerComplete,
                EnemyTransitionCondition.IdleComplete => context.IsStateTimerComplete,
                EnemyTransitionCondition.PatrolComplete => context.IsStateTimerComplete,
                EnemyTransitionCondition.PlayerDetected => context.IsSpawned && context.HasPlayerInDetectionRange,
                EnemyTransitionCondition.PlayerInAttackRange => context.IsSpawned && context.HasPlayerInAttackRange,
                EnemyTransitionCondition.PlayerLost => context.HasPlayerBeyondLeash,
                EnemyTransitionCondition.AttackComplete => context.IsAttackComplete,
                EnemyTransitionCondition.HurtRequested => context.IsHurtRequested,
                EnemyTransitionCondition.HurtComplete => context.IsHurtComplete,
                EnemyTransitionCondition.Dead => context.IsDead,
                _ => false
            };
        }
    }
}
