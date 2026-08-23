using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class TestEnemyNode : CharacterBody2D, IEnemyBehaviorContext
    {
        [Export] public EnemyDefinitionResource EnemyDefinition;

        [Export] public string DebugName = "TestEnemy";
        [Export] public int MaxHealth = 3;
        [Export] public int StartingHealth = 3;
        [Export] public int ExperienceReward = 25;
        [Export] public bool DisableOnDefeat = true;
        [Export] public bool ShowDebugLogs = true;

        [ExportGroup("Enemy Behavior")]
        [Export(PropertyHint.Enum, "Active,Timed,Proximity")] public int SpawnModeValue = (int)EnemySpawnMode.Timed;
        [Export] public float SpawnDelaySeconds = 1f;
        [Export] public float SpawnProximityDistance = 96f;
        [Export] public float IdleDurationSeconds = 1.25f;
        [Export] public float PatrolDurationSeconds = 2.5f;
        [Export] public float PatrolRadius = 96f;
        [Export] public Vector2 PatrolCenterOffset = Vector2.Zero;
        [Export] public float PatrolMoveSpeed = 45f;
        [Export] public float PursueMoveSpeed = 80f;
        [Export] public float DetectionRange = 128f;
        [Export] public float LeashRange = 192f;
        [Export] public float AttackRange = 28f;
        [Export] public float AttackDamage = 6f;
        [Export] public string AttackDamageType = "Physical";
        [Export] public string AttackAbilityId = "EnemyBasicAttack";
        [Export] public float AttackCooldownSeconds = 1.1f;
        [Export] public float AttackWindupSeconds = 0.16f;
        [Export] public float AttackDurationSeconds = 0.45f;
        [Export] public float AttackActiveDurationSeconds = 0.12f;
        [Export] public float HurtDurationSeconds = 0.25f;

        [ExportGroup("Enemy Stats")]
        [Export] public int Strength = 1;
        [Export] public int Dexterity = 1;
        [Export] public int Intelligence = 1;
        [Export] public int Spirit = 1;
        [Export] public int Vitality = 1;
        [Export] public int Luck = 1;

        [ExportGroup("Animation Keys")]
        [Export] public string SpawnAnimationKey = "Spawn";
        [Export] public string IdleAnimationKey = "Idle";
        [Export] public string PatrolAnimationKey = "Patrol";
        [Export] public string PursueAnimationKey = "Pursue";
        [Export] public string AttackAnimationKey = "Attack";
        [Export] public string HurtAnimationKey = "Hurt";
        [Export] public string DieAnimationKey = "Die";

        [ExportGroup("Node Paths")]

        [Export] public NodePath HurtBoxPath = "HurtBox";
        [Export] public NodePath AttackHitBoxPath = "AttackHitBox";
        [Export] public NodePath HealthBarPath = "HealthBar";
        [Export] public NodePath HealthLabelPath = "HealthLabel";
        [Export] public NodePath StateLabelPath = "StateLabel";
        [Export] public NodePath BodyCollisionPath = "CollisionShape2D";
        [Export] public NodePath AnimationPlayerPath = "AnimationPlayer";
        [Export] public NodePath LootDropperPath = "LootDropper";

        private Enemy _enemy;
        private HurtBox _hurtBox;
        private HitBox _attackHitBox;
        private Range _healthBar;
        private Label _healthLabel;
        private Label _stateLabel;
        private CollisionShape2D _bodyCollision;
        private AnimationPlayer _animationPlayer;
        private LootDropper _lootDropper;
        private readonly RandomNumberGenerator _rng = new();
        private readonly HashSet<string> _missingAnimationWarnings = new();
        private Vector2 _patrolCenter;
        private Vector2 _patrolDestination;
        private float _stateTimerRemaining;
        private float _attackCooldownRemaining;
        private float _attackWindupRemaining;
        private float _attackActiveRemaining;
        private bool _attackWaitingForWindup;
        private bool _hasPatrolDestination;
        private bool _stateTimerComplete;
        private bool _attackComplete;
        private bool _hurtRequested;
        private bool _hurtComplete;
        private bool _spawned;
        private bool _defeated;
        private bool _definitionApplied;
        private Player _lastPlayerDamageSource;

        public Enemy EnemyModel => _enemy;
        public int CurrentHealth => _enemy is IStats stats ? stats.CurHP : 0;
        public EnemySpawnMode SpawnMode => (EnemySpawnMode)SpawnModeValue;
        EnemySpawnMode IEnemyBehaviorContext.SpawnMode => SpawnMode;
        float IEnemyBehaviorContext.SpawnDelaySeconds => SpawnDelaySeconds;
        float IEnemyBehaviorContext.SpawnProximityDistance => SpawnProximityDistance;
        float IEnemyBehaviorContext.DetectionRange => DetectionRange;
        float IEnemyBehaviorContext.LeashRange => LeashRange;
        float IEnemyBehaviorContext.AttackRange => AttackRange;
        float IEnemyBehaviorContext.PatrolRadius => PatrolRadius;
        public bool IsSpawned => _spawned;
        public bool IsDead => _defeated || (_enemy is IStats stats && stats.CurHP <= 0);
        public bool IsStateTimerComplete => _stateTimerComplete;
        public bool IsAttackComplete => _attackComplete;
        public bool IsHurtRequested => _hurtRequested && !IsDead;
        public bool IsHurtComplete => _hurtComplete;
        public bool HasPlayerInDetectionRange => IsSpawned && DistanceToPlayer() <= DetectionRange;
        public bool HasPlayerInAttackRange => IsSpawned && !IsStatusStunned() && _attackCooldownRemaining <= 0f && DistanceToPlayer() <= AttackRange;
        public bool HasPlayerBeyondLeash => !HasPlayerInDetectionRange || GlobalPosition.DistanceTo(PatrolCenter) > LeashRange;
        public Vector2 PlayerGlobalPosition => GetPlayerNode()?.GlobalPosition ?? GlobalPosition;
        public Vector2 CurrentPatrolDestination => _patrolDestination;
        public bool HasPatrolDestination => _hasPatrolDestination;
        public Vector2 PatrolCenter => _patrolCenter;

        public override void _Ready()
        {
            ApplyEnemyDefinition();

            _hurtBox = GetNodeOrNull<HurtBox>(HurtBoxPath);
            _attackHitBox = GetNodeOrNull<HitBox>(AttackHitBoxPath);
            _healthBar = GetNodeOrNull<Range>(HealthBarPath);
            _healthLabel = GetNodeOrNull<Label>(HealthLabelPath);
            _stateLabel = GetNodeOrNull<Label>(StateLabelPath);
            _bodyCollision = GetNodeOrNull<CollisionShape2D>(BodyCollisionPath);
            _animationPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
            _lootDropper = GetNodeOrNull<LootDropper>(LootDropperPath);
            _patrolCenter = GlobalPosition + PatrolCenterOffset;
            _rng.Randomize();

            CreateEnemyModel();
            BindHurtBox();
            BindAttackHitBox();
            BuildEnemyStateMachine();
            UpdateHealthDisplay();

            CombatFeedbackBus.HitResolved += OnHitResolved;
            AddToGroup("Enemy");
            AddToGroup("DebugEnemy");
        }

        public override void _ExitTree()
        {
            CombatFeedbackBus.HitResolved -= OnHitResolved;
        }

        public override void _Process(double delta)
        {
            UpdateHealthDisplay();
        }

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;
            _attackCooldownRemaining = Mathf.Max(0f, _attackCooldownRemaining - dt);
            TickAttackWindow(dt);

            if (_enemy == null)
            {
                return;
            }

            _enemy.Tick(dt);

            Vector2 desiredVelocity = _enemy.DesiredVelocity;
            if (GameManager.Instance?.Combat != null)
            {
                desiredVelocity = GameManager.Instance.Combat.ResolveMovementVelocity(_enemy, desiredVelocity);
            }

            Velocity = IsDead || !IsSpawned ? Vector2.Zero : desiredVelocity;
            MoveAndSlide();
        }

        private void CreateEnemyModel()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                GD.PushWarning($"{DebugName}: GameManager is missing; TestEnemy cannot receive combat damage until a manager exists.");
                return;
            }

            _enemy = gameManager.CreateEnemy(DebugName, 1, gameManager, gameManager, new StateMachine());
            _enemy.SetName(DebugName);
            _enemy.BehaviorContext = this;
            ConfigureStats(_enemy);

            if (!gameManager.registeredEnemies.Contains(_enemy))
            {
                gameManager.registeredEnemies.Add(_enemy);
            }
        }

        private void ConfigureStats(IStats stats)
        {
            if (stats == null)
            {
                return;
            }

            stats.MaxHP = Mathf.Max(1, MaxHealth);
            stats.CurHP = Mathf.Clamp(StartingHealth, 1, stats.MaxHP);
            stats.MaxMana = 0;
            stats.CurMana = 0;
            stats.Strength = Strength;
            stats.Dexterity = Dexterity;
            stats.Intelligence = Intelligence;
            stats.Spirit = Spirit;
            stats.Vitality = Vitality;
            stats.Luck = Luck;
        }

        private void BindHurtBox()
        {
            if (_hurtBox == null)
            {
                GD.PushWarning($"{DebugName}: HurtBox node is missing.");
                return;
            }

            _hurtBox.Bind(_enemy);
        }

        private void BindAttackHitBox()
        {
            if (_attackHitBox == null)
            {
                GD.PushWarning($"{DebugName}: AttackHitBox node is missing; enemy attacks will not deal damage.");
                return;
            }

            _attackHitBox.Bind(_enemy);
            _attackHitBox.Deactivate();
        }

        private void BuildEnemyStateMachine()
        {
            if (_enemy == null)
            {
                return;
            }

            List<BaseState> states = EnemyStateBuilder.BuildTestEnemyStates(
                _enemy,
                IdleDurationSeconds,
                PatrolDurationSeconds,
                PatrolMoveSpeed,
                PursueMoveSpeed,
                AttackDamage,
                AttackDurationSeconds,
                AttackActiveDurationSeconds,
                HurtDurationSeconds,
                AttackDamageType,
                AttackAbilityId,
                SpawnAnimationKey,
                IdleAnimationKey,
                PatrolAnimationKey,
                PursueAnimationKey,
                AttackAnimationKey,
                HurtAnimationKey,
                DieAnimationKey);

            _enemy.SetStates(states);
            _enemy.SetInitialState(states[0]);
            _enemy.StartStateMachine();
        }

        private void OnHitResolved(Entity source, Entity target, float damage, bool crit, string damageType, string element)
        {
            if (target != _enemy || _hurtBox == null || damage <= 0f)
            {
                return;
            }

            if (source is Player playerSource)
            {
                _lastPlayerDamageSource = playerSource;
            }

            if (!IsDead)
            {
                _hurtRequested = true;
            }

            if (_hurtBox.LastPopupPhysicsFrame == Engine.GetPhysicsFrames())
            {
                return;
            }

            _hurtBox.ShowDamagePopup(Mathf.RoundToInt(damage));
        }

        private void UpdateHealthDisplay()
        {
            if (_enemy is not IStats stats)
            {
                return;
            }

            if (_healthBar != null)
            {
                _healthBar.MaxValue = stats.MaxHP;
                _healthBar.Value = stats.CurHP;
            }

            if (_healthLabel != null)
            {
                _healthLabel.Text = $"{stats.CurHP}/{stats.MaxHP}";
            }
        }

        public void SetStateText(string stateText)
        {
            if (_stateLabel != null)
            {
                _stateLabel.Text = FormatStateText(stateText);
            }
        }

        public void PlayEnemyAnimation(string animationKey)
        {
            if (_animationPlayer == null || string.IsNullOrWhiteSpace(animationKey))
            {
                return;
            }

            if (_animationPlayer.HasAnimation(animationKey))
            {
                _animationPlayer.Play(animationKey);
                return;
            }

            if (_missingAnimationWarnings.Add(animationKey))
            {
                GD.PushWarning($"{DebugName}: animation '{animationKey}' is not available yet.");
            }
        }

        public void SetSpawned(bool spawned)
        {
            _spawned = spawned;
            Visible = spawned;

            if (_hurtBox != null)
            {
                _hurtBox.Monitoring = spawned && !IsDead;
                _hurtBox.Monitorable = spawned && !IsDead;
            }

            if (_bodyCollision != null)
            {
                _bodyCollision.Disabled = !spawned || IsDead;
            }

            if (_attackHitBox != null && !spawned)
            {
                _attackHitBox.Deactivate();
            }
        }

        public void ActivateSpawn()
        {
            SetSpawned(true);
            SetStateText("Spawning");
        }

        public void StartStateTimer(float durationSeconds)
        {
            _stateTimerRemaining = Mathf.Max(0f, durationSeconds);
            _stateTimerComplete = _stateTimerRemaining <= 0f;
        }

        public void TickStateTimer(float delta)
        {
            if (_stateTimerComplete)
            {
                return;
            }

            _stateTimerRemaining = Mathf.Max(0f, _stateTimerRemaining - delta);
            _stateTimerComplete = _stateTimerRemaining <= 0f;
        }

        public void ClearHurtRequest()
        {
            _hurtRequested = false;
        }

        public void MarkHurtComplete(bool complete)
        {
            _hurtComplete = complete;
        }

        public void MarkAttackComplete(bool complete)
        {
            _attackComplete = complete;
        }

        public void ChooseRandomPatrolDestination()
        {
            float radius = Mathf.Max(0f, PatrolRadius);
            float angle = _rng.RandfRange(0f, Mathf.Tau);
            float distance = Mathf.Sqrt(_rng.Randf()) * radius;
            _patrolDestination = PatrolCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            _hasPatrolDestination = true;
        }

        public void ClearPatrolDestination()
        {
            _hasPatrolDestination = false;
        }

        public void SetDesiredVelocity(Vector2 velocity)
        {
            if (_enemy != null)
            {
                _enemy.DesiredVelocity = velocity;
            }
        }

        public void FaceMovement(Vector2 direction)
        {
            if (_enemy == null || direction.LengthSquared() <= 0.001f)
            {
                return;
            }

            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
            {
                _enemy.Facing = direction.X >= 0f ? FacingDirection.Right : FacingDirection.Left;
            }
            else
            {
                _enemy.Facing = direction.Y >= 0f ? FacingDirection.Down : FacingDirection.Up;
            }
        }

        public void BeginAttack(float damage, string damageType, string abilityId, float activeDurationSeconds)
        {
            if (_attackHitBox == null || _enemy == null)
            {
                return;
            }

            if (IsStatusStunned())
            {
                MarkAttackComplete(true);
                return;
            }

            FaceMovement(PlayerGlobalPosition - GlobalPosition);
            PositionAttackHitBox();
            GameManager.Instance?.Audio?.PlayEntity("sound.enemy.attack", this);
            _attackHitBox.Bind(_enemy);
            _attackHitBox.DamageAmount = Mathf.Max(1f, damage);
            _attackHitBox.DamageType = string.IsNullOrWhiteSpace(damageType) ? "Physical" : damageType;
            _attackHitBox.AbilityId = string.IsNullOrWhiteSpace(abilityId) ? "EnemyBasicAttack" : abilityId;
            _attackWaitingForWindup = true;
            _attackWindupRemaining = Mathf.Max(0f, AttackWindupSeconds);
            _attackActiveRemaining = Mathf.Max(0.01f, activeDurationSeconds);
            if (_attackWindupRemaining <= 0f)
            {
                ActivateAttackHitBox();
            }

            _attackCooldownRemaining = Mathf.Max(_attackCooldownRemaining, AttackCooldownSeconds);
        }

        public void ApplyEnemyDefinition()
        {
            if (_definitionApplied || EnemyDefinition == null)
            {
                return;
            }

            _definitionApplied = true;
            DebugName = EnemyDefinition.EnemyName;
            MaxHealth = EnemyDefinition.MaxHealth;
            StartingHealth = EnemyDefinition.StartingHealth;
            ExperienceReward = EnemyDefinition.ExperienceReward;
            DisableOnDefeat = EnemyDefinition.DisableOnDefeat;
            Strength = EnemyDefinition.Strength;
            Dexterity = EnemyDefinition.Dexterity;
            Intelligence = EnemyDefinition.Intelligence;
            Spirit = EnemyDefinition.Spirit;
            Vitality = EnemyDefinition.Vitality;
            Luck = EnemyDefinition.Luck;
            SpawnModeValue = EnemyDefinition.SpawnModeValue;
            SpawnDelaySeconds = EnemyDefinition.SpawnDelaySeconds;
            SpawnProximityDistance = EnemyDefinition.SpawnProximityDistance;
            IdleDurationSeconds = EnemyDefinition.IdleDurationSeconds;
            PatrolDurationSeconds = EnemyDefinition.PatrolDurationSeconds;
            PatrolRadius = EnemyDefinition.PatrolRadius;
            PatrolCenterOffset = EnemyDefinition.PatrolCenterOffset;
            PatrolMoveSpeed = EnemyDefinition.PatrolMoveSpeed;
            PursueMoveSpeed = EnemyDefinition.PursueMoveSpeed;
            DetectionRange = EnemyDefinition.DetectionRange;
            LeashRange = EnemyDefinition.LeashRange;
            AttackRange = EnemyDefinition.AttackRange;
            AttackDamage = EnemyDefinition.AttackDamage;
            AttackDamageType = EnemyDefinition.AttackDamageType;
            AttackAbilityId = EnemyDefinition.AttackAbilityId;
            AttackCooldownSeconds = EnemyDefinition.AttackCooldownSeconds;
            AttackWindupSeconds = EnemyDefinition.AttackWindupSeconds;
            AttackDurationSeconds = EnemyDefinition.AttackDurationSeconds;
            AttackActiveDurationSeconds = EnemyDefinition.AttackActiveDurationSeconds;
            HurtDurationSeconds = EnemyDefinition.HurtDurationSeconds;
            SpawnAnimationKey = EnemyDefinition.SpawnAnimationKey;
            IdleAnimationKey = EnemyDefinition.IdleAnimationKey;
            PatrolAnimationKey = EnemyDefinition.PatrolAnimationKey;
            PursueAnimationKey = EnemyDefinition.PursueAnimationKey;
            AttackAnimationKey = EnemyDefinition.AttackAnimationKey;
            HurtAnimationKey = EnemyDefinition.HurtAnimationKey;
            DieAnimationKey = EnemyDefinition.DieAnimationKey;
        }

        public void EndAttack()
        {
            _attackWaitingForWindup = false;
            _attackWindupRemaining = 0f;
            _attackActiveRemaining = 0f;
            _attackHitBox?.Deactivate();
        }

        public void EnterDeadState()
        {
            if (_defeated)
            {
                return;
            }

            _defeated = true;
            _spawned = true;
            GameManager.Instance?.Audio?.PlayEntity("sound.enemy.death", this);
            AwardExperience();
            DropLoot();

            if (ShowDebugLogs)
            {
                GD.Print($"[TestEnemy] {DebugName} defeated.");
            }

            Modulate = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            Velocity = Vector2.Zero;
            _enemy.DesiredVelocity = Vector2.Zero;
            EndAttack();

            if (_hurtBox != null)
            {
                _hurtBox.Monitoring = false;
                _hurtBox.Monitorable = false;
            }

            if (_bodyCollision != null)
            {
                _bodyCollision.Disabled = true;
            }

            if (!DisableOnDefeat)
            {
                Visible = true;
            }
        }

        public void DropLoot()
        {
            _lootDropper?.DropLoot();
        }

        private void AwardExperience()
        {
            int reward = Mathf.Max(0, ExperienceReward);
            if (reward <= 0)
            {
                return;
            }

            Player player = _lastPlayerDamageSource ?? GameManager.Instance?.GameState?.GetPlayer();
            if (player == null)
            {
                GD.PushWarning($"[Progression] {DebugName} defeated, but no player was available for XP reward.");
                return;
            }

            GD.Print($"[Progression] Enemy defeated: enemy.debug.{DebugName.ToLowerInvariant().Replace(' ', '_')}");
            GD.Print($"[Progression] Awarded XP: {reward}");
            player.GainExperience(reward);
            GameManager.Instance?.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest($"+{reward} XP", NotificationType.Info));
        }

        private void TickAttackWindow(float delta)
        {
            if (_attackWaitingForWindup)
            {
                if (IsStatusStunned())
                {
                    EndAttack();
                    MarkAttackComplete(true);
                    return;
                }

                _attackWindupRemaining = Mathf.Max(0f, _attackWindupRemaining - delta);
                if (_attackWindupRemaining <= 0f)
                {
                    ActivateAttackHitBox();
                }

                return;
            }

            if (_attackActiveRemaining <= 0f)
            {
                return;
            }

            _attackActiveRemaining = Mathf.Max(0f, _attackActiveRemaining - delta);
            if (_attackActiveRemaining <= 0f)
            {
                _attackHitBox?.Deactivate();
            }
        }

        private void ActivateAttackHitBox()
        {
            _attackWaitingForWindup = false;
            _attackHitBox?.Activate();
        }

        private void PositionAttackHitBox()
        {
            if (_attackHitBox == null || _enemy == null)
            {
                return;
            }

            _attackHitBox.Position = _enemy.Facing switch
            {
                FacingDirection.Up => new Vector2(0f, -AttackRange),
                FacingDirection.Down => new Vector2(0f, AttackRange),
                FacingDirection.Left => new Vector2(-AttackRange, 0f),
                FacingDirection.Right => new Vector2(AttackRange, 0f),
                _ => new Vector2(0f, AttackRange)
            };
        }

        private float DistanceToPlayer()
        {
            Node2D playerNode = GetPlayerNode();
            return playerNode == null ? float.PositiveInfinity : GlobalPosition.DistanceTo(playerNode.GlobalPosition);
        }

        private Node2D GetPlayerNode()
        {
            return GetTree()?.GetFirstNodeInGroup("Player") as Node2D;
        }

        private bool IsStatusStunned()
        {
            return _enemy != null && GameManager.Instance?.Combat?.IsStunned(_enemy) == true;
        }

        private string FormatStateText(string stateText)
        {
            CombatManager combat = GameManager.Instance?.Combat;
            if (_enemy == null || combat == null)
            {
                return stateText;
            }

            IReadOnlyList<string> statuses = combat.GetActiveStatusDisplayNames(_enemy);
            if (statuses.Count == 0)
            {
                return stateText;
            }

            if (combat.IsStunned(_enemy))
            {
                return $"Stunned\n{string.Join(", ", statuses)}";
            }

            return $"{stateText}\n{string.Join(", ", statuses)}";
        }
    }
}
