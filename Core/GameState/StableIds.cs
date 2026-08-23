using System;

namespace ethra.V1
{
    /// <summary>
    /// Shared stable ID prefixes and lightweight validation for authored persistent IDs.
    /// These IDs are save-facing identity keys, not display names, node names, or resource paths.
    /// </summary>
    public static class StableIds
    {
        public const string Item = "item.";
        public const string Consumable = "item.consumable.";
        public const string Weapon = "item.weapon.";
        public const string Armor = "item.armor.";
        public const string Material = "item.material.";
        public const string Quest = "quest.";
        public const string MainQuest = "quest.main.";
        public const string SideQuest = "quest.side.";
        public const string QuestObjective = "objective.";
        public const string Npc = "npc.";
        public const string Enemy = "enemy.";
        public const string Area = "area.";
        public const string Scene = "scene.";
        public const string Spawn = "spawn.";
        public const string Chest = "chest.";
        public const string Door = "door.";
        public const string Puzzle = "puzzle.";
        public const string Cutscene = "cutscene.";
        public const string DialogTree = "dialog.";
        public const string DialogNode = "node.";
        public const string Shop = "shop.";
        public const string LootTable = "loot.";
        public const string Recipe = "recipe.";
        public const string Skill = "skill.";
        public const string StatusEffect = "status.";
        public const string WorldFlag = "flag.";

        public static bool IsValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            string trimmed = id.Trim();
            if (!string.Equals(trimmed, id, StringComparison.Ordinal))
            {
                return false;
            }

            foreach (char c in trimmed)
            {
                bool allowed = c is >= 'a' and <= 'z'
                    || c is >= '0' and <= '9'
                    || c == '.'
                    || c == '_'
                    || c == '-';

                if (!allowed)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasPrefix(string id, string prefix)
        {
            return IsValidId(id)
                && !string.IsNullOrWhiteSpace(prefix)
                && id.StartsWith(prefix, StringComparison.Ordinal);
        }
    }
}
