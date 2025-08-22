using Godot;

public partial class BleedEffect : Node, IStatusEffect
{
	public int   DamagePerTick = 1;
	public float TickRate      = 1.0f;  // seconds
	public float Duration      = 4.0f;  // seconds
	public StatusEffectType type;


	private float _timer;
	private float _elapsed;

	public void Start(Entity target)
	{
		_timer   = TickRate;
		_elapsed = 0f;
	}

	public bool Tick(float delta, Entity target)
	{
		_elapsed += delta;
		_timer   -= delta;

		if (_timer <= 0f)
		{

			// to ensure the status effect notifier plays

			target.AddStatus(type);


			target.TakeDamage(DamagePerTick, DamageType.Slash, false);
			_timer = TickRate;
		}

		return _elapsed >= Duration;    // done when duration elapsed
	}
}
