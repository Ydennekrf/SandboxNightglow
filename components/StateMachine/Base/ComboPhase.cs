using Godot;
using System.Collections.Generic;

/// Runs a single animation clip AND optional phase-actions.
/// • One node per swing / cast.
/// • Executes extra effects only while this phase is active.
public partial class ComboPhase : Node, IStateAction
{
    [Export] public string AnimationName = "";
    [Export] public float  ComboWindow   = 0.25f;     // seconds before clip end

    // Drop other nodes (IPhaseAction) into this for custom effects
    [Export] public Godot.Collections.Array<NodePath> EffectPaths = new();

    private AnimationPlayer _anim;
    private List<IPhaseAction> _effects = new();

    public bool IsDone    { get; private set; }
    public bool QueueNext { get; private set; }

    public override void _Ready()
    {
        foreach (var p in EffectPaths)
            if (GetNode(p) is IPhaseAction eff) _effects.Add(eff);
    }

    public void Enter(Entity owner, BaseState state)
    {
        _anim = owner.GetNode<AnimationPlayer>("AnimationPlayer");
        _anim.Play(AnimationName);

        foreach (var e in _effects) e.OnPhaseStart(owner, this);

        IsDone   = false;
        QueueNext = false;
    }

    public void Execute(float delta, Entity owner, BaseState state)
    {
        float timeLeft = (float)(_anim.CurrentAnimationLength - _anim.CurrentAnimationPosition);

        if (timeLeft < ComboWindow &&
            Input.IsActionJustPressed(state.InputActionName))
            QueueNext = true;

        if (_anim.CurrentAnimationPosition >= _anim.CurrentAnimationLength)
            IsDone = true;
    }

    public void Exit(Entity owner)
    {
        foreach (var e in _effects) e.OnPhaseEnd(owner, this);
    }
}

