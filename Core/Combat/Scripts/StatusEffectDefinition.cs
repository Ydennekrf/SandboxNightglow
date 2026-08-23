using System;
using System.Collections.Generic;

namespace ethra.V1
{
    /// <summary>
    /// Static status-effect definition used by CombatManager at runtime.
    /// </summary>
    /// <remarks>
    /// StatusEffectId is the stable authored key. Runtime stacks, timers, and source entities are stored by
    /// CombatManager; this class only describes behavior constants.
    /// </remarks>
    public sealed class StatusEffectDefinition
    {
        public string StatusEffectId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public float DurationSeconds { get; init; }
        public float TickIntervalSeconds { get; init; }
        public float DamagePerTick { get; init; }
        public float ManaDamagePerTick { get; init; }
        public float MovementSlowPercent { get; init; }
        public float AttackSlowPercent { get; init; }
        public float PhysicalDamageTakenMultiplier { get; init; } = 1f;
        public float KnockbackForce { get; init; }
        public float StunDuration { get; init; }
        public bool Silences { get; init; }
        public float BlindHitChancePenalty { get; init; }
        public float ThornsReflectPercent { get; init; }
        public StatusEffectStackBehavior StackBehavior { get; init; } = StatusEffectStackBehavior.RefreshDuration;
    }

    /// <summary>
    /// In-code catalog for early status effects and compatibility aliases.
    /// </summary>
    /// <remarks>
    /// Keep stable IDs in the "status.*" form and use aliases only for older/debug authoring strings.
    /// </remarks>
    public static class StatusEffectCatalog
    {
        public const string Knockback = "status.knockback";
        public const string Stun = "status.stun";
        public const string ArmorBreak = "status.armor_break";
        public const string Cold = "status.cold";
        public const string Burn = "status.burn";
        public const string Poison = "status.poison";
        public const string ManaBurn = "status.mana_burn";
        public const string Silence = "status.silence";
        public const string Blind = "status.blind";
        public const string Thorns = "status.thorns";

        private static readonly Dictionary<string, StatusEffectDefinition> Definitions = new(StringComparer.OrdinalIgnoreCase)
        {
            [Knockback] = new()
            {
                StatusEffectId = Knockback,
                DisplayName = "Knockback",
                Description = "Pushes the target away from the hit source.",
                DurationSeconds = 0.18f,
                KnockbackForce = 240f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Stun] = new()
            {
                StatusEffectId = Stun,
                DisplayName = "Stun",
                Description = "Prevents movement and actions briefly.",
                DurationSeconds = 1.0f,
                StunDuration = 1.0f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [ArmorBreak] = new()
            {
                StatusEffectId = ArmorBreak,
                DisplayName = "Armor Break",
                Description = "Increases physical damage taken.",
                DurationSeconds = 5.0f,
                PhysicalDamageTakenMultiplier = 1.25f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Cold] = new()
            {
                StatusEffectId = Cold,
                DisplayName = "Cold",
                Description = "Slows movement and marks attack speed slow for later integration.",
                DurationSeconds = 4.0f,
                MovementSlowPercent = 0.35f,
                AttackSlowPercent = 0.20f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Burn] = new()
            {
                StatusEffectId = Burn,
                DisplayName = "Burn",
                Description = "Deals damage over time and increases physical vulnerability.",
                DurationSeconds = 4.0f,
                TickIntervalSeconds = 1.0f,
                DamagePerTick = 2.0f,
                PhysicalDamageTakenMultiplier = 1.15f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Poison] = new()
            {
                StatusEffectId = Poison,
                DisplayName = "Poison",
                Description = "Deals damage over time.",
                DurationSeconds = 6.0f,
                TickIntervalSeconds = 1.0f,
                DamagePerTick = 2.0f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [ManaBurn] = new()
            {
                StatusEffectId = ManaBurn,
                DisplayName = "Mana Burn",
                Description = "Deals mana damage over time when the target has mana.",
                DurationSeconds = 5.0f,
                TickIntervalSeconds = 1.0f,
                ManaDamagePerTick = 3.0f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Silence] = new()
            {
                StatusEffectId = Silence,
                DisplayName = "Silence",
                Description = "Prevents magic casting while active.",
                DurationSeconds = 4.0f,
                Silences = true,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Blind] = new()
            {
                StatusEffectId = Blind,
                DisplayName = "Blind",
                Description = "Reduces hit chance while active.",
                DurationSeconds = 5.0f,
                BlindHitChancePenalty = 0.35f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            },
            [Thorns] = new()
            {
                StatusEffectId = Thorns,
                DisplayName = "Thorns",
                Description = "Reflects a portion of physical damage back to attackers.",
                DurationSeconds = 8.0f,
                ThornsReflectPercent = 0.30f,
                StackBehavior = StatusEffectStackBehavior.RefreshDuration
            }
        };

        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Knockback"] = Knockback,
            ["Stun"] = Stun,
            ["ArmorBreak"] = ArmorBreak,
            ["Armor Break"] = ArmorBreak,
            ["Cold"] = Cold,
            ["Slow"] = Cold,
            ["Burn"] = Burn,
            ["Poison"] = Poison,
            ["ManaBurn"] = ManaBurn,
            ["Mana Burn"] = ManaBurn,
            ["Silence"] = Silence,
            ["Blind"] = Blind,
            ["Thorns"] = Thorns
        };

        /// <summary>
        /// All known status definitions for debug UI and validation.
        /// </summary>
        public static IReadOnlyCollection<StatusEffectDefinition> All => Definitions.Values;

        /// <summary>
        /// Resolves a status ID or alias into a concrete definition.
        /// </summary>
        public static bool TryGet(string statusId, out StatusEffectDefinition definition)
        {
            definition = null;
            string stableId = NormalizeId(statusId);
            return !string.IsNullOrWhiteSpace(stableId) && Definitions.TryGetValue(stableId, out definition);
        }

        /// <summary>
        /// Converts aliases such as "Cold" or "Slow" into stable status IDs when known.
        /// </summary>
        public static string NormalizeId(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return string.Empty;
            }

            string trimmed = statusId.Trim();
            if (Definitions.ContainsKey(trimmed))
            {
                return trimmed;
            }

            return Aliases.TryGetValue(trimmed, out string stableId) ? stableId : trimmed;
        }
    }
}
