public interface IEquippableAction
{
    /// Called once when the item is equipped.
    void OnEquip(Entity user, StateMachine fsm, InventoryItem data);

    /// Called once when the item is unequipped.
    void OnUnequip(Entity user, StateMachine fsm, InventoryItem data);
}