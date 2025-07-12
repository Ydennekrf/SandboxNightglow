using Godot;
using System;
using System.Collections.Generic;

public partial class StaticAnimationAction : Node, IStateAction
{

    public string BaseName;

    public void Enter(Entity owner, BaseState state)
    {
       
        BaseName = state?.StateId.ToString() ?? "Idle";
        UpdateAnim(owner, state);
    }

    public void Execute(float delta, Entity owner, BaseState state) => UpdateAnim(owner, state);

    public void Exit(Entity owner) {}

    private void UpdateAnim(Entity owner, BaseState state)
    {
        if (owner._anim == null) return;

        owner._anim.Play(BaseName);
    }

}