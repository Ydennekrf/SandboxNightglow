using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    /// <summary>
    /// Result object returned by rune socketing attempts for direct UI feedback.
    /// </summary>
    public class WeaponSocketingResult
    {
        public bool Success { get; }
        public string Message { get; }

        public WeaponSocketingResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    /// <summary>
    /// Applies rune socketing rules to weapon instances held by InventoryManager.
    /// </summary>
    /// <remarks>
    /// This manager owns validation for upgrade and elemental slots. It does not own inventory storage;
    /// successful socketing mutates WeaponInstanceState through InventoryManager.
    /// </remarks>
    public class WeaponUpgradeManager
    {
        private readonly InventoryManager _inventory;
        private readonly MasterRepository _db;

        public WeaponUpgradeManager(InventoryManager inventory, MasterRepository db)
        {
            _inventory = inventory;
            _db = db;
        }

        /// <summary>
        /// Returns weapon instances that can be displayed or selected by upgrade UI.
        /// </summary>
        public IReadOnlyList<WeaponInstanceState> GetWeapons()
        {
            return _inventory?.GetWeaponInstances() ?? Array.Empty<WeaponInstanceState>();
        }

        public IReadOnlyList<RuneItem> GetAvailableUpgradeRunes() => GetAvailableRunes(RuneKind.Upgrade);

        public IReadOnlyList<RuneItem> GetAvailableElementalRunes() => GetAvailableRunes(RuneKind.Elemental);

        /// <summary>
        /// Attempts to consume and socket an upgrade rune into the selected weapon instance.
        /// </summary>
        public WeaponSocketingResult SocketUpgradeRune(string weaponInstanceId, int runeItemId)
        {
            WeaponInstanceState instance = _inventory?.GetWeaponInstance(weaponInstanceId);
            if (instance == null)
            {
                return new WeaponSocketingResult(false, "Select a weapon first.");
            }

            if (_db.GetItemFromRepo(instance.ItemId) is not WeaponItem weapon)
            {
                return new WeaponSocketingResult(false, "Selected item is not a weapon.");
            }

            if (weapon.UpgradeRuneSlotCount <= 0)
            {
                return new WeaponSocketingResult(false, "This weapon has no rune slots.");
            }

            if (instance.UpgradeRuneItemIds.Count >= weapon.UpgradeRuneSlotCount)
            {
                return new WeaponSocketingResult(false, "All upgrade rune slots are occupied.");
            }

            if (_db.GetItemFromRepo(runeItemId) is not RuneItem rune || !rune.IsUpgradeRune)
            {
                return new WeaponSocketingResult(false, "That rune cannot go in an upgrade slot.");
            }

            if (!_inventory.RemoveItemQuantity(runeItemId, 1))
            {
                return new WeaponSocketingResult(false, "Rune is no longer available.");
            }

            _inventory.TryMutateWeaponInstance(weaponInstanceId, state => state.UpgradeRuneItemIds.Add(runeItemId));
            return new WeaponSocketingResult(true, $"Socketed {rune.Name}.");
        }

        /// <summary>
        /// Attempts to consume and socket an elemental rune into the selected weapon instance.
        /// </summary>
        public WeaponSocketingResult SocketElementalRune(string weaponInstanceId, int runeItemId)
        {
            WeaponInstanceState instance = _inventory?.GetWeaponInstance(weaponInstanceId);
            if (instance == null)
            {
                return new WeaponSocketingResult(false, "Select a weapon first.");
            }

            if (_db.GetItemFromRepo(instance.ItemId) is not WeaponItem weapon)
            {
                return new WeaponSocketingResult(false, "Selected item is not a weapon.");
            }

            if (!weapon.HasElementalRuneSlot)
            {
                return new WeaponSocketingResult(false, "This weapon has no elemental slot.");
            }

            if (instance.ElementalRuneItemId.HasValue)
            {
                return new WeaponSocketingResult(false, "The elemental slot is already occupied.");
            }

            if (_db.GetItemFromRepo(runeItemId) is not RuneItem rune || !rune.IsElementalRune)
            {
                return new WeaponSocketingResult(false, "That rune cannot go in an elemental slot.");
            }

            if (!_inventory.RemoveItemQuantity(runeItemId, 1))
            {
                return new WeaponSocketingResult(false, "Rune is no longer available.");
            }

            _inventory.TryMutateWeaponInstance(weaponInstanceId, state => state.ElementalRuneItemId = runeItemId);
            return new WeaponSocketingResult(true, $"Socketed {rune.Name}.");
        }

        /// <summary>
        /// Builds human-readable socket modifier text for the selected weapon instance.
        /// </summary>
        public string BuildModifierSummary(WeaponInstanceState instance)
        {
            if (instance == null)
            {
                return "Select a weapon.";
            }

            List<string> lines = new();
            foreach (int runeId in instance.UpgradeRuneItemIds)
            {
                InventoryItem item = _db.GetItemFromRepo(runeId);
                if (item == null)
                {
                    continue;
                }

                string effectSummary = item.Effects.Count > 0
                    ? string.Join(", ", item.Effects.Select(effect => effect.EffectName))
                    : item.Description;
                lines.Add($"{item.Name}: {effectSummary}");
            }

            if (instance.ElementalRuneItemId.HasValue && _db.GetItemFromRepo(instance.ElementalRuneItemId.Value) is RuneItem elemental)
            {
                lines.Add($"{elemental.Name}: {elemental.Element} {elemental.Shape}");
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "No socketed modifiers.";
        }

        /// <summary>
        /// Resolves the elemental rune item currently socketed into a weapon instance, when present.
        /// </summary>
        public RuneItem GetElementalRune(WeaponInstanceState instance)
        {
            if (instance?.ElementalRuneItemId == null)
            {
                return null;
            }

            return _db.GetItemFromRepo(instance.ElementalRuneItemId.Value) as RuneItem;
        }

        private IReadOnlyList<RuneItem> GetAvailableRunes(RuneKind kind)
        {
            if (_inventory == null || _db == null)
            {
                return Array.Empty<RuneItem>();
            }

            return _inventory.GetItemCounts()
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => _db.GetItemFromRepo(kvp.Key) as RuneItem)
                .Where(rune => rune != null && rune.RuneKind == kind)
                .OrderBy(rune => rune.Name)
                .ToList();
        }
    }
}
