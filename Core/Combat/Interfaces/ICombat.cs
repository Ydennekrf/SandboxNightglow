
using System.Collections.Generic;

namespace ethra.V1
{
	public interface ICombat
	{


		/// <summary>
		/// Resolves an attack (hit/crit/mitigation) and returns the final damage value.
		/// Returns true if the attack hits; false if it misses/whiffs.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="target"></param>
		/// <param name="abilityId"></param>
		/// <param name="finalDamage"></param>
		/// <param name="isCritical"></param>
		/// <returns>float finaldamage, bool isCrit</returns>
		bool TryResolveAttack(Entity attacker, Entity target, string abilityId, out float finalDamage, out bool isCritical);

		/// <summary>
		///Applies damage directly to a target (bypassing resolution).
		/// </summary>
		/// <param name="target"></param>
		/// <param name="amount"></param>
		/// <param name="damageType"></param>
		/// <param name="source"></param>
		/// <param name="tags"></param>
		void DealDamage(Entity target, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null);

		/// <summary>
		/// Applies healing directly to a target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="amount"></param>
		/// <param name="source"></param>
		/// <param name="tags"></param>
		void Heal(Entity target, float amount, Entity source = null, IEnumerable<string> tags = null);

		// --- Status / effects ---
		/// <summary>
		/// Applies a status effect (buff/debuff/DoT/HoT) to a target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="statusId"></param>
		/// <param name="stacks"></param>
		/// <param name="durationSeconds"></param>
		/// <param name="source"></param>
		void ApplyStatus(Entity target, string statusId, int stacks = 1, float? durationSeconds = null, Entity source = null);

		/// <summary>
		///  Removes a status effect from a target. 'stacks' can be int.MaxValue to remove all.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="statusId"></param>
		/// <param name="stacks"></param>
		void RemoveStatus(Entity target, string statusId, int stacks = int.MaxValue);

		// --- Queries / previews ---
		/// <summary>
		/// Quick check before committing an attack (range, LoS, resource costs, immunity, etc.).
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="target"></param>
		/// <param name="abilityId"></param>
		/// <returns></returns>
		bool CanHit(Entity attacker, Entity target, string abilityId);

		/// <summary>
		/// Returns a best-effort preview of damage for UI/AI (no side effects).
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="target"></param>
		/// <param name="abilityId"></param>
		/// <returns></returns>
		float PreviewDamage(Entity attacker, Entity target, string abilityId);

		// --- Group helpers (optional but handy) ---
		/// <summary>
		/// Applies the same damage packet to multiple targets (e.g., AoE).
		/// </summary>
		/// <param name="targets"></param>
		/// <param name="amount"></param>
		/// <param name="damageType"></param>
		/// <param name="source"></param>
		/// <param name="tags"></param>
		/// <param name="statusIds"></param>
		/// 
		void DealAreaDamage(IEnumerable<Entity> targets, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null, IEnumerable<string> statusIds = null);
	}
}
