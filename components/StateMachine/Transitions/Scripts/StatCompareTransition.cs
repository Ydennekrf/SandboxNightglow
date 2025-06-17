using Godot;

public partial class StatCompareTransition : Node, IStateTransition
{
    [Export] public StatType WatchedStat = StatType.CurrentStamina;
    [Export] public int Threshold = 0;                 // trigger when ≤ this
    [Export] public NodePath TargetPath;
    [Export] public bool GreaterThan = false;

    public BaseState Target => GetNode<BaseState>(TargetPath);

    public bool ShouldTransition(Entity owner)
    {
        if (owner?.Data == null) return false;

        if (!owner.Data.EntityStats.TryGetValue(WatchedStat, out var stat))
        {
            GD.PushWarning($"[{Name}] Stat not found: {WatchedStat}");
            return false;
        }
        
        if (!GreaterThan)
        {
            return stat.Value <= Threshold;
        }
        return stat.Value >= Threshold;
        
    }
}
