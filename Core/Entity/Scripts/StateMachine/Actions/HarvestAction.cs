using Godot;

namespace ethra.V1.Actions
{
    public sealed class HarvestAction : IStateAction
    {
        private const string PlayerAnimLibraryPath = "res://ArtAssets/AnimationLibraries/playerActions.tres";

        private static AnimationLibrary _playerAnimLibrary;
        private readonly float _fallbackDuration;

        private PlayerHarvestRequest _request;
        private string _clipName = "Harvest";
        private bool _completionInvoked;
        private bool _missingAnimationLogged;

        public HarvestAction(float fallbackDuration = 0.35f)
        {
            _fallbackDuration = fallbackDuration;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (owner is not Player player)
            {
                return;
            }

            _completionInvoked = false;
            _request = player.ConsumePendingHarvestRequest();
            player.ActiveHarvestRequest = _request;
            player.HarvestComplete = false;
            player.HarvestTimerRemaining = ResolveDuration(player, _request);
            owner.DesiredVelocity = Vector2.Zero;

            FaceHarvestTarget(player, _request?.Target);
            GetPlayerNode()?.BeginHarvestVisuals(_request?.ToolType ?? HarvestToolType.None);
            owner.RequestedAnimation = _clipName;
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (owner is not Player player)
            {
                return;
            }

            FaceHarvestTarget(player, _request?.Target);
            owner.RequestedAnimation = _clipName;
            owner.DesiredVelocity = Vector2.Zero;

            player.HarvestTimerRemaining = Mathf.Max(0f, player.HarvestTimerRemaining - delta);
            if (player.HarvestTimerRemaining > 0f || _completionInvoked)
            {
                return;
            }

            _completionInvoked = true;
            player.CompleteActiveHarvestRequest();
            GetPlayerNode()?.EndHarvestVisuals();
        }

        public void Exit(Entity owner)
        {
            if (owner is not Player player)
            {
                return;
            }

            player.HarvestTimerRemaining = 0f;
            player.HarvestComplete = false;
            player.ActiveHarvestRequest = null;
            GetPlayerNode()?.EndHarvestVisuals();
            _request = null;
            _completionInvoked = false;
        }

        private float ResolveDuration(Player player, PlayerHarvestRequest request)
        {
            string key = string.IsNullOrWhiteSpace(request?.AnimationKey) ? "Harvest" : request.AnimationKey.Trim();
            foreach (string candidate in new[] { $"{key}_{player.Facing}", key })
            {
                float duration = ResolveClipDuration(candidate);
                if (duration <= 0f)
                {
                    continue;
                }

                _clipName = candidate;
                return request?.DurationSeconds > 0f ? Mathf.Min(request.DurationSeconds, duration) : duration;
            }

            _clipName = $"{key}_{player.Facing}";
            if (!_missingAnimationLogged)
            {
                _missingAnimationLogged = true;
                GD.Print("[PlayerFSM] Harvest animation missing or unavailable; completing harvest with fallback timing.");
            }

            return request?.DurationSeconds > 0f ? request.DurationSeconds : _fallbackDuration;
        }

        private static float ResolveClipDuration(string clipName)
        {
            _playerAnimLibrary ??= ResourceLoader.Load<AnimationLibrary>(PlayerAnimLibraryPath);
            if (_playerAnimLibrary == null)
            {
                return 0f;
            }

            Animation animation = _playerAnimLibrary.GetAnimation(clipName);
            return animation?.Length ?? 0f;
        }

        private static void FaceHarvestTarget(Player player, Node2D target)
        {
            if (player == null || target == null || !GodotObject.IsInstanceValid(target))
            {
                return;
            }

            PlayerNode playerNode = GetPlayerNode();
            if (playerNode == null)
            {
                return;
            }

            Vector2 delta = target.GlobalPosition - playerNode.GlobalPosition;
            if (delta.LengthSquared() < 0.001f)
            {
                return;
            }

            if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
            {
                player.Facing = delta.X > 0f ? FacingDirection.Right : FacingDirection.Left;
                return;
            }

            player.Facing = delta.Y > 0f ? FacingDirection.Down : FacingDirection.Up;
        }

        private static PlayerNode GetPlayerNode()
        {
            Node root = GameManager.Instance;
            return root?.GetTree()?.GetFirstNodeInGroup("Player") as PlayerNode;
        }
    }
}
