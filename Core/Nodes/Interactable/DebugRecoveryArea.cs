using Godot;

namespace ethra.V1
{
    public partial class DebugRecoveryArea : Area2D
    {
        [Export] public int HealthPerTick { get; set; } = 999;
        [Export] public int ManaPerTick { get; set; } = 999;
        [Export] public float TickSeconds { get; set; } = 0.25f;
        [Export] public string NotificationText { get; set; } = "Recovered health and mana.";

        private bool _playerInside;
        private float _tickTimer;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
        }

        public override void _Process(double delta)
        {
            if (!_playerInside)
            {
                return;
            }

            _tickTimer -= (float)delta;
            if (_tickTimer > 0f)
            {
                return;
            }

            _tickTimer = Mathf.Max(0.05f, TickSeconds);
            RecoverPlayer();
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is not PlayerNode)
            {
                return;
            }

            _playerInside = true;
            _tickTimer = 0f;
            GameManager.Instance?.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest(NotificationText, NotificationType.Info));
        }

        private void OnBodyExited(Node2D body)
        {
            if (body is PlayerNode)
            {
                _playerInside = false;
            }
        }

        private void RecoverPlayer()
        {
            Player player = GameManager.Instance?.GetPlayer();
            if (player == null)
            {
                return;
            }

            int missingHp = Mathf.Max(0, player.MaxHP - player.CurHP);
            int missingMana = Mathf.Max(0, player.MaxMana - player.CurMana);

            if (missingHp > 0)
            {
                player.CurHP = Mathf.Min(HealthPerTick, missingHp);
            }

            if (missingMana > 0)
            {
                player.CurMana = Mathf.Min(ManaPerTick, missingMana);
            }
        }
    }
}
