using Godot;
using System.Threading.Tasks;

namespace ethra.V1
{
    public partial class CutsceneActionNode : CutsceneTargetStepNode
    {
        [ExportGroup("Animation")]
        [Export] public string AnimationName { get; set; } = string.Empty;
        [Export] public bool WaitForAnimationComplete { get; set; } = true;
        [Export] public float FallbackDurationSeconds { get; set; } = 0.25f;
        [Export] public bool StopBeforePlay { get; set; } = false;

        public override async Task ExecuteAsync(CutsceneContext context)
        {
            Node target = ResolveTarget(context);
            if (target == null)
            {
                Warn("Target entity was not found; skipping action.");
                await WaitSeconds(FallbackDurationSeconds);
                return;
            }

            if (string.IsNullOrWhiteSpace(AnimationName))
            {
                Warn("AnimationName is empty; skipping action.");
                await WaitSeconds(FallbackDurationSeconds);
                return;
            }

            if (target is TestEnemyNode enemy)
            {
                enemy.PlayEnemyAnimation(AnimationName);
                await WaitForKnownAnimation(enemy.GetNodeOrNull<AnimationPlayer>(enemy.AnimationPlayerPath));
                return;
            }

            AnimationPlayer animationPlayer = FindAnimationPlayer(target);
            if (animationPlayer == null)
            {
                Warn($"Target '{target.Name}' has no AnimationPlayer child named 'AnimationPlayer' or 'Actions'.");
                await WaitSeconds(FallbackDurationSeconds);
                return;
            }

            if (!animationPlayer.HasAnimation(AnimationName))
            {
                Warn($"Target '{target.Name}' is missing animation '{AnimationName}'.");
                await WaitSeconds(FallbackDurationSeconds);
                return;
            }

            if (StopBeforePlay)
            {
                animationPlayer.Stop();
            }

            animationPlayer.Play(AnimationName);
            await WaitForKnownAnimation(animationPlayer);
        }

        private async Task WaitForKnownAnimation(AnimationPlayer animationPlayer)
        {
            if (!WaitForAnimationComplete)
            {
                return;
            }

            if (animationPlayer == null || !animationPlayer.HasAnimation(AnimationName))
            {
                await WaitSeconds(FallbackDurationSeconds);
                return;
            }

            double length = animationPlayer.GetAnimation(AnimationName)?.Length ?? 0.0;
            if (length <= 0.0)
            {
                await WaitSeconds(FallbackDurationSeconds);
                return;
            }

            await WaitSeconds((float)length);
        }

        internal static AnimationPlayer FindAnimationPlayer(Node target)
        {
            if (target == null)
            {
                return null;
            }

            return target as AnimationPlayer
                ?? target.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?? target.GetNodeOrNull<AnimationPlayer>("Actions")
                ?? FindDescendantAnimationPlayer(target);
        }

        private static AnimationPlayer FindDescendantAnimationPlayer(Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                if (child is AnimationPlayer animationPlayer)
                {
                    return animationPlayer;
                }

                AnimationPlayer nested = FindDescendantAnimationPlayer(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        public override string[] _GetConfigurationWarnings()
        {
            return string.IsNullOrWhiteSpace(AnimationName)
                ? new[] { "AnimationName is empty." }
                : System.Array.Empty<string>();
        }
    }
}
