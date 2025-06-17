using System;
using Godot;

public partial class Entity : CharacterBody2D
{

    [Export] public NodePath ActionsAnimPath;
    public StateMachine fsm { get; private set; }
    public EntityData Data;

    public Facing FacingDirection { get; private set; } = Facing.Down;
    public string currentSpawnId { get; protected set; }

    public AnimationPlayer _anim;

    public override void _EnterTree()
    {
        _anim = GetNode<AnimationPlayer>(ActionsAnimPath);
        fsm = GetNode<StateMachine>("StateMachine");
    }
    public override void _Ready()
    {

        
    }

    public void SetInitialData(EntityData data)
    {
        Data = data;
    }



    public void UpdateFacing(Vector2 dir)
    {
        if (dir.LengthSquared() < 0.01f) return;          // ignore tiny jitter
        FacingDirection = dir.Abs().X > dir.Abs().Y
                          ? (dir.X < 0 ? Facing.Left : Facing.Right)
                          : (dir.Y < 0 ? Facing.Up : Facing.Down);
    }

    public void SetImmune(bool toggle)
    {
        
    }

    public void SpawnAfterImage(float lifetime = 0.3f, float startAlpha = 0.6f)
{
     var clone = new Node2D
    {
        Position = Position, 
        ZIndex   = ZIndex  
    }; 

    foreach (Sprite2D src in GetNode<Node2D>("Sprites").GetChildren())
    {
        var ghost = new Sprite2D
        {
            Texture = src.Texture,
            RegionEnabled = src.RegionEnabled,
            RegionRect = src.RegionRect,
            FlipH = src.FlipH,
            FlipV = src.FlipV,
            Hframes       = src.Hframes,
            Vframes       = src.Vframes,
            Frame         = src.Frame,  
            Modulate = new Color(1, 1, 1, startAlpha),
            Position = src.Position,
            Scale = src.Scale
            
        };
        clone.AddChild(ghost);
    }

    GetTree().CurrentScene.AddChild(clone);

    // Fade & delete
    var tween = clone.CreateTween();
    tween.TweenProperty(clone, "modulate:a", 0, lifetime)
         .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
    tween.TweenCallback(Callable.From(() => clone.QueueFree()));
}


}