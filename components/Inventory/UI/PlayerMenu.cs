using Godot;
using System.Collections.Generic;
using System.Text;

public partial class PlayerMenu : Control
{
    [Export] private NodePath _inventoryPath;
    [Export] private GridContainer _inventoryGrid;
    [Export] private PlayerMenuSlot _weaponSlot;
    [Export] private PlayerMenuSlot _armorSlot;
    [Export] private RichTextLabel _descriptionLabel;
    [Export] private RichTextLabel _statsStatusLabel;

    private readonly List<PlayerMenuSlot> _inventorySlots = new();
    private IInventoryReadOnly _inventory;

    private PlayerMenuSlot _selectedSlot;
    private int _selectedInventoryIndex = -1;

    public override void _Ready()
    {
        if (_inventoryPath == null || _inventoryPath.IsEmpty)
        {
            GD.PushWarning("PlayerMenu: InventoryPath is not assigned.");
            return;
        }

        _inventory = GetNodeOrNull<IInventoryReadOnly>(_inventoryPath);
        if (_inventory == null)
        {
            GD.PushWarning($"PlayerMenu: unable to resolve inventory at '{_inventoryPath}'.");
            return;
        }

        BindInventorySlots();
        BindEquipmentSlot(_weaponSlot, -1);
        BindEquipmentSlot(_armorSlot, -2);

        if (_weaponSlot != null)
        {
            _weaponSlot.SlotPressed += OnAnySlotPressed;
        }

        if (_armorSlot != null)
        {
            _armorSlot.SlotPressed += OnAnySlotPressed;
        }

        _inventory.Changed += OnInventoryChanged;

        FullRefresh();
        SelectSlot(null);
    }

    public override void _ExitTree()
    {
        if (_inventory != null)
        {
            _inventory.Changed -= OnInventoryChanged;
        }

        if (_weaponSlot != null)
        {
            _weaponSlot.SlotPressed -= OnAnySlotPressed;
        }

        if (_armorSlot != null)
        {
            _armorSlot.SlotPressed -= OnAnySlotPressed;
        }

        foreach (PlayerMenuSlot slot in _inventorySlots)
        {
            slot.SlotPressed -= OnAnySlotPressed;
        }
    }

    private void BindInventorySlots()
    {
        _inventorySlots.Clear();

        if (_inventoryGrid == null)
        {
            GD.PushWarning("PlayerMenu: InventoryGrid is not assigned.");
            return;
        }

        int index = 0;
        foreach (Node child in _inventoryGrid.GetChildren())
        {
            if (child is not PlayerMenuSlot slot)
            {
                continue;
            }

            slot.BindSlotIndex(index);
            slot.SlotPressed += OnAnySlotPressed;
            slot.SetSelected(false);
            _inventorySlots.Add(slot);
            index++;
        }
    }

    private static void BindEquipmentSlot(PlayerMenuSlot slot, int slotIndex)
    {
        if (slot == null)
        {
            return;
        }

        slot.BindSlotIndex(slotIndex);
    }

    private void OnInventoryChanged(InventoryChange _)
    {
        FullRefresh();
        RefreshSelectionText();
    }

    private void FullRefresh()
    {
        if (_inventory == null)
        {
            return;
        }

        int count = Mathf.Min(_inventorySlots.Count, _inventory.Slots.Count);
        for (int i = 0; i < count; i++)
        {
            _inventorySlots[i].SetStack(_inventory.Slots[i]);
        }

        for (int i = count; i < _inventorySlots.Count; i++)
        {
            _inventorySlots[i].SetStack(null);
        }

        ItemStack? weapon = null;
        ItemStack? armor = null;

        _inventory.Equipped.TryGetValue(EquipmentSlot.Weapon, out weapon);
        _inventory.Equipped.TryGetValue(EquipmentSlot.Armor, out armor);

        if (_weaponSlot != null)
        {
            _weaponSlot.SetStack(weapon);
        }

        if (_armorSlot != null)
        {
            _armorSlot.SetStack(armor);
        }
    }

    private void OnAnySlotPressed(PlayerMenuSlot slot)
    {
        SelectSlot(slot);

        if (slot == null || slot.SlotIndex < 0 || slot.Stack == null)
        {
            return;
        }

        InventoryItem item = InventoryManager.I.Get(slot.Stack.ItemId);

        if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor)
        {
            EventManager.I.Publish(GameEvent.UseItem, new UseItemRequest(slot.Stack, slot.SlotIndex));
        }
    }

    private void SelectSlot(PlayerMenuSlot slot)
    {
        _selectedSlot = slot;
        _selectedInventoryIndex = slot != null ? slot.SlotIndex : -1;

        foreach (PlayerMenuSlot invSlot in _inventorySlots)
        {
            invSlot.SetSelected(invSlot == slot);
        }

        if (_weaponSlot != null)
        {
            _weaponSlot.SetSelected(_weaponSlot == slot);
        }

        if (_armorSlot != null)
        {
            _armorSlot.SetSelected(_armorSlot == slot);
        }

        RefreshSelectionText();
    }

    private void RefreshSelectionText()
    {
        if (_descriptionLabel == null || _statsStatusLabel == null)
        {
            return;
        }

        if (_selectedSlot == null || _selectedSlot.Stack == null)
        {
            _descriptionLabel.Text = "Select an item to view details.";
            _statsStatusLabel.Text = "";
            return;
        }

        InventoryItem item = InventoryManager.I.Get(_selectedSlot.Stack.ItemId);

        _descriptionLabel.Text = item.ItemDescription;

        var details = new StringBuilder();
        details.AppendLine($"Name: {item.ItemName}");
        details.AppendLine($"Type: {item.itemType}");
        details.AppendLine($"Rarity: {item.Rarity}");
        details.AppendLine($"Stack: {_selectedSlot.Stack.Count}/{item.ItemStackSize}");

        if (item.Actions != null && item.Actions.Length > 0)
        {
            details.AppendLine("Effects:");
            foreach (ItemAction action in item.Actions)
            {
                if (action != null)
                {
                    details.AppendLine($"- {action.ResourceName}");
                }
            }
        }

        if (_selectedInventoryIndex >= 0 && (item.itemType == ItemType.Weapon || item.itemType == ItemType.Armor))
        {
            details.AppendLine();
            details.AppendLine("Click again to swap this equipment.");
        }

        _statsStatusLabel.Text = details.ToString();
    }
}
