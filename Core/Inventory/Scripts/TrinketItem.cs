using System.Collections.Generic;

namespace ethra.V1
{
    public class TrinketItem : InventoryItem
    {
        public string TrinketSlotGroup => "Trinket";

        public TrinketItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            string subtype,
            int maxStack,
            List<ItemEffects> effects = null,
            string iconPath = "")
            : base(id, name, value, description, rarity, effects, category: "Trinket", subtype: subtype, maxStack: maxStack, iconPath: iconPath)
        {
        }
    }
}
