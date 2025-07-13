using Godot;

public partial class AggroGainedTransition : Node, IStateTransition
{
    [Export] public BaseState Target { get; set; }

    private bool _hasAggro;

    public override void _Ready()
    {
        EventManager.I.Subscribe<AggroEvent>(GameEvent.AggroGained, OnAggroGained);
        EventManager.I.Subscribe<AggroEvent>(GameEvent.AggroLost, OnAggroLost);
    }

    public bool ShouldTransition(Entity owner) => _hasAggro;

    private void OnAggroGained(AggroEvent e)
    {
        if (e.enemy is Enemy enemy) // Enemy root from State Node
            _hasAggro = true;

        e.enemy.AddStatus(StatusEffectType.Aggro);
    }

    private void OnAggroLost(AggroEvent e)
    {
        if (e.enemy is Enemy enemy)
            _hasAggro = false;
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<AggroEvent>(GameEvent.AggroGained, OnAggroGained);
        EventManager.I.Unsubscribe<AggroEvent>(GameEvent.AggroLost, OnAggroLost);
    }
}
