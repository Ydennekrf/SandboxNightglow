using System.Collections.Generic;

namespace ethra.V1
{
    public class InventorySave
    {
        public List<InventoryStackEntry> Items { get; set; } = new();
        public Dictionary<string, int> EquippedArmorBySlot { get; set; } = new();
        public Dictionary<string, int> EquippedWeaponBySlot { get; set; } = new();
    }

    public class InventoryStackEntry
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
