using Godot;

namespace ethra.V1
{
    public partial class DebugChestInteraction : Area2D
    {
        [Export] public string LootText { get; set; } = "Found: Test Loot";
        [Export] public int RewardItemId { get; set; } = 3001;
        [Export] public int RewardQuantity { get; set; } = 1;
        [Export] public NodePath ClosedVisualPath { get; set; } = "ClosedVisual";
        [Export] public NodePath OpenedVisualPath { get; set; } = "OpenedVisual";

        private PlayerNode _player;
        private CanvasItem _closedVisual;
        private CanvasItem _openedVisual;
        private bool _opened;

        public override void _Ready()
        {
            _closedVisual = GetNodeOrNull<CanvasItem>(ClosedVisualPath);
            _openedVisual = GetNodeOrNull<CanvasItem>(OpenedVisualPath);
            SetOpenedVisual(false);

            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
            AreaEntered += OnAreaEntered;
            AreaExited += OnAreaExited;
        }

        public override void _Process(double delta)
        {
            if (_player == null || !Input.IsActionJustPressed("Interact"))
            {
                return;
            }

            TryOpen();
        }

        private void TryOpen()
        {
            if (_opened)
            {
                GD.Print("[ChestDebug] Chest is already open.");
                return;
            }

            _opened = true;
            SetOpenedVisual(true);
            AddRewardItems();
            ShowPickupPopup(_player);
            GD.Print("[ChestDebug] Player opened chest and received Test Loot.");
        }

        private void AddRewardItems()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || RewardItemId <= 0)
            {
                return;
            }

            int quantity = RewardQuantity > 0 ? RewardQuantity : 1;
            for (int i = 0; i < quantity; i++)
            {
                gameManager.AddItem(RewardItemId);
            }
        }

        private void ShowPickupPopup(PlayerNode player)
        {
            if (player == null)
            {
                return;
            }

            PickupPopupLabel popup = new()
            {
                Text = LootText,
                Position = new Vector2(-56f, -36f),
                Size = new Vector2(112f, 18f),
                Modulate = new Color(0.85f, 1f, 0.65f, 1f)
            };

            player.AddChild(popup);
        }

        private void SetOpenedVisual(bool opened)
        {
            if (_closedVisual != null)
            {
                _closedVisual.Visible = !opened;
            }

            if (_openedVisual != null)
            {
                _openedVisual.Visible = opened;
            }
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is PlayerNode player)
            {
                _player = player;
            }
        }

        private void OnBodyExited(Node2D body)
        {
            if (body == _player)
            {
                _player = null;
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            if (area.GetParent() is PlayerNode player)
            {
                _player = player;
            }
        }

        private void OnAreaExited(Area2D area)
        {
            if (area.GetParent() == _player)
            {
                _player = null;
            }
        }
    }
}
