using System;
using System.Collections.Generic;

namespace ethra.V1
{
    public enum RuneKind
    {
        Unknown,
        Upgrade,
        Elemental
    }

    public enum ElementType
    {
        None,
        Electricity,
        Ice,
        Fire,
        Acid,
        Darkness
    }

    public enum MagicShape
    {
        None,
        ProjectileBolt,
        Linear,
        Cone,
        CircleWaveAwayFromPlayer
    }

    public class RuneItem : InventoryItem
    {
        public RuneKind RuneKind { get; }
        public ElementType Element { get; }
        public MagicShape Shape { get; }

        public bool IsUpgradeRune => RuneKind == RuneKind.Upgrade;
        public bool IsElementalRune => RuneKind == RuneKind.Elemental;

        public RuneItem(
            int id,
            string name,
            int value,
            string description,
            string rarity,
            string subtype,
            int maxStack,
            List<ItemEffects> effects = null,
            string iconPath = "",
            string runeKind = "",
            string runeElement = "",
            string magicShape = "")
            : base(id, name, value, description, rarity, effects, category: "Rune", subtype: subtype, maxStack: maxStack, iconPath: iconPath)
        {
            RuneKind = ParseEnum(runeKind, RuneKind.Unknown);
            Element = ParseEnum(runeElement, ElementType.None);
            Shape = ParseEnum(magicShape, MagicShape.None);
        }

        private static T ParseEnum<T>(string raw, T fallback) where T : struct, Enum
        {
            return !string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out T parsed)
                ? parsed
                : fallback;
        }
    }
}
