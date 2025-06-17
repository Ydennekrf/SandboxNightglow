using Godot;

[Tool]   // so the spawn radius is visible in the editor
public partial class SpawnOnProximity : Marker2D
{
    [Export] public PackedScene NpcScene;           // drag NPCExample1.tscn here
    [Export] public NodePath    NpcLayerPath;       // point at "/root/TestZone/NPCLayer"
    [Export] public float       TriggerRadius = 96; // pixels

    private Node2D _npcLayer;
    private bool   _spawned;

    public override void _Ready()
    {
        _npcLayer = GetNode<Node2D>("NPCLayer");

        // Create an Area2D child for proximity detection (runtime only)
        var area = new Area2D();
        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = TriggerRadius }
        };
        area.AddChild(shape);
        AddChild(area);

        // Listen for player entry
        area.BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (_spawned) return;
        if (!(body is PhysicsBody2D pb && pb.IsInGroup("Player"))) return;

        SpawnNpc();
    }

    private void SpawnNpc()
    {
        if (NpcScene == null)
        {
            GD.PushError($"{Name}: NpcScene not assigned.");
            return;
        }

        var npc = NpcScene.Instantiate<Node2D>();
        npc.GlobalPosition = GlobalPosition;

        _npcLayer.AddChild(npc);
        _spawned = true;

    }

    // Draw gizmo in the editor
    public override void _Draw()
    {
        if (Engine.IsEditorHint())
            DrawCircle(Vector2.Zero, TriggerRadius, Color.FromHsv(345,95,100,0.2f));
    }
}