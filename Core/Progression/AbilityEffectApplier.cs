using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    public static class AbilityEffectApplier
    {
        public static void ApplyEffects(
            Player player,
            IEnumerable<AbilityEffectDefinition> effects,
            ISet<string> activeAbilityIds,
            ISet<string> passiveAbilityIds)
        {
            if (player == null || effects == null)
            {
                return;
            }

            foreach (AbilityEffectDefinition effect in effects)
            {
                ApplyEffect(player, effect, activeAbilityIds, passiveAbilityIds);
            }
        }

        public static string Describe(AbilityEffectDefinition effect)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            return effect.EffectType switch
            {
                AbilityEffectType.StatModifier => $"{FormatSigned(effect.Amount)} {FormatStat(effect.TargetStat)}",
                AbilityEffectType.UnlockActiveAbility => $"Unlock active ability: {effect.AbilityId}",
                AbilityEffectType.UnlockPassiveAbility => $"Unlock passive: {effect.PassiveId}",
                _ => effect.EffectId
            };
        }

        private static void ApplyEffect(
            Player player,
            AbilityEffectDefinition effect,
            ISet<string> activeAbilityIds,
            ISet<string> passiveAbilityIds)
        {
            if (effect == null)
            {
                return;
            }

            switch (effect.EffectType)
            {
                case AbilityEffectType.StatModifier:
                    ApplyStatModifier(player, effect.TargetStat, effect.Amount);
                    break;
                case AbilityEffectType.UnlockActiveAbility:
                    if (!string.IsNullOrWhiteSpace(effect.AbilityId))
                    {
                        activeAbilityIds?.Add(effect.AbilityId);
                    }
                    break;
                case AbilityEffectType.UnlockPassiveAbility:
                    if (!string.IsNullOrWhiteSpace(effect.PassiveId))
                    {
                        passiveAbilityIds?.Add(effect.PassiveId);
                    }
                    break;
            }
        }

        private static void ApplyStatModifier(IStats stats, AbilityTargetStat targetStat, int amount)
        {
            if (stats == null || amount == 0)
            {
                return;
            }

            switch (targetStat)
            {
                case AbilityTargetStat.MaxHealth:
                    stats.MaxHP = Mathf.Max(1, stats.MaxHP + amount);
                    if (amount > 0)
                    {
                        stats.CurHP = amount;
                    }
                    break;
                case AbilityTargetStat.MaxMana:
                    stats.MaxMana = Mathf.Max(0, stats.MaxMana + amount);
                    if (amount > 0)
                    {
                        stats.CurMana = amount;
                    }
                    break;
                case AbilityTargetStat.Strength:
                    stats.Strength = stats.Strength + amount;
                    break;
                case AbilityTargetStat.Dexterity:
                    stats.Dexterity = stats.Dexterity + amount;
                    break;
                case AbilityTargetStat.Intelligence:
                    stats.Intelligence = stats.Intelligence + amount;
                    break;
                case AbilityTargetStat.Spirit:
                    stats.Spirit = stats.Spirit + amount;
                    break;
                case AbilityTargetStat.Vitality:
                    stats.Vitality = stats.Vitality + amount;
                    break;
                case AbilityTargetStat.Luck:
                    stats.Luck = stats.Luck + amount;
                    break;
            }
        }

        private static string FormatSigned(int amount) => amount >= 0 ? $"+{amount}" : amount.ToString();

        private static string FormatStat(AbilityTargetStat stat)
        {
            return stat switch
            {
                AbilityTargetStat.MaxHealth => "Max Health",
                AbilityTargetStat.MaxMana => "Max Mana",
                _ => stat.ToString()
            };
        }
    }
}
