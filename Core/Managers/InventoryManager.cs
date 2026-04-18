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
        public event Action Changed;

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
                     NotifyChanged();
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
                NotifyChanged();
                return true;
            }
        }

        public object CaptureSnapshot()
        {
            List<int> items = new List<int>();
            foreach(int id in _itemDict.Keys)
            {
                int count = _itemDict[id];
                for(int i =0; i < count; i++)
                {
                    items.Add(id);
                }
            }
            return items.ToArray();
        }

        public void DropItem(int id)
        {
            throw new NotImplementedException();
            // this will require a spawner manager or something.
        }


        public void RestoreSnapshot(object snapshot)
        {
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
                if (itemToUse is ArmorItem armor)
                {
                    ToggleArmorEquip(armor);
                    NotifyChanged();
                    return;
                }

                if (itemToUse is WeaponItem weapon)
                {
                    ToggleWeaponEquip(weapon);
                    NotifyChanged();
                    return;
                }

                itemToUse.Use();

                if (itemToUse is ConsumeItem)
                {
                    ConsumeOne(id);
                    NotifyChanged();
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

        private void ConsumeOne(int id)
        {
            if(!_itemDict.TryGetValue(id, out int currentCount) || currentCount <= 0)
            {
                return;
            }

            if(currentCount == 1)
            {
                _itemDict.Remove(id);
            }
            else
            {
                _itemDict[id] = currentCount - 1;
            }
        }

        public IReadOnlyDictionary<int, int> GetItemCounts()
        {
            return _itemDict;
        }

        public IReadOnlyDictionary<string, int> GetEquippedWeapons()
        {
            return _equippedWeaponBySlot;
        }

        public IReadOnlyDictionary<string, int> GetEquippedArmor()
        {
            return _equippedArmorBySlot;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }


    }
}
