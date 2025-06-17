

public readonly record struct ItemPickedUp(
    Entity Player,
    string ItemId,
    int Count
);