using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    public partial class PlayerMenu : Control, IUIRefresh
    {
        [Export] public NodePath InventoryGridPath { get; set; } = "NinePatchRect/VBoxContainer/Inventory/VBoxContainer2/Inventory";
        [Export] public NodePath WeaponEquipPath { get; set; } = "NinePatchRect/VBoxContainer/Inventory/VBoxContainer/PlayerView/HBoxContainer/PlayerEquip/WeaponEquip";
        [Export] public NodePath ArmorEquipPath { get; set; } = "NinePatchRect/VBoxContainer/Inventory/VBoxContainer/PlayerView/HBoxContainer/PlayerEquip/ArmorEquip";

        public bool needsRefresh { get; set; } = true;

        private readonly List<InventorySlotView> _inventorySlots = new();
        private InventorySlotView _weaponSlot;
        private InventorySlotView _armorSlot;

        public override void _Ready()
        {
            CacheSlots();
            BindSlots();

            GameManager.Instance?.UI?.Register(this);
            needsRefresh = true;
        }

        public void Refresh()
        {
            InventoryManager inventory = GameManager.Instance?.Inventory;
            MasterRepository repo = GameManager.Instance?.DB;

            if (inventory == null || repo == null)
            {
                needsRefresh = false;
                return;
            }

            foreach (InventorySlotView slot in _inventorySlots)
            {
                slot.SetEmpty();
            }

            if (_weaponSlot != null)
            {
                _weaponSlot.SetEmpty();
            }

            if (_armorSlot != null)
            {
                _armorSlot.SetEmpty();
            }

            int slotIndex = 0;
            foreach (KeyValuePair<int, int> stack in inventory.GetItemCounts())
            {
                if (slotIndex >= _inventorySlots.Count)
                {
                    break;
                }

                InventoryItem itemData = repo.GetItemFromRepo(stack.Key);
                if (itemData == null || stack.Value <= 0)
                {
                    continue;
                }

                InventorySlotView slot = _inventorySlots[slotIndex++];
                slot.SetItem(itemData.Id, stack.Value);
                slot.SetEquipped(inventory.IsEquipped(itemData.Id));
            }

            PopulateEquipSlots(inventory, repo);
            needsRefresh = false;
        }

        private void CacheSlots()
        {
            _inventorySlots.Clear();

            GridContainer grid = GetNodeOrNull<GridContainer>(InventoryGridPath);
            if (grid == null)
            {
                GD.PushError($"PlayerMenu: Inventory grid not found at '{InventoryGridPath}'.");
                return;
            }

            foreach (Node child in grid.GetChildren())
            {
                if (child is InventorySlotView slot)
                {
                    _inventorySlots.Add(slot);
                }
            }

            _weaponSlot = GetNodeOrNull<InventorySlotView>(WeaponEquipPath);
            _armorSlot = GetNodeOrNull<InventorySlotView>(ArmorEquipPath);
        }

        private void BindSlots()
        {
            foreach (InventorySlotView slot in _inventorySlots)
            {
                slot.Clicked += OnSlotClicked;
            }

            if (_weaponSlot != null)
            {
                _weaponSlot.Clicked += OnSlotClicked;
            }

            if (_armorSlot != null)
            {
                _armorSlot.Clicked += OnSlotClicked;
            }
        }

        private void PopulateEquipSlots(InventoryManager inventory, MasterRepository repo)
        {
            foreach (KeyValuePair<string, int> weapon in inventory.GetEquippedWeaponSlots())
            {
                if (_weaponSlot == null)
                {
                    continue;
                }

                InventoryItem item = repo.GetItemFromRepo(weapon.Value);
                if (item != null)
                {
                    _weaponSlot.SetItem(item.Id, 1);
                    _weaponSlot.SetEquipped(true);
                }
            }

            foreach (KeyValuePair<string, int> armor in inventory.GetEquippedArmorSlots())
            {
                if (_armorSlot == null)
                {
                    continue;
                }

                InventoryItem item = repo.GetItemFromRepo(armor.Value);
                if (item != null)
                {
                    _armorSlot.SetItem(item.Id, 1);
                    _armorSlot.SetEquipped(true);
                    break;
                }
            }
        }

        private void OnSlotClicked(int itemId)
        {
            if (itemId <= 0)
            {
                return;
            }

            GameManager.Instance?.UseItem(itemId);
            needsRefresh = true;
        }
    }
}
