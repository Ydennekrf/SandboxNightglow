using System;

public readonly record struct UseItemRequest(
    ItemStack item,
    int slot

);