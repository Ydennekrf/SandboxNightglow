using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class DialogGraphCanvas : Control
    {
        public event Action<string> NodeSelected;

        private const float CardWidth = 260f;
        private const float CardHeight = 150f;
        private const float ColumnGap = 80f;
        private const float RowGap = 70f;

        private readonly Dictionary<string, Control> _cards = new();
        private DialogTree _tree;
        private string _selectedNodeId = string.Empty;

        public void SetTree(DialogTree tree, string selectedNodeId)
        {
            _tree = tree;
            _selectedNodeId = selectedNodeId ?? string.Empty;
            RebuildCards();
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_tree?.Nodes == null)
            {
                return;
            }

            foreach (DialogNode node in _tree.Nodes)
            {
                if (!_cards.TryGetValue(node.NodeId, out Control fromCard))
                {
                    continue;
                }

                foreach (DialogChoice choice in node.Choices ?? new List<DialogChoice>())
                {
                    if (string.IsNullOrWhiteSpace(choice.NextNodeId)
                        || !_cards.TryGetValue(choice.NextNodeId, out Control toCard))
                    {
                        continue;
                    }

                    Vector2 from = fromCard.Position + new Vector2(fromCard.Size.X, fromCard.Size.Y * 0.5f);
                    Vector2 to = toCard.Position + new Vector2(0f, toCard.Size.Y * 0.5f);
                    Color color = choice.ActionId == DialogActionRunner.StoreStubActionId
                        ? new Color(0.95f, 0.72f, 0.22f)
                        : new Color(0.45f, 0.65f, 0.95f);

                    DrawLine(from, to, color, 2f, true);
                    DrawCircle(to, 4f, color);
                }
            }
        }

        private void RebuildCards()
        {
            foreach (Node child in GetChildren())
            {
                RemoveChild(child);
                child.QueueFree();
            }

            _cards.Clear();

            if (_tree?.Nodes == null)
            {
                CustomMinimumSize = new Vector2(900f, 600f);
                return;
            }

            for (int i = 0; i < _tree.Nodes.Count; i++)
            {
                DialogNode node = _tree.Nodes[i];
                PanelContainer card = BuildCard(node);
                int column = i % 3;
                int row = i / 3;
                card.Position = new Vector2(30f + column * (CardWidth + ColumnGap), 30f + row * (CardHeight + RowGap));
                card.Size = new Vector2(CardWidth, CardHeight);
                AddChild(card);
                _cards[node.NodeId] = card;
            }

            int rows = Math.Max(1, (_tree.Nodes.Count + 2) / 3);
            CustomMinimumSize = new Vector2(3 * (CardWidth + ColumnGap) + 80f, rows * (CardHeight + RowGap) + 80f);
        }

        private PanelContainer BuildCard(DialogNode node)
        {
            PanelContainer card = new()
            {
                CustomMinimumSize = new Vector2(CardWidth, CardHeight),
                MouseFilter = MouseFilterEnum.Stop
            };

            VBoxContainer content = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            card.AddChild(content);

            Label idLabel = new()
            {
                Text = node.NodeId == _tree.StartingNodeId ? $"{node.NodeId}  [START]" : node.NodeId,
                ClipText = true,
                ThemeTypeVariation = "HeaderSmall"
            };
            content.AddChild(idLabel);

            Label speakerLabel = new()
            {
                Text = node.SpeakerName,
                ClipText = true
            };
            content.AddChild(speakerLabel);

            Label textLabel = new()
            {
                Text = node.Text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(CardWidth - 18f, 48f)
            };
            content.AddChild(textLabel);

            int choiceCount = node.Choices?.Count ?? 0;
            Label choiceLabel = new()
            {
                Text = $"choices: {choiceCount}",
                ClipText = true
            };
            content.AddChild(choiceLabel);

            if (!string.IsNullOrEmpty(_selectedNodeId) && node.NodeId == _selectedNodeId)
            {
                card.Modulate = new Color(1f, 0.95f, 0.62f);
            }

            card.GuiInput += input =>
            {
                if (input is InputEventMouseButton mouseButton
                    && mouseButton.Pressed
                    && mouseButton.ButtonIndex == MouseButton.Left)
                {
                    _selectedNodeId = node.NodeId;
                    NodeSelected?.Invoke(node.NodeId);
                }
            };

            return card;
        }
    }
}
