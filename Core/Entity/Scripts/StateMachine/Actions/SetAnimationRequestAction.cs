namespace ethra.V1.Actions
{
	/// <summary>
	/// Sets the current animation.
	/// WORKS FOR ANY ENTITY with a Facing value.
	/// </summary>
	public sealed class SetAnimationRequestAction : IStateAction
	{
		private readonly string _anim;

		public SetAnimationRequestAction(string anim) => _anim = anim;

		public void Enter(Entity owner, BaseState baseState)
		{
			owner.RequestedAnimation = $"{_anim}_{owner.Facing}";
		}

		public void Execute(float delta, Entity owner, BaseState baseState)
		{
			owner.RequestedAnimation = $"{_anim}_{owner.Facing}";
		}

		public void Exit(Entity owner)
		{
			// no-op
		}
	}
}
