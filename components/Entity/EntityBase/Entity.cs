using System;
using Godot;
using System.Collections.Generic;

public partial class Entity : CharacterBody2D
{

    [Export] public NodePath ActionsAnimPath;
    public StateMachine fsm { get; private set; }
    public EntityData Data;

    public Facing FacingDirection { get; private set; } = Facing.Down;
    public string currentSpawnId { get; protected set; }

    public ComboPhase ActivePhase { get; set; }
    private CollisionShape2D _hitShape;
    public AnimationPlayer _anim;
    
    private readonly List<IStatusEffect> _effects = new();

    public override void _EnterTree()
    {
        _anim = GetNode<AnimationPlayer>(ActionsAnimPath);
        fsm = GetNode<StateMachine>("StateMachine");
        _hitShape = GetNode<CollisionShape2D>("HitBoxArea/CollisionShape2D");
        _hitShape.Disabled = true;
    }
    public override void _Ready()
    {


    }

    public void TakeDamage(int amount, Entity? attacker = null)
    {
        if (amount <= 0) return;

        Stat currHp = Data.EntityStats[StatType.CurrentHealth];
        // eventually we will check the type of damage is happening.
        //  and we will lower the damage dealt by the armor/ mag defence of the entity

        // for not we will just deal flat damage
        if (!currHp.changeStatValue(amount))
        {
            //DIE
            GD.Print($"{this.Name} should die now");
        }

    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            if (_effects[i].Tick((float)delta, this))
                _effects.RemoveAt(i);   // auto-cleanup
        }
    }

    public void SetInitialData(EntityData data)
    {
        Data = data;
    }

    public void AddStatusEffect(IStatusEffect effect)
    {
        _effects.Add(effect);
        effect.Start(this);
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
            ZIndex = ZIndex
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
                Hframes = src.Hframes,
                Vframes = src.Vframes,
                Frame = src.Frame,
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


    public void ActivateHitBox() // lifetime before auto-disable
    {
        ComboPhase p = ActivePhase;

        if (p == null) return;

        var rect = _hitShape.Shape as RectangleShape2D;

        if (rect == null)
        {
            rect = new RectangleShape2D();
            _hitShape.Shape = rect;
        }
        rect.Size = new Vector2(p.Width, p.Height);

         Vector2 offset = FacingDirection switch
    {
        Facing.Down  => new Vector2( 0,  p.Forward),
        Facing.Up    => new Vector2( 0, -p.Forward),
        Facing.Left  => new Vector2(-p.Forward, 0),
        Facing.Right => new Vector2( p.Forward, 0),
        _            => Vector2.Zero
    };
    _hitShape.Position = offset;

    // 5) Turn the collider on, then auto-disable after the active window
    _hitShape.Disabled = false;

    var tween = CreateTween();
    tween.TweenCallback(Callable.From(() => _hitShape.Disabled = true))
         .SetDelay(p.ActiveSecs);

        
    }

}