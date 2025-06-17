using Godot;
using System.Collections.Generic;

/// <summary>
/// Reversible stat bonuses that are applied while an item is equipped.
/// Attach one or more of these to a WeaponItem / ArmorItem via the
/// IEquippable.Actions array.
/// </summary>
[GlobalClass]               // appear in “New Resource” dialog
public partial class StatModifierAction :  EquipAction
{

    public Godot.Collections.Dictionary<StatType, int> Buffs = new();

    // --------------------------------------------------------------------
    // 2) Runtime bookkeeping (so we undo exactly what we added)
    // --------------------------------------------------------------------
    // Using a C# dictionary here; no need to serialise this field.
    private readonly Dictionary<StatType, int> _applied = new();

    // --------------------------------------------------------------------
    // 3) Called by WeaponComponent / Inventory system when the item equips
    // --------------------------------------------------------------------
    public void OnEquip(Entity wearer, StateMachine stateMachine, InventoryItem item)
    {
        var data = wearer.Data;      // EntityData (has GetValue/AddModifier)

        foreach (var kv in Buffs)
        {
            data.AddModifier(kv.Key, kv.Value);
            _applied[kv.Key] = kv.Value;
        }

        GD.Print($"[StatModifier] +{string.Join(", +", Buffs)} applied to {wearer.Name}");
    }

    // --------------------------------------------------------------------
    // 4) Called on unequip — perfect reversal
    // --------------------------------------------------------------------
    public void OnUnequip(Entity wearer, StateMachine stateMachine, InventoryItem item)
    {
        var data = wearer.Data;

        foreach (var kv in _applied)
            data.AddModifier(kv.Key, -kv.Value);

        _applied.Clear();

        GD.Print($"[StatModifier] buffs removed from {wearer.Name}");
    }
}
