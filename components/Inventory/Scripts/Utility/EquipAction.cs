using Godot;
using System;

[GlobalClass]
public partial class EquipAction : Resource, IEquippableAction
{


    public void OnEquip(Entity user, StateMachine fsm, InventoryItem data) {
        
    }

    /// Called once when the item is unequipped.
    public void OnUnequip(Entity user, StateMachine fsm, InventoryItem data) {
        
    }
}