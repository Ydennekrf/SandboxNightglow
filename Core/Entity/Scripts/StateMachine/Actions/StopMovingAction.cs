using Godot;

namespace ethra.V1.Actions
{
    public sealed class StopMovingAction : IStateAction
    {
        public void Enter(Entity owner, BaseState baseState)
        {
            owner.DesiredVelocity = Vector2.Zero;
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            owner.DesiredVelocity = Vector2.Zero;
        }

        public void Exit(Entity owner)
        {
            // no-op
        }
    }
}
