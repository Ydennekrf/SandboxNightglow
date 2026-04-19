using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace ethra.V1
{
    public partial class InventoryManager :  ISaveable, IInventory
	{
		private string _saveKey = "Inventory";
        /// <summary>
        /// <id, count>
        /// </summary>
        private Dictionary<int, int> _itemDict;
        private Dictionary<string, int> _equippedArmorBySlot;
        private Dictionary<string, int> _equippedWeaponBySlot;
        private int maxStack = 99;
        private readonly MasterRepository _db;

		public string SaveKey => _saveKey;

        public InventoryManager(MasterRepository db)
        {
            _db = db;
            _itemDict = new Dictionary<int, int>();
            _equippedArmorBySlot = new Dictionary<string, int>();
            _equippedWeaponBySlot = new Dictionary<string, int>();
        }


        public bool AddItem(int id)
        {
            InventoryItem itemData = _db.GetItemFromRepo(id);
            if(itemData == null)
            {
                GD.Print($"Unable to add item to inventory. id:{id} not found in repo.");
                return false;
            }

            int maxAllowed = itemData.MaxStack > 0 ? itemData.MaxStack : maxStack;

            if(_itemDict.TryGetValue(id, out int count))
            {
                if(count + 1 <= maxAllowed)
                {
                     _itemDict[id] = count + 1;
                     return true;
                }
                else
                {
                    GD.Print($"Unable to add item to inventory. max stack exceeded for id:{id}.");
                    return false;
                }
               
            }
            else
            {
                _itemDict.Add(id, 1);
                return true;
            }
        }

        public object CaptureSnapshot()
        {
            InventorySave save = new InventorySave();

            foreach(var kvp in _itemDict)
            {
                if (kvp.Value <= 0)
                {
                    continue;
                }

                save.Items.Add(new InventoryStackEntry
                {
                    ItemId = kvp.Key,
                    Quantity = kvp.Value
                });
            }

            save.EquippedArmorBySlot = new Dictionary<string, int>(_equippedArmorBySlot);
            save.EquippedWeaponBySlot = new Dictionary<string, int>(_equippedWeaponBySlot);
            return save;
        }

        public void DropItem(int id)
        {
            if(!_itemDict.TryGetValue(id, out int currentCount) || currentCount <= 0)
            {
                GD.Print($"DropItem: item id:{id} is not in inventory.");
                return;
            }

            if (currentCount == 1)
            {
                _itemDict.Remove(id);
                UnequipItemIfNeeded(id);
            }
            else
            {
                _itemDict[id] = currentCount - 1;
            }

            GD.Print($"Dropped item id:{id}. Remaining quantity: {_itemDict.GetValueOrDefault(id, 0)}");
        }


        public void RestoreSnapshot(object snapshot)
        {
            ClearAllEquipment();
            _itemDict.Clear();

            if(snapshot is InventorySave inventorySave)
            {
                RestoreFromInventorySave(inventorySave);
                return;
            }

            if(snapshot is List<int> listItems)
            {
                foreach(int i in listItems)
                {
                    AddItem(i);
                }
            }
            else if(snapshot is int[] arrayItems)
            {
                foreach(int i in arrayItems)
                {
                    AddItem(i);
                }
            }
        }

        public void UseItem(int id)
        {
            if(!_itemDict.TryGetValue(id, out int count) || count <= 0)
            {
                GD.Print($"Unable to use item. id:{id} is not in inventory.");
                return;
            }

            InventoryItem itemToUse = _db.GetItemFromRepo(id);

            if(itemToUse != null)
            {
                Player player = GameManager.Instance?.GetPlayer();
                if (player != null)
                {
                    itemToUse.SetOwner(player);
                }

                if (itemToUse is ArmorItem armor)
                {
                    ToggleArmorEquip(armor);
                    return;
                }

                if (itemToUse is WeaponItem weapon)
                {
                    ToggleWeaponEquip(weapon);
                    return;
                }

                itemToUse.Use();

                if (itemToUse is ConsumeItem)
                {
                    int remaining = ConsumeOne(id);
                    GD.Print($"Consumed item id:{id}. Remaining quantity: {remaining}");
                }
            }
            else
            {
                GD.Print($"Item not found in master repo id:{id}");
            }
            

            // at this point check what type of item to know if you consume it on use, and drop the count of the inventory and call
            //then set the UI as dirty forcing it to update.
        }

        private void ToggleArmorEquip(ArmorItem armor)
        {
            string slot = string.IsNullOrWhiteSpace(armor.ArmorSlot) ? "Armor" : armor.ArmorSlot;

            if (_equippedArmorBySlot.TryGetValue(slot, out int equippedId))
            {
                if (equippedId == armor.Id)
                {
                    armor.Unequip();
                    _equippedArmorBySlot.Remove(slot);
                    return;
                }

                InventoryItem currentlyEquipped = _db.GetItemFromRepo(equippedId);
                if (currentlyEquipped is ArmorItem equippedArmor)
                {
                    equippedArmor.Unequip();
                }
            }

            armor.Equip();
            _equippedArmorBySlot[slot] = armor.Id;
        }

        private void ToggleWeaponEquip(WeaponItem weapon)
        {
            string slot = weapon.WeaponSlot;

            if (_equippedWeaponBySlot.TryGetValue(slot, out int equippedId))
            {
                if (equippedId == weapon.Id)
                {
                    weapon.Unequip();
                    _equippedWeaponBySlot.Remove(slot);
                    return;
                }

                InventoryItem currentlyEquipped = _db.GetItemFromRepo(equippedId);
                if (currentlyEquipped is WeaponItem equippedWeapon)
                {
                    equippedWeapon.Unequip();
                }
            }

            weapon.Equip();
            _equippedWeaponBySlot[slot] = weapon.Id;
        }

        private int ConsumeOne(int id)
        {
            if(!_itemDict.TryGetValue(id, out int currentCount) || currentCount <= 0)
            {
                return 0;
            }

            if(currentCount == 1)
            {
                _itemDict.Remove(id);
                return 0;
            }
            else
            {
                int remaining = currentCount - 1;
                _itemDict[id] = remaining;
                return remaining;
            }
        }

        private void RestoreFromInventorySave(InventorySave save)
        {
            if (save?.Items != null)
            {
                foreach (InventoryStackEntry entry in save.Items)
                {
                    if (entry == null || entry.Quantity <= 0)
                    {
                        continue;
                    }

                    for (int i = 0; i < entry.Quantity; i++)
                    {
                        AddItem(entry.ItemId);
                    }
                }
            }

            if (save?.EquippedArmorBySlot != null)
            {
                foreach (var kvp in save.EquippedArmorBySlot)
                {
                    if (!_itemDict.TryGetValue(kvp.Value, out int qty) || qty <= 0)
                    {
                        continue;
                    }

                    InventoryItem item = _db.GetItemFromRepo(kvp.Value);
                    if (item is not ArmorItem armor)
                    {
                        continue;
                    }

                    Player player = GameManager.Instance?.GetPlayer();
                    if (player != null)
                    {
                        armor.SetOwner(player);
                    }

                    armor.Equip();
                    _equippedArmorBySlot[kvp.Key] = kvp.Value;
                }
            }

            if (save?.EquippedWeaponBySlot != null)
            {
                foreach (var kvp in save.EquippedWeaponBySlot)
                {
                    if (!_itemDict.TryGetValue(kvp.Value, out int qty) || qty <= 0)
                    {
                        continue;
                    }

                    InventoryItem item = _db.GetItemFromRepo(kvp.Value);
                    if (item is not WeaponItem weapon)
                    {
                        continue;
                    }

                    Player player = GameManager.Instance?.GetPlayer();
                    if (player != null)
                    {
                        weapon.SetOwner(player);
                    }

                    weapon.Equip();
                    _equippedWeaponBySlot[kvp.Key] = kvp.Value;
                }
            }
        }

        private void UnequipItemIfNeeded(int id)
        {
            InventoryItem item = _db.GetItemFromRepo(id);

            if (item is ArmorItem armor)
            {
                armor.Unequip();
                string foundSlot = null;
                foreach (var slot in _equippedArmorBySlot)
                {
                    if (slot.Value == id)
                    {
                        foundSlot = slot.Key;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(foundSlot))
                {
                    _equippedArmorBySlot.Remove(foundSlot);
                }
            }
            else if (item is WeaponItem weapon)
            {
                weapon.Unequip();
                string foundSlot = null;
                foreach (var slot in _equippedWeaponBySlot)
                {
                    if (slot.Value == id)
                    {
                        foundSlot = slot.Key;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(foundSlot))
                {
                    _equippedWeaponBySlot.Remove(foundSlot);
                }
            }
        }

        private void ClearAllEquipment()
        {
            foreach (var kvp in _equippedArmorBySlot)
            {
                if (_db.GetItemFromRepo(kvp.Value) is ArmorItem armor)
                {
                    armor.Unequip();
                }
            }

            foreach (var kvp in _equippedWeaponBySlot)
            {
                if (_db.GetItemFromRepo(kvp.Value) is WeaponItem weapon)
                {
                    weapon.Unequip();
                }
            }

            _equippedArmorBySlot.Clear();
            _equippedWeaponBySlot.Clear();
        }


    }
}
