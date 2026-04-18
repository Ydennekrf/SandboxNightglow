using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public class ArmorItem : InventoryItem
    {
        private bool _isEquipped;

        public string ArmorSlot => Subtype;
        public bool IsEquipped => _isEquipped;

        public ArmorItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            string slot,
            int maxStack,
            List<ItemEffects> effects = null)
            : base(id, name, value, description, rarity, effects, category: "Armor", subtype: slot, maxStack: maxStack)
        {
        }

        public void Equip()
        {
            _isEquipped = true;
            GD.Print($"Equipped armor '{Name}' in slot '{ArmorSlot}'.");
        }

        public void Unequip()
        {
            _isEquipped = false;
            GD.Print($"Unequipped armor '{Name}' from slot '{ArmorSlot}'.");
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
