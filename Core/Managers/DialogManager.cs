using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ethra.V1
{
    /// <summary>
    /// Plays authored dialog trees through the active InteractionDialogPanel.
    /// </summary>
    /// <remarks>
    /// DialogManager owns dialog session state and publishes dialog/quest events. Dialog content is loaded
    /// from MasterRepository, while individual interactables decide which tree ID to start.
    /// </remarks>
    public partial class DialogManager
    {
        private DialogTree _activeTree;
        private DialogNode _currentNode;
        private InteractionDialogPanel _panel;
        private string _npcName;
        private bool _endingDialog;

        /// <summary>
        /// True while a dialog panel is bound and showing dialog.
        /// </summary>
        public bool IsDialogOpen => _panel != null && _panel.IsOpen;

        /// <summary>
        /// Shows a one-shot authored cutscene line through the existing dialog panel.
        /// </summary>
        public async Task PlayCutsceneLineAsync(
            InteractionDialogPanel panel,
            string speakerName,
            string body,
            Texture2D portrait,
            bool waitForPlayerInput,
            float autoAdvanceDelaySeconds,
            string lineId)
        {
            if (panel == null)
            {
                GD.PushWarning("DialogManager: cannot show cutscene line without an InteractionDialogPanel.");
                return;
            }

            string safeLineId = string.IsNullOrWhiteSpace(lineId) ? "cutscene_line" : lineId;
            string safeSpeaker = string.IsNullOrWhiteSpace(speakerName) ? "Speaker" : speakerName;
            bool completed = !waitForPlayerInput;
            void OnClosed() => completed = true;

            panel.DialogClosed -= OnClosed;
            panel.DialogClosed += OnClosed;
            GameManager.Instance?.Publish(GameEvent.DialogStarted, new DialogStartedEvent(safeLineId));
            GameManager.Instance?.Publish(
                GameEvent.DialogNodeChanged,
                new DialogNodeChangedEvent(safeLineId, safeLineId, safeSpeaker, IsPlayerSpeaker(safeSpeaker), "Dialog"));

            panel.ShowDialog(
                safeSpeaker,
                body,
                portrait,
                waitForPlayerInput
                    ? new[] { new InteractionDialogChoice("Continue", () => completed = true) }
                    : Array.Empty<InteractionDialogChoice>());

            if (autoAdvanceDelaySeconds > 0f)
            {
                await panel.ToSignal(panel.GetTree().CreateTimer(autoAdvanceDelaySeconds), SceneTreeTimer.SignalName.Timeout);
                completed = true;
            }

            while (!completed)
            {
                await panel.ToSignal(panel.GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            panel.DialogClosed -= OnClosed;
            panel.HideDialog();
            GameManager.Instance?.Publish(GameEvent.DialogEnded, new DialogEndedEvent(safeLineId));
        }

        /// <summary>
        /// Starts a dialog tree by stable ID and binds it to the supplied panel.
        /// </summary>
        public bool StartDialog(string treeId, string npcName, InteractionDialogPanel panel)
        {
            if (panel == null)
            {
                GD.PushWarning("DialogManager: cannot start dialog without an InteractionDialogPanel.");
                return false;
            }

            DialogTree tree = GameManager.Instance?.DB?.GetDialogTreeFromRepo(treeId);
            if (tree == null)
            {
                GD.PushWarning($"DialogManager: dialog tree '{treeId}' was not found in MasterRepository.");
                return false;
            }

            DialogNode startingNode = FindNode(tree, tree.StartingNodeId);
            if (startingNode == null)
            {
                GD.PushWarning($"DialogManager: starting node '{tree.StartingNodeId}' was not found in tree '{tree.TreeId}'.");
                return false;
            }

            _activeTree = tree;
            _currentNode = startingNode;
            _panel = panel;
			_npcName = npcName;
            _panel.DialogClosed -= OnPanelClosed;
            _panel.DialogClosed += OnPanelClosed;

            GameManager.Instance?.Publish(GameEvent.DialogStarted, new DialogStartedEvent(_activeTree.TreeId));
			ShowCurrentNode();
			GameManager.Instance?.Publish(GameEvent.NpcSpokenTo, new NpcSpokenToQuestEvent(npcName));
			return true;
		}

        /// <summary>
        /// Ends the active dialog session, unbinds panel events, and publishes DialogEnded.
        /// </summary>
        public void EndDialog()
        {
            if (_endingDialog)
            {
                return;
            }

            _endingDialog = true;
            string treeId = _activeTree?.TreeId ?? string.Empty;
            if (_panel != null)
            {
                _panel.DialogClosed -= OnPanelClosed;
            }

            _panel?.HideDialog();
            _activeTree = null;
            _currentNode = null;
            _panel = null;
            _npcName = string.Empty;
            GameManager.Instance?.Publish(GameEvent.DialogEnded, new DialogEndedEvent(treeId));
            _endingDialog = false;
        }

        private void ShowCurrentNode()
        {
            if (_activeTree == null || _currentNode == null || _panel == null)
            {
                return;
            }

            _panel.ShowDialog(
                ResolveSpeakerName(_currentNode),
                _currentNode.Text,
                BuildChoices(_currentNode.Choices));
            PublishNodeChanged();
        }

        private IEnumerable<InteractionDialogChoice> BuildChoices(IEnumerable<DialogChoice> choices)
        {
            List<DialogChoice> choiceList = choices?
                .Where(choice => DialogConditionRunner.IsMet(choice, _npcName))
                .ToList() ?? new List<DialogChoice>();
            if (choiceList.Count == 0)
            {
                return new List<InteractionDialogChoice>
                {
                    new("Continue", EndDialog)
                };
            }

            return choiceList.Select(choice =>
                new InteractionDialogChoice(choice.ChoiceText, () => SelectChoice(choice)));
        }

        private void SelectChoice(DialogChoice choice)
        {
            if (choice == null)
            {
                EndDialog();
                return;
            }

            DialogActionRunner.Run(choice, _npcName);

            if (choice.EndsDialog)
            {
                EndDialog();
                return;
            }

            if (string.IsNullOrWhiteSpace(choice.NextNodeId))
            {
                EndDialog();
                return;
            }

            DialogNode nextNode = FindNode(_activeTree, choice.NextNodeId);
            if (nextNode == null)
            {
                GD.PushWarning($"DialogManager: choice '{choice.ChoiceText}' points to missing node '{choice.NextNodeId}' in tree '{_activeTree.TreeId}'.");
                EndDialog();
                return;
            }

            _currentNode = nextNode;
            ShowCurrentNode();
        }

        private string ResolveSpeakerName(DialogNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.SpeakerName))
            {
                return node.SpeakerName;
            }

            return string.IsNullOrWhiteSpace(_npcName) ? "NPC" : _npcName;
        }

        private void PublishNodeChanged()
        {
            if (_activeTree == null || _currentNode == null)
            {
                return;
            }

            string speakerName = ResolveSpeakerName(_currentNode);
            GameManager.Instance?.Publish(
                GameEvent.DialogNodeChanged,
                new DialogNodeChangedEvent(
                    _activeTree.TreeId,
                    _currentNode.NodeId,
                    speakerName,
                    IsPlayerSpeaker(speakerName),
                    string.IsNullOrWhiteSpace(_currentNode.PlayerAnimationKey) ? "Dialog" : _currentNode.PlayerAnimationKey));
        }

        private static bool IsPlayerSpeaker(string speakerName)
        {
            if (string.IsNullOrWhiteSpace(speakerName))
            {
                return false;
            }

            if (string.Equals(speakerName, "Player", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(speakerName, "Selene", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string playerName = GameManager.Instance?.GetPlayer()?.Name;
            return !string.IsNullOrWhiteSpace(playerName)
                && string.Equals(speakerName, playerName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static DialogNode FindNode(DialogTree tree, string nodeId)
        {
            return tree?.Nodes?.FirstOrDefault(node => node.NodeId == nodeId);
        }

        private void OnPanelClosed()
        {
            EndDialog();
        }
    }
}
