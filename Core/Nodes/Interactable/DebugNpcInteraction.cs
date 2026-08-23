using Godot;
using Game.Interact;

namespace ethra.V1
{
    public partial class DebugNpcInteraction : Area2D, IInteractionPromptSource
    {
        private const string DefaultNpcName = "Debug Merchant";
        private const string DefaultDialogTreeId = "interaction_debug_merchant";
        private const string DefaultDialogPanelPath = "../UI/InteractionDialogPanel";

        [Export] public string NpcName { get; set; } = "Debug Merchant";
        [Export] public string DialogTreeId { get; set; } = "interaction_debug_merchant";
        [Export] public NodePath DialogPanelPath { get; set; } = "../UI/InteractionDialogPanel";
        [Export] public string InteractionVerb { get; set; } = "Talk";
        [Export] public string InteractionPromptText { get; set; } = "Press E to Talk";
        [Export] public int InteractionPriority { get; set; } = 0;
        public bool CanInteract => _dialogPanel == null || !_dialogPanel.IsOpen;

        private PlayerNode _player;
        private PlayerNode _lockedPlayer;
        private InteractionDialogPanel _dialogPanel;

        public override void _Ready()
        {
            _dialogPanel = GetNodeOrFallback<InteractionDialogPanel>(DialogPanelPath, DefaultDialogPanelPath)
                ?? GetTree().CurrentScene.GetNodeOrNull<InteractionDialogPanel>("UI/InteractionDialogPanel");

            if (_dialogPanel == null)
            {
                GD.PushWarning("DebugNpcInteraction: InteractionDialogPanel was not found.");
            }
            else
            {
                _dialogPanel.DialogClosed += UnlockPlayer;
            }

            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
            AreaEntered += OnAreaEntered;
            AreaExited += OnAreaExited;
        }

        public override void _ExitTree()
        {
            if (_dialogPanel != null)
            {
                _dialogPanel.DialogClosed -= UnlockPlayer;
            }
        }

        public override void _Process(double delta)
        {
            if (_player == null || _dialogPanel == null)
            {
                return;
            }

            if (Input.IsActionJustPressed("Interact")
                && !_dialogPanel.IsOpen
                && GameManager.Instance?.UI?.BlocksGameplayInput != true)
            {
                PublishPrompt(string.Empty);
                StartDialogTree();
            }
        }

        private void StartDialogTree()
        {
            LockPlayer();
            GameManager gameManager = GameManager.Instance;
            if (gameManager?.Dialog == null)
            {
                GD.PushWarning("DebugNpcInteraction: DialogManager is not available.");
                UnlockPlayer();
                return;
            }

            string dialogTreeId = GetValueOrDefault(DialogTreeId, DefaultDialogTreeId);
            string npcName = GetValueOrDefault(NpcName, DefaultNpcName);

            if (!gameManager.Dialog.StartDialog(dialogTreeId, npcName, _dialogPanel))
            {
                UnlockPlayer();
            }
        }

        private T GetNodeOrFallback<T>(NodePath configuredPath, string fallbackPath) where T : Node
        {
            T node = null;
            if (!IsEmptyNodePath(configuredPath))
            {
                node = GetNodeOrNull<T>(configuredPath);
            }

            return node ?? GetNodeOrNull<T>(fallbackPath);
        }

        private static string GetValueOrDefault(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private void LockPlayer()
        {
            _lockedPlayer = _player;
            _lockedPlayer?.SetInteractionLocked(true);
        }

        private void UnlockPlayer()
        {
            PlayerNode playerToUnlock = _lockedPlayer ?? _player;
            playerToUnlock?.SetInteractionLocked(false);
            _lockedPlayer = null;
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

        private static bool IsEmptyNodePath(NodePath path)
        {
            return path == null || string.IsNullOrWhiteSpace(path.ToString());
        }
    }
}
