using Godot;

/// Adds a SlowEffect to every enemy your hit-box touches **during this phase**.
public partial class SlowPhaseAction : Node, IPhaseAction
{
    [ExportGroup("Slow")]
    [Export] public int Divider = 2;  // 50% slower  
    [Export] public float Duration   = 4f;     // seconds

    private Entity _owner;

    /* ─────────────── phase lifecycle ─────────────── */
    public void OnPhaseStart(Entity owner, ComboPhase _)
    {
        _owner = owner;
        EventManager.I.Subscribe<HitEvent>(GameEvent.Hit, OnHit);
    }

    public void OnPhaseEnd(Entity _, ComboPhase __)
    {
        EventManager.I.Unsubscribe<HitEvent>(GameEvent.Hit, OnHit);
        _owner = null;
    }

    /* ─────────────── event handler ──────────────── */
    private void OnHit(HitEvent e)
    {
        if (e.attacker != _owner) return;          // ensure it's our swing

        e.target.AddStatusEffect(new SlowEffect
        {
            Divider = Divider,
            Duration   = Duration
        });
    }
}