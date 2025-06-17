using Godot;
using System;
using System.Collections.Generic;

public partial class DirectionalAnimationAction : Node, IStateAction
{

    public string BaseName;
    private static readonly Dictionary<Facing, string> _suffix =
        new() { {Facing.Down, "_Down"}, {Facing.Left, "_Left"},
                {Facing.Right, "_Right"}, {Facing.Up, "_Up"} };

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

        Facing facing = GetFacingFromOwner(owner);
        owner._anim.Play(BaseName + _suffix[facing]);
    }

    private Facing GetFacingFromOwner(Node owner)
    {
        if (owner is Entity entity)
        {
            return entity.FacingDirection;
        }
        return Facing.Down;

    }
}
