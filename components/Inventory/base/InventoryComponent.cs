using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
// this has two elements, a dictionary for everything in the active inventory.
// and a dictionary for all the equipped gear.
// it is able to save and load all items within the inventory without issue.

// i need a function here or within the UI that gets all the itemStacks to populate the UI.


public partial class InventoryComponent : Node , IInventoryReadOnly
{
    [Export] public int MaxSlots = 24;
    [Export] public int MaxStack = 99;   // default; individual item may override


    private readonly List<ItemStack?> _slots = new();
    private readonly Dictionary<EquipmentSlot, ItemStack> _eq = new();

    public IReadOnlyList<ItemStack?> Slots => _slots;
    public IReadOnlyDictionary<EquipmentSlot, ItemStack?> Equipped => _eq;
    public event Action<InventoryChange>? Changed;

    private Entity owner;

    public override void _Ready()
    {
        owner = GetOwner<Entity>();
        _slots.AddRange(Enumerable.Repeat<ItemStack?>(null, MaxSlots));
        EventManager.I.Subscribe<SaveRequest>(GameEvent.SaveRequested, OnSave);
        EventManager.I.Subscribe<LoadRequest>(GameEvent.LoadRequested, OnLoad);
        EventManager.I.Subscribe<UseItemRequest>(GameEvent.UseItem, UseSlot);
        EventManager.I.Subscribe<ItemStack>(GameEvent.PickupItem, PickUpItem);
        AddItem("Health_Potion", 4);

    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<SaveRequest>(GameEvent.SaveRequested, OnSave);
        EventManager.I.Unsubscribe<LoadRequest>(GameEvent.LoadRequested, OnLoad);
        EventManager.I.Unsubscribe<UseItemRequest>(GameEvent.UseItem, UseSlot);
        EventManager.I.Unsubscribe<ItemStack>(GameEvent.PickupItem, PickUpItem);
    }

    private void NotifyChange(InventoryChange args)
    {
        GD.Print("Notify Inventory Change");
        Changed?.Invoke(args);
        EventManager.I.Publish(GameEvent.InventoryChange, args);
    }

    /* ─────────────────────────── Public API ─────────────────────────── */

    /// <summary>Try to add count items; returns false if not all fit.</summary>
    public bool AddItem(string itemId, int count = 1)
    {
        InventoryItem def = InventoryManager.I.Get(itemId);
        int room = FindRoom(itemId, def.ItemStackSize);

        if (room < count) return false;   // not enough space

        // 1) merge into existing stacks
        for (int i = 0; i < _slots.Count && count > 0; i++)
        {
            if (_slots[i]?.ItemId == itemId)
            {
                int take = Mathf.Min(def.ItemStackSize - _slots[i]!.Count, count);
                var before = _slots[i];
                _slots[i] = _slots[i] with { Count = _slots[i]!.Count + take };

                count -= take;

                NotifyChange(new InventoryChange(i, before , _slots[i]));
            }
        }
        // 2) create new stacks
        for (int i = 0; i < _slots.Count && count > 0; i++)
        {
            if (_slots[i] == null)
            {
                int take = Mathf.Min(def.ItemStackSize, count);
                _slots[i] = new ItemStack(itemId, take);
                count -= take;
                NotifyChange(new InventoryChange(i, null, _slots[i]));
            }
        }
        
        return true;
        
    }

    /// <summary>Remove exactly one item from the first matching stack.</summary>
    public void RemoveOne(string itemId)
    {
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i]?.ItemId == itemId)
            {
                var before = _slots[i];
                int left = _slots[i]!.Count - 1;
                _slots[i] = left > 0 ? _slots[i] with { Count = left } : null;
                NotifyChange(new InventoryChange(i, before, _slots[i]));
                return;
            }
    }

    /// <summary>Player clicked slot – use the item.</summary>
    public void UseSlot(UseItemRequest request)
    {
        ItemStack stack = _slots[request.slot];
        if (stack == null) return;

        InventoryItem data = InventoryManager.I.Get(stack.ItemId);
        bool remove = false;

        foreach (ItemAction act in data.Actions)
            remove |= act.Execute(owner, this, data);   // execute every action


        if (data is IEquippable)
        {
            GD.Print("clicked an equippable item");
            Equip(request.slot);
        }

        if (remove) RemoveOne(stack.ItemId);
         NotifyChange(new InventoryChange(request.slot, request.item, stack));
    }

    private void PickUpItem(ItemStack item)
    {
        AddItem(item.ItemId, item.Count);
    }

    /* ─────────────────────────── Helpers ─────────────────────────── */
    private int FindRoom(string itemId, int stackSize)
    {
        int space = 0;
        foreach (var s in _slots)
        {
            if (s == null) space += stackSize;
            else if (s.ItemId == itemId) space += stackSize - s.Count;
        }
        return space;
    }

    /* ───── Serialization (simple list of stacks) ───── */
    public List<ItemStack?> ExportStacks() => _slots;
    public void ImportStacks(List<ItemStack?> data)
    {
        _slots.Clear();
        _slots.AddRange(data);
        while (_slots.Count < MaxSlots) _slots.Add(null);
        NotifyChange(new InventoryChange(-1, null, null));
    }

    private void OnSave(SaveRequest request)
    {
        request.Data.Inventory = ExportStacks();
    }

    private void OnLoad(LoadRequest request)
    {
        ImportStacks(request.Data.Inventory);
    }

    private void PublishEquipChange(InventoryItem? oldItem, InventoryItem? newItem, EquipmentSlot slot)
{
    var payload = new EquipmentChange
    {
        User = owner,
        Slot = slot,
        Old  = oldItem,
        New  = newItem
    };

    EventManager.I.Publish(GameEvent.EquipmentChanged, payload);
}


    public void Equip(int slotIndex)
    {
        EquipmentSlot slot;
        ItemStack newItem = _slots[slotIndex];
        InventoryItem itemOld = null;
        InventoryItem item = InventoryManager.I.Get(newItem.ItemId);
        // item to equip
        if (item is not IEquippable equip)
            return;
        // Weapon, Head, etc.
        slot = equip.slot;
        ItemStack oldItem = _eq.GetValueOrDefault(slot);
        if (oldItem != null)
        {
                itemOld = InventoryManager.I.Get(oldItem.ItemId);
        }
  
        _eq[slot] = newItem;          // move to equipment dict
        _slots[slotIndex] = null;           // clear bag slot

        PublishEquipChange(itemOld, item, slot);   // <<< single publish point
    }

    public void Unequip(EquipmentSlot slot)
    {
            // 1. Get the currently equipped stack, if any
    if (!_eq.TryGetValue(slot, out var oldStack) || oldStack == null)
    {
        GD.PushWarning($"UnEquip: slot {slot} is already empty.");
        return;                                  // nothing to do
    }

    // 2. Remove from equipment dictionary
        _eq.Remove(slot);

    // 3. Convert stack → InventoryItem to publish later
        InventoryItem oldItem = InventoryManager.I.Get(oldStack.ItemId);

    // 4. Put the stack back into the bag (merges or creates a new slot)
        AddItem(oldStack.ItemId, oldStack.Count);    // AddItem already raises NotifyChange

    // 5. Publish the authoritative equipment event
        PublishEquipChange(oldItem, null, slot);
    }
}
