using Godot;
using System;

public partial class ComboPhase : Node, IStateAction
{
    [Export] public string AnimationName;
    [Export] public float Damage;
    [Export] public float ComboWindow = 0.25f;   // seconds before anim end

    public bool IsDone { get; private set; }
    public bool QueueNext { get; private set; }

    private AnimationPlayer _anim;

    public void Enter(Entity owner, BaseState state)
    {
        _anim = owner.GetNode<AnimationPlayer>("Actions");
        _anim.Play(AnimationName);
        IsDone = false;
        QueueNext = false;
    }

    public void Execute(float delta, Entity owner, BaseState state)
    {
        double timeLeft =
            _anim.CurrentAnimationLength - _anim.CurrentAnimationPosition;

        // player pressed the same button again within the window → chain
        if (timeLeft < ComboWindow &&
            Input.IsActionJustPressed(state.InputActionName))
        {
            QueueNext = true;
        }

        if (_anim.CurrentAnimationPosition >= _anim.CurrentAnimationLength)
            IsDone = true;
    }

    public void Exit(Entity owner) { }
}
