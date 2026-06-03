using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public partial class DialogManager
    {
        private DialogTree _activeTree;
        private DialogNode _currentNode;
        private InteractionDialogPanel _panel;
        private string _npcName;

        public bool IsDialogOpen => _panel != null && _panel.IsOpen;

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

            ShowCurrentNode();
            return true;
        }

        public void EndDialog()
        {
            _panel?.HideDialog();
            _activeTree = null;
            _currentNode = null;
            _panel = null;
            _npcName = string.Empty;
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
        }

        private IEnumerable<InteractionDialogChoice> BuildChoices(IEnumerable<DialogChoice> choices)
        {
            List<DialogChoice> choiceList = choices?.ToList() ?? new List<DialogChoice>();
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

        private static DialogNode FindNode(DialogTree tree, string nodeId)
        {
            return tree?.Nodes?.FirstOrDefault(node => node.NodeId == nodeId);
        }
    }
}
