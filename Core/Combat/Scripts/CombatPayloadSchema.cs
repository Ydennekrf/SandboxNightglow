using System;
using System.Collections.Generic;

namespace ethra.V1
{
    /// <summary>
    /// Shared schema ids used by combo payload authoring + combat runtime.
    /// Keep these ids stable to avoid breaking authored resources.
    /// </summary>
    public static class CombatPayloadSchema
    {
        public static readonly IReadOnlyList<string> KnownDeliveryShapeIds = new[]
        {
            "SingleTarget",
            "Cone",
            "Linear"
        };

        public static readonly IReadOnlyList<string> KnownEffectIds = new[]
        {
            "Knockback",
            "Slow",
            "Stun",
            "Root"
        };

        public static bool IsKnownDeliveryShape(string shapeId)
        {
            if (string.IsNullOrWhiteSpace(shapeId))
            {
                return true;
            }

            return ContainsIgnoreCase(KnownDeliveryShapeIds, shapeId);
        }

        public static bool IsKnownEffectId(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                return true;
            }

            return ContainsIgnoreCase(KnownEffectIds, effectId);
        }

        private static bool ContainsIgnoreCase(IReadOnlyList<string> source, string value)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (string.Equals(source[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
