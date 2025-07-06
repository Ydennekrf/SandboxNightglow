using Godot;
using System.Collections.Generic;

/// Runs a single animation clip AND optional phase-actions.
/// • One node per swing / cast.
/// • Executes extra effects only while this phase is active.
public partial class ComboPhase : Node, IStateAction
{
	[Export] public string AnimationName = "";
	[Export] public bool useDirectional;
	[Export] public float ComboWindow = 0.25f;     // seconds before clip end
	[Export] public int Damage = 5;
	[Export] public DamageType DmgType;

	// Drop other nodes (IPhaseAction) into this for custom effects
	[Export] public Godot.Collections.Array<NodePath> EffectPaths = new();

	[ExportGroup("HitBox")]
	[Export] public float Width = 32;
	[Export] public float Height = 12;
	[Export] public float Forward = 28;
	[Export] public float ActiveSecs = 0.10f;

	private AnimationPlayer _anim;
	private Entity _owner;
	private List<IPhaseAction> _effects = new();

	public bool IsDone { get; private set; }
	public bool QueueNext { get; private set; }

	public override void _Ready()
	{
		foreach (var p in EffectPaths)
			if (GetNode(p) is IPhaseAction eff) _effects.Add(eff);
	}

	public void Enter(Entity owner, BaseState state)
	{
		
		_owner = owner;

		string clip = AnimationName;
		if (useDirectional)
		{
			var suffix = owner.FacingDirection switch
			{
				Facing.Down => "_Down",
				Facing.Up => "_Up",
				Facing.Left => "_Left",
				Facing.Right => "_Right",
				_ => "_Down"
			};
			clip += suffix;
		}



		_owner._anim.Play(clip);

		foreach (var e in _effects) e.OnPhaseStart(owner, this);

		IsDone = false;
		QueueNext = false;
	}

	public void Execute(float delta, Entity owner, BaseState state)
	{
		
		float timeLeft = (float)(_owner._anim.CurrentAnimationLength - _owner._anim.CurrentAnimationPosition);

		if (timeLeft < ComboWindow &&
			Input.IsActionJustPressed(state.InputActionName))
			QueueNext = true;

		if (_owner._anim.CurrentAnimationPosition >= _owner._anim.CurrentAnimationLength)
			IsDone = true;
	}

	public void Exit(Entity owner)
	{

		foreach (var e in _effects) e.OnPhaseEnd(owner, this);
		_owner = null;
	}

	private void OnHit(HitEvent payload)
	{
		if (payload.attacker != _owner)
		{
			return;
		}

		payload.target.TakeDamage(Damage, DmgType, payload.attacker);
	}
}
