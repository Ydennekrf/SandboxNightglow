using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public partial class CombatManager : ISaveable, ICombat, IResolveable
    {
        private sealed class StatusRuntime
        {
            public string Id { get; init; } = string.Empty;
            public int Stacks { get; set; }
            public float RemainingSeconds { get; set; }
            public Entity Source { get; set; }
        }

        private const bool DebugCombatFlow = true;
        private const float CritMultiplier = 1.5f;

        private readonly string _saveKey = "Combat";
        private readonly int _resolveOrder = 20;
        private readonly Queue<AttackPayloadPacket> _payloadQueue = new();
        private readonly Dictionary<string, Func<AttackPayloadPacket, IReadOnlyList<Entity>>> _deliveryHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Action<Entity, AttackPayloadPacket, string>> _effectHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Entity, Dictionary<string, StatusRuntime>> _activeStatuses = new();
        private readonly RandomNumberGenerator _rng = new();

        public string SaveKey => _saveKey;
        public int ResolveOrder => _resolveOrder;

        public CombatManager()
        {
            _rng.Randomize();
            RegisterDefaultHandlers();
        }

        public void QueueAttackPayload(AttackPayloadPacket packet)
        {
            if (packet?.Payload == null)
            {
                return;
            }

            _payloadQueue.Enqueue(packet);
            Log($"QueueAttackPayload: queued source={packet.Source?.Name} phase={packet.ComboPhase} shape={packet.Payload.DeliveryShapeId} queueCount={_payloadQueue.Count}");
        }

        public void ApplyStatus(Entity target, string statusId, int stacks = 1, float? durationSeconds = null, Entity source = null)
        {
            if (target == null || string.IsNullOrWhiteSpace(statusId) || stacks <= 0)
            {
                return;
            }

            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                statusMap = new Dictionary<string, StatusRuntime>(StringComparer.OrdinalIgnoreCase);
                _activeStatuses[target] = statusMap;
            }

            if (!statusMap.TryGetValue(statusId, out StatusRuntime runtime))
            {
                runtime = new StatusRuntime
                {
                    Id = statusId,
                    Stacks = 0,
                    RemainingSeconds = 0f,
                    Source = source
                };
                statusMap[statusId] = runtime;
            }

            runtime.Stacks += stacks;
            runtime.Source = source;
            if (durationSeconds.HasValue)
            {
                runtime.RemainingSeconds = Mathf.Max(runtime.RemainingSeconds, durationSeconds.Value);
            }

            Log($"ApplyStatus: target={target.Name} id={statusId} stacks={runtime.Stacks} remaining={runtime.RemainingSeconds:0.###}");
        }

        public bool CanHit(Entity attacker, Entity target, string abilityId)
        {
            if (attacker == null || target == null)
            {
                return false;
            }

            if (target is IStats targetStats && targetStats.CurHP <= 0)
            {
                return false;
            }

            return true;
        }

        public bool TryResolveAttack(Entity attacker, Entity target, string abilityId, out float finalDamage, out bool isCritical)
        {
            finalDamage = 0f;
            isCritical = false;

            if (!CanHit(attacker, target, abilityId))
            {
                return false;
            }

            AttackPayloadResource payload = BuildAdHocPayload(abilityId);
            finalDamage = ComputeDamage(attacker, target, payload, out isCritical);
            return finalDamage > 0f;
        }

        public void DealAreaDamage(IEnumerable<Entity> targets, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null, IEnumerable<string> statusIds = null)
        {
            if (targets == null)
            {
                return;
            }

            foreach (Entity target in targets)
            {
                DealDamage(target, amount, damageType, source, tags);

                if (statusIds == null)
                {
                    continue;
                }

                foreach (string status in statusIds)
                {
                    ApplyStatus(target, status, 1, null, source);
                }
            }
        }

        public void DealDamage(Entity target, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null)
        {
            if (target is not IStats stats)
            {
                return;
            }

            int delta = -Mathf.Max(1, Mathf.RoundToInt(amount));
            stats.CurHP = delta;
            Log($"DealDamage: target={target.Name} amount={-delta} type={damageType} hpNow={stats.CurHP}");
        }

        public void Heal(Entity target, float amount, Entity source = null, IEnumerable<string> tags = null)
        {
            if (target is not IStats stats)
            {
                return;
            }

            int delta = Mathf.Max(1, Mathf.RoundToInt(amount));
            stats.CurHP = delta;
            Log($"Heal: target={target.Name} amount={delta} hpNow={stats.CurHP}");
        }

        public float PreviewDamage(Entity attacker, Entity target, string abilityId)
        {
            AttackPayloadResource payload = BuildAdHocPayload(abilityId);
            return ComputeExpectedDamage(attacker, target, payload);
        }

        public void RemoveStatus(Entity target, string statusId, int stacks = int.MaxValue)
        {
            if (target == null || string.IsNullOrWhiteSpace(statusId))
            {
                return;
            }

            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                return;
            }

            if (!statusMap.TryGetValue(statusId, out StatusRuntime runtime))
            {
                return;
            }

            if (stacks >= runtime.Stacks)
            {
                statusMap.Remove(statusId);
                Log($"RemoveStatus: target={target.Name} id={statusId} removed=all");
            }
            else
            {
                runtime.Stacks -= stacks;
                Log($"RemoveStatus: target={target.Name} id={statusId} removed={stacks} remainingStacks={runtime.Stacks}");
            }

            if (statusMap.Count == 0)
            {
                _activeStatuses.Remove(target);
            }
        }

        public void Resolve()
        {
            while (_payloadQueue.Count > 0)
            {
                AttackPayloadPacket packet = _payloadQueue.Dequeue();
                Log($"Resolve: dequeued source={packet.Source?.Name} phase={packet.ComboPhase} remaining={_payloadQueue.Count}");
                ExecuteAttackPayload(packet);
            }
        }

        public void Resolve(object obj)
        {
            if (obj is double d)
            {
                TickStatuses((float)d);
                return;
            }

            if (obj is float f)
            {
                TickStatuses(f);
            }
        }

        public void RestoreSnapshot(object snapshot)
        {
            throw new NotImplementedException();
        }

        public object CaptureSnapshot()
        {
            throw new NotImplementedException();
        }

        private void ExecuteAttackPayload(AttackPayloadPacket packet)
        {
            AttackPayloadResource payload = packet.Payload;
            string shapeId = string.IsNullOrWhiteSpace(payload.DeliveryShapeId) ? "SingleTarget" : payload.DeliveryShapeId;
            Log($"ExecuteAttackPayload: source={packet.Source?.Name} phase={packet.ComboPhase} anim='{packet.AnimationName}' origin={packet.OriginPosition} forward={packet.ForwardDirection} shape={shapeId} damageType={payload.DamageType} element={payload.ElementType}");

            IReadOnlyList<Entity> targets = _deliveryHandlers.TryGetValue(shapeId, out Func<AttackPayloadPacket, IReadOnlyList<Entity>> deliveryHandler)
                ? deliveryHandler(packet)
                : Array.Empty<Entity>();

            if (targets.Count == 0)
            {
                Log($"ExecuteAttackPayload: no targets resolved for shape='{shapeId}'.");
            }

            foreach (Entity target in targets)
            {
                float damage = ComputeDamage(packet.Source, target, payload, out bool crit);
                if (damage > 0f)
                {
                    DealDamage(target, damage, payload.DamageType, packet.Source);
                }

                Log($"ExecuteAttackPayload: target={target.Name} damage={damage:0.###} crit={crit}");
                CombatFeedbackBus.EmitHitResolved(packet.Source, target, damage, crit, payload.DamageType, payload.ElementType);

                if (payload.EffectIds == null)
                {
                    continue;
                }

                foreach (string effectId in payload.EffectIds)
                {
                    if (string.IsNullOrWhiteSpace(effectId))
                    {
                        continue;
                    }

                    if (_effectHandlers.TryGetValue(effectId, out Action<Entity, AttackPayloadPacket, string> effectHandler))
                    {
                        Log($"ExecuteAttackPayload: applying effect='{effectId}' duration={payload.EffectDurationSeconds:0.###}");
                        effectHandler(target, packet, effectId);
                        CombatFeedbackBus.EmitEffectApplied(target, effectId, payload.EffectDurationSeconds);
                    }
                    else
                    {
                        GD.PushWarning($"CombatManager: unknown effect id '{effectId}' for payload.");
                    }
                }
            }
        }

        private void RegisterDefaultHandlers()
        {
            Log("RegisterDefaultHandlers: registering default delivery/effect handlers.");

            _deliveryHandlers["SingleTarget"] = packet =>
            {
                List<Entity> targets = GetEnemyTargets();
                if (targets.Count > 1)
                {
                    targets = new List<Entity> { targets[0] };
                }
                return targets;
            };

            _deliveryHandlers["Cone"] = packet => GetEnemyTargets();
            _deliveryHandlers["Linear"] = packet => GetEnemyTargets();

            _effectHandlers["Knockback"] = (target, packet, effectId) =>
                ApplyStatus(target, "Knockback", 1, packet.Payload.EffectDurationSeconds, packet.Source);

            _effectHandlers["Slow"] = (target, packet, effectId) =>
                ApplyStatus(target, "Slow", 1, packet.Payload.EffectDurationSeconds, packet.Source);

            _effectHandlers["Stun"] = (target, packet, effectId) =>
                ApplyStatus(target, "Stun", 1, packet.Payload.EffectDurationSeconds, packet.Source);

            _effectHandlers["Root"] = (target, packet, effectId) =>
                ApplyStatus(target, "Root", 1, packet.Payload.EffectDurationSeconds, packet.Source);
        }

        private List<Entity> GetEnemyTargets()
        {
            GameManager gm = GameManager.Instance;
            if (gm?.registeredEnemies == null)
            {
                return new List<Entity>();
            }

            return gm.registeredEnemies
                .Where(enemy => enemy != null)
                .Cast<Entity>()
                .ToList();
        }

        private float ComputeDamage(Entity attacker, Entity target, AttackPayloadResource payload, out bool isCritical)
        {
            isCritical = false;
            if (attacker is not IStats attackerStats || target is not IStats targetStats)
            {
                return 0f;
            }

            int mainStat = payload.OverlayMode == AttackOverlayMode.Magic
                ? attackerStats.Intelligence
                : attackerStats.Strength;

            float baseDamage = payload.BasePower * (1f + mainStat * 0.05f);
            float critChance = Mathf.Clamp(0.02f + attackerStats.Dexterity * 0.002f + attackerStats.Luck * 0.001f, 0f, 0.65f);

            if (_rng.Randf() <= critChance)
            {
                isCritical = true;
                baseDamage *= CritMultiplier;
            }

            float mitigation = Mathf.Clamp(targetStats.Vitality * 0.005f, 0f, 0.60f);
            float finalDamage = Mathf.Max(1f, baseDamage * (1f - mitigation));
            return finalDamage;
        }

        private float ComputeExpectedDamage(Entity attacker, Entity target, AttackPayloadResource payload)
        {
            if (attacker is not IStats attackerStats || target is not IStats targetStats)
            {
                return 0f;
            }

            int mainStat = payload.OverlayMode == AttackOverlayMode.Magic
                ? attackerStats.Intelligence
                : attackerStats.Strength;

            float baseDamage = payload.BasePower * (1f + mainStat * 0.05f);
            float critChance = Mathf.Clamp(0.02f + attackerStats.Dexterity * 0.002f + attackerStats.Luck * 0.001f, 0f, 0.65f);
            float expectedCritFactor = 1f + critChance * (CritMultiplier - 1f);
            float mitigation = Mathf.Clamp(targetStats.Vitality * 0.005f, 0f, 0.60f);
            return Mathf.Max(1f, baseDamage * expectedCritFactor * (1f - mitigation));
        }

        private AttackPayloadResource BuildAdHocPayload(string abilityId)
        {
            AttackOverlayMode mode = abilityId != null && abilityId.Contains("magic", StringComparison.OrdinalIgnoreCase)
                ? AttackOverlayMode.Magic
                : AttackOverlayMode.Melee;

            return new AttackPayloadResource
            {
                OverlayMode = mode,
                BasePower = 1f,
                DamageType = mode == AttackOverlayMode.Magic ? "Elemental" : "Physical",
                ElementType = mode == AttackOverlayMode.Magic ? "Arcane" : string.Empty,
                DeliveryShapeId = "SingleTarget"
            };
        }

        private void TickStatuses(float delta)
        {
            if (delta <= 0f || _activeStatuses.Count == 0)
            {
                return;
            }

            List<(Entity target, string status)> expired = new();

            foreach ((Entity target, Dictionary<string, StatusRuntime> statuses) in _activeStatuses)
            {
                foreach ((string statusId, StatusRuntime runtime) in statuses)
                {
                    if (runtime.RemainingSeconds <= 0f)
                    {
                        continue;
                    }

                    runtime.RemainingSeconds = Mathf.Max(0f, runtime.RemainingSeconds - delta);
                    if (runtime.RemainingSeconds <= 0f)
                    {
                        expired.Add((target, statusId));
                    }
                }
            }

            foreach ((Entity target, string status) in expired)
            {
                RemoveStatus(target, status, int.MaxValue);
            }
        }

        private static void Log(string message)
        {
            if (!DebugCombatFlow)
            {
                return;
            }

            GD.Print($"[CombatManager] {message}");
        }
    }
}
