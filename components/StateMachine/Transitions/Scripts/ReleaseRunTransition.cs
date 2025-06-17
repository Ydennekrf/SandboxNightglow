using Godot;

public partial class ReleaseRunTransition : Node, IStateTransition
{
    [Export] public NodePath TargetPath;
    public BaseState Target => GetNode<BaseState>(TargetPath);

   public bool ShouldTransition(Entity o)
{
    Vector2 input = Input.GetVector("Left", "Right", "Up", "Down");
    bool notHoldingRun = !Input.IsActionPressed("Run");
    bool notMoving = input.Length() <= 0.1f;

    return notHoldingRun || notMoving;
}
}