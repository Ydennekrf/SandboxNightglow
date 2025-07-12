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
    protected CollisionShape2D _hitShape;
    public AnimationPlayer _anim;

    private readonly List<IStatusEffect> _effects = new();

    public override void _EnterTree()
    {
        _anim = GetNode<AnimationPlayer>(ActionsAnimPath);
        fsm = GetNode<StateMachine>("StateMachine");

    }
    public override void _Ready()
    {


    }

    public virtual void TakeDamage(int amount, DamageType type, Entity? attacker = null)
    {
        // in here we will check the Entity's gear and see if they have any current resistances to damage types.
        // for magic, the Entity will take an affinity which is dependent on the weapon equipped.
        // the entity's Affinity vs the DamageType will dictate the damage modifier.
        // water defending a electric attack takes 2.0 X damage, while Fire Defending electric would stay at 1.0 X
        // and Water Defending Fire would take 0.5 X

        
        GD.Print($"{this.Name}: took {amount} points of {type} damage");
        Modulate = Colors.Red;
        CreateTween()
            .TweenProperty(this, "modulate", Colors.White, 0.15f);

    }

    private float DamageMultiplier(DamageType atkType, DamageType defType)
    {
        float dmgMulti;
        switch (atkType)
        {
            case DamageType.Slash:
                dmgMulti = 1.0f;
                break;
            case DamageType.Blunt:
                dmgMulti = 1.0f;
                break;
            case DamageType.Pierce:
                dmgMulti = 1.0f;
                break;
            case DamageType.Fire:
                dmgMulti = 1.0f;
                break;
            case DamageType.Ice:
                dmgMulti = 1.0f;
                break;
            case DamageType.Void:
                dmgMulti = 1.0f;
                break;
            case DamageType.Holy:
                dmgMulti = 1.0f;
                break;
            case DamageType.Lightning:
                dmgMulti = 1.0f;
                break;
            case DamageType.Earth:
                dmgMulti = 1.0f;
                break;
            default: return 1.0f;


        }

        return dmgMulti;
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

    public void ActivateHitBox()
    {
        GD.Print("Test anim to hit method");
        var weapon = GetNodeOrNull<WeaponBase>("WeaponHolder/Weapon");
        weapon?.ActivateHitBox();
    }


    

}