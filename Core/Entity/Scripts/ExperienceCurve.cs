using Godot;

namespace ethra.V1
{
    public static class ExperienceCurve
    {
        private const int BaseRequirement = 50;
        private const int RequirementPerLevel = 25;

        // Linear first-pass curve: level 1 requires 50 XP, then +25 XP per level.
        public static int GetExperienceToNextLevel(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            return BaseRequirement + ((safeLevel - 1) * RequirementPerLevel);
        }

        // Integer division keeps rewards readable: levels 2-4 grant 1, 5-9 grant 2, 10-14 grant 3.
        public static int GetAbilityPointsGrantedOnLevelUp(int newLevel)
        {
            int safeLevel = Mathf.Max(2, newLevel);
            return 1 + (safeLevel / 5);
        }
    }
}
