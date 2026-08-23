using Game.Interact;
using Godot;

namespace ethra.V1
{
    /// <summary>
    /// Player-facing harvest node that requests the player's harvest state and grants inventory items on completion.
    /// </summary>
    /// <remarks>
    /// The interactable owns availability/visual state. The player state machine owns animation timing through
    /// PlayerHarvestRequest, and InventoryManager owns the item grant.
    /// </remarks>
    public partial class HarvestableInteractable : Area2D, IPlayerInteractable
    {
        /// <summary>Stable world-object ID reserved for persistence when respawn/harvest state saving is added.</summary>
        [Export] public string StableId { get; set; } = string.Empty;
        /// <summary>Numeric item ID granted after a successful harvest.</summary>
        [Export] public int ItemId { get; set; } = 3001;
        /// <summary>Quantity granted after a successful harvest.</summary>
        [Export] public int Quantity { get; set; } = 1;
        /// <summary>When true, this object becomes unavailable after the first successful harvest.</summary>
        [Export] public bool OneShot { get; set; } = true;
        /// <summary>Requested harvest animation duration; invalid values fall back to a short default.</summary>
        [Export] public float HarvestDurationSeconds { get; set; } = 0.35f;
        /// <summary>Base animation key sent to the player harvest state.</summary>
        [Export] public string AnimationKey { get; set; } = "Harvest";
        /// <summary>Optional tool visual requested while the player harvest animation runs.</summary>
        [Export] public HarvestToolType ToolType { get; set; } = HarvestToolType.None;
        /// <summary>Verb exposed through interaction prompt selection.</summary>
        [Export] public string InteractionVerb { get; set; } = "Harvest";
        /// <summary>Prompt text shown while this object is currently selectable.</summary>
        [Export] public string InteractionPromptText { get; set; } = "Press E to Harvest";
        /// <summary>Selection priority when multiple prompt sources overlap.</summary>
        [Export] public int InteractionPriority { get; set; } = 5;
        /// <summary>Visual shown while the object can still be harvested.</summary>
        [Export] public NodePath AvailableVisualPath { get; set; } = "Visual";
        /// <summary>Visual shown after a one-shot harvest completes.</summary>
        [Export] public NodePath HarvestedVisualPath { get; set; } = "HarvestedVisual";

        public bool CanInteract => !_harvested && !_harvestInProgress;

        private bool _harvested;
        private bool _harvestInProgress;
        private CanvasItem _availableVisual;
        private CanvasItem _harvestedVisual;

        public override void _Ready()
        {
            _availableVisual = GetNodeOrNull<CanvasItem>(AvailableVisualPath);
            _harvestedVisual = GetNodeOrNull<CanvasItem>(HarvestedVisualPath);
            SetHarvestedVisual(false);
        }

        /// <summary>
        /// Starts a player harvest request when this object is available.
        /// </summary>
        public void BeginInteraction(PlayerNode player)
        {
            if (player == null || !CanInteract)
            {
                return;
            }

            int safeQuantity = Quantity > 0 ? Quantity : 1;
            PlayerHarvestRequest request = new(this, ItemId, safeQuantity, CompleteHarvest)
            {
                AnimationKey = string.IsNullOrWhiteSpace(AnimationKey) ? "Harvest" : AnimationKey,
                DurationSeconds = HarvestDurationSeconds > 0f ? HarvestDurationSeconds : 0.35f,
                ToolType = ToolType
            };

            if (!player.RequestHarvest(request))
            {
                GD.Print("[Harvest] Player could not start harvest right now.");
                return;
            }

            _harvestInProgress = true;
            PublishPrompt(string.Empty);
        }

        private void CompleteHarvest(PlayerHarvestRequest request)
        {
            _harvestInProgress = false;

            if (_harvested || request == null)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            bool added = gameManager?.Inventory?.AddItemQuantity(request.ItemId, request.Quantity) == true;
            if (!added)
            {
                GD.Print($"[Harvest] Failed to collect {request.Quantity} x {request.ItemId}.");
                PublishPrompt(CanInteract ? InteractionPromptText : string.Empty);
                return;
            }

			if (OneShot)
			{
				_harvested = true;
				SetHarvestedVisual(true);
			}

			string displayName = gameManager?.DB?.GetItemDisplayName(request.ItemId) ?? request.ItemId.ToString();
			string pickupText = $"{displayName} x{request.Quantity}";
			Node2D playerNode = GetTree()?.GetFirstNodeInGroup("Player") as Node2D;
			gameManager?.Audio?.PlayEntity("sound.player.harvest", playerNode);

            gameManager?.Publish(
                GameEvent.FloatingTextRequested,
                new FloatingTextRequest
                {
                    Text = pickupText,
                    WorldTarget = playerNode,
                    WorldPosition = GlobalPosition,
                    HasWorldPosition = playerNode == null,
                    Type = FloatingTextType.Pickup,
                    DurationSeconds = 1.1f
                });

            gameManager?.Publish(
                GameEvent.NotificationRequested,
                new NotificationRequest($"Collected {pickupText}", NotificationType.Loot));

            GD.Print($"[Harvest] Collected {request.Quantity} x {request.ItemId}");
            PublishPrompt(CanInteract ? InteractionPromptText : string.Empty);
        }

        private void SetHarvestedVisual(bool harvested)
        {
            if (_availableVisual != null)
            {
                _availableVisual.Visible = !harvested;
            }

            if (_harvestedVisual != null)
            {
                _harvestedVisual.Visible = harvested;
            }
        }

        private void PublishPrompt(string text)
        {
            GameManager.Instance?.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(text, this));
        }
    }
}
