using Godot;

namespace ethra.V1.Actions
{
    public sealed class MoveFromInputAction : IStateAction
    {
        private readonly float _speed;

        public MoveFromInputAction(float speed)
        {
            _speed = speed;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            // no-op
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if(owner is Player player)
            {
                Vector2 dir = player.MoveInput;

				if (Engine.GetPhysicsFrames() % 30 == 0)
					GD.Print($"[MoveFromInput] state={baseState.StateID} input={dir} speed={_speed}");

				if (dir.LengthSquared() > 0.0001f)
                {
                    dir = dir.Normalized();
                }
                

                owner.DesiredVelocity = dir * _speed;
            }           
        }

        public void Exit(Entity owner)
        {

        }
    }
}
