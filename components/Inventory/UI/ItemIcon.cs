using Godot;
using System;

public partial class ItemIcon : Control
{
    [Export] private TextureRect _icon;
    [Export] private Label _count;
    [Export] private TextureRect _highlight;

    // add an export here for the Icon or inventory's tooltip that or send an event with the payload to it
    public int SlotIndex;


    private ItemStack? _stack;

    public override void _Ready()
    {
        MouseEntered += () => _highlight.Visible = true;
        MouseExited += () => _highlight.Visible = false;
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

    // this will need some form of updating to allow for dynamic keybinds down the line.
    public override void _GuiInput(InputEvent e)
    {        
         
    if (e.IsActionPressed("Melee"))        
    {
        if (_stack == null) return;
                 
        EventManager.I.Publish(
            GameEvent.UseItem,
            new UseItemRequest(_stack, SlotIndex));
            AcceptEvent();             
    }
    }
    
}