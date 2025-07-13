using Godot;

public partial class AggroLostTransition : Node, IStateTransition
{
    [Export] public BaseState Target { get; set; }

    private bool _hasAggro = true;

    public override void _Ready()
    {
        EventManager.I.Subscribe<AggroEvent>(GameEvent.AggroGained, OnAggroGained);
        EventManager.I.Subscribe<AggroEvent>(GameEvent.AggroLost, OnAggroLost);
    }

    public bool ShouldTransition(Entity owner) => !_hasAggro;

    private void OnAggroGained(AggroEvent e)
    {
        if (e.enemy is Enemy enemy)
            _hasAggro = true;
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
