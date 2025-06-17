using Godot;

[GlobalClass]                 // shows up in the “New Resource” dialog
public abstract partial class ItemAction : Resource, IItemAction
{
    /// Return true if the item should be consumed after executing.
    public abstract bool Execute(
        Entity user,
        InventoryComponent inventory,
        InventoryItem data);
}