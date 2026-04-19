using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public class ConsumeItem : InventoryItem
    {
        public string ConsumeType => Subtype;

        public ConsumeItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            string subtype,
            int maxStack,
            List<ItemEffects> effects = null,
            string iconPath = "")
            : base(id, name, value, description, rarity, effects, category: "Consumable", subtype: subtype, maxStack: maxStack, iconPath: iconPath)
        {
        }

        public override void Use()
        {
            int healAmount = 0;

            foreach (ItemEffects effect in Effects)
            {
                if (effect is IEffect itemEffect)
                {
                    itemEffect.ResolveItemEffect();
                }
            }

        

            GD.Print($"Consumed item '{Name}' [{ConsumeType}] heal={healAmount}.");
        }
    }
}
