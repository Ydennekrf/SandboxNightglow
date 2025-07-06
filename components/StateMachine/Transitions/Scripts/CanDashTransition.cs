using Godot;
public partial class CanDashTransition : Node, IStateTransition
{
	[Export] public NodePath TargetPath;
	[Export] public int StaminaCost = 15;
	public BaseState Target => GetNode<BaseState>(TargetPath);

	public bool ShouldTransition(Entity o)
	{
		bool dashPressed = Input.IsActionJustPressed("Dodge");
		if (!dashPressed) return false;

		return o.Data.EntityStats.TryGetValue(StatType.CurrentStamina, out var s)
			   && s.Value >= StaminaCost;
	}
}
