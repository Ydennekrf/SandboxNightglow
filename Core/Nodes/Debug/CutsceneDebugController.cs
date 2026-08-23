using Godot;
using System.Threading.Tasks;
using ethra.V1;

public partial class CutsceneDebugController : Node
{
    private int _resolveAttempts;
    private bool _started;

    [Export] public NodePath CharacterAPath { get; set; }
    [Export] public NodePath CharacterBPath { get; set; }
    [Export] public NodePath CharacterAMarkerPath { get; set; }
    [Export] public NodePath CharacterASecondMarkerPath { get; set; }
    [Export] public NodePath CharacterBMarkerPath { get; set; }
    [Export] public NodePath DialogPanelPath { get; set; } = "../UI/InteractionDialogPanel";
    [Export] public NodePath PlayerPath { get; set; } = "../World/Entities/Player/Player";
    [Export] public float MoveSpeed { get; set; } = 70f;
    [Export] public bool AutoStart { get; set; } = true;

    private Node2D _characterA;
    private Node2D _characterB;
    private Marker2D _characterAMarker;
    private Marker2D _characterASecondMarker;
    private Marker2D _characterBMarker;
    private InteractionDialogPanel _dialogPanel;
    private PlayerNode _playerNode;

    public override void _Ready()
    {
        CallDeferred(nameof(ResolveAndMaybeStart));
    }

    private void ResolveAndMaybeStart()
    {
        _characterA = GetNodeOrNull<Node2D>(CharacterAPath);
        _characterB = GetNodeOrNull<Node2D>(CharacterBPath);
        _characterAMarker = GetNodeOrNull<Marker2D>(CharacterAMarkerPath);
        _characterASecondMarker = GetNodeOrNull<Marker2D>(CharacterASecondMarkerPath);
        _characterBMarker = GetNodeOrNull<Marker2D>(CharacterBMarkerPath);
        _dialogPanel = GetNodeOrNull<InteractionDialogPanel>(DialogPanelPath)
            ?? GetTree().CurrentScene.GetNodeOrNull<InteractionDialogPanel>("UI/InteractionDialogPanel");
        _playerNode = GetNodeOrNull<PlayerNode>(PlayerPath);

        if (!HasRequiredReferences())
        {
            _resolveAttempts++;
            if (_resolveAttempts > 60)
            {
                GD.PushError("[CutsceneDebug] Required character, marker, or dialog panel reference is missing.");
                return;
            }

            CallDeferred(nameof(ResolveAndMaybeStart));
            return;
        }

        if (AutoStart && !_started)
        {
            _started = true;
            _ = RunCutscene();
        }
    }

    private async Task RunCutscene()
    {
        if (!HasRequiredReferences())
        {
            GD.PushError("[CutsceneDebug] Required character, marker, or dialog panel reference is missing.");
            return;
        }

        _playerNode?.SetCutsceneControlled(true);

        await MoveActorToMarker(_characterA, _characterAMarker, "CharacterA");
        await ShowLine("Character A", "We made it.");
        await MoveActorToMarker(_characterB, _characterBMarker, "CharacterB");
        await ShowLine("Character B", "This path leads deeper into the woods.");
        await MoveActorToMarker(_characterA, _characterASecondMarker, "CharacterA");
        PlayAnimationHook(_characterA, "nod");
        await WaitSeconds(0.35f);
        await ShowLine("Character A", "Then we keep moving.");

        _dialogPanel.HideDialog();
        _playerNode?.SetCutsceneControlled(false);
        GD.Print("[CutsceneDebug] Cutscene complete.");
    }

    private async Task MoveActorToMarker(Node2D actor, Marker2D marker, string actorLabel)
    {
        GD.Print($"[CutsceneDebug] Moving {actorLabel} to marker: {marker.Name}");

        while (actor.GlobalPosition.DistanceTo(marker.GlobalPosition) > 2f)
        {
            float delta = (float)GetProcessDeltaTime();
            actor.GlobalPosition = actor.GlobalPosition.MoveToward(marker.GlobalPosition, MoveSpeed * delta);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        actor.GlobalPosition = marker.GlobalPosition;
    }

    private async Task ShowLine(string speaker, string body)
    {
        bool advanced = false;
        _dialogPanel.ShowDialog(
            speaker,
            body,
            new[] { new InteractionDialogChoice("Continue", () => advanced = true) });

        while (!advanced)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void PlayAnimationHook(Node actor, string animationName)
    {
        GD.Print($"[CutsceneDebug] Play animation hook: {animationName}");

        AnimationPlayer animationPlayer = actor.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
            ?? actor.GetNodeOrNull<AnimationPlayer>("Actions");
        if (animationPlayer == null || string.IsNullOrWhiteSpace(animationName))
        {
            return;
        }

        if (animationPlayer.HasAnimation(animationName))
        {
            animationPlayer.Play(animationName);
        }
    }

    private async Task WaitSeconds(float seconds)
    {
        SceneTreeTimer timer = GetTree().CreateTimer(Mathf.Max(0f, seconds));
        await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    private bool HasRequiredReferences()
    {
        return _characterA != null
            && _characterB != null
            && _characterAMarker != null
            && _characterASecondMarker != null
            && _characterBMarker != null
            && _dialogPanel != null
            && _playerNode != null;
    }
}
