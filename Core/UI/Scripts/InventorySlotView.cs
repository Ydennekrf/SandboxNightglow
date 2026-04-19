using System;
using Godot;

namespace ethra.V1
{
    public partial class InventorySlotView : Button
    {
        [Signal]
        public delegate void ClickedEventHandler(int itemId);

        private TextureRect _icon;
        private Label _countLabel;
        private CanvasItem _countContainer;
        private TextureRect _selector;

        public int ItemId { get; private set; } = -1;

        public override void _Ready()
        {
            _icon = GetNodeOrNull<TextureRect>("Icon");
            _countContainer = GetNodeOrNull<CanvasItem>("Panel");
            _countLabel = GetNodeOrNull<Label>("Panel/Label");
            _selector = GetNodeOrNull<TextureRect>("Selector");

            Pressed += OnPressed;
            SetEmpty();
        }

        public void SetItem(int itemId, int count, Texture2D icon = null)
        {
            ItemId = itemId;
            Disabled = false;

            if (_icon != null)
            {
                _icon.Texture = icon;
                _icon.Visible = icon != null;
            }

            if (_countLabel != null)
            {
                _countLabel.Text = count.ToString();
            }

            if (_countContainer != null)
            {
                _countContainer.Visible = count > 1;
            }

            SetEquipped(false);
        }

        public void SetEmpty()
        {
            ItemId = -1;
            Disabled = true;

            if (_icon != null)
            {
                _icon.Texture = null;
                _icon.Visible = false;
            }

            if (_countLabel != null)
            {
                _countLabel.Text = string.Empty;
            }

            if (_countContainer != null)
            {
                _countContainer.Visible = false;
            }

            SetEquipped(false);
        }

        public void SetEquipped(bool equipped)
        {
            if (_selector != null)
            {
                _selector.Visible = equipped;
            }
        }

        private void OnPressed()
        {
            if (ItemId <= 0)
            {
                return;
            }

            EmitSignal(SignalName.Clicked, ItemId);
        }
    }
}
