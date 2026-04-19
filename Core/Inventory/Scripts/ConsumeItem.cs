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
            List<ItemEffects> effects = null)
            : base(id, name, value, description, rarity, effects, category: "Consumable", subtype: subtype, maxStack: maxStack)
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

            if (Owner is IStats stats && string.Equals(ConsumeType, "Potion", System.StringComparison.OrdinalIgnoreCase))
            {
                healAmount = 25;
                stats.CurHP = healAmount;
            }

            GD.Print($"Consumed item '{Name}' [{ConsumeType}] heal={healAmount}.");
        }
    }
}
