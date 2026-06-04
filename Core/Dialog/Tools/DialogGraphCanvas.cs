using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public partial class DialogGraphCanvas : Control
    {
        public event Action<string> NodeSelected;
        public event Action<Vector2> PanRequested;
        public event Action<float> ZoomRequested;

        private const float CardWidth = 260f;
        private const float CardHeight = 150f;
        private const float ColumnGap = 80f;
        private const float RowGap = 70f;
        private const float CanvasPadding = 40f;
        private const float MinCanvasWidth = 900f;
        private const float MinCanvasHeight = 600f;
        private const string AnchorNameLeft = "left";
        private const string AnchorNameRight = "right";
        private const string AnchorNameTop = "top";
        private const string AnchorNameBottom = "bottom";

        private readonly Dictionary<string, Control> _cards = new();
        private DialogTree _tree;
        private string _selectedNodeId = string.Empty;
        private float _zoom = 1f;
        private bool _isPanning;
        private DialogNode _draggedNode;

        public void SetTree(DialogTree tree, string selectedNodeId)
        {
            _tree = tree;
            _selectedNodeId = selectedNodeId ?? string.Empty;
            EnsureNodeLayout();
            RebuildCards();
            QueueRedraw();
        }

        public void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, 0.35f, 2.25f);
            RebuildCards();
            QueueRedraw();
        }

        public void AutoLayout()
        {
            if (_tree?.Nodes == null || _tree.Nodes.Count == 0)
            {
                return;
            }

            Dictionary<string, DialogNode> nodesById = new();
            foreach (DialogNode node in _tree.Nodes)
            {
                nodesById[node.NodeId] = node;
            }

            Dictionary<string, int> levels = new();
            Queue<string> pending = new();
            if (!string.IsNullOrWhiteSpace(_tree.StartingNodeId) && nodesById.ContainsKey(_tree.StartingNodeId))
            {
                levels[_tree.StartingNodeId] = 0;
                pending.Enqueue(_tree.StartingNodeId);
            }

            while (pending.Count > 0)
            {
                string nodeId = pending.Dequeue();
                int level = levels[nodeId];
                foreach (DialogChoice choice in nodesById[nodeId].Choices ?? new List<DialogChoice>())
                {
                    if (string.IsNullOrWhiteSpace(choice.NextNodeId)
                        || !nodesById.ContainsKey(choice.NextNodeId)
                        || levels.ContainsKey(choice.NextNodeId))
                    {
                        continue;
                    }

                    levels[choice.NextNodeId] = level + 1;
                    pending.Enqueue(choice.NextNodeId);
                }
            }

            int fallbackLevel = levels.Count == 0 ? 0 : levels.Values.Max() + 1;
            foreach (DialogNode node in _tree.Nodes)
            {
                if (!levels.ContainsKey(node.NodeId))
                {
                    levels[node.NodeId] = fallbackLevel;
                }
            }

            Dictionary<int, int> rowByLevel = new();
            foreach (DialogNode node in _tree.Nodes)
            {
                int level = levels[node.NodeId];
                rowByLevel.TryGetValue(level, out int row);
                node.GraphX = level * (CardWidth + ColumnGap);
                node.GraphY = row * (CardHeight + RowGap);
                rowByLevel[level] = row + 1;
            }

            RebuildCards();
            QueueRedraw();
        }

        public override void _GuiInput(InputEvent input)
        {
            if (input is InputEventMouseButton mouseButton)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
                {
                    ZoomRequested?.Invoke(1.1f);
                    AcceptEvent();
                    return;
                }

                if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
                {
                    ZoomRequested?.Invoke(0.9f);
                    AcceptEvent();
                    return;
                }

                if (mouseButton.ButtonIndex == MouseButton.Middle || mouseButton.ButtonIndex == MouseButton.Right)
                {
                    _isPanning = mouseButton.Pressed;
                    AcceptEvent();
                }
            }

            if (input is InputEventMouseMotion motion && _isPanning)
            {
                PanRequested?.Invoke(-motion.Relative);
                AcceptEvent();
            }
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

                    Vector2 from = ResolveAnchorPoint(fromCard.Position, choice.FromAnchor, defaultAnchor: AnchorNameRight);
                    Vector2 to = ResolveAnchorPoint(toCard.Position, choice.ToAnchor, defaultAnchor: AnchorNameLeft);
                    Color color = !string.IsNullOrWhiteSpace(choice.ConditionId)
                        ? new Color(0.74f, 0.48f, 0.95f)
                        : !string.IsNullOrWhiteSpace(choice.ActionId)
                        ? new Color(0.95f, 0.72f, 0.22f)
                        : new Color(0.45f, 0.65f, 0.95f);

                    DrawLine(from, to, color, Mathf.Max(1f, 2f * _zoom), true);
                    DrawCircle(to, Mathf.Max(3f, 4f * _zoom), color);

                    if (!string.IsNullOrWhiteSpace(choice.ConditionId))
                    {
                        DrawConnectionLabel(from, to, choice.ConditionId);
                    }
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
                CustomMinimumSize = new Vector2(MinCanvasWidth, MinCanvasHeight);
                return;
            }

            foreach (DialogNode node in _tree.Nodes)
            {
                PanelContainer card = BuildCard(node);
                card.Position = GraphToCanvas(new Vector2(node.GraphX, node.GraphY));
                card.Size = new Vector2(CardWidth, CardHeight);
                card.Scale = Vector2.One * _zoom;
                AddChild(card);
                _cards[node.NodeId] = card;
            }

            UpdateCanvasMinimumSize();
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
            int conditionalChoiceCount = node.Choices?.Count(choice => !string.IsNullOrWhiteSpace(choice.ConditionId)) ?? 0;
            Label choiceLabel = new()
            {
                Text = conditionalChoiceCount == 0
                    ? $"choices: {choiceCount}"
                    : $"choices: {choiceCount} ({conditionalChoiceCount} conditional)",
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
                    && mouseButton.ButtonIndex == MouseButton.Left)
                {
                    if (mouseButton.Pressed)
                    {
                        _selectedNodeId = node.NodeId;
                        _draggedNode = node;
                        ApplySelectionHighlight();
                        NodeSelected?.Invoke(node.NodeId);
                    }
                    else if (_draggedNode == node)
                    {
                        _draggedNode = null;
                    }
                }

                if (input is InputEventMouseMotion motion && _draggedNode == node)
                {
                    node.GraphX = Mathf.Max(0f, node.GraphX + motion.Relative.X / _zoom);
                    node.GraphY = Mathf.Max(0f, node.GraphY + motion.Relative.Y / _zoom);
                    card.Position = GraphToCanvas(new Vector2(node.GraphX, node.GraphY));
                    UpdateCanvasMinimumSize();
                    QueueRedraw();
                }
            };

            return card;
        }

        private void ApplySelectionHighlight()
        {
            foreach (KeyValuePair<string, Control> entry in _cards)
            {
                entry.Value.Modulate = !string.IsNullOrEmpty(_selectedNodeId) && entry.Key == _selectedNodeId
                    ? new Color(1f, 0.95f, 0.62f)
                    : Colors.White;
            }
        }

        private void EnsureNodeLayout()
        {
            if (_tree?.Nodes == null)
            {
                return;
            }

            bool hasAnyPosition = _tree.Nodes.Any(node => node.GraphX != 0f || node.GraphY != 0f);
            if (hasAnyPosition)
            {
                return;
            }

            AutoLayout();
        }

        private Vector2 GraphToCanvas(Vector2 graphPosition)
        {
            return (graphPosition + Vector2.One * CanvasPadding) * _zoom;
        }

        private Vector2 ResolveAnchorPoint(Vector2 cardPosition, string anchor, string defaultAnchor)
        {
            string normalizedAnchor = string.IsNullOrWhiteSpace(anchor) ? defaultAnchor : anchor;
            Vector2 size = new(CardWidth * _zoom, CardHeight * _zoom);
            return normalizedAnchor switch
            {
                AnchorNameLeft => cardPosition + new Vector2(0f, size.Y * 0.5f),
                AnchorNameTop => cardPosition + new Vector2(size.X * 0.5f, 0f),
                AnchorNameBottom => cardPosition + new Vector2(size.X * 0.5f, size.Y),
                _ => cardPosition + new Vector2(size.X, size.Y * 0.5f)
            };
        }

        private void DrawConnectionLabel(Vector2 from, Vector2 to, string text)
        {
            Font font = GetThemeDefaultFont();
            if (font == null)
            {
                return;
            }

            Vector2 midpoint = from.Lerp(to, 0.5f);
            Vector2 labelPosition = midpoint + new Vector2(8f * _zoom, -8f * _zoom);
            DrawString(font, labelPosition, text, HorizontalAlignment.Left, -1f, Mathf.RoundToInt(Mathf.Max(10f, 12f * _zoom)), new Color(0.9f, 0.78f, 1f));
        }

        private void UpdateCanvasMinimumSize()
        {
            if (_tree?.Nodes == null || _tree.Nodes.Count == 0)
            {
                CustomMinimumSize = new Vector2(MinCanvasWidth, MinCanvasHeight);
                return;
            }

            float maxX = _tree.Nodes.Max(node => node.GraphX) + CardWidth + CanvasPadding * 2f;
            float maxY = _tree.Nodes.Max(node => node.GraphY) + CardHeight + CanvasPadding * 2f;
            CustomMinimumSize = new Vector2(
                Math.Max(MinCanvasWidth, maxX * _zoom),
                Math.Max(MinCanvasHeight, maxY * _zoom));
        }
    }
}
