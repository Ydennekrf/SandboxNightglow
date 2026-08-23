using Godot;

public partial class DebugDoor : Node2D
{
    [Export] public NodePath CollisionPath { get; set; } = "StaticBody2D/CollisionShape2D";
    [Export] public NodePath VisualPath { get; set; } = "Visual";
    [Export] public bool HideVisualWhenOpen { get; set; } = false;
    [Export] public Vector2 OpenOffset { get; set; } = new(0f, -40f);

    private CollisionShape2D _collision;
    private CanvasItem _visual;
    private Vector2 _closedPosition;
    private bool _opened;

    public override void _Ready()
    {
        _closedPosition = Position;
        _collision = GetNodeOrNull<CollisionShape2D>(CollisionPath);
        _visual = GetNodeOrNull<CanvasItem>(VisualPath);
    }

    public void OpenDoor()
    {
        if (_opened)
        {
            return;
        }

        _opened = true;

        if (_collision != null)
        {
            _collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }

        if (_visual != null)
        {
            _visual.Visible = !HideVisualWhenOpen;
        }

        Position = _closedPosition + OpenOffset;
        GD.Print("[ButtonDoorDebug] Door opened.");
    }
}
