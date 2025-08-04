using Godot;

public partial class HurtTransition : Node, IStateTransition
{
    [Export] public BaseState Target { get; set; }   
    [Export] public StateType _parentStateId;

    private bool _isActiveState = false;
    private bool _hurt;

    public override void _Ready()
    {

        EventManager.I.Subscribe<StateType>(GameEvent.StateEntered, OnStateEntered);
        EventManager.I.Subscribe<Entity>(GameEvent.Hurt, OnEntityHurt);
    }

     private void OnStateEntered(StateType entered)
    {
        _isActiveState = entered == _parentStateId;
    }

    private void OnEntityHurt(Entity entity)
    {
        if (_isActiveState && entity != null)
            _hurt = true;
    }

    public bool ShouldTransition(Entity owner)
    {
        if (_hurt && !owner.IsImmune())
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
