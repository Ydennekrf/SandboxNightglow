using Godot;
using System;
using System.Collections.Generic;

public partial class ArmorItem : InventoryItem, IStats
{
    public int DefencePower;

    [Export] public Texture2D Clothes;
    [Export] public BodyType type;
    [Export(PropertyHint.Enum)]
    public Godot.Collections.Dictionary<StatType, int> statsImport;

    public Dictionary<StatType, Stat> Stats { get; set; }

    public Stat GetStat(StatType type) => Stats[type];

    public bool TryGetStat(StatType type, out Stat stat) => Stats.TryGetValue(type, out stat);
}