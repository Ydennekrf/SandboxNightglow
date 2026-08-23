using System.Collections.Generic;

namespace ethra.V1
{
    public class InventorySave
    {
        public List<InventoryStackEntry> Items { get; set; } = new();
        public List<WeaponInstanceSave> WeaponInstances { get; set; } = new();
        public Dictionary<string, int> EquippedArmorBySlot { get; set; } = new();
        public Dictionary<string, int> EquippedTrinketBySlot { get; set; } = new();
        public Dictionary<string, int> EquippedWeaponBySlot { get; set; } = new();
        public Dictionary<string, string> EquippedWeaponInstanceBySlot { get; set; } = new();
    }

    public class InventoryStackEntry
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class WeaponInstanceSave
    {
        public string InstanceId { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public List<int> UpgradeRuneItemIds { get; set; } = new();
        public int? ElementalRuneItemId { get; set; }
    }
}
