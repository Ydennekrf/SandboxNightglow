using Godot;
using Game.Interact;

namespace ethra.V1
{
    public partial class DebugChestInteraction : Area2D, IPlayerInteractable
    {
        [Export] public string RequiredQuestId { get; set; } = string.Empty;
        [Export] public NodePath LootDropperPath { get; set; } = "LootDropper";
        [Export] public float OpenDurationSeconds { get; set; } = 0.35f;
        [Export] public string AnimationKey { get; set; } = "Harvest";
        [Export] public NodePath ClosedVisualPath { get; set; } = "ClosedVisual";
        [Export] public NodePath OpenedVisualPath { get; set; } = "OpenedVisual";
        [Export] public string InteractionVerb { get; set; } = "Open";
        [Export] public string InteractionPromptText { get; set; } = "Press E to Open";
        [Export] public int InteractionPriority { get; set; } = 0;
        public bool CanInteract => !_opened && !_openInProgress && MeetsQuestRequirement();

        private PlayerNode _player;
        private LootDropper _lootDropper;
        private CanvasItem _closedVisual;
        private CanvasItem _openedVisual;
        private bool _opened;
        private bool _openInProgress;
        private ulong _lastInteractionFrame;

        public override void _Ready()
        {
            _lootDropper = GetNodeOrNull<LootDropper>(LootDropperPath);
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
            if (_player == null || GameManager.Instance?.UI?.BlocksGameplayInput == true || !Input.IsActionJustPressed("Interact"))
            {
                return;
            }

            BeginInteraction(_player);
        }

        public void BeginInteraction(PlayerNode player)
        {
            if (player == null || !CanInteract)
            {
                return;
            }

            ulong currentFrame = Engine.GetProcessFrames();
            if (_lastInteractionFrame == currentFrame)
            {
                return;
            }

            _lastInteractionFrame = currentFrame;

            PlayerHarvestRequest request = new(this, 0, 0, CompleteOpen)
            {
                AnimationKey = string.IsNullOrWhiteSpace(AnimationKey) ? "Harvest" : AnimationKey,
                DurationSeconds = OpenDurationSeconds > 0f ? OpenDurationSeconds : 0.35f,
                ToolType = HarvestToolType.None
            };

            if (!player.RequestHarvest(request))
            {
                GD.Print("[ChestDebug] Player could not open chest right now.");
                return;
            }

            _openInProgress = true;
            PublishPrompt(string.Empty);
        }

        private void CompleteOpen(PlayerHarvestRequest request)
        {
            _openInProgress = false;
            if (_opened)
            {
                GD.Print("[ChestDebug] Chest is already open.");
                return;
            }

            _opened = true;
            SetOpenedVisual(true);
            GameManager.Instance?.Audio?.PlaySceneSound("sound.world.chest_open", this);
            _lootDropper?.DropLoot();
            PublishPrompt(string.Empty);
            GD.Print("[ChestDebug] Player opened chest and dropped its loot.");
        }

        private bool MeetsQuestRequirement()
        {
            if (string.IsNullOrWhiteSpace(RequiredQuestId))
            {
                return true;
            }

            return GameManager.Instance?.Quest?.IsQuestActive(RequiredQuestId) == true;
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
                PublishPrompt(CanInteract ? InteractionPromptText : string.Empty);
            }
        }

        private void OnBodyExited(Node2D body)
        {
            if (body == _player)
            {
                _player = null;
                PublishPrompt(string.Empty);
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            if (area.GetParent() is PlayerNode player)
            {
                _player = player;
                PublishPrompt(CanInteract ? InteractionPromptText : string.Empty);
            }
        }

        private void OnAreaExited(Area2D area)
        {
            if (area.GetParent() == _player)
            {
                _player = null;
                PublishPrompt(string.Empty);
            }
        }

        private void PublishPrompt(string text)
        {
            GameManager.Instance?.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(text, this));
        }
    }
}
