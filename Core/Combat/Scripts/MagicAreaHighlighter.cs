using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class MagicAreaHighlighter : Node2D
    {
        [Export] public float CellSize { get; set; } = 24f;
        [Export] public float PreviewSeconds { get; set; } = 2.5f;

        private readonly List<Node> _previewNodes = new();

        public void ShowPreview(RuneItem elementalRune, Vector2 worldOrigin, Vector2I direction)
        {
            if (elementalRune == null || !elementalRune.IsElementalRune)
            {
                return;
            }

            ShowPreview(elementalRune.Element, elementalRune.Shape, worldOrigin, direction, CellSize, GetPlayerMagicRangeBonus());
        }

        public void ShowPreview(ElementType element, MagicShape shape, Vector2 worldOrigin, Vector2I direction)
        {
            ShowPreview(element, shape, worldOrigin, direction, CellSize);
        }

        public void ShowPreview(ElementType element, MagicShape shape, Vector2 worldOrigin, Vector2I direction, float cellSize)
        {
            ShowPreview(element, shape, worldOrigin, direction, cellSize, GetPlayerMagicRangeBonus());
        }

        public void ShowPreview(ElementType element, MagicShape shape, Vector2 worldOrigin, Vector2I direction, float cellSize, int rangeBonus)
        {
            ClearPreview();
            Color color = MagicShapePreview.GetElementColor(element);
            IReadOnlyList<Vector2I> cells = MagicShapePreview.Calculate(shape, Vector2I.Zero, direction, rangeBonus);
            float resolvedCellSize = Mathf.Max(1f, cellSize);

            foreach (Vector2I cell in cells)
            {
                ColorRect rect = new()
                {
                    Color = color,
                    Size = new Vector2(resolvedCellSize, resolvedCellSize),
                    Position = worldOrigin + new Vector2(cell.X * resolvedCellSize, cell.Y * resolvedCellSize) - new Vector2(resolvedCellSize * 0.5f, resolvedCellSize * 0.5f),
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };

                AddChild(rect);
                _previewNodes.Add(rect);
            }

            SceneTreeTimer timer = GetTree()?.CreateTimer(PreviewSeconds);
            if (timer != null)
            {
                timer.Timeout += ClearPreview;
            }
        }

        private static int GetPlayerMagicRangeBonus()
        {
            return GameManager.Instance?.GetPlayer()?.MagicRangeBonus ?? 0;
        }

        public void ClearPreview()
        {
            foreach (Node node in _previewNodes)
            {
                if (GodotObject.IsInstanceValid(node))
                {
                    node.QueueFree();
                }
            }

            _previewNodes.Clear();
        }
    }
}
