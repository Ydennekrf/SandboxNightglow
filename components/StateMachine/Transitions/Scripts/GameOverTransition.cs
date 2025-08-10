using System;
using Godot;

public partial class GameOverTransition : Node, IStateTransition
{
    [Export] public float Time = 1f;          // seconds before firing
    [Export] public NodePath TargetPath;

    private float _timer;
    private bool fired;
    public BaseState Target => GetNode<BaseState>(TargetPath);

    public override void _Ready()
    {
        EventManager.I.Subscribe<StateType>(GameEvent.StateEntered, ResetTimer);
        _timer = Time;
        fired = false;
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
        
         _timer -= (float)GetProcessDeltaTime();

        // Fire a single “GameOverRequested” event the instant we cross zero.
        if (!fired && _timer <= 0f) {
            fired = true;
            EventManager.I.Publish(GameEvent.GameOverRequested, owner);
        }

        return _timer <= 0f;
    }
}