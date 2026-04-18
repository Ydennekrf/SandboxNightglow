using Godot;

namespace ethra.V1
{
    public partial class ItemPickup : Area2D
    {
        [Export] public int ItemId { get; set; }
        [Export] public int Quantity { get; set; } = 1;
        [Export] public bool RequireInteract { get; set; } = true;
        [Export] public NodePath SpritePath { get; set; } = "Sprite2D";
        [Export] public Texture2D PickupTexture { get; set; }

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
            if (!RequireInteract || !_playerInside)
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

            int attempts = Quantity > 0 ? Quantity : 1;
            int added = 0;

            for (int i = 0; i < attempts; i++)
            {
                if (!gm.AddItem(ItemId))
                {
                    break;
                }

                added++;
            }

            if (added == attempts)
            {
                QueueFree();
            }
            else
            {
                GD.Print($"ItemPickup: pickup incomplete for item {ItemId}. added={added}/{attempts}");
            }
        }
    }
}
