using Godot;
using System;
using System.Collections.Generic;

public partial class WeaponItem : InventoryItem, IStats, IEquippable
{
    [Export] public int AttackPower;
    [Export] public int AttackSpeed;
    [Export] public PackedScene WeaponScene;
    [Export] public BodyType type;
    [Export] public EquipmentSlot slot { get; set; } = EquipmentSlot.Weapon;

    public Dictionary<StatType, Stat> Stats { get; set; }

    public Stat GetStat(StatType type) => Stats[type];

    public bool TryGetStat(StatType type, out Stat stat) => Stats.TryGetValue(type, out stat);

    [Export] public EquipAction[] EquipActions = System.Array.Empty<EquipAction>();

    public void Equip(string ItemId)
    {
        // public the OnEquip Event here

    }

    public void UnEquip(string ItemId)
    {
        
    }

}