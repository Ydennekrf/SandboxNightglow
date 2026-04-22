namespace ethra.V1
{
    public sealed class AttackPayloadPacket
    {
        public Entity Source { get; init; }
        public AttackPayloadResource Payload { get; init; }
        public int ComboPhase { get; init; }
        public FacingDirection Facing { get; init; }
        public string AnimationName { get; init; } = string.Empty;
        public Godot.Vector2 OriginPosition { get; init; } = Godot.Vector2.Zero;
        public Godot.Vector2 ForwardDirection { get; init; } = Godot.Vector2.Zero;
    }
}
