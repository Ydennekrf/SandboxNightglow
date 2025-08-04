
using Godot;
using System;

[GlobalClass]
public partial class InventoryItem : Resource
{
    [Export] public string ItemId { get; set; } = string.Empty;
    [Export] public string ItemName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string ItemDescription { get; set; } = string.Empty;


    [Export] public int ItemValue { get; set; }
    [Export] public Texture2D IconSprite { get; set; }
    [Export] public int ItemStackSize { get; set; }
    [Export] public ItemRarity Rarity          { get; set; } = ItemRarity.Common;
    [Export] public ItemType itemType { get; set; }
    [Export] public string AssetPath { get; set; } // scene or resource location

    // these happen on use
    [Export] public ItemAction[] Actions     { get; set; } = Array.Empty<ItemAction>();
}