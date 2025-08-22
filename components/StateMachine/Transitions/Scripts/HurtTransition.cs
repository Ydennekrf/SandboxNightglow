using Godot;

public partial class HurtTransition : Node, IStateTransition
{
    [Export] public BaseState _defaultTarget { get; set; }
    [Export] public BaseState DieTarget { get; set; }   
    [Export] public StateType _parentStateId;

    private bool _isActiveState = false;
    private bool _hurt;

    private bool _forceDie;
    private Entity _owner;

     public BaseState Target
    {
        get => _forceDie ? DieTarget : _defaultTarget;
        set => _defaultTarget = value;
    }

    public override void _Ready()
    {
         _owner = GetOwner<Entity>();
        EventManager.I.Subscribe<StateType>(GameEvent.StateEntered, OnStateEntered);
        EventManager.I.Subscribe<Entity>(GameEvent.Hurt, OnEntityHurt);
    }

     private void OnStateEntered(StateType entered)
    {
        _isActiveState = entered == _parentStateId;
    }

    private void OnEntityHurt(Entity entity)
    {
        if (!_isActiveState || entity != _owner) return;

         // If dead, skip hurt entirely and flip to die.
        if (entity.Data.EntityStats[StatType.CurrentHealth].Value <= 0)
        {
            GD.Print("SHOULD DIE NOW!!!!!!!!!!!!!!!!!!!!!");
            _forceDie = true;
            _hurt = false;
            return;
        }
            _hurt = true;
    }

    public bool ShouldTransition(Entity owner)
    {
        if (_forceDie)
        {
            _forceDie = false;
            return true;
        }

        if (_hurt && !owner.IsImmune())
        {
            _hurt = false; // reset flag
            return true;
        }
        return false;
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<StateType>(GameEvent.StateEntered, OnStateEntered);
        EventManager.I.Unsubscribe<Entity>(GameEvent.Hurt, OnEntityHurt);
    }
}
