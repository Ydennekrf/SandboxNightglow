using Godot;
using System.Collections.Generic;

/// <summary>
/// Popup / HUD panel that visualises the player's inventory.
/// It observes an <see cref="IInventoryReadOnly"/> and never mutates data itself.
/// </summary>
public partial class InventoryWindow : Control
{
    // ---------------------------------------------------------------------
    // Inspector hooks (set these in the Godot editor)
    // ---------------------------------------------------------------------

    [Export] private NodePath _inventoryPath;          // Player/Logic/InventoryComponent
    [Export] private GridContainer _grid;              // The GridContainer node inside the ScrollContainer
    [Export] private GridContainer EquipGrid;
    [Export] private PackedScene _slotScene;           // PackedScene for InventorySlotUI

    // ---------------------------------------------------------------------
    // Internals
    // ---------------------------------------------------------------------

    private IInventoryReadOnly _inventory;
    private readonly List<ItemIcon> _icons = new();

    // ---------------------------------------------------------------------
    // Godot lifecycle
    // ---------------------------------------------------------------------

    public override void _Ready()
    {
        // Resolve the read‑only inventory reference via NodePath.
        _inventory = GetNode<IInventoryReadOnly>(_inventoryPath);

        // Build one UI icon per slot.
        BuildIconPool(_inventory.Slots.Count);

        // Initial draw.
        FullRefresh();

        // Listen for inventory changes.
        _inventory.Changed += OnInventoryChanged;

        // Optional: close the window when escape is pressed.
        GuiInput += e =>
        {
            if (e.IsActionPressed("ui_cancel")) Visible = false;
        };
    }

    public override void _ExitTree()
    {
        if (_inventory != null)
            _inventory.Changed -= OnInventoryChanged;
    }

    // ---------------------------------------------------------------------
    // Building & refreshing
    // ---------------------------------------------------------------------

    private void BuildIconPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var icon = _slotScene.Instantiate<ItemIcon>();
            icon.SlotIndex = i;           // Tell the icon which slot it represents.
            icon.SetStack(null);          // Start empty; will be filled in FullRefresh.
            _grid.AddChild(icon);
            _icons.Add(icon);
        }    
    }

    private void FullRefresh()
    {
        for (int i = 0; i < _icons.Count; i++)
            _icons[i].SetStack(_inventory.Slots[i]);
    }

    private void OnInventoryChanged(InventoryChange e)
    {
        if (e.SlotIndex == -1)
        {
            // Bulk refresh – easiest path.
            FullRefresh();
            return;
        }

        // Update only the affected slot for efficiency.
        _icons[e.SlotIndex].SetStack(e.NewValue);
    }

    // ---------------------------------------------------------------------
    // Developer helper – toggle visibility via hot‑key.
    // ---------------------------------------------------------------------
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Inventory_Toggle"))
            Visible = !Visible;
    }
}
