using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    /// <summary>
    /// Owns inventory stacks, equipment slots, weapon instance state, and inventory save snapshots.
    /// </summary>
    /// <remarks>
    /// InventoryManager treats item definitions from MasterRepository as static data and stores only runtime
    /// quantities/equipped state here. UI panels should query this manager rather than editing dictionaries.
    /// </remarks>
    public partial class InventoryManager :  ISaveable, IInventory
	{
        /// <summary>
        /// UI-facing flattened inventory row. Weapons appear as individual rows so rune state can be selected.
        /// </summary>
        public sealed class InventoryDisplayEntry
        {
            public int ItemId { get; init; }
            public int Quantity { get; init; } = 1;
            public string WeaponInstanceId { get; init; } = string.Empty;
        }

        public sealed class EquipmentChangeResult
        {
            public bool Success { get; }
            public string Message { get; }

            public EquipmentChangeResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }
        }

        private const bool DebugWeaponVisuals = true;
		private string _saveKey = "Inventory";
        /// <summary>Runtime stack counts keyed by static numeric item ID.</summary>
        private Dictionary<int, int> _itemDict;
        private Dictionary<string, int> _equippedArmorBySlot;
        private Dictionary<string, int> _equippedTrinketBySlot;
        private Dictionary<string, int> _equippedWeaponBySlot;
        private Dictionary<string, string> _equippedWeaponInstanceBySlot;
        private Dictionary<string, WeaponInstanceState> _weaponInstancesById;
        private int maxStack = 99;
        private readonly MasterRepository _db;
        public event Action Changed;

		public string SaveKey => _saveKey;

        public InventoryManager(MasterRepository db)
        {
            _db = db;
            _itemDict = new Dictionary<int, int>();
            _equippedArmorBySlot = new Dictionary<string, int>();
            _equippedTrinketBySlot = new Dictionary<string, int>();
            _equippedWeaponBySlot = new Dictionary<string, int>();
            _equippedWeaponInstanceBySlot = new Dictionary<string, string>();
            _weaponInstancesById = new Dictionary<string, WeaponInstanceState>();
        }


        /// <summary>
        /// Adds one item when capacity rules allow it.
        /// </summary>
        public bool AddItem(int id)
        {
            InventoryItem itemData = _db.GetItemFromRepo(id);
            if(itemData == null)
            {
                GD.Print($"Unable to add item to inventory. id:{id} not found in repo.");
                return false;
            }

            int maxAllowed = itemData.MaxStack > 0 ? itemData.MaxStack : maxStack;
            if (itemData is WeaponItem)
            {
                CreateWeaponInstance(id);
                _itemDict[id] = GetItemCount(id) + 1;
                Changed?.Invoke();
                PublishItemCollected(id, 1);
                return true;
            }

            if (itemData.MaxStack <= 1)
            {
                _itemDict[id] = GetItemCount(id) + 1;
                Changed?.Invoke();
                PublishItemCollected(id, 1);
                return true;
            }

            if(_itemDict.TryGetValue(id, out int count))
            {
				if(count + 1 <= maxAllowed)
				{
					 _itemDict[id] = count + 1;
					 Changed?.Invoke();
					 PublishItemCollected(id, 1);
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
				Changed?.Invoke();
				PublishItemCollected(id, 1);
				return true;
			}
		}

        /// <summary>
        /// Adds multiple items as one transaction, creating per-weapon instances when needed.
        /// </summary>
        public bool AddItemQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            InventoryItem itemData = _db.GetItemFromRepo(id);
            if (itemData == null)
            {
                GD.Print($"Unable to add item to inventory. id:{id} not found in repo.");
                return false;
            }

            if (!CanAddItem(id, quantity))
            {
                GD.Print($"Unable to add item to inventory. max stack exceeded for id:{id} quantity:{quantity}.");
                return false;
            }

            int currentCount = GetItemCount(id);
            if (itemData is WeaponItem)
            {
                for (int i = 0; i < quantity; i++)
                {
                    CreateWeaponInstance(id);
                }
            }
            _itemDict[id] = currentCount + quantity;
            Changed?.Invoke();
            PublishItemCollected(id, quantity);
            return true;
        }

        /// <summary>
        /// Checks stack capacity without mutating the inventory.
        /// </summary>
        public bool CanAddItem(int id, int quantity = 1)
        {
            if (quantity <= 0)
            {
                return false;
            }

            InventoryItem itemData = _db.GetItemFromRepo(id);
            if (itemData == null)
            {
                return false;
            }

            int maxAllowed = itemData.MaxStack > 0 ? itemData.MaxStack : maxStack;
            if (itemData is WeaponItem)
            {
                return true;
            }

            if (itemData.MaxStack <= 1)
            {
                return true;
            }

            return GetItemCount(id) + quantity <= maxAllowed;
        }

        public int GetItemCount(int id)
        {
            return _itemDict.TryGetValue(id, out int count) ? count : 0;
        }

        public bool HasItemQuantity(int id, int quantity)
        {
            return quantity > 0 && GetItemCount(id) >= quantity;
        }

        public bool RemoveItemQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            if (!HasItemQuantity(id, quantity))
            {
                GD.Print($"RemoveItemQuantity: item id:{id} has insufficient quantity. current={GetItemCount(id)} required={quantity}.");
                return false;
            }

            int remaining = GetItemCount(id) - quantity;
            if (remaining <= 0)
            {
                _itemDict.Remove(id);
                UnequipItemIfNeeded(id);
            }
            else
            {
                _itemDict[id] = remaining;
            }

            if (_db.GetItemFromRepo(id) is WeaponItem)
            {
                RemoveWeaponInstances(id, quantity);
            }

            Changed?.Invoke();
            return true;
        }

		private static void PublishItemCollected(int id, int quantity)
		{
			GameManager.Instance?.Publish(GameEvent.PickupItem, new ItemCollectedQuestEvent(id, quantity));
		}

        /// <summary>
        /// Captures inventory, equipment, and weapon instance state for save/load.
        /// </summary>
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

            foreach (WeaponInstanceState instance in _weaponInstancesById.Values)
            {
                save.WeaponInstances.Add(instance.CaptureSnapshot());
            }

            save.EquippedArmorBySlot = new Dictionary<string, int>(_equippedArmorBySlot);
            save.EquippedTrinketBySlot = new Dictionary<string, int>(_equippedTrinketBySlot);
            save.EquippedWeaponBySlot = new Dictionary<string, int>(_equippedWeaponBySlot);
            save.EquippedWeaponInstanceBySlot = new Dictionary<string, string>(_equippedWeaponInstanceBySlot);
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

            if (_db.GetItemFromRepo(id) is WeaponItem)
            {
                RemoveWeaponInstances(id, 1);
            }

            GD.Print($"Dropped item id:{id}. Remaining quantity: {_itemDict.GetValueOrDefault(id, 0)}");
            Changed?.Invoke();
        }


        /// <summary>
        /// Restores inventory from the current save shape or older list-based snapshots.
        /// </summary>
        public void RestoreSnapshot(object snapshot)
        {
            ClearAllEquipment();
            _itemDict.Clear();
            _weaponInstancesById.Clear();

            if(snapshot is InventorySave inventorySave)
            {
                RestoreFromInventorySave(inventorySave);
                Changed?.Invoke();
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

            Changed?.Invoke();
        }

        /// <summary>
        /// Uses, equips, or consumes the first available matching item.
        /// </summary>
        public void UseItem(int id)
        {
            UseItem(id, null);
        }

        /// <summary>
        /// Uses, equips, or consumes an item, optionally targeting a specific weapon instance.
        /// </summary>
        public void UseItem(int id, string weaponInstanceId)
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
                    PublishEquipmentResult(ToggleArmorEquip(armor));
                    Changed?.Invoke();
                    return;
                }

                if (itemToUse is TrinketItem trinket)
                {
                    PublishEquipmentResult(ToggleTrinketEquip(trinket));
                    Changed?.Invoke();
                    return;
                }

                if (itemToUse is WeaponItem weapon)
                {
                    ToggleWeaponEquip(weapon, string.IsNullOrWhiteSpace(weaponInstanceId) ? FindFirstWeaponInstanceId(id) : weaponInstanceId);
                    Changed?.Invoke();
                    return;
                }

                itemToUse.Use();

                if (itemToUse is ConsumeItem)
                {
                    int remaining = ConsumeOne(id);
                    GD.Print($"Consumed item id:{id}. Remaining quantity: {remaining}");
                    Changed?.Invoke();
                }
            }
            else
            {
                GD.Print($"Item not found in master repo id:{id}");
            }
            
        }

        public EquipmentChangeResult EquipItem(int id)
        {
            InventoryItem item = _db.GetItemFromRepo(id);
            if (item is ArmorItem armor)
            {
                EquipmentChangeResult result = EquipArmor(armor);
                PublishEquipmentResult(result);
                Changed?.Invoke();
                return result;
            }

            if (item is TrinketItem trinket)
            {
                EquipmentChangeResult result = EquipTrinket(trinket);
                PublishEquipmentResult(result);
                Changed?.Invoke();
                return result;
            }

            return new EquipmentChangeResult(false, "Selected item cannot be equipped here.");
        }

        public EquipmentChangeResult UnequipEquipmentSlot(string slotKey)
        {
            EquipmentChangeResult result;
            if (string.Equals(slotKey, "Armor", StringComparison.OrdinalIgnoreCase))
            {
                result = UnequipArmorSlot("Armor");
            }
            else if (IsTrinketSlot(slotKey))
            {
                result = UnequipTrinketSlot(NormalizeTrinketSlot(slotKey));
            }
            else
            {
                result = new EquipmentChangeResult(false, "Selected equipment slot cannot be unequipped here.");
            }

            PublishEquipmentResult(result);
            Changed?.Invoke();
            return result;
        }

        private EquipmentChangeResult ToggleArmorEquip(ArmorItem armor)
        {
            string slot = NormalizeArmorSlot(armor);

            if (_equippedArmorBySlot.TryGetValue(slot, out int equippedId))
            {
                if (equippedId == armor.Id)
                {
                    return UnequipArmorSlot(slot);
                }
            }

            return EquipArmor(armor);
        }

        private EquipmentChangeResult EquipArmor(ArmorItem armor)
        {
            if (armor == null)
            {
                return new EquipmentChangeResult(false, "Selected item is not armor.");
            }

            if (!HasItemQuantity(armor.Id, 1))
            {
                return new EquipmentChangeResult(false, $"{armor.Name} is not in inventory.");
            }

            string slot = NormalizeArmorSlot(armor);
            if (_equippedArmorBySlot.TryGetValue(slot, out int equippedId))
            {
                RemoveEquipmentEffects(EquipmentSourceId(slot, equippedId));
            }

            _equippedArmorBySlot[slot] = armor.Id;
            ApplyEquipmentEffects(slot, armor);
            return new EquipmentChangeResult(true, $"Equipped {armor.Name}.");
        }

        private EquipmentChangeResult UnequipArmorSlot(string slot)
        {
            if (!_equippedArmorBySlot.TryGetValue(slot, out int equippedId))
            {
                return new EquipmentChangeResult(false, "No armor is equipped.");
            }

            InventoryItem item = _db.GetItemFromRepo(equippedId);
            RemoveEquipmentEffects(EquipmentSourceId(slot, equippedId));
            _equippedArmorBySlot.Remove(slot);
            return new EquipmentChangeResult(true, $"Unequipped {item?.Name ?? "armor"}.");
        }

        private EquipmentChangeResult ToggleTrinketEquip(TrinketItem trinket)
        {
            string equippedSlot = _equippedTrinketBySlot
                .FirstOrDefault(kvp => kvp.Value == trinket.Id)
                .Key;

            if (!string.IsNullOrWhiteSpace(equippedSlot))
            {
                return UnequipTrinketSlot(equippedSlot);
            }

            return EquipTrinket(trinket);
        }

        private EquipmentChangeResult EquipTrinket(TrinketItem trinket, string preferredSlot = null)
        {
            if (trinket == null)
            {
                return new EquipmentChangeResult(false, "Selected item is not a trinket.");
            }

            int equippedCopies = _equippedTrinketBySlot.Count(kvp => kvp.Value == trinket.Id);
            if (GetItemCount(trinket.Id) <= equippedCopies)
            {
                return new EquipmentChangeResult(false, $"{trinket.Name} is already equipped.");
            }

            string slot = NormalizeTrinketSlot(preferredSlot);
            if (string.IsNullOrWhiteSpace(slot))
            {
                slot = _equippedTrinketBySlot.ContainsKey("Trinket1") ? "Trinket2" : "Trinket1";
            }

            if (_equippedTrinketBySlot.TryGetValue(slot, out int equippedId))
            {
                RemoveEquipmentEffects(EquipmentSourceId(slot, equippedId));
            }

            _equippedTrinketBySlot[slot] = trinket.Id;
            ApplyEquipmentEffects(slot, trinket);
            return new EquipmentChangeResult(true, $"Equipped {trinket.Name}.");
        }

        private EquipmentChangeResult UnequipTrinketSlot(string slot)
        {
            slot = NormalizeTrinketSlot(slot);
            if (string.IsNullOrWhiteSpace(slot) || !_equippedTrinketBySlot.TryGetValue(slot, out int equippedId))
            {
                return new EquipmentChangeResult(false, "No trinket is equipped in that slot.");
            }

            InventoryItem item = _db.GetItemFromRepo(equippedId);
            RemoveEquipmentEffects(EquipmentSourceId(slot, equippedId));
            _equippedTrinketBySlot.Remove(slot);
            return new EquipmentChangeResult(true, $"Unequipped {item?.Name ?? "trinket"}.");
        }

        private void ToggleWeaponEquip(WeaponItem weapon, string instanceId = null)
        {
            string slot = weapon.WeaponSlot;
            instanceId ??= FindFirstWeaponInstanceId(weapon.Id);

            if (!string.IsNullOrWhiteSpace(instanceId)
                && _equippedWeaponInstanceBySlot.TryGetValue(slot, out string equippedInstanceId)
                && equippedInstanceId == instanceId)
            {
                weapon.Unequip();
                _equippedWeaponBySlot.Remove(slot);
                _equippedWeaponInstanceBySlot.Remove(slot);
                ApplyEquippedWeaponVisuals();
                return;
            }

            if (_equippedWeaponBySlot.TryGetValue(slot, out int equippedId))
            {
                if (equippedId == weapon.Id
                    && (string.IsNullOrWhiteSpace(instanceId)
                        || !_equippedWeaponInstanceBySlot.TryGetValue(slot, out string currentInstanceId)
                        || currentInstanceId == instanceId))
                {
                    weapon.Unequip();
                    _equippedWeaponBySlot.Remove(slot);
                    _equippedWeaponInstanceBySlot.Remove(slot);
                    ApplyEquippedWeaponVisuals();
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
            if (!string.IsNullOrWhiteSpace(instanceId))
            {
                _equippedWeaponInstanceBySlot[slot] = instanceId;
            }
            ApplyEquippedWeaponVisuals();
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
                if (_db.GetItemFromRepo(id) is WeaponItem)
                {
                    RemoveWeaponInstances(id, 1);
                }
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

            if (save?.WeaponInstances != null && save.WeaponInstances.Count > 0)
            {
                _weaponInstancesById.Clear();
                foreach (WeaponInstanceSave savedInstance in save.WeaponInstances)
                {
                    WeaponInstanceState instance = WeaponInstanceState.FromSave(savedInstance);
                    if (instance.ItemId <= 0 || _db.GetItemFromRepo(instance.ItemId) is not WeaponItem)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(instance.InstanceId))
                    {
                        instance.InstanceId = CreateWeaponInstanceId(instance.ItemId);
                    }

                    _weaponInstancesById[instance.InstanceId] = instance;
                }

                RecountWeaponInstances();
            }
            else
            {
                BackfillWeaponInstancesForLegacyStacks();
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

                    _equippedArmorBySlot[kvp.Key] = kvp.Value;
                    ApplyEquipmentEffects(kvp.Key, armor);
                }
            }

            if (save?.EquippedTrinketBySlot != null)
            {
                foreach (var kvp in save.EquippedTrinketBySlot)
                {
                    string slot = NormalizeTrinketSlot(kvp.Key);
                    if (string.IsNullOrWhiteSpace(slot) || !_itemDict.TryGetValue(kvp.Value, out int qty) || qty <= 0)
                    {
                        continue;
                    }

                    InventoryItem item = _db.GetItemFromRepo(kvp.Value);
                    if (item is not TrinketItem trinket)
                    {
                        continue;
                    }

                    _equippedTrinketBySlot[slot] = kvp.Value;
                    ApplyEquipmentEffects(slot, trinket);
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

            if (save?.EquippedWeaponInstanceBySlot != null)
            {
                foreach (var kvp in save.EquippedWeaponInstanceBySlot)
                {
                    if (_weaponInstancesById.TryGetValue(kvp.Value, out WeaponInstanceState instance)
                        && _db.GetItemFromRepo(instance.ItemId) is WeaponItem weapon)
                    {
                        weapon.Equip();
                        _equippedWeaponBySlot[kvp.Key] = instance.ItemId;
                        _equippedWeaponInstanceBySlot[kvp.Key] = kvp.Value;
                    }
                }
            }

            ApplyEquippedWeaponVisuals();
        }

        private void UnequipItemIfNeeded(int id)
        {
            InventoryItem item = _db.GetItemFromRepo(id);

            if (item is ArmorItem armor)
            {
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
                    RemoveEquipmentEffects(EquipmentSourceId(foundSlot, id));
                    _equippedArmorBySlot.Remove(foundSlot);
                }
            }
            else if (item is TrinketItem)
            {
                foreach (string foundSlot in _equippedTrinketBySlot
                             .Where(kvp => kvp.Value == id)
                             .Select(kvp => kvp.Key)
                             .ToList())
                {
                    RemoveEquipmentEffects(EquipmentSourceId(foundSlot, id));
                    _equippedTrinketBySlot.Remove(foundSlot);
                }
            }
            else if (item is WeaponItem weapon)
            {
                weapon.Unequip();
                ApplyEquippedWeaponVisuals();
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
                    _equippedWeaponInstanceBySlot.Remove(foundSlot);
                }
            }
        }

        private void ClearAllEquipment()
        {
            foreach (var kvp in _equippedArmorBySlot)
            {
                RemoveEquipmentEffects(EquipmentSourceId(kvp.Key, kvp.Value));
            }

            foreach (var kvp in _equippedTrinketBySlot)
            {
                RemoveEquipmentEffects(EquipmentSourceId(kvp.Key, kvp.Value));
            }

            foreach (var kvp in _equippedWeaponBySlot)
            {
                if (_db.GetItemFromRepo(kvp.Value) is WeaponItem weapon)
                {
                    weapon.Unequip();
                }
            }

            _equippedArmorBySlot.Clear();
            _equippedTrinketBySlot.Clear();
            _equippedWeaponBySlot.Clear();
            _equippedWeaponInstanceBySlot.Clear();
            ApplyEquippedWeaponVisuals();
        }

        public IReadOnlyDictionary<int, int> GetItemCounts()
        {
            return new Dictionary<int, int>(_itemDict);
        }

        public IReadOnlyDictionary<string, int> GetEquippedArmor()
        {
            return new Dictionary<string, int>(_equippedArmorBySlot);
        }

        public IReadOnlyDictionary<string, int> GetEquippedTrinkets()
        {
            return new Dictionary<string, int>(_equippedTrinketBySlot);
        }

        public IReadOnlyDictionary<string, int> GetEquippedWeapons()
        {
            return new Dictionary<string, int>(_equippedWeaponBySlot);
        }

        public IReadOnlyDictionary<string, string> GetEquippedWeaponInstances()
        {
            return new Dictionary<string, string>(_equippedWeaponInstanceBySlot);
        }

        public WeaponInstanceState GetEquippedWeaponInstance(string slotKey)
        {
            if (string.IsNullOrWhiteSpace(slotKey)
                || !_equippedWeaponInstanceBySlot.TryGetValue(slotKey, out string instanceId))
            {
                return null;
            }

            return GetWeaponInstance(instanceId);
        }

        /// <summary>
        /// Returns inventory rows sorted for display, splitting weapon instances into selectable single rows.
        /// </summary>
        public IReadOnlyList<InventoryDisplayEntry> GetInventoryDisplayEntries()
        {
            List<InventoryDisplayEntry> entries = new();
            HashSet<int> weaponItemIds = new();

            foreach (WeaponInstanceState instance in GetWeaponInstances())
            {
                weaponItemIds.Add(instance.ItemId);
                entries.Add(new InventoryDisplayEntry
                {
                    ItemId = instance.ItemId,
                    Quantity = 1,
                    WeaponInstanceId = instance.InstanceId
                });
            }

            Dictionary<int, int> equippedItemCounts = GetEquippedDisplayCounts();
            foreach (var kvp in _itemDict.OrderBy(pair => pair.Key))
            {
                int displayCount = kvp.Value - equippedItemCounts.GetValueOrDefault(kvp.Key, 0);
                if (displayCount <= 0 || weaponItemIds.Contains(kvp.Key))
                {
                    continue;
                }

                InventoryItem item = _db.GetItemFromRepo(kvp.Key);
                if (item?.MaxStack <= 1)
                {
                    for (int i = 0; i < displayCount; i++)
                    {
                        entries.Add(new InventoryDisplayEntry { ItemId = kvp.Key, Quantity = 1 });
                    }
                }
                else
                {
                    entries.Add(new InventoryDisplayEntry { ItemId = kvp.Key, Quantity = displayCount });
                }
            }

            return entries
                .OrderBy(entry => _db.GetItemFromRepo(entry.ItemId)?.Name ?? entry.ItemId.ToString())
                .ThenBy(entry => entry.WeaponInstanceId)
                .ToList();
        }

        /// <summary>
        /// Returns all tracked weapon instances sorted for stable UI display.
        /// </summary>
        public IReadOnlyList<WeaponInstanceState> GetWeaponInstances()
        {
            return _weaponInstancesById.Values
                .OrderBy(instance => _db.GetItemFromRepo(instance.ItemId)?.Name ?? instance.ItemId.ToString())
                .ThenBy(instance => instance.InstanceId)
                .ToList();
        }

        public WeaponInstanceState GetWeaponInstance(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            return _weaponInstancesById.TryGetValue(instanceId, out WeaponInstanceState instance) ? instance : null;
        }

        /// <summary>
        /// Applies a controlled mutation to one weapon instance and raises Changed when successful.
        /// </summary>
        public bool TryMutateWeaponInstance(string instanceId, Action<WeaponInstanceState> mutate)
        {
            if (!_weaponInstancesById.TryGetValue(instanceId, out WeaponInstanceState instance) || mutate == null)
            {
                return false;
            }

            mutate(instance);
            Changed?.Invoke();
            return true;
        }

        private WeaponInstanceState CreateWeaponInstance(int itemId)
        {
            WeaponInstanceState instance = new()
            {
                InstanceId = CreateWeaponInstanceId(itemId),
                ItemId = itemId
            };
            _weaponInstancesById[instance.InstanceId] = instance;
            return instance;
        }

        private static string CreateWeaponInstanceId(int itemId)
        {
            return $"weaponinst.{itemId}.{Guid.NewGuid():N}";
        }

        private string FindFirstWeaponInstanceId(int itemId)
        {
            return _weaponInstancesById.Values
                .Where(instance => instance.ItemId == itemId)
                .OrderBy(instance => instance.InstanceId)
                .Select(instance => instance.InstanceId)
                .FirstOrDefault();
        }

        private void RemoveWeaponInstances(int itemId, int quantity)
        {
            int remaining = Math.Max(0, quantity);
            foreach (string instanceId in _weaponInstancesById.Values
                         .Where(instance => instance.ItemId == itemId && !IsWeaponInstanceEquipped(instance.InstanceId))
                         .Select(instance => instance.InstanceId)
                         .ToList())
            {
                if (remaining <= 0)
                {
                    break;
                }

                _weaponInstancesById.Remove(instanceId);
                remaining--;
            }

            foreach (string instanceId in _weaponInstancesById.Values
                         .Where(instance => instance.ItemId == itemId)
                         .Select(instance => instance.InstanceId)
                         .ToList())
            {
                if (remaining <= 0)
                {
                    break;
                }

                RemoveEquippedWeaponInstance(instanceId);
                _weaponInstancesById.Remove(instanceId);
                remaining--;
            }

            RecountWeaponInstances();
        }

        private bool IsWeaponInstanceEquipped(string instanceId)
        {
            return _equippedWeaponInstanceBySlot.ContainsValue(instanceId);
        }

        private void RemoveEquippedWeaponInstance(string instanceId)
        {
            string slot = _equippedWeaponInstanceBySlot
                .FirstOrDefault(kvp => kvp.Value == instanceId)
                .Key;

            if (string.IsNullOrWhiteSpace(slot))
            {
                return;
            }

            _equippedWeaponInstanceBySlot.Remove(slot);
            _equippedWeaponBySlot.Remove(slot);
        }

        private void BackfillWeaponInstancesForLegacyStacks()
        {
            foreach (var kvp in _itemDict.ToList())
            {
                if (_db.GetItemFromRepo(kvp.Key) is not WeaponItem)
                {
                    continue;
                }

                int existing = _weaponInstancesById.Values.Count(instance => instance.ItemId == kvp.Key);
                for (int i = existing; i < kvp.Value; i++)
                {
                    CreateWeaponInstance(kvp.Key);
                }
            }
        }

        private void RecountWeaponInstances()
        {
            foreach (int itemId in _itemDict.Keys.ToList())
            {
                if (_db.GetItemFromRepo(itemId) is WeaponItem)
                {
                    _itemDict.Remove(itemId);
                }
            }

            foreach (var group in _weaponInstancesById.Values.GroupBy(instance => instance.ItemId))
            {
                _itemDict[group.Key] = group.Count();
            }
        }

        private Dictionary<int, int> GetEquippedDisplayCounts()
        {
            Dictionary<int, int> counts = new();
            foreach (int itemId in _equippedArmorBySlot.Values.Concat(_equippedTrinketBySlot.Values))
            {
                counts[itemId] = counts.GetValueOrDefault(itemId, 0) + 1;
            }

            return counts;
        }

        private void ApplyEquipmentEffects(string slot, InventoryItem item)
        {
            Player player = GameManager.Instance?.GetPlayer();
            if (player == null || item == null)
            {
                return;
            }

            item.SetOwner(player);
            player.ApplyEquipmentModifiers(EquipmentSourceId(slot, item.Id), item.Effects);
            GameManager.Instance?.UI?.BindPlayerHud(player);
        }

        private void RemoveEquipmentEffects(string sourceId)
        {
            Player player = GameManager.Instance?.GetPlayer();
            player?.RemoveEquipmentModifiers(sourceId);
            if (player != null)
            {
                GameManager.Instance?.UI?.BindPlayerHud(player);
            }
        }

        private static string EquipmentSourceId(string slot, int itemId)
        {
            return $"equipment:{slot}:{itemId}";
        }

        private static string NormalizeArmorSlot(ArmorItem armor)
        {
            return "Armor";
        }

        private static bool IsTrinketSlot(string slotKey)
        {
            return string.Equals(slotKey, "Trinket1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slotKey, "Trinket2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slotKey, "Trinket", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeTrinketSlot(string slotKey)
        {
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                return string.Empty;
            }

            if (string.Equals(slotKey, "Trinket", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slotKey, "Trinket1", StringComparison.OrdinalIgnoreCase))
            {
                return "Trinket1";
            }

            return string.Equals(slotKey, "Trinket2", StringComparison.OrdinalIgnoreCase) ? "Trinket2" : string.Empty;
        }

        private static void PublishEquipmentResult(EquipmentChangeResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Message))
            {
                return;
            }

            GameManager.Instance?.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest(result.Message, result.Success ? NotificationType.Info : NotificationType.Error));
        }

        private void ApplyEquippedWeaponVisuals()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                if (DebugWeaponVisuals)
                {
                    GD.Print("InventoryManager.ApplyEquippedWeaponVisuals: skipped (GameManager.Instance is null).");
                }
                return;
            }

            SceneTree tree = gm.GetTree();
            if (tree == null)
            {
                if (DebugWeaponVisuals)
                {
                    GD.Print("InventoryManager.ApplyEquippedWeaponVisuals: skipped (SceneTree is null).");
                }
                return;
            }

            PlayerNode playerNode = tree.GetFirstNodeInGroup("Player") as PlayerNode;
            if (playerNode == null)
            {
                if (DebugWeaponVisuals)
                {
                    GD.Print("InventoryManager.ApplyEquippedWeaponVisuals: skipped (no PlayerNode in group 'Player').");
                }
                return;
            }

            if (_equippedWeaponBySlot.TryGetValue("MainHand", out int weaponId)
                && _db.GetItemFromRepo(weaponId) is WeaponItem weapon)
            {
                if (DebugWeaponVisuals)
                {
                    GD.Print($"InventoryManager.ApplyEquippedWeaponVisuals: applying weapon id={weaponId} name='{weapon.Name}'.");
                }

                playerNode.ApplyWeaponSprites(
                    weapon.WeaponUpDraw ?? gm.WepUpDraw,
                    weapon.WeaponDownDraw ?? gm.WepDownDraw,
                    weapon.WeaponUpStow ?? gm.WepUpStow,
                    weapon.WeaponDownStow ?? gm.WepDownStow);
                return;
            }

            if (DebugWeaponVisuals)
            {
                GD.Print("InventoryManager.ApplyEquippedWeaponVisuals: no equipped MainHand weapon, applying GameManager fallback sprites.");
            }

            playerNode.ApplyWeaponSprites(gm.WepUpDraw, gm.WepDownDraw, gm.WepUpStow, gm.WepDownStow);
        }


    }
}
