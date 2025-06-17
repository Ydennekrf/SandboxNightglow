using Godot;
using System;
using System.Collections.Generic;


public readonly record struct EquipmentChange(
    Entity User,
    EquipmentSlot Slot,
    InventoryItem? Old,
    InventoryItem? New
);    