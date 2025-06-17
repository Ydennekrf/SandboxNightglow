using System;
using Godot;

public partial class TimerTransition : Node, IStateTransition
{
    [Export] public float Time = 0.25f;          // seconds before firing
    [Export] public NodePath TargetPath;

    private float _timer;
    public BaseState Target => GetNode<BaseState>(TargetPath);

    public override void _Ready() {
        EventManager.I.Subscribe<StateType>(GameEvent.StateEntered, ResetTimer);
        _timer = Time;
    }
    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<StateType>(GameEvent.StateEntered, ResetTimer);
    }

    private void ResetTimer(StateType _) {
        _timer = Time;
    } 
    public bool ShouldTransition(Entity owner)
    {
        GD.Print(_timer);
        _timer -= (float)GetProcessDeltaTime();
        return _timer <= 0f;
    }
}