using Godot;

public partial class EnterRunTransition : Node, IStateTransition
{
    [Export] public NodePath TargetPath;
    public BaseState Target => GetNode<BaseState>(TargetPath);

  public bool ShouldTransition(Entity o)
{
    Vector2 input = Input.GetVector("Left", "Right", "Up", "Down");
    bool isRunning = Input.IsActionPressed("Run");

    // Only enter run if there's movement input, run key is held, and stamina is above a safe threshold
    bool hasStamina = o.Data.EntityStats.TryGetValue(StatType.CurrentStamina, out var s) && s.Value > 5;

    return isRunning && input.Length() > 0.1f && hasStamina;
}
}