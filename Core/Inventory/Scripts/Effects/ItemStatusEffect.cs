using System;
using Godot;

namespace ethra.V1
{
    public enum ItemStatusTrigger
    {
        Unknown,
        OnEquip,
        OnUse,
        OnHit
    }

    /// <summary>
    /// Item-authored bridge into CombatManager status effects.
    /// </summary>
    /// <remarks>
    /// Equip/use triggers apply to the item owner. On-hit triggers are collected by attack payload assembly
    /// and apply to the hit target through CombatManager's existing payload status path.
    /// </remarks>
    public class ItemStatusEffect : ItemEffects, IEffect
    {
        private const float DefaultChance = 1f;

        public string StatusId { get; }
        public ItemStatusTrigger Trigger { get; }
        public float Chance { get; }
        public float DurationSeconds { get; }
        public int Stacks { get; }

        public bool IsOnHit => Trigger == ItemStatusTrigger.OnHit;

        public ItemStatusEffect(
            string statusId,
            ItemStatusTrigger trigger,
            float chance = DefaultChance,
            float durationSeconds = 0f,
            int stacks = 1,
            Entity owner = null)
            : base(StatusEffectCatalog.NormalizeId(statusId), Math.Max(1, stacks), owner)
        {
            StatusId = StatusEffectCatalog.NormalizeId(statusId);
            Trigger = trigger;
            Chance = Mathf.Clamp(chance <= 0f ? DefaultChance : chance, 0f, 1f);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Stacks = Math.Max(1, stacks);
            EffectName = StatusId;
        }

        public void ResolveItemEffect()
        {
            if (Trigger == ItemStatusTrigger.OnHit)
            {
                return;
            }

            if (!ShouldApply())
            {
                return;
            }

            ApplyToOwner();
        }

        public void RemoveItemEffect()
        {
            if (Trigger != ItemStatusTrigger.OnEquip || Owner == null || string.IsNullOrWhiteSpace(StatusId))
            {
                return;
            }

            GameManager.Instance?.RemoveStatus(Owner, StatusId, Stacks);
        }

        public bool ShouldApply()
        {
            return Chance >= 1f || GD.Randf() <= Chance;
        }

        public bool IsKnownStatus()
        {
            return StatusEffectCatalog.TryGet(StatusId, out _);
        }

        public bool DebugValidateNoOwnerCall()
        {
            try
            {
                ResolveItemEffect();
                RemoveItemEffect();
                return IsKnownStatus();
            }
            catch (Exception ex)
            {
                GD.PushError($"ItemStatusEffect debug validation failed for '{StatusId}': {ex.Message}");
                return false;
            }
        }

        public override string ToString()
        {
            string trigger = Trigger.ToString();
            string duration = DurationSeconds > 0f ? $" {DurationSeconds:0.##}s" : string.Empty;
            string chance = Chance < 1f ? $" {Chance:0.##} chance" : string.Empty;
            return $"{StatusId} {trigger} x{Stacks}{duration}{chance}";
        }

        public static ItemStatusTrigger ParseTrigger(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ItemStatusTrigger.Unknown;
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "on_equip" => ItemStatusTrigger.OnEquip,
                "onequip" => ItemStatusTrigger.OnEquip,
                "equip" => ItemStatusTrigger.OnEquip,
                "on_use" => ItemStatusTrigger.OnUse,
                "onuse" => ItemStatusTrigger.OnUse,
                "use" => ItemStatusTrigger.OnUse,
                "on_hit" => ItemStatusTrigger.OnHit,
                "onhit" => ItemStatusTrigger.OnHit,
                "hit" => ItemStatusTrigger.OnHit,
                _ => ItemStatusTrigger.Unknown
            };
        }

        private void ApplyToOwner()
        {
            if (Owner == null || string.IsNullOrWhiteSpace(StatusId))
            {
                return;
            }

            float? duration = DurationSeconds > 0f ? DurationSeconds : null;
            GameManager.Instance?.ApplyStatus(Owner, StatusId, Stacks, duration, Owner);
        }
    }
}
