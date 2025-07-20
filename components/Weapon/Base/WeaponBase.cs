using Godot;
using System.Collections.Generic;

/// Base class for all pixel-art weapons.  Inherit this script—don’t modify it
/// per weapon; just tweak the exported fields in the Inspector.
///
///   RustyShortSword.tscn      → inherits WeaponBase
///   IronAxe.tscn              → inherits WeaponBase
///
/// Every concrete scene must keep the same child names:
///   Art     ← Sprite2D   (frame set by body AnimationPlayer)
///   Hitbox  ← Area2D     (Monitoring toggled ON/OFF by body AnimationPlayer)
///
/// No additional AnimationPlayer lives in the weapon scene.
[GlobalClass]
public partial class WeaponBase : Node2D, IWeapon           // IWeapon = your node-based interface
{
	/* ───────────────────────────  Inspector fields  ───────────────────────── */

	private Entity ownerCache;

	[Export] public string ItemId { get; set; }
	[Export] public string ItemName { get; set; }

	[Export] public int ItemValue { get; set; }
	[Export] public Texture2D IconSprite { get; set; }
	[Export] public int ItemStackSize { get; set; }
	[Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;

	[Export(PropertyHint.MultilineText)] public string ItemDescription { get; set; } = string.Empty;
	[Export] public Texture2D WepUpStow;
	[Export] public Texture2D WepUpDraw;
	[Export] public Texture2D WepDownStow;
	[Export] public Texture2D WepDownDraw;
	[Export] public Texture2D AbilityOverlay;

	[Export] public int Damage = 1;              // raw damage dealt when Hitbox is ON

	// Enum-key/Int-value dictionary – Godot 4.3 shows dropdown for keys
	[Export] public Godot.Collections.Array<StatDelta> StatBuffs = new();

	[Export] public Godot.Collections.Array<NodePath> ExtraActionPaths = new();

	private readonly List<IStateAction> _weaponActions = new();

	/* ─────────────────────────────  Cached children  ──────────────────────── */
	private Area2D _hitbox;
	private CollisionShape2D _hitShape;

	/* ───────────────────────────────  Runtime init  ───────────────────────── */
	public override void _Ready()
	{
		_hitbox = GetNode<Area2D>("Hitbox");
		_hitShape = (CollisionShape2D)_hitbox.GetChild(0);


		// Damage callback only when Hitbox monitoring is enabled by AnimationPlayer
		_hitbox.AreaEntered += body =>
		{
			GD.Print("Hit occured");
			if (ownerCache == null || ownerCache.IsImmune()) return;
			bool isGroup = body.IsInGroup("Hurtbox");
			bool isType = false;

			if (body.GetParent() is Entity)
			{
				isType = true;
			}

			if (body.IsInGroup("Hurtbox") && body.GetParent() is Entity target)
			{
				GD.Print("Hit Hurtbox");
				EventManager.I.Publish(
				GameEvent.Hit,
				new HitEvent(ownerCache, target));
			}
		};

		

		foreach (var p in ExtraActionPaths)
		{
			if (GetNode(p) is IStateAction act)
				_weaponActions.Add(act);
			else
				GD.PushError($"{Name}: path {p} does not implement IStateAction");
		}
	}

	/* ───────────────────────────  IWeapon interface  ─────────────────────── */

	public Dictionary<StatType, int> GetStatMods()
	{
		var dict = new Dictionary<StatType, int>();

		foreach (var buff in StatBuffs)
			dict[buff.Type] = buff.Delta;   // last entry wins if duplicates

		return dict;
	}

	public void OnEquip(Entity owner, StateMachine fsm)
	{
		ownerCache = owner;

		// Apply buffs
		foreach (var b in StatBuffs)
			owner.Data.AddModifier(b.Type, b.Delta, owner);
		// get new weapons combo phases
		fsm.MeleeTracker = new ComboTracker(GatherPhases("ComboPhases/Melee"));
		fsm.MagicTracker = new ComboTracker(GatherPhases("ComboPhases/Magic"));

		var attackState = fsm.GetState(StateType.Attack);
		// add the Attack State actions that are attached to the weapon scene
		foreach (var a in _weaponActions)
			attackState.AddAction(a, runEnter: true);

		if (owner is Player player)
		{
			player.SetWeaponSprites(this);
		}
	}

	public void OnUnequip(Entity owner, StateMachine fsm)
	{
		ownerCache = null;
		foreach (var b in StatBuffs)
			owner.Data.AddModifier(b.Type, -b.Delta, owner);

		// fsm.RemoveAction("Swing");
	}

	private List<ComboPhase> GatherPhases(string containerPath)
	{
		var list = new List<ComboPhase>();

		if (GetNodeOrNull<Node>(containerPath) is Node cont)
		{
			foreach (var child in cont.GetChildren())
				if (child is ComboPhase phase)
					list.Add(phase);
		}
		return list;
	}
		

	public void ActivateHitBox()
    {
        GD.Print($"Current Combo: {ownerCache.ActivePhase.Name} Actions this Phase {ownerCache.ActivePhase.EffectPaths.Count}");
			ComboPhase p = ownerCache.ActivePhase;

			if (p == null) return;

			var rect = _hitShape.Shape as RectangleShape2D;

			if (rect == null)
			{
				rect = new RectangleShape2D();
				_hitShape.Shape = rect;
			}
			rect.Size = new Vector2(p.Width, p.Height);

			Vector2 offset = ownerCache.FacingDirection switch
		{
			Facing.Down  => new Vector2( 0,  p.Forward),
			Facing.Up    => new Vector2( 0, -p.Forward),
			Facing.Left  => new Vector2(-p.Forward, 0),
			Facing.Right => new Vector2( p.Forward, 0),
			_            => Vector2.Zero
		};
		_hitShape.Position = offset;

		// 5) Turn the collider on, then auto-disable after the active window
		_hitShape.Disabled = false;

		var tween = CreateTween();
		tween.TweenCallback(Callable.From(() => _hitShape.Disabled = true))
			.SetDelay(p.ActiveSecs);

        
    }
}
