using Godot;

namespace ethra.V1
{
    public partial class TestEnemyNode : CharacterBody2D
    {
        [Export] public string DebugName { get; set; } = "TestEnemy";
        [Export] public int MaxHealth { get; set; } = 100;
        [Export] public int StartingHealth { get; set; } = 100;
        [Export] public int LootTableId { get; set; } = 0;
        [Export] public bool DisableOnDefeat { get; set; } = true;
        [Export] public bool ShowDebugLogs { get; set; } = true;

        [Export] public NodePath HurtBoxPath { get; set; } = "HurtBox";
        [Export] public NodePath HealthBarPath { get; set; } = "HealthBar";
        [Export] public NodePath HealthLabelPath { get; set; } = "HealthLabel";
        [Export] public NodePath BodyCollisionPath { get; set; } = "CollisionShape2D";

        private Enemy _enemy;
        private HurtBox _hurtBox;
        private Range _healthBar;
        private Label _healthLabel;
        private CollisionShape2D _bodyCollision;
        private bool _defeated;

        public Enemy EnemyModel => _enemy;
        public int CurrentHealth => _enemy is IStats stats ? stats.CurHP : 0;

        public override void _Ready()
        {
            _hurtBox = GetNodeOrNull<HurtBox>(HurtBoxPath);
            _healthBar = GetNodeOrNull<Range>(HealthBarPath);
            _healthLabel = GetNodeOrNull<Label>(HealthLabelPath);
            _bodyCollision = GetNodeOrNull<CollisionShape2D>(BodyCollisionPath);

            CreateEnemyModel();
            BindHurtBox();
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

            if (!_defeated && _enemy is IStats stats && stats.CurHP <= 0)
            {
                Defeat();
            }
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
            stats.Strength = 1;
            stats.Dexterity = 1;
            stats.Intelligence = 1;
            stats.Spirit = 1;
            stats.Vitality = 1;
            stats.Luck = 1;
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

        private void OnHitResolved(Entity source, Entity target, float damage, bool crit, string damageType, string element)
        {
            if (target != _enemy || _hurtBox == null || damage <= 0f)
            {
                return;
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

        private void Defeat()
        {
            _defeated = true;
            DropLoot();

            if (ShowDebugLogs)
            {
                GD.Print($"[TestEnemy] {DebugName} defeated.");
            }

            if (!DisableOnDefeat)
            {
                return;
            }

            Modulate = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            SetProcess(false);

            if (_hurtBox != null)
            {
                _hurtBox.Monitoring = false;
                _hurtBox.Monitorable = false;
            }

            if (_bodyCollision != null)
            {
                _bodyCollision.Disabled = true;
            }
        }

        public void DropLoot()
        {
            if (ShowDebugLogs)
            {
                GD.Print($"[TestEnemy] DropLoot stub invoked for loot table {LootTableId}.");
            }
        }
    }
}
