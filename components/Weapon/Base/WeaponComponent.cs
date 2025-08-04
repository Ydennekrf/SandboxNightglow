using Godot;
using System.Collections.Generic;

/// Keeps the correct weapon sprite / hit-box scene under WeaponHolder and
/// fires its IWeapon callbacks whenever the player equips or unequips.
public partial class WeaponComponent : Node
{
    // ────────────────────────────── Inspector ─────────────────────────────
    [Export] public NodePath WeaponHolderPath;   // drag VisualRoot/WeaponHolder
    // ───────────────────────────────────────────────────────────────────────

    private Node2D      _holder;      // resolved in _Ready()
    public Node2D?     _current;     // the weapon scene currently shown
    private Entity      _owner;       // cached player entity
    private StateMachine _fsm;        // cached state-machine (for OnEquip hooks)

    // Simple in-memory cache so we only load each scene once.
    private static readonly Dictionary<StringName, PackedScene> _sceneCache = new();

    /* ------------------------------------------------------------------ */
    /*  LIFECYCLE                                                         */
    /* ------------------------------------------------------------------ */
    public override void _Ready()
    {
        _holder = GetNode<Node2D>(WeaponHolderPath);
        if (_holder == null)
        {
            GD.PushError($"{Name}: WeaponHolderPath is not set to a valid node.");
            return;
        }

        _owner = GetOwner<Entity>();
        _fsm   = _owner.GetNode<StateMachine>("StateMachine");

        // Subscribe to the authoritative equipment event.
        EventManager.I.Subscribe<EquipmentChange>(
            GameEvent.EquipmentChanged, OnEquipmentChange);
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<EquipmentChange>(
            GameEvent.EquipmentChanged, OnEquipmentChange);
    }

    /* ------------------------------------------------------------------ */
    /*  EVENT HANDLER                                                     */
    /* ------------------------------------------------------------------ */
    private void OnEquipmentChange(EquipmentChange e)
    {
        GD.Print("Weapon COmponent Recieved the Equip CHange EVent");
        // Ignore events for other players or other slots.
        if (e.User != _owner || e.Slot != EquipmentSlot.Weapon)
            return;

        /* Unequip any existing weapon ---------------------------------- */
        if (_current != null)
        {
            if (_current is IWeapon wOld)
                wOld.OnUnequip(_owner, _fsm);

            _current.QueueFree();
            _current = null;
        }

        /* Equip the new weapon (if any) -------------------------------- */
        if (e.New == null)
            return;                       // player simply unequipped

        var scene = ResolveScene(e.New.ItemId);
        if (scene == null)
        {
            GD.PrintErr($"WeaponComponent: no scene mapped for ItemId '{e.New.ItemId}'.");
            return;
        }

        _current = scene.Instantiate<Node2D>();
        _current.Name = "Weapon";         // <–– keeps AnimationPlayer track path valid
        _holder.AddChild(_current);

        if (_current is IWeapon wNew)
            wNew.OnEquip(_owner, _fsm);
    }

    /* ------------------------------------------------------------------ */
    /*  ID → SCENE RESOLUTION                                             */
    /* ------------------------------------------------------------------ */
    private PackedScene ResolveScene(StringName itemId)
    {
        return InventoryManager.I.GetGear(InventoryManager.I.Get(itemId));
    }
}

