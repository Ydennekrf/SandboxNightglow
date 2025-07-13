using Godot;

public partial class HealthZeroTransition : Node, IStateTransition
{
    [Export] public BaseState Target { get; set; }

    private bool _died;

    public override void _Ready()
    {
        EventManager.I.Subscribe<Entity>(GameEvent.Died, OnEntityDied);
    }

    private void OnEntityDied(Entity entity)
    {
        if (entity == Owner.GetParent().GetParent())
            _died = true;
    }

    public bool ShouldTransition(Entity owner)
    {
        return _died;
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<Entity>(GameEvent.Died, OnEntityDied);
    }
}
