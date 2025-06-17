using Godot;

public partial class InputStopMovingTransition : Node, IStateTransition
{
    [Export] public NodePath TargetPath;
    [Export] public float Threshold = 0.1f;

    public BaseState Target => GetNode<BaseState>(TargetPath);

    public bool ShouldTransition(Entity owner)
{
        Vector2 v = Input.GetVector("Left","Right","Up","Down");
        return v.Length() <= Threshold;
    }
}