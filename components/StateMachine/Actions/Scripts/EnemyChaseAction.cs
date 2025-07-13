using Godot;

public partial class EnemyChaseAction : Node, IStateAction
{
    private NavigationAgent2D _navAgent;
    private Entity _target;
    private Entity _owner;

    [Export] public float MoveSpeed = 100f;

    public override void _Ready()
    {
        _navAgent = Owner.GetNodeOrNull<NavigationAgent2D>("NavAgent");
        EventManager.I.Subscribe<AggroEvent>(GameEvent.AggroGained, OnAggroGained);
        EventManager.I.Subscribe<AggroEvent>(GameEvent.AggroLost, OnAggroLost);
    }

    public void Enter(Entity owner, BaseState baseState)
    {
        _owner = owner;
    }

    public void Execute(float delta, Entity owner, BaseState baseState)
    {
        if (_navAgent == null || _target == null)
            return;

        _navAgent.TargetPosition = _target.GlobalPosition;
        if (_navAgent.IsNavigationFinished()) return;

        Vector2 nextPosition = _navAgent.GetNextPathPosition();
        Vector2 direction = (nextPosition - owner.GlobalPosition).Normalized();

        owner.Velocity = direction * MoveSpeed;
        owner.MoveAndSlide();
        owner.UpdateFacing(direction);
    }

    public void Exit(Entity owner)
    {
        owner.Velocity = Vector2.Zero;
    }

    private void OnAggroGained(AggroEvent e)
    {
        if (e.enemy is Enemy enemy)
            _target = e.target;
    }

    private void OnAggroLost(AggroEvent e)
    {
        if (e.enemy is Enemy enemy)
            _target = null;
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<AggroEvent>(GameEvent.AggroGained, OnAggroGained);
        EventManager.I.Unsubscribe<AggroEvent>(GameEvent.AggroLost, OnAggroLost);
    }
}
