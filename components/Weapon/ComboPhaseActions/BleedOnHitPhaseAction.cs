using Godot;

/// <summary>
/// Applies a bleed DOT to every enemy your hit-box touches **during
/// this ComboPhase only**.
/// Use BleedType to set the aspect of the damage which will impact VFX and SFX
/// </summary>
public partial class BleedOnHitPhaseAction : Node, IPhaseAction
{
    [Export] public int TickDamage = 1;
    [Export] public float Duration = 4.0f;   // seconds
    [Export] public float TickRate = 1.0f;   // seconds
    [Export] public StatusEffectType BleedType = StatusEffectType.None;

    private Entity _owner;

    public void OnPhaseStart(Entity owner, ComboPhase phase)
    {
        _owner = owner;

        EventManager.I.Subscribe<HitEvent>(GameEvent.Hit, OnHit);
    }

    public void OnPhaseEnd(Entity owner, ComboPhase phase)
    {
        EventManager.I.Unsubscribe<HitEvent>(GameEvent.Hit, OnHit);
        _owner = null;
    }

    private void OnHit(HitEvent payload)
    {
        if (payload.attacker != _owner) return;  // only apply from this weapon’s owner


        payload.target.AddStatusEffect(new BleedEffect
        {
            DamagePerTick = TickDamage,
            TickRate = TickRate,
            Duration = Duration,
            type = BleedType
        });
    }
}