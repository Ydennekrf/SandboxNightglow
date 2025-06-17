using Godot;
using System.Collections.Generic;

/// Implement this on the root script of every weapon scene.
/// WeaponComponent calls the two lifecycle methods when the item
/// is equipped / unequipped, and it queries stat bonuses once.
public interface IWeapon
{
    /// Return a dictionary of statType → delta that should be applied
    /// *while the weapon is equipped*.  Positive or negative deltas are ok.
    Dictionary<StatType, int> GetStatMods();

    /// Called exactly once when the weapon scene has been added under
    /// WeaponHolder and renamed “Weapon”.
    /// Typical tasks here:
    ///   • Apply stat modifiers.
    ///   • Register actions / callbacks with the player’s StateMachine.
    ///   • Reset timers, play equip SFX, etc.
    void OnEquip(Entity owner, StateMachine fsm);

    /// Called once right before the weapon scene is freed.
    /// Undo everything you did in OnEquip.
    void OnUnequip(Entity owner, StateMachine fsm);
}
