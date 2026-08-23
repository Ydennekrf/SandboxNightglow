using Godot;
using System.Threading.Tasks;

namespace ethra.V1
{
    public partial class CutsceneMovementNode : CutsceneTargetStepNode
    {
        [ExportGroup("Movement")]
        [Export] public NodePath StartMarkerPath { get; set; }
        [Export] public NodePath EndMarkerPath { get; set; }
        [Export] public float MoveSpeed { get; set; } = 70f;
        [Export] public string MovementAnimationName { get; set; } = string.Empty;
        [Export] public string IdleAnimationName { get; set; } = string.Empty;
        [Export] public bool SnapToStartOnBegin { get; set; } = false;
        [Export] public bool WaitForMovementComplete { get; set; } = true;

        public override async Task ExecuteAsync(CutsceneContext context)
        {
            Node target = ResolveTarget(context);
            if (target is not Node2D actor)
            {
                Warn("Target entity must be a Node2D; skipping movement.");
                return;
            }

            Marker2D startMarker = ResolveConfiguredNode<Marker2D>(StartMarkerPath);
            Marker2D endMarker = ResolveConfiguredNode<Marker2D>(EndMarkerPath);
            if (endMarker == null)
            {
                Warn("EndMarker is missing; skipping movement.");
                return;
            }

            if (SnapToStartOnBegin)
            {
                if (startMarker == null)
                {
                    Warn("SnapToStartOnBegin is enabled, but StartMarker is missing.");
                }
                else
                {
                    actor.GlobalPosition = startMarker.GlobalPosition;
                }
            }

            Task movementTask = MoveActorAsync(actor, endMarker, context);
            if (WaitForMovementComplete)
            {
                await movementTask;
            }
        }

        private async Task MoveActorAsync(Node2D actor, Marker2D endMarker, CutsceneContext context)
        {
            AnimationPlayer animationPlayer = CutsceneActionNode.FindAnimationPlayer(actor);
            PlayAnimationIfAvailable(animationPlayer, MovementAnimationName);

            float speed = Mathf.Max(1f, MoveSpeed);
            while (actor != null
                && IsInstanceValid(actor)
                && context?.IsCancellationRequested != true
                && actor.GlobalPosition.DistanceTo(endMarker.GlobalPosition) > 1f)
            {
                float delta = (float)GetProcessDeltaTime();
                Vector2 direction = actor.GlobalPosition.DirectionTo(endMarker.GlobalPosition);
                Vector2 velocity = direction * speed;

                if (actor is CharacterBody2D characterBody)
                {
                    characterBody.Velocity = velocity;
                    characterBody.MoveAndSlide();
                }
                else
                {
                    actor.GlobalPosition = actor.GlobalPosition.MoveToward(endMarker.GlobalPosition, speed * delta);
                }

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            if (actor != null && IsInstanceValid(actor))
            {
                actor.GlobalPosition = endMarker.GlobalPosition;
                if (actor is CharacterBody2D characterBody)
                {
                    characterBody.Velocity = Vector2.Zero;
                }
            }

            PlayAnimationIfAvailable(animationPlayer, IdleAnimationName);
        }

        private void PlayAnimationIfAvailable(AnimationPlayer animationPlayer, string animationName)
        {
            if (animationPlayer == null || string.IsNullOrWhiteSpace(animationName))
            {
                return;
            }

            if (animationPlayer.HasAnimation(animationName))
            {
                animationPlayer.Play(animationName);
                return;
            }

            Warn($"Animation '{animationName}' was not found on '{animationPlayer.GetPath()}'.");
        }

        public override string[] _GetConfigurationWarnings()
        {
            if (IsEmptyNodePath(EndMarkerPath))
            {
                return new[] { "EndMarkerPath is required." };
            }

            return System.Array.Empty<string>();
        }
    }
}
