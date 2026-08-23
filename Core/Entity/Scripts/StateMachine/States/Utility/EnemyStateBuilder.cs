using System.Collections.Generic;
using ethra.V1.Actions;
using ethra.V1.Transitions;

namespace ethra.V1
{
    public static class EnemyStateBuilder
    {
        public static List<BaseState> BuildTestEnemyStates(
            Enemy enemy,
            float idleDurationSeconds,
            float patrolDurationSeconds,
            float patrolMoveSpeed,
            float pursueMoveSpeed,
            float attackDamage,
            float attackDurationSeconds,
            float attackActiveDurationSeconds,
            float hurtDurationSeconds,
            string attackDamageType = "Physical",
            string attackAbilityId = "EnemyBasicAttack",
            string spawnAnimationKey = "Spawn",
            string idleAnimationKey = "Idle",
            string patrolAnimationKey = "Patrol",
            string pursueAnimationKey = "Pursue",
            string attackAnimationKey = "Attack",
            string hurtAnimationKey = "Hurt",
            string dieAnimationKey = "Die")
        {
            BaseState spawn = new("Spawn", enemy, new List<IStateAction>
            {
                new EnemySpawnAction(spawnAnimationKey)
            }, new List<IStateTransition>());

            BaseState idle = new("Idle", enemy, new List<IStateAction>
            {
                new EnemyIdleAction(idleDurationSeconds, idleAnimationKey)
            }, new List<IStateTransition>());

            BaseState patrol = new("Patrol", enemy, new List<IStateAction>
            {
                new ethra.V1.Actions.EnemyPatrolAction(patrolMoveSpeed, patrolDurationSeconds, animationKey: patrolAnimationKey)
            }, new List<IStateTransition>());

            BaseState pursue = new("Pursue", enemy, new List<IStateAction>
            {
                new EnemyPursueAction(pursueMoveSpeed, pursueAnimationKey)
            }, new List<IStateTransition>());

            BaseState attack = new("Attack", enemy, new List<IStateAction>
            {
                new EnemyBasicAttackAction(
                    attackDamage,
                    attackDurationSeconds,
                    attackActiveDurationSeconds,
                    attackDamageType,
                    attackAbilityId,
                    attackAnimationKey)
            }, new List<IStateTransition>());

            BaseState hurt = new("Hurt", enemy, new List<IStateAction>
            {
                new EnemyHurtAction(hurtDurationSeconds, hurtAnimationKey)
            }, new List<IStateTransition>());

            BaseState die = new("Dead", enemy, new List<IStateAction>
            {
                new EnemyDieAction(dieAnimationKey)
            }, new List<IStateTransition>());

            spawn.Transitions.Add(new EnemyConditionTransition(die, EnemyTransitionCondition.Dead));
            spawn.Transitions.Add(new EnemyConditionTransition(idle, EnemyTransitionCondition.SpawnComplete));

            AddSharedAwakeTransitions(idle, die, hurt, attack, pursue);
            idle.Transitions.Add(new EnemyConditionTransition(patrol, EnemyTransitionCondition.IdleComplete));

            AddSharedAwakeTransitions(patrol, die, hurt, attack, pursue);
            patrol.Transitions.Add(new EnemyConditionTransition(idle, EnemyTransitionCondition.PatrolComplete));

            pursue.Transitions.Add(new EnemyConditionTransition(die, EnemyTransitionCondition.Dead));
            pursue.Transitions.Add(new EnemyConditionTransition(hurt, EnemyTransitionCondition.HurtRequested));
            pursue.Transitions.Add(new EnemyConditionTransition(attack, EnemyTransitionCondition.PlayerInAttackRange));
            pursue.Transitions.Add(new EnemyConditionTransition(patrol, EnemyTransitionCondition.PlayerLost));

            attack.Transitions.Add(new EnemyConditionTransition(die, EnemyTransitionCondition.Dead));
            attack.Transitions.Add(new EnemyConditionTransition(hurt, EnemyTransitionCondition.HurtRequested));
            attack.Transitions.Add(new EnemyConditionTransition(pursue, EnemyTransitionCondition.AttackComplete));

            hurt.Transitions.Add(new EnemyConditionTransition(die, EnemyTransitionCondition.Dead));
            hurt.Transitions.Add(new EnemyConditionTransition(pursue, EnemyTransitionCondition.HurtComplete));

            return new List<BaseState> { spawn, idle, patrol, pursue, attack, hurt, die };
        }

        private static void AddSharedAwakeTransitions(
            BaseState state,
            BaseState die,
            BaseState hurt,
            BaseState attack,
            BaseState pursue)
        {
            state.Transitions.Add(new EnemyConditionTransition(die, EnemyTransitionCondition.Dead));
            state.Transitions.Add(new EnemyConditionTransition(hurt, EnemyTransitionCondition.HurtRequested));
            state.Transitions.Add(new EnemyConditionTransition(attack, EnemyTransitionCondition.PlayerInAttackRange));
            state.Transitions.Add(new EnemyConditionTransition(pursue, EnemyTransitionCondition.PlayerDetected));
        }
    }
}
