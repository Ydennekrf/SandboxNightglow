using Godot;

namespace ethra.V1.Actions
{
    public sealed class DodgeMoveAction : IStateAction
    {
        private const string PlayerAnimLibraryPath = "res://ArtAssets/AnimationLibraries/playerActions.tres";

        private static AnimationLibrary _playerAnimLibrary;
        private readonly float _dodgeSpeed;
        private readonly float _fallbackDuration;

        private Vector2 _dodgeDirection = Vector2.Zero;
        private string _clipName = "Dodge_Down";

        public DodgeMoveAction(float dodgeSpeed, float fallbackDuration = 0.2f)
        {
            _dodgeSpeed = dodgeSpeed;
            _fallbackDuration = fallbackDuration;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (owner is not Player player) return;

            _dodgeDirection = player.MoveInput;
            if (_dodgeDirection.LengthSquared() > 0.0001f)
            {
                _dodgeDirection = _dodgeDirection.Normalized();
            }
            else
            {
                _dodgeDirection = owner.Facing switch
                {
                    FacingDirection.Down => Vector2.Down,
                    FacingDirection.Up => Vector2.Up,
                    FacingDirection.Left => Vector2.Left,
                    FacingDirection.Right => Vector2.Right,
                    _ => Vector2.Down
                };
            }

            _clipName = $"Dodge_{owner.Facing}";
            owner.RequestedAnimation = _clipName;

            player.DodgeTimerRemaining = ResolveDuration(_clipName);
            if (player.DodgeTimerRemaining <= 0f)
                player.DodgeTimerRemaining = _fallbackDuration;

            owner.DesiredVelocity = _dodgeDirection * _dodgeSpeed;
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (owner is not Player player) return;

            player.DodgeTimerRemaining = Mathf.Max(0f, player.DodgeTimerRemaining - delta);
            owner.RequestedAnimation = _clipName;
            owner.DesiredVelocity = _dodgeDirection * _dodgeSpeed;
        }

        public void Exit(Entity owner)
        {
            owner.DesiredVelocity = Vector2.Zero;
            if (owner is Player player)
                player.DodgeTimerRemaining = 0f;
        }

        private static float ResolveDuration(string clipName)
        {
            _playerAnimLibrary ??= ResourceLoader.Load<AnimationLibrary>(PlayerAnimLibraryPath);
            if (_playerAnimLibrary == null) return 0f;

            var animation = _playerAnimLibrary.GetAnimation(clipName);
            return animation?.Length ?? 0f;
        }
    }
}
