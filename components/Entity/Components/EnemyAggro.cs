using Godot;

public partial class EnemyAggro : Area2D
{
    private Entity _owner;

    public override void _Ready()
    {
        _owner = GetParent<Entity>(); // assuming Area2D is direct child of Enemy
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        GD.Print("collision happened AGGRO");
        if (body.IsInGroup("Player"))
        {
            EventManager.I.Publish(GameEvent.AggroGained, new AggroEvent(_owner, body as Entity));
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("Player"))
        {
            EventManager.I.Publish(GameEvent.AggroLost, new AggroEvent(_owner, body as Entity));
        }
    }
}
