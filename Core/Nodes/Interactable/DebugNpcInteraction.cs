using Godot;

namespace ethra.V1
{
    public partial class DebugNpcInteraction : Area2D
    {
        [Export] public string NpcName { get; set; } = "Debug Merchant";
        [Export] public string DialogTreeId { get; set; } = "interaction_debug_merchant";
        [Export] public NodePath DialogPanelPath { get; set; } = "../UI/InteractionDialogPanel";

        private PlayerNode _player;
        private PlayerNode _lockedPlayer;
        private InteractionDialogPanel _dialogPanel;

        public override void _Ready()
        {
            _dialogPanel = GetNodeOrNull<InteractionDialogPanel>(DialogPanelPath)
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

            if (Input.IsActionJustPressed("Interact") && !_dialogPanel.IsOpen)
            {
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

            if (!gameManager.Dialog.StartDialog(DialogTreeId, NpcName, _dialogPanel))
            {
                UnlockPlayer();
            }
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
