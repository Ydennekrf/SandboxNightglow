using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public partial class AbilityPathGraphView : Control
    {
        public event Action<string> FocusedNodeChanged;

        private const float NodeRadius = 24f;
        private const float HitRadius = 34f;

        private Player _player;
        private IReadOnlyList<AbilityPathNodeDefinition> _nodes = Array.Empty<AbilityPathNodeDefinition>();
        private readonly Dictionary<string, Vector2> _positions = new(StringComparer.OrdinalIgnoreCase);
        private string _focusedNodeId = string.Empty;

        public string FocusedNodeId => _focusedNodeId;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.All;
        }

        public override void _Draw()
        {
            DrawConnections();
            DrawNodes();
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left)
            {
                return;
            }

            string hitNodeId = FindNodeAt(mouse.Position);
            if (string.IsNullOrWhiteSpace(hitNodeId))
            {
                return;
            }

            SetFocusedNode(hitNodeId);
            AcceptEvent();
        }

        public void Bind(Player player)
        {
            _player = player;
            Refresh();
        }

        public void Refresh()
        {
            _nodes = _player?.AbilityPath.GetNodes() ?? Array.Empty<AbilityPathNodeDefinition>();
            RebuildPositions();

            if (string.IsNullOrWhiteSpace(_focusedNodeId) || !_nodes.Any(node => node.NodeId == _focusedNodeId))
            {
                string defaultFocus = _player?.AbilityPath.CurrentNodeId;
                if (string.IsNullOrWhiteSpace(defaultFocus))
                {
                    defaultFocus = _nodes.FirstOrDefault()?.NodeId ?? string.Empty;
                }

                SetFocusedNode(defaultFocus, emit: false);
            }

            QueueRedraw();
        }

        public void SetFocusedNode(string nodeId, bool emit = true)
        {
            if (string.Equals(_focusedNodeId, nodeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _focusedNodeId = nodeId ?? string.Empty;
            QueueRedraw();
            if (emit)
            {
                FocusedNodeChanged?.Invoke(_focusedNodeId);
            }
        }

        private void RebuildPositions()
        {
            _positions.Clear();
            if (_nodes.Count == 0)
            {
                return;
            }

            Vector2 center = Size * 0.5f;
            float radius = Mathf.Max(80f, Mathf.Min(Size.X, Size.Y) * 0.32f);
            int fallbackIndex = 0;

            foreach (AbilityPathNodeDefinition node in _nodes)
            {
                if (node.GraphPosition != Vector2.Zero)
                {
                    _positions[node.NodeId] = node.GraphPosition;
                    continue;
                }

                float angle = Mathf.Tau * fallbackIndex / Mathf.Max(1, _nodes.Count);
                _positions[node.NodeId] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                fallbackIndex++;
            }
        }

        private void DrawConnections()
        {
            foreach (AbilityPathNodeDefinition node in _nodes)
            {
                if (!_positions.TryGetValue(node.NodeId, out Vector2 from))
                {
                    continue;
                }

                foreach (string connectedNodeId in node.ConnectedNodeIds ?? Enumerable.Empty<string>())
                {
                    if (string.Compare(node.NodeId, connectedNodeId, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        continue;
                    }

                    if (!_positions.TryGetValue(connectedNodeId, out Vector2 to))
                    {
                        continue;
                    }

                    bool available = _player?.AbilityPath.IsUnlocked(node.NodeId) == true
                        || _player?.AbilityPath.IsUnlocked(connectedNodeId) == true;
                    Color color = available ? new Color(0.75f, 0.86f, 1f, 0.8f) : new Color(0.35f, 0.38f, 0.43f, 0.7f);
                    DrawLine(from, to, color, available ? 3f : 2f, true);
                }
            }
        }

        private void DrawNodes()
        {
            foreach (AbilityPathNodeDefinition node in _nodes)
            {
                if (!_positions.TryGetValue(node.NodeId, out Vector2 position))
                {
                    continue;
                }

                bool unlocked = _player?.AbilityPath.IsUnlocked(node.NodeId) == true;
                bool unlockable = _player?.AbilityPath.IsUnlockable(node.NodeId) == true;
                bool focused = string.Equals(_focusedNodeId, node.NodeId, StringComparison.OrdinalIgnoreCase);
                bool current = string.Equals(_player?.AbilityPath.CurrentNodeId, node.NodeId, StringComparison.OrdinalIgnoreCase);

                Color fill = unlocked
                    ? new Color(0.32f, 0.72f, 0.48f, 1f)
                    : unlockable ? new Color(0.9f, 0.73f, 0.28f, 1f) : new Color(0.23f, 0.25f, 0.29f, 1f);
                Color outline = focused
                    ? new Color(1f, 1f, 1f, 1f)
                    : current ? new Color(0.45f, 0.8f, 1f, 1f) : new Color(0.08f, 0.09f, 0.11f, 1f);

                DrawCircle(position, NodeRadius + (focused ? 5f : current ? 3f : 0f), outline);
                DrawCircle(position, NodeRadius, fill);
            }
        }

        private string FindNodeAt(Vector2 point)
        {
            foreach ((string nodeId, Vector2 position) in _positions)
            {
                if (point.DistanceTo(position) <= HitRadius)
                {
                    return nodeId;
                }
            }

            return string.Empty;
        }
    }
}
