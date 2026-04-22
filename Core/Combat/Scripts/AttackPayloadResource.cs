using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class AttackPayloadResource : Resource
    {
        [Export] public AttackOverlayMode OverlayMode { get; set; } = AttackOverlayMode.Melee;
        [Export] public int ManaCost { get; set; } = 0;

        [Export] public string DeliveryShapeId { get; set; } = "SingleTarget";
        [Export] public string DamageType { get; set; } = "Physical";
        [Export] public string ElementType { get; set; } = string.Empty;
        [Export] public float BasePower { get; set; } = 1f;

        [Export] public Godot.Collections.Array<string> EffectIds { get; set; } = new();
        [Export] public float EffectDurationSeconds { get; set; } = 0f;
    }
}
