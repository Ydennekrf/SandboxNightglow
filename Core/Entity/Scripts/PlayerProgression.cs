using System;
using Godot;

namespace ethra.V1
{
    public readonly record struct PlayerExperienceChangedEvent(int CurrentExperience, int ExperienceToNextLevel, int TotalExperience);
    public readonly record struct PlayerLevelChangedEvent(int Level);
    public readonly record struct PlayerLevelUpEvent(int PreviousLevel, int NewLevel, int AbilityPoints);
    public readonly record struct PlayerAbilityPointsChangedEvent(int AbilityPoints);

    public sealed class PlayerProgression
    {
        public event Action<PlayerExperienceChangedEvent> ExperienceChanged;
        public event Action<PlayerLevelChangedEvent> LevelChanged;
        public event Action<PlayerLevelUpEvent> LeveledUp;

        public int Level { get; private set; } = 1;
        public int CurrentExperience { get; private set; }
        public int TotalExperience { get; private set; }
        public int AbilityPoints { get; private set; }
        public int SkillPoints => AbilityPoints;
        public int ExperienceToNextLevel => ExperienceCurve.GetExperienceToNextLevel(Level);

        public void GainExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentExperience += amount;
            TotalExperience += amount;
            GD.Print($"[Progression] Gained XP: {amount}");

            while (CurrentExperience >= ExperienceToNextLevel)
            {
                int previousLevel = Level;
                CurrentExperience -= ExperienceToNextLevel;
                Level++;
                int pointsGranted = ExperienceCurve.GetAbilityPointsGrantedOnLevelUp(Level);
                AbilityPoints += pointsGranted;

                GD.Print($"[Progression] Level up! New level: {Level}. Ability points +{pointsGranted} (total {AbilityPoints}).");
                LevelChanged?.Invoke(new PlayerLevelChangedEvent(Level));
                AbilityPointsChanged?.Invoke(new PlayerAbilityPointsChangedEvent(AbilityPoints));
                LeveledUp?.Invoke(new PlayerLevelUpEvent(previousLevel, Level, AbilityPoints));
            }

            GD.Print($"[Progression] XP: {CurrentExperience} / {ExperienceToNextLevel}");
            ExperienceChanged?.Invoke(new PlayerExperienceChangedEvent(CurrentExperience, ExperienceToNextLevel, TotalExperience));
        }

        public void RestoreSnapshot(int level, int currentExperience, int totalExperience, int skillPoints)
        {
            Level = Mathf.Max(1, level);
            CurrentExperience = Mathf.Max(0, currentExperience);
            TotalExperience = Mathf.Max(CurrentExperience, totalExperience);
            AbilityPoints = Mathf.Max(0, skillPoints);

            while (CurrentExperience >= ExperienceToNextLevel)
            {
                CurrentExperience -= ExperienceToNextLevel;
                Level++;
            }

            LevelChanged?.Invoke(new PlayerLevelChangedEvent(Level));
            AbilityPointsChanged?.Invoke(new PlayerAbilityPointsChangedEvent(AbilityPoints));
            ExperienceChanged?.Invoke(new PlayerExperienceChangedEvent(CurrentExperience, ExperienceToNextLevel, TotalExperience));
        }

        public bool TrySpendAbilityPoints(int cost)
        {
            int safeCost = Mathf.Max(0, cost);
            if (AbilityPoints < safeCost)
            {
                return false;
            }

            AbilityPoints -= safeCost;
            AbilityPointsChanged?.Invoke(new PlayerAbilityPointsChangedEvent(AbilityPoints));
            return true;
        }

        public void Refresh()
        {
            LevelChanged?.Invoke(new PlayerLevelChangedEvent(Level));
            AbilityPointsChanged?.Invoke(new PlayerAbilityPointsChangedEvent(AbilityPoints));
            ExperienceChanged?.Invoke(new PlayerExperienceChangedEvent(CurrentExperience, ExperienceToNextLevel, TotalExperience));
        }

        public event Action<PlayerAbilityPointsChangedEvent> AbilityPointsChanged;
    }
}
