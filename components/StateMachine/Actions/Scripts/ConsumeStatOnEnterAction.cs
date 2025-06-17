using Godot;
public partial class ConsumeStatOnEnterAction : Node, IStateAction
{
    [Export] public StatType Stat = StatType.CurrentStamina;
    [Export] public int Cost = 15;

    public void Enter(Entity o, BaseState state)
    {
        if (o.Data.EntityStats.TryGetValue(Stat, out var s))
            s.changeStatValue(-Cost);   // fires stat‑change event
    }
    public void Execute( float dt, Entity o, BaseState state) { }
    public void Exit(Entity o) { }
}