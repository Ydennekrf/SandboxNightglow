using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    /// <summary>
    /// Runtime state for one owned weapon copy, including socketed rune item IDs.
    /// </summary>
    /// <remarks>
    /// Static weapon data lives on WeaponItem. WeaponInstanceState is serialized through WeaponInstanceSave
    /// so duplicate weapons can hold different rune sockets.
    /// </remarks>
    public class WeaponInstanceState
    {
        /// <summary>Stable save/runtime ID for this owned weapon copy.</summary>
        public string InstanceId { get; set; } = string.Empty;
        /// <summary>Static item ID for the weapon definition.</summary>
        public int ItemId { get; set; }
        /// <summary>Socketed upgrade rune item IDs in slot order.</summary>
        public List<int> UpgradeRuneItemIds { get; set; } = new();
        /// <summary>Optional socketed elemental rune item ID.</summary>
        public int? ElementalRuneItemId { get; set; }

        /// <summary>
        /// Captures a serializable copy for inventory saves.
        /// </summary>
        public WeaponInstanceSave CaptureSnapshot()
        {
            return new WeaponInstanceSave
            {
                InstanceId = InstanceId,
                ItemId = ItemId,
                UpgradeRuneItemIds = UpgradeRuneItemIds.Where(id => id > 0).ToList(),
                ElementalRuneItemId = ElementalRuneItemId
            };
        }

        /// <summary>
        /// Restores runtime state from a serialized weapon instance save.
        /// </summary>
        public static WeaponInstanceState FromSave(WeaponInstanceSave save)
        {
            return new WeaponInstanceState
            {
                InstanceId = save?.InstanceId ?? string.Empty,
                ItemId = save?.ItemId ?? 0,
                UpgradeRuneItemIds = save?.UpgradeRuneItemIds?.Where(id => id > 0).ToList() ?? new List<int>(),
                ElementalRuneItemId = save?.ElementalRuneItemId
            };
        }
    }
}
