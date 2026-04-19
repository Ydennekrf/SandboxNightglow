using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public class WeaponItem : InventoryItem
    {
        private bool _isEquipped;

        public string WeaponSlot => "MainHand";
        public bool IsEquipped => _isEquipped;

        public WeaponItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            int maxStack,
            List<ItemEffects> effects = null)
            : base(id, name, value, description, rarity, effects, category: "Weapon", subtype: "MainHand", maxStack: maxStack)
        {
        }

        public void Equip()
        {
            if (_isEquipped)
            {
                return;
            }

            foreach (ItemEffects effect in Effects)
            {
                if (effect is IEffect itemEffect)
                {
                    itemEffect.ResolveItemEffect();
                }
            }

            _isEquipped = true;
            GD.Print($"Equipped weapon '{Name}' in slot '{WeaponSlot}'.");
        }

        public void Unequip()
        {
            if (!_isEquipped)
            {
                return;
            }

            foreach (ItemEffects effect in Effects)
            {
                if (effect is IEffect itemEffect)
                {
                    itemEffect.RemoveItemEffect();
                }
            }

            _isEquipped = false;
            GD.Print($"Unequipped weapon '{Name}' from slot '{WeaponSlot}'.");
        }

        public override void Use()
        {
            if (IsEquipped)
            {
                Unequip();
            }
            else
            {
                Equip();
            }
        }
    }
}
