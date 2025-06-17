using Godot;
using Game.Interact;        // for IInteractable & DialogueStartDTO


public partial class LootPickup : Area2D, IInteractable
{

    [Export] public NodePath animPath;
    [Export] public StringName ItemId = "";   // e.g. "rusty_short_sword"
    [Export] public int        Count  = 1;

    private AnimationPlayer anim;

    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>(animPath);
    }

    // Called by InteractComponent when the player presses the Interact key
    public void BeginInteraction(DialogueStartDTO data)
    {
        var player = data.Initiator;
        if (player == null) return;
        anim.Play("Open");
        var inv = player.GetNode<InventoryComponent>("Inventory");
        inv.AddItem(ItemId, Count);

        // (optional) publish a bus event so HUD can show a toast later
        EventManager.I.Publish(
            GameEvent.ToastMessage, $"{ItemId} x{Count}");

        QueueFree();   // destroy the pickup in the world
    }
}