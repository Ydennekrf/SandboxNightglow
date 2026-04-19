using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public class CraftingItem : InventoryItem
    {
        public string MaterialType => Subtype;

        public CraftingItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            string subtype,
            int maxStack,
            List<ItemEffects> effects = null)
            : base(id, name, value, description, rarity, effects, category: "Crafting", subtype: subtype, maxStack: maxStack)
        {
        }

        public override void Use()
        {
            GD.Print($"Crafting item '{Name}' cannot be used directly.");
        }
    }
}
