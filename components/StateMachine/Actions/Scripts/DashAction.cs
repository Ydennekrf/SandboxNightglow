using Godot;
public partial class DashAction : Node, IStateAction
{
    [Export] public float Impulse = 350f;   // pixels per second for the burst
    [Export] public float DecelRate = 8f;   // how quickly we ease to zero
    
    public void Enter(Entity o, BaseState state)
    {

        if (o is CharacterBody2D body)
            body.Velocity = o.FacingDirection.ToVector() * Impulse;
    }

    public void Execute(float dt, Entity o, BaseState state)
    {
        if (o is CharacterBody2D body)
        {
            body.Velocity = body.Velocity.MoveToward(Vector2.Zero, DecelRate * dt);
            body.MoveAndSlide();
        }
    }

    public void Exit(Entity o)
    {
        if (o is CharacterBody2D body)
            body.Velocity = Vector2.Zero;
    }
}