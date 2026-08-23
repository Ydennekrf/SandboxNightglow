using Godot;

namespace ethra.V1.Actions
{
    public sealed class DialogAction : IStateAction
    {
        private const string PlayerAnimLibraryPath = "res://ArtAssets/AnimationLibraries/playerActions.tres";

        private static AnimationLibrary _playerAnimLibrary;
        private string _clipName = "Dialog_Down";
        private bool _missingAnimationLogged;

        public void Enter(Entity owner, BaseState baseState)
        {
            owner.DesiredVelocity = Vector2.Zero;
            if (owner is Player player)
            {
                _clipName = ResolveDialogClip(player, "Dialog") ?? $"Dialog_{owner.Facing}";
            }

            owner.RequestedAnimation = _clipName;
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            owner.DesiredVelocity = Vector2.Zero;

            if (owner is not Player player)
            {
                return;
            }

            if (player.ConsumeDialogAnimationRequest(out string animationKey))
            {
                _clipName = ResolveDialogClip(player, animationKey) ?? _clipName;
            }

            owner.RequestedAnimation = _clipName;
        }

        public void Exit(Entity owner)
        {
            owner.DesiredVelocity = Vector2.Zero;
        }

        private string ResolveDialogClip(Player player, string animationKey)
        {
            string key = string.IsNullOrWhiteSpace(animationKey) ? "Dialog" : animationKey.Trim();
            foreach (string candidate in new[] { $"{key}_{player.Facing}", key, $"Talk_{player.Facing}", "Talk", $"Dialog_{player.Facing}", "Dialog" })
            {
                if (HasAnimation(candidate))
                {
                    return candidate;
                }
            }

            if (!_missingAnimationLogged)
            {
                _missingAnimationLogged = true;
                GD.Print("[PlayerFSM] Dialog animation missing or unavailable; skipping player speaker animation hook.");
            }

            return null;
        }

        private static bool HasAnimation(string clipName)
        {
            _playerAnimLibrary ??= ResourceLoader.Load<AnimationLibrary>(PlayerAnimLibraryPath);
            return _playerAnimLibrary?.HasAnimation(clipName) == true;
        }
    }
}
