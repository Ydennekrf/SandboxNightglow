using Godot;

public interface IEquippable
{
    EquipmentSlot slot { get; set; }

    void Equip(string ItemId);
    void UnEquip(string ItemId);
}