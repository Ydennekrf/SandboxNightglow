using Godot;
using System;

public partial class ItemIcon : Control
{
    [Export] private TextureRect _icon;
    [Export] private Label _count;
    [Export] private TextureRect _highlight;
    public int SlotIndex;
    

    private ItemStack? _stack;

    public override void _Ready()
    {
            MouseEntered += () => _highlight.Visible = true;
            MouseExited  += () => _highlight.Visible = false;
    }

    public void SetStack(ItemStack? stack)
    {
        _stack = stack;
        _highlight.Visible = false;
        if (stack == null)
        {
            _icon.Texture = null;
            _count.Text = "";
            return;
        }
        InventoryItem item = InventoryManager.I.Get(stack.ItemId);
        _icon.Texture = item.IconSprite;
        _count.Text = stack.Count.ToString();

    }

    
    /* ------------------------------------------------------------------ */
    //             CUSTOM TOOLTIP (Godot 4.x, C#)
    /* ------------------------------------------------------------------ */
    public override Control _MakeCustomTooltip(string forText)
    {
        InventoryItem item = InventoryManager.I.Get(_stack.ItemId);
        // ── Root container ──────────────────────────────────────────────
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel",
            new StyleBoxFlat
            {
                BgColor = new Color(0, 0, 0, 0.9f),
                BorderColor = new Color(1, 1, 1),
                CornerRadiusBottomLeft = 4,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomRight = 4,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1
            });

        var vbox = new VBoxContainer { CustomMinimumSize = new Vector2(180, 0) };
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        // ── Item name (bold) ────────────────────────────────────────────
        var name = new Label { Text = forText, AutowrapMode = TextServer.AutowrapMode.Word };
        name.AddThemeFontSizeOverride("font_size", 12);
        name.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(name);

        // ── Icon & stack count row ──────────────────────────────────────
        if (_stack != null)
        {

            var hbox = new HBoxContainer();
            var iconRect = new TextureRect
            {
                Texture = item.IconSprite,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(32, 32)
            };
            hbox.AddChild(iconRect);

            if (_stack.Count > 1)
            {
                var qty = new Label { Text = $"×{_stack.Count}" };
                hbox.AddChild(qty);
            }

            vbox.AddChild(hbox);

            // ── Description (wraps) ────────────────────────────────────
            if (!string.IsNullOrEmpty(item.ItemDescription))
            {
                var desc = new Label
                {
                    Text = item.ItemDescription,
                    AutowrapMode = TextServer.AutowrapMode.Word,
                    Modulate = new Color(0.9f, 0.9f, 0.9f)
                };
                vbox.AddChild(desc);
            }

            // // ── Example: stats block ──────────────────────────────────
            // if (item is IStats itemStats)
            // {
            //     string TextValue = string.Empty;
            //     foreach (Stat stat in itemStats.Stats.Values)
            //     {
            //         TextValue += $"{stat.type}: {stat.Value}\n";
            //     }
            //     var statsLbl = new Label
            //     {


            //         Text = TextValue,
            //         AutowrapMode = TextServer.AutowrapMode.Word,
            //         Modulate = new Color(0.7f, 0.9f, 1f)
            //     };
            //     vbox.AddChild(statsLbl);
            // }
        }

        return panel;   // Godot will add & position it for you
    }

    public override void _GuiInput(InputEvent @event)
    {

        // LEFT click --------------------------------------------------------
        if (@event is InputEventMouseButton mb
            && mb.ButtonIndex == MouseButton.Left
            && mb.Pressed)
        {
            GD.Print(this._stack, SlotIndex);
            UseItemRequest request = new UseItemRequest(_stack, SlotIndex);
            EventManager.I.Publish(GameEvent.UseItem, request);
        }
    }
}