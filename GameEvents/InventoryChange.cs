
public readonly record struct InventoryChange(
    int SlotIndex,
    ItemStack? OldValue,
    ItemStack? NewValue
);