using Godot;
using Game.Interact;

namespace ethra.V1
{
    public partial class ItemPickup : Area2D, IInteractionPromptSource
    {
        [Export] public int ItemId { get; set; }
        [Export] public int Quantity { get; set; } = 1;
        [Export] public bool RequireInteract { get; set; } = true;
        [Export] public NodePath SpritePath { get; set; } = "Sprite2D";
        [Export] public Texture2D PickupTexture { get; set; }
        [Export] public string InteractionVerb { get; set; } = "Pick Up";
        [Export] public string InteractionPromptText { get; set; } = "Press E to Pick Up";
        [Export] public int InteractionPriority { get; set; } = 0;
        public bool CanInteract => RequireInteract;

        private bool _playerInside;
        private Sprite2D _sprite;

        public override void _Ready()
        {
            _sprite = GetNodeOrNull<Sprite2D>(SpritePath);
            if (_sprite == null)
            {
                GD.PushWarning($"ItemPickup: Sprite2D not found at path '{SpritePath}'.");
            }
            else if (PickupTexture != null)
            {
                _sprite.Texture = PickupTexture;
            }

            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
        }

        public override void _Process(double delta)
        {
            if (!RequireInteract || !_playerInside || GameManager.Instance?.UI?.BlocksGameplayInput == true)
            {
                return;
            }

            if (Input.IsActionJustPressed("Interact"))
            {
                TryPickup();
            }
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is not PlayerNode)
            {
                return;
            }

            _playerInside = true;
            if (RequireInteract)
            {
                PublishPrompt(InteractionPromptText);
            }
            if (!RequireInteract)
            {
                TryPickup();
            }
        }

        private void OnBodyExited(Node2D body)
        {
            if (body is PlayerNode)
            {
                _playerInside = false;
                PublishPrompt(string.Empty);
            }
        }

        private void TryPickup()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                GD.PushError("ItemPickup: GameManager.Instance was null.");
                return;
            }

            int quantity = Mathf.Max(1, Quantity);
            if (gm.Inventory?.AddItemQuantity(ItemId, quantity) == true)
            {
                Node2D player = GetTree()?.GetFirstNodeInGroup("Player") as Node2D;
                GameManager.Instance?.Publish(
                    GameEvent.FloatingTextRequested,
                    new FloatingTextRequest
                    {
                        Text = $"Picked up item {ItemId} x{quantity}",
                        WorldTarget = player,
                        WorldPosition = GlobalPosition,
                        HasWorldPosition = player == null,
                        Type = FloatingTextType.Pickup,
                        DurationSeconds = 1.1f
                    });
                GameManager.Instance?.Publish(
                    GameEvent.NotificationRequested,
                    new NotificationRequest($"Picked up item {ItemId} x{quantity}", NotificationType.Loot));
                PublishPrompt(string.Empty);
                QueueFree();
            }
            else
            {
                GD.Print($"ItemPickup: inventory could not accept item {ItemId} x{quantity}.");
            }
        }

        private void PublishPrompt(string text)
        {
            GameManager.Instance?.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(text, this));
        }
    }
}
