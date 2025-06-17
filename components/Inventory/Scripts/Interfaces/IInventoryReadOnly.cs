using System.Collections.Generic;
using System;

public interface IInventoryReadOnly
{
    // --------------------------------------------------------------------
    IReadOnlyList<ItemStack?> Slots { get; }   // bag slots
    IReadOnlyDictionary<EquipmentSlot, ItemStack?> Equipped { get; }

    /// Raised after any change (add/remove/equip/unequip/import).
    event Action<InventoryChange> Changed;
}
