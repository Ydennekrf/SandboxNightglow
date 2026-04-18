using Godot;
using System;
using ethra.V1;

public partial class InventorySlotView : Button
{
    [Export] private TextureRect Icon;
    [Export] private Label QuantityLabel;
    [Export] private TextureRect Selector;

    public int? ItemId { get; private set; }
    public int Count { get; private set; }
    public InventoryItem ItemData { get; private set; }

    public event Action<InventorySlotView> Clicked;

    public override void _Ready()
    {
        Pressed += OnPressed;
        SetSelected(false);
    }

    public void SetEmpty()
    {
        ItemId = null;
        Count = 0;
        ItemData = null;

        if (QuantityLabel != null)
        {
            QuantityLabel.Text = string.Empty;
        }

        if (Icon != null)
        {
            Icon.Visible = false;
        }

        SetSelected(false);
    }

    public void SetItem(InventoryItem item, int count)
    {
        ItemData = item;
        ItemId = item?.Id;
        Count = count;

        if (QuantityLabel != null)
        {
            QuantityLabel.Text = count > 1 ? count.ToString() : string.Empty;
        }

        if (Icon != null)
        {
            Icon.Visible = item != null;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (Selector != null)
        {
            Selector.Visible = isSelected;
        }
    }

    private void OnPressed()
    {
        Clicked?.Invoke(this);
    }
}
