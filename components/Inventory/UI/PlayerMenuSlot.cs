using Godot;
using System;

public partial class PlayerMenuSlot : Button
{
    [Export] private TextureRect _icon;
    [Export] private Label _quantity;
    [Export] private TextureRect _selector;

    public int SlotIndex { get; private set; } = -1;
    public ItemStack? Stack { get; private set; }

    public event Action<PlayerMenuSlot>? SlotPressed;

    public override void _Ready()
    {
        Pressed += OnPressed;
        SetSelected(false);
    }

    public void BindSlotIndex(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    public void SetStack(ItemStack? stack)
    {
        Stack = stack;

        if (stack == null)
        {
            if (_icon != null)
            {
                _icon.Texture = null;
                _icon.Visible = false;
            }

            if (_quantity != null)
            {
                _quantity.Text = string.Empty;
            }

            return;
        }

        InventoryItem item = InventoryManager.I.Get(stack.ItemId);

        if (_icon != null)
        {
            _icon.Texture = item.IconSprite;
            _icon.Visible = item.IconSprite != null;
        }

        if (_quantity != null)
        {
            _quantity.Text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
        }
    }

    public void SetSelected(bool selected)
    {
        if (_selector != null)
        {
            _selector.Visible = selected;
        }
    }

    private void OnPressed()
    {
        SlotPressed?.Invoke(this);
    }
}
