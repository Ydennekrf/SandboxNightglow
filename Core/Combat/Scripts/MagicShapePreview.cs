using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
    public static class MagicShapePreview
    {
        public static IReadOnlyList<Vector2I> Calculate(MagicShape shape, Vector2I origin, Vector2I direction)
        {
            return Calculate(shape, origin, direction, 0);
        }

        public static IReadOnlyList<Vector2I> Calculate(MagicShape shape, Vector2I origin, Vector2I direction, int rangeBonus)
        {
            Vector2I forward = NormalizeGridDirection(direction);
            List<Vector2I> cells = new();
            int bonus = Math.Max(0, rangeBonus);

            switch (shape)
            {
                case MagicShape.ProjectileBolt:
                    for (int i = 1; i <= 4 + bonus; i++)
                    {
                        cells.Add(origin + forward * i);
                    }
                    break;
                case MagicShape.Linear:
                    for (int i = 1; i <= 5 + bonus; i++)
                    {
                        cells.Add(origin + forward * i);
                    }
                    break;
                case MagicShape.Cone:
                    Vector2I side = new(-forward.Y, forward.X);
                    for (int distance = 1; distance <= 3 + bonus; distance++)
                    {
                        for (int width = -distance + 1; width <= distance - 1; width++)
                        {
                            cells.Add(origin + forward * distance + side * width);
                        }
                    }
                    break;
                case MagicShape.CircleWaveAwayFromPlayer:
                    int radius = 2 + bonus;
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            int distance = Math.Abs(x) + Math.Abs(y);
                            if (distance >= 1 && distance <= radius)
                            {
                                cells.Add(origin + new Vector2I(x, y));
                            }
                        }
                    }
                    break;
            }

            return cells;
        }

        public static Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Electricity => new Color(0.35f, 0.85f, 1f, 0.72f),
                ElementType.Ice => new Color(0.58f, 0.9f, 1f, 0.72f),
                ElementType.Fire => new Color(1f, 0.28f, 0.08f, 0.72f),
                ElementType.Acid => new Color(0.35f, 1f, 0.24f, 0.72f),
                ElementType.Darkness => new Color(0.45f, 0.18f, 0.72f, 0.72f),
                _ => new Color(1f, 1f, 1f, 0.65f)
            };
        }

        private static Vector2I NormalizeGridDirection(Vector2I direction)
        {
            if (direction == Vector2I.Zero)
            {
                return Vector2I.Right;
            }

            if (Math.Abs(direction.X) >= Math.Abs(direction.Y))
            {
                return new Vector2I(Math.Sign(direction.X), 0);
            }

            return new Vector2I(0, Math.Sign(direction.Y));
        }
    }
}
