using Godot;

namespace ethra.V1
{
    public enum HitboxShapeType
    {
        Rectangle,
        Circle
    }

    public enum HitboxDirectionalBehavior
    {
        FacingDirection,
        FixedOffset
    }

    [GlobalClass]
    public partial class HitboxProfileResource : Resource
    {
        [Export] public string ProfileId { get; set; } = string.Empty;
        [Export] public HitboxShapeType ShapeType { get; set; } = HitboxShapeType.Rectangle;
        [Export] public HitboxDirectionalBehavior DirectionalBehavior { get; set; } = HitboxDirectionalBehavior.FacingDirection;
        [Export] public Vector2 Offset { get; set; } = new(24f, 0f);
        [Export] public Vector2 Size { get; set; } = new(32f, 20f);
        [Export] public float Radius { get; set; } = 12f;
        [Export] public float ActiveDurationSeconds { get; set; } = 0.10f;
    }
}
