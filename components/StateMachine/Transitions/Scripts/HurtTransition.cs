using Godot;

public partial class HurtTransition : Node, IStateTransition
{
    [Export] public BaseState Target { get; set; }

    private bool _hurt;

    public override void _Ready()
    {
        EventManager.I.Subscribe<Entity>(GameEvent.Hurt, OnEntityHurt);
    }

    private void OnEntityHurt(Entity entity)
    {
        if (entity != null)
            _hurt = true;
    }

    public bool ShouldTransition(Entity owner)
    {
        if (_hurt)
        {
            _hurt = false; // reset flag
            return true;
        }
        return false;
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<Entity>(GameEvent.Hurt, OnEntityHurt);
    }
}
