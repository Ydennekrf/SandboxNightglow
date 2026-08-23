using Godot;
using ethra.V1;

public partial class DebugButtonDoorButton : Area2D
{
    [Export] public NodePath DoorPath { get; set; }
    [Export] public bool RequireInteract { get; set; } = true;
    [Export] public NodePath VisualPath { get; set; } = "Visual";

    private DebugDoor _door;
    private CanvasItem _visual;
    private bool _playerInside;
    private bool _pressed;

    public override void _Ready()
    {
        _door = GetNodeOrNull<DebugDoor>(DoorPath);
        _visual = GetNodeOrNull<CanvasItem>(VisualPath);

        if (_door == null)
        {
            GD.PushWarning("[ButtonDoorDebug] DoorPath is not assigned or does not point to a DebugDoor.");
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (!RequireInteract || !_playerInside || _pressed || ethra.V1.GameManager.Instance?.UI?.BlocksGameplayInput == true)
        {
            return;
        }

        if (Input.IsActionJustPressed("Interact"))
        {
            PressButton();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerNode)
        {
            return;
        }

        _playerInside = true;
        if (!RequireInteract)
        {
            PressButton();
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is PlayerNode)
        {
            _playerInside = false;
        }
    }

    private void PressButton()
    {
        if (_pressed)
        {
            return;
        }

        _pressed = true;
        if (_visual != null)
        {
            _visual.Modulate = new Color(0.35f, 0.9f, 0.45f, 1f);
        }

        GD.Print("[ButtonDoorDebug] Button pressed.");
        _door?.OpenDoor();
    }
}
