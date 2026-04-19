using System.Collections.Generic;

namespace ethra.V1
{
    public class BasicInventoryItem : InventoryItem
    {
        public BasicInventoryItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            List<ItemEffects> effects = null,
            string category = "",
            string subtype = "",
            int maxStack = 99,
            string iconPath = "")
            : base(id, name, value, description, rarity, effects, category, subtype, maxStack, iconPath)
        {
        }
    }
}
