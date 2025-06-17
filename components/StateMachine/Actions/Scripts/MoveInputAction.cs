using Godot;

public partial class MoveInputAction : Node, IStateAction
{
    [Export] public float SpeedModifier = 1.0f;
    private float Speed = 0;

    public void Enter(Entity owner, BaseState state)
    {
        
        Speed = owner.Data.EntityStats[StatType.MoveSpeed].Value * SpeedModifier;
     }
    public void Exit(Entity owner)
    {
        
     }

    public void Execute(float delta, Entity owner, BaseState state)
    {
        if (owner is not CharacterBody2D body) return;
        // Godot‑style twin‑stick vector
        Vector2 dir = Input.GetVector("Left", "Right", "Up", "Down");
        owner.UpdateFacing(dir);
        // Set velocity & slide every physics tick
        body.Velocity = dir.LengthSquared() > 0.01f
                        ? dir.Normalized() * Speed
                        : Vector2.Zero;

        body.MoveAndSlide();
    }
}
