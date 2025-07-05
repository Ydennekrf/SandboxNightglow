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

    /* ───────────────────────────────  Runtime init  ───────────────────────── */
    public override void _Ready()
    {
        _hitbox = GetNode<Area2D>("Hitbox");


        // Damage callback only when Hitbox monitoring is enabled by AnimationPlayer
        _hitbox.BodyEntered += body =>
        {
            if (body.IsInGroup("hurtbox"))
                body.Call("TakeDamage", Damage);
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
        // Apply buffs
        foreach (var b in StatBuffs)
            owner.Data.AddModifier(b.Type, b.Delta);
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
        foreach (var b in StatBuffs)
            owner.Data.AddModifier(b.Type, -b.Delta);

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
}
