using Godot;
using Game.Interact;        // for IInteractable & DialogueStartDTO


public partial class LootPickup : Area2D, IInteractable
{

    [Export] public NodePath animPath;
    [Export] public StringName ItemId = "";   // e.g. "rusty_short_sword"
    [Export] public int        Count  = 1;
    [Export] public string InteractionVerb { get; set; } = "Pick Up";
    [Export] public string InteractionPromptText { get; set; } = "Press E to Pick Up";
    [Export] public int InteractionPriority { get; set; } = 0;
    [Export] public bool CanInteract { get; set; } = true;

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

        PublishFeedback($"{ItemId} x{Count}", player as Node2D);

        QueueFree();   // destroy the pickup in the world
    }

    private static void PublishFeedback(string text, Node2D target)
    {
        ethra.V1.GameManager gameManager = ethra.V1.GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.Publish(GameEvent.ToastMessage, text);
            gameManager.Publish(
                GameEvent.FloatingTextRequested,
                FloatingTextRequest.AtTarget(text, target, FloatingTextType.Pickup));
            return;
        }

        EventManager.I?.Publish(GameEvent.ToastMessage, text);
        EventManager.I?.Publish(
            GameEvent.FloatingTextRequested,
            FloatingTextRequest.AtTarget(text, target, FloatingTextType.Pickup));
    }
}
