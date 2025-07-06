using Godot;

public partial class KnockbackPhaseAction : Node, IPhaseAction
{
    [ExportGroup("Knockback")]
    [Export] public float Force     = 250f;   // pixels per second impulse
    [Export] public float Duration  = 0.15f;  // seconds

    private Entity _owner;

    public void OnPhaseStart(Entity owner, ComboPhase phase)
    {
        _owner = owner;
        EventManager.I.Subscribe<HitEvent>(GameEvent.Hit, OnHit);
    }

    public void OnPhaseEnd(Entity o, ComboPhase _)
    {
        EventManager.I.Unsubscribe<HitEvent>(GameEvent.Hit, OnHit);
        _owner = null;
    }

    private void OnHit(HitEvent payload)
    {
        if (payload.attacker != _owner) return;


        Vector2 dir = (payload.target.GlobalPosition - payload.attacker.GlobalPosition).Normalized();
        Vector2 impulse = dir * Force;

        if (payload.target is CharacterBody2D body)
        {
            body.Velocity += impulse;

            body.CreateTween()
                .TweenProperty(body, "velocity", Vector2.Zero, Duration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }
    }
}