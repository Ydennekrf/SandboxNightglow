using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class ComboPhaseResource : Resource
    {
        [Export] public string StepId { get; set; } = string.Empty;
        [Export] public int StepIndex { get; set; } = 0;
        [Export] public string InputAction { get; set; } = "Attack";
        [Export] public string DebugLabel { get; set; } = string.Empty;
        [Export] public float DamageMultiplier { get; set; } = 1f;
        [Export] public string SharedAnimationName { get; set; } = "Melee1";
        [Export] public bool PreferFacingSuffix { get; set; } = true;
        [Export] public float DurationOverrideSeconds { get; set; } = 0f;

        [Export] public float StartupSeconds { get; set; } = 0f;
        [Export] public float ActiveSeconds { get; set; } = 0f;
        [Export] public float RecoverySeconds { get; set; } = 0f;
        [Export] public float ComboWindowStartSeconds { get; set; } = 0f;
        [Export] public float ComboWindowEndSeconds { get; set; } = 0f;
        [Export] public bool MovementLock { get; set; } = true;
        [Export] public float MovementImpulse { get; set; } = 0f;
        [Export] public float ChargeSeconds { get; set; } = 0f;
        [Export] public float MaxChargeSeconds { get; set; } = 0f;
        [Export] public float ChargedDamageMultiplier { get; set; } = 1f;

        [Export] public float ActiveWindowStart { get; set; } = 0f;
        [Export] public float ActiveWindowEnd { get; set; } = 0f;
        [Export] public float BufferWindowStart { get; set; } = 0f;
        [Export] public float BufferWindowEnd { get; set; } = 0f;

        [Export] public HitboxProfileResource HitboxProfile { get; set; }
        [Export] public Godot.Collections.Array<string> StatusEffectsToApply { get; set; } = new();
        [Export] public Godot.Collections.Array<string> ChargedStatusEffectsToApply { get; set; } = new();
        [Export] public Godot.Collections.Array<string> AbilityEffectsToApply { get; set; } = new();
        [Export] public AttackPayloadResource MeleePayload { get; set; }
        [Export] public AttackPayloadResource MagicPayload { get; set; }

        public float AuthoredDurationSeconds =>
            Mathf.Max(0f, StartupSeconds)
            + Mathf.Max(0f, ActiveSeconds)
            + Mathf.Max(0f, RecoverySeconds);

        public string Label => string.IsNullOrWhiteSpace(DebugLabel) ? SharedAnimationName : DebugLabel;
    }
}
