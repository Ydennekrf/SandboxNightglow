using Godot;

namespace ethra.V1
{
    public partial class PlayerHud : Control
    {
        [Export] public NodePath HealthLabelPath { get; set; } = "Panel/Stack/TopRow/HealthLabel";
        [Export] public NodePath ManaLabelPath { get; set; } = "Panel/Stack/TopRow/ManaLabel";
        [Export] public NodePath LevelLabelPath { get; set; } = "Panel/Stack/BottomRow/LevelLabel";
        [Export] public NodePath ExperienceBarPath { get; set; } = "Panel/Stack/ExperienceBar";

        private TextureProgressBar _healthLabel;
        private TextureProgressBar _manaLabel;
        private Label _levelLabel;
        private ProgressBar _experienceBar;
        private Player _player;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            ResolveNodes();
            Refresh();
        }

        public override void _ExitTree()
        {
            UnbindPlayer();
        }

        public void BindPlayer(Player player)
        {
            if (_player == player)
            {
                Refresh();
                return;
            }

            UnbindPlayer();
            _player = player;

            if (_player != null)
            {
                _player.HealthChanged += OnHealthChanged;
                _player.ManaChanged += OnManaChanged;
                _player.Progression.ExperienceChanged += OnExperienceChanged;
                _player.Progression.LevelChanged += OnLevelChanged;
                _player.Progression.LeveledUp += OnLeveledUp;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_player == null)
            {
                SetHealth(0, 0);
                SetMana(0, 0);
                SetLevel(1);
                SetExperience(0, ExperienceCurve.GetExperienceToNextLevel(1));
                return;
            }

            SetHealth(_player.CurHP, _player.MaxHP);
            SetMana(_player.CurMana, _player.MaxMana);
            SetLevel(_player.Progression.Level);
            SetExperience(_player.Progression.CurrentExperience, _player.Progression.ExperienceToNextLevel);
        }

        public void SetHealth(int current, int max)
        {
            if (_healthLabel != null)
            {
                
                _healthLabel.Value = current;
            }
        }

        public void SetMana(int current, int max)
        {
            if (_manaLabel != null)
            {
                _manaLabel.Value = current;
            }
        }

        public void SetLevel(int level)
        {
            if (_levelLabel != null)
            {
                _levelLabel.Text = $"LV {Mathf.Max(1, level)}";
            }
        }

        public void SetExperience(int currentXp, int xpToNextLevel)
        {
            int safeRequired = Mathf.Max(1, xpToNextLevel);
            int safeCurrent = Mathf.Clamp(currentXp, 0, safeRequired);

            if (_experienceBar != null)
            {
                _experienceBar.MaxValue = safeRequired;
                _experienceBar.Value = safeCurrent;
            }
        }

        private void ResolveNodes()
        {
            _healthLabel = GetNodeOrNull<TextureProgressBar>(HealthLabelPath);
            _manaLabel = GetNodeOrNull<TextureProgressBar>(ManaLabelPath);
            _levelLabel = GetNodeOrNull<Label>(LevelLabelPath);
            _experienceBar = GetNodeOrNull<ProgressBar>(ExperienceBarPath);

            if (_healthLabel == null || _manaLabel == null || _levelLabel == null || _experienceBar == null)
            {
                GD.PushWarning("PlayerHud: packed scene node paths are incomplete; building fallback HUD layout.");
                
            }
        }

        private void UnbindPlayer()
        {
            if (_player == null)
            {
                return;
            }

            _player.HealthChanged -= OnHealthChanged;
            _player.ManaChanged -= OnManaChanged;
            _player.Progression.ExperienceChanged -= OnExperienceChanged;
            _player.Progression.LevelChanged -= OnLevelChanged;
            _player.Progression.LeveledUp -= OnLeveledUp;
            _player = null;
        }

        private void OnHealthChanged(int current, int max) => SetHealth(current, max);
        private void OnManaChanged(int current, int max) => SetMana(current, max);
        private void OnExperienceChanged(PlayerExperienceChangedEvent evt) => SetExperience(evt.CurrentExperience, evt.ExperienceToNextLevel);
        private void OnLevelChanged(PlayerLevelChangedEvent evt) => SetLevel(evt.Level);

        private void OnLeveledUp(PlayerLevelUpEvent evt)
        {
            GameManager.Instance?.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest($"Level up! LV {evt.NewLevel}", NotificationType.Info));
        }
    }
}
