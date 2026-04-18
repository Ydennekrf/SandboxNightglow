using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ethra.V1;

public partial class PlayerMenu : Control
{
    [Export] private GridContainer InventoryGrid;
    [Export] private InventorySlotView EquippedWeaponSlot;
    [Export] private InventorySlotView EquippedArmorSlot;
    [Export] private RichTextLabel ToolTip;
    [Export] private RichTextLabel StatsStatus;

    private readonly List<InventorySlotView> _inventorySlots = new();
    private InventoryManager _inventory;
    private MasterRepository _repo;
    private InventorySlotView _selectedSlot;

    public override void _Ready()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            GD.PushWarning("PlayerMenu: GameManager.Instance is null.");
            return;
        }

        _inventory = gm.Inventory;
        _repo = gm.DB;

        CacheInventorySlots();
        BindEquipmentSlots();

        if (_inventory != null)
        {
            _inventory.Changed += RefreshAll;
        }

        RefreshAll();
    }

    public override void _ExitTree()
    {
        if (_inventory != null)
        {
            _inventory.Changed -= RefreshAll;
        }

        foreach (InventorySlotView slot in _inventorySlots)
        {
            slot.Clicked -= OnSlotClicked;
        }

        if (EquippedWeaponSlot != null)
        {
            EquippedWeaponSlot.Clicked -= OnSlotClicked;
        }

        if (EquippedArmorSlot != null)
        {
            EquippedArmorSlot.Clicked -= OnSlotClicked;
        }
    }

    private void CacheInventorySlots()
    {
        _inventorySlots.Clear();

        if (InventoryGrid == null)
        {
            GD.PushWarning("PlayerMenu: InventoryGrid export is not assigned.");
            return;
        }

        foreach (Node child in InventoryGrid.GetChildren())
        {
            if (child is InventorySlotView slot)
            {
                slot.Clicked += OnSlotClicked;
                _inventorySlots.Add(slot);
            }
        }
    }

    private void BindEquipmentSlots()
    {
        if (EquippedWeaponSlot != null)
        {
            EquippedWeaponSlot.Clicked += OnSlotClicked;
        }

        if (EquippedArmorSlot != null)
        {
            EquippedArmorSlot.Clicked += OnSlotClicked;
        }
    }

    private void RefreshAll()
    {
        RefreshInventoryGrid();
        RefreshEquipmentSlots();
        RefreshDetails(_selectedSlot);
    }

    private void RefreshInventoryGrid()
    {
        if (_inventory == null || _repo == null)
        {
            return;
        }

        List<KeyValuePair<int, int>> items = _inventory.GetItemCounts()
            .OrderBy(pair => pair.Key)
            .ToList();

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            if (i >= items.Count)
            {
                _inventorySlots[i].SetEmpty();
                continue;
            }

            KeyValuePair<int, int> entry = items[i];
            InventoryItem item = _repo.GetItemFromRepo(entry.Key);

            if (item == null)
            {
                _inventorySlots[i].SetEmpty();
                continue;
            }

            _inventorySlots[i].SetItem(item, entry.Value);
        }
    }

    private void RefreshEquipmentSlots()
    {
        if (_inventory == null || _repo == null)
        {
            return;
        }

        SetEquipmentSlot(EquippedWeaponSlot, _inventory.GetEquippedWeapons(), "MainHand");
        SetEquipmentSlot(EquippedArmorSlot, _inventory.GetEquippedArmor(), "Armor");
    }

    private void SetEquipmentSlot(InventorySlotView slotView, IReadOnlyDictionary<string, int> equipped, string slotKey)
    {
        if (slotView == null)
        {
            return;
        }

        if (equipped == null || !equipped.TryGetValue(slotKey, out int itemId))
        {
            slotView.SetEmpty();
            return;
        }

        InventoryItem item = _repo.GetItemFromRepo(itemId);
        if (item == null)
        {
            slotView.SetEmpty();
            return;
        }

        slotView.SetItem(item, 1);
    }

    private void OnSlotClicked(InventorySlotView slot)
    {
        SelectSlot(slot);

        if (_inventory == null || slot?.ItemId == null)
        {
            return;
        }

        if (slot.ItemData is WeaponItem || slot.ItemData is ArmorItem)
        {
            _inventory.UseItem(slot.ItemId.Value);
            RefreshAll();
        }
    }

    private void SelectSlot(InventorySlotView slot)
    {
        _selectedSlot = slot;

        foreach (InventorySlotView inventorySlot in _inventorySlots)
        {
            inventorySlot.SetSelected(inventorySlot == slot);
        }

        if (EquippedWeaponSlot != null)
        {
            EquippedWeaponSlot.SetSelected(EquippedWeaponSlot == slot);
        }

        if (EquippedArmorSlot != null)
        {
            EquippedArmorSlot.SetSelected(EquippedArmorSlot == slot);
        }

        RefreshDetails(slot);
    }

    private void RefreshDetails(InventorySlotView slot)
    {
        if (ToolTip == null || StatsStatus == null)
        {
            return;
        }

        if (slot?.ItemData == null)
        {
            ToolTip.Text = "Select an item to view its description.";
            StatsStatus.Text = string.Empty;
            return;
        }

        InventoryItem item = slot.ItemData;

        ToolTip.Text = item.Description;

        var details = new StringBuilder();
        details.AppendLine($"Name: {item.Name}");
        details.AppendLine($"Category: {item.Category}");
        details.AppendLine($"Subtype: {item.Subtype}");
        details.AppendLine($"Rarity: {item.Rarity}");
        details.AppendLine($"Count: {slot.Count}");

        if (item.Effects != null && item.Effects.Count > 0)
        {
            details.AppendLine("Effects:");
            foreach (ItemEffects effect in item.Effects)
            {
                details.AppendLine($"- {effect.EffectName}");
            }
        }

        StatsStatus.Text = details.ToString();
    }
}
