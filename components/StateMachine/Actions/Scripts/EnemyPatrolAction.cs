using Godot;

public partial class EnemyPatrolAction : Node, IStateAction
{
    [Export] public float PatrolSpeed = 50f;
    [Export] public float PatrolRadius = 100f;
    [Export] public float WaitTimeAtPoint = 1.5f;

    private NavigationAgent2D _navAgent;
    private Entity _owner;
    private Vector2 _originPosition;

    private float _waitTimer = 0;

    public override void _Ready()
    {
        _navAgent = Owner.GetNodeOrNull<NavigationAgent2D>("NavAgent");
        if (_navAgent == null)
        {
            GD.PrintErr("[EnemyRandomPatrolAction] NavigationAgent2D not found.");
        }
    }

    public void Enter(Entity owner, BaseState baseState)
    {
        _owner = owner;
        _originPosition = owner.GlobalPosition;
        SetRandomPatrolDestination();
    }

    public void Execute(float delta, Entity owner, BaseState baseState)
    {
        if (_navAgent == null) return;

        if (_navAgent.IsNavigationFinished())
        {
            if (_waitTimer <= 0)
            {
                _waitTimer = WaitTimeAtPoint;
            }
            else
            {
                _waitTimer -= delta;
                if (_waitTimer <= 0)
                {
                    SetRandomPatrolDestination();
                }
            }
            owner.Velocity = Vector2.Zero;
            return;
        }

        Vector2 nextPosition = _navAgent.GetNextPathPosition();
        Vector2 direction = (nextPosition - owner.GlobalPosition).Normalized();

        owner.Velocity = direction * PatrolSpeed;
        owner.MoveAndSlide();
        owner.UpdateFacing(direction);
    }

    public void Exit(Entity owner)
    {
        owner.Velocity = Vector2.Zero;
    }

    private void SetRandomPatrolDestination()
    {
        Vector2 randomPoint;
        bool validPoint = false;

        int attempts = 0;
        while (!validPoint && attempts < 10)
        {
            randomPoint = _originPosition + new Vector2(
                (float)GD.RandRange(-PatrolRadius, PatrolRadius),
                (float)GD.RandRange(-PatrolRadius, PatrolRadius));

            var navRegion = _navAgent.GetNavigationMap();
            if (NavigationServer2D.MapGetClosestPoint(navRegion, randomPoint).DistanceTo(randomPoint) < 10f)
            {
                _navAgent.TargetPosition = randomPoint;
                validPoint = true;
            }
            attempts++;
        }

        if (!validPoint)
        {
            GD.Print("[EnemyRandomPatrolAction] Failed to find valid random patrol point.");
        }
    }
}
