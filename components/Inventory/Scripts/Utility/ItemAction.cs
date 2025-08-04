using Godot;

[GlobalClass]
public abstract partial class ItemAction : Resource
{
    /// Return true if the item should be consumed after executing.
    public abstract bool Execute(
        Entity user,
        InventoryComponent inventory,
        InventoryItem data);
}