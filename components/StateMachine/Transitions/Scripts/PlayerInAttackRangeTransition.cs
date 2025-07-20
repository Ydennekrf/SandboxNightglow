using Godot;

[GlobalClass]
public partial class PlayerInAttackRangeTransition : Node, IStateTransition
{
    [Export] public NodePath TargetPath;
    [Export] public float    Range = 24f;
    private Player _player;
    public BaseState Target => GetNode<BaseState>(TargetPath);

    public bool ShouldTransition(Entity owner)
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("Player") as Player;

        if (_player == null)            // still not found? nothing to do
            return false;

        return owner.GlobalPosition.DistanceTo(_player.GlobalPosition) <= Range;
    }
}