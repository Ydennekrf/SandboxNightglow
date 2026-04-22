using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class ComboPhaseResource : Resource
    {
        [Export] public string SharedAnimationName { get; set; } = "Melee1";
        [Export] public bool PreferFacingSuffix { get; set; } = true;
        [Export] public float DurationOverrideSeconds { get; set; } = 0f;

        [Export] public float ActiveWindowStart { get; set; } = 0f;
        [Export] public float ActiveWindowEnd { get; set; } = 0f;
        [Export] public float BufferWindowStart { get; set; } = 0f;
        [Export] public float BufferWindowEnd { get; set; } = 0f;

        [Export] public AttackPayloadResource MeleePayload { get; set; }
        [Export] public AttackPayloadResource MagicPayload { get; set; }
    }
}
