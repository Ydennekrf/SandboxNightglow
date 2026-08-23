using Godot;

namespace ethra.V1.Actions
{
    public sealed class EnemySpawnAction : IStateAction
    {
        private readonly string _animationKey;
        private readonly float _spawnPresentationSeconds;
        private float _delayRemaining;
        private float _presentationRemaining;
        private bool _spawnTriggered;

        public EnemySpawnAction(string animationKey = "Spawn", float spawnPresentationSeconds = 0.35f)
        {
            _animationKey = animationKey;
            _spawnPresentationSeconds = Mathf.Max(0f, spawnPresentationSeconds);
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            _delayRemaining = Mathf.Max(0f, context.SpawnDelaySeconds);
            _presentationRemaining = _spawnPresentationSeconds;
            _spawnTriggered = context.SpawnMode == EnemySpawnMode.Active;
            context.MarkAttackComplete(false);
            context.MarkHurtComplete(false);
            context.SetDesiredVelocity(Vector2.Zero);

            if (_spawnTriggered)
            {
                context.ActivateSpawn();
            }
            else
            {
                context.SetSpawned(false);
            }
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (!TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                return;
            }

            context.SetDesiredVelocity(Vector2.Zero);

            if (!_spawnTriggered)
            {
                if (context.SpawnMode == EnemySpawnMode.Timed)
                {
                    _delayRemaining = Mathf.Max(0f, _delayRemaining - delta);
                    _spawnTriggered = _delayRemaining <= 0f;
                }
                else if (context.SpawnMode == EnemySpawnMode.Proximity)
                {
                    _spawnTriggered = context.PlayerGlobalPosition.DistanceTo(context.GlobalPosition) <= context.SpawnProximityDistance;
                }

                if (_spawnTriggered)
                {
                    context.ActivateSpawn();
                }
            }

            if (!_spawnTriggered)
            {
                return;
            }

            context.SetStateText("Spawning");
            context.PlayEnemyAnimation(_animationKey);
            _presentationRemaining = Mathf.Max(0f, _presentationRemaining - delta);

            if (_presentationRemaining <= 0f)
            {
                context.StartStateTimer(0f);
            }
        }

        public void Exit(Entity owner)
        {
            if (TryGetContext(owner, out IEnemyBehaviorContext context))
            {
                context.SetSpawned(true);
            }
        }

        private static bool TryGetContext(Entity owner, out IEnemyBehaviorContext context)
        {
            context = owner is Enemy enemy ? enemy.BehaviorContext : null;
            return context != null;
        }
    }
}
