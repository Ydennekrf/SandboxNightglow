namespace ethra.V1
{
    public sealed class AttackPayloadPacket
    {
        public Entity Source { get; init; }
        public AttackPayloadResource Payload { get; init; }
        public System.Collections.Generic.List<string> AdditionalEffectIds { get; } = new();
        public string ComboId { get; init; } = string.Empty;
        public string ComboStepId { get; init; } = string.Empty;
        public string ComboStepLabel { get; init; } = string.Empty;
        public int ComboPhase { get; init; }
        public bool IsCharged { get; init; }
        public float ChargeSeconds { get; init; }
        public FacingDirection Facing { get; init; }
        public string AnimationName { get; init; } = string.Empty;
        public HitboxProfileResource HitboxProfile { get; init; }
        public Godot.Vector2 OriginPosition { get; init; } = Godot.Vector2.Zero;
        public Godot.Vector2 ForwardDirection { get; init; } = Godot.Vector2.Zero;
    }
}
