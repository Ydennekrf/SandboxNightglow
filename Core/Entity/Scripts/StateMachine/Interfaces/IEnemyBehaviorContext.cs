using Godot;

namespace ethra.V1
{
    public enum EnemySpawnMode
    {
        Active,
        Timed,
        Proximity
    }

    public enum EnemyTransitionCondition
    {
        SpawnComplete,
        IdleComplete,
        PatrolComplete,
        PlayerDetected,
        PlayerInAttackRange,
        PlayerLost,
        AttackComplete,
        HurtRequested,
        HurtComplete,
        Dead
    }

    public interface IEnemyBehaviorContext
    {
        EnemySpawnMode SpawnMode { get; }
        float SpawnDelaySeconds { get; }
        float SpawnProximityDistance { get; }
        float DetectionRange { get; }
        float LeashRange { get; }
        float AttackRange { get; }
        float PatrolRadius { get; }
        Vector2 PatrolCenter { get; }

        bool IsSpawned { get; }
        bool IsDead { get; }
        bool IsStateTimerComplete { get; }
        bool IsAttackComplete { get; }
        bool IsHurtRequested { get; }
        bool IsHurtComplete { get; }
        bool HasPlayerInDetectionRange { get; }
        bool HasPlayerInAttackRange { get; }
        bool HasPlayerBeyondLeash { get; }

        Vector2 GlobalPosition { get; }
        Vector2 PlayerGlobalPosition { get; }
        Vector2 CurrentPatrolDestination { get; }
        bool HasPatrolDestination { get; }

        void SetStateText(string stateText);
        void PlayEnemyAnimation(string animationKey);
        void SetSpawned(bool spawned);
        void ActivateSpawn();
        void StartStateTimer(float durationSeconds);
        void TickStateTimer(float delta);
        void ClearHurtRequest();
        void MarkHurtComplete(bool complete);
        void MarkAttackComplete(bool complete);
        void ChooseRandomPatrolDestination();
        void ClearPatrolDestination();
        void SetDesiredVelocity(Vector2 velocity);
        void FaceMovement(Vector2 direction);
        void BeginAttack(float damage, string damageType, string abilityId, float activeDurationSeconds);
        void EndAttack();
        void EnterDeadState();
    }
}
