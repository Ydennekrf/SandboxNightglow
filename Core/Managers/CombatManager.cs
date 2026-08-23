using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    /// <summary>
    /// Resolves combat payloads, direct damage/healing, status effects, and combat movement modifiers.
    /// </summary>
    /// <remarks>
    /// CombatManager owns gameplay combat rules. HitBox/HurtBox and entity nodes should report intent
    /// into this manager rather than duplicating damage, status, or targeting calculations.
    /// </remarks>
    public partial class CombatManager : ISaveable, ICombat, IResolveable
    {
        /// <summary>
        /// Runtime status instance tracked per target entity.
        /// </summary>
        private sealed class StatusRuntime
        {
            public string Id { get; init; } = string.Empty;
            public StatusEffectDefinition Definition { get; init; }
            public int Stacks { get; set; }
            public float RemainingSeconds { get; set; }
            public float TickTimerSeconds { get; set; }
            public Entity Source { get; set; }
        }

        private static readonly bool DebugCombatFlow = true;
        private const float CritMultiplier = 1.5f;
        private const string ThirdHitKnockbackPassiveId = "passive.combo.third_hit_knockback";

        private readonly string _saveKey = "Combat";
        private readonly int _resolveOrder = 20;
        private readonly Queue<AttackPayloadPacket> _payloadQueue = new();
        private readonly Dictionary<string, Func<AttackPayloadPacket, IReadOnlyList<Entity>>> _deliveryHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Entity, Dictionary<string, StatusRuntime>> _activeStatuses = new();
        private readonly Dictionary<Entity, Vector2> _knockbackVelocities = new();
        private readonly RandomNumberGenerator _rng = new();

        public string SaveKey => _saveKey;
        public int ResolveOrder => _resolveOrder;

        public CombatManager()
        {
            _rng.Randomize();
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// Enqueues an authored attack payload for resolution during the manager resolve tick.
        /// </summary>
        public void QueueAttackPayload(AttackPayloadPacket packet)
        {
            if (packet?.Payload == null)
            {
                return;
            }

            _payloadQueue.Enqueue(packet);
            Log($"QueueAttackPayload: queued source={packet.Source?.Name} phase={packet.ComboPhase} shape={packet.Payload.DeliveryShapeId} queueCount={_payloadQueue.Count}");
        }

        /// <summary>
        /// Applies or refreshes a status effect on a target using the stable status catalog ID.
        /// </summary>
        public void ApplyStatus(Entity target, string statusId, int stacks = 1, float? durationSeconds = null, Entity source = null)
        {
            if (target == null || string.IsNullOrWhiteSpace(statusId) || stacks <= 0)
            {
                return;
            }

            if (!StatusEffectCatalog.TryGet(statusId, out StatusEffectDefinition definition))
            {
                GD.PushWarning($"CombatManager: unknown status effect id '{statusId}'.");
                return;
            }

            string stableId = definition.StatusEffectId;
            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                statusMap = new Dictionary<string, StatusRuntime>(StringComparer.OrdinalIgnoreCase);
                _activeStatuses[target] = statusMap;
            }

            if (!statusMap.TryGetValue(stableId, out StatusRuntime runtime))
            {
                runtime = new StatusRuntime
                {
                    Id = stableId,
                    Definition = definition,
                    Stacks = 0,
                    RemainingSeconds = 0f,
                    TickTimerSeconds = definition.TickIntervalSeconds,
                    Source = source
                };
                statusMap[stableId] = runtime;
            }

            if (runtime.Stacks > 0 && definition.StackBehavior == StatusEffectStackBehavior.IgnoreDuplicate)
            {
                LogStatus($"Ignored duplicate {stableId} on {target.Name}.");
                return;
            }

            runtime.Stacks = definition.StackBehavior == StatusEffectStackBehavior.StackIntensity
                ? runtime.Stacks + stacks
                : Mathf.Max(runtime.Stacks, stacks);
            runtime.Source = source;
            float duration = durationSeconds.GetValueOrDefault(definition.DurationSeconds);
            runtime.RemainingSeconds = duration > 0f ? Mathf.Max(runtime.RemainingSeconds, duration) : runtime.RemainingSeconds;
            runtime.TickTimerSeconds = definition.TickIntervalSeconds;

            if (definition.KnockbackForce > 0f)
            {
                ApplyKnockbackImpulse(target, source, definition.KnockbackForce);
            }

            LogStatus($"Applied {stableId} to {target.Name} stacks={runtime.Stacks} remaining={runtime.RemainingSeconds:0.###}");
            CombatFeedbackBus.EmitEffectApplied(target, stableId, runtime.RemainingSeconds);
        }

        /// <summary>
        /// Checks high-level combat eligibility before damage is calculated.
        /// </summary>
        public bool CanHit(Entity attacker, Entity target, string abilityId)
        {
            if (attacker == null || target == null)
            {
                return false;
            }

            if (IsStunned(attacker))
            {
                return false;
            }

            float hitChanceMultiplier = GetHitChanceMultiplier(attacker);
            if (hitChanceMultiplier < 0.999f && _rng.Randf() > hitChanceMultiplier)
            {
                LogStatus($"Blind caused {attacker.Name}'s {abilityId} to miss {target.Name}.");
                return false;
            }

            if (target is IStats targetStats && targetStats.CurHP <= 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves an immediate ad-hoc ability hit and returns computed damage/critical state.
        /// </summary>
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

        /// <summary>
        /// Applies damage and optional status effects to each target in a resolved target set.
        /// </summary>
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

        /// <summary>
        /// Applies direct damage to a target using current combat modifiers.
        /// </summary>
        public void DealDamage(Entity target, float amount, string damageType = "Physical", Entity source = null, IEnumerable<string> tags = null)
        {
            int actualDamage = ApplyDamageInternal(target, amount, damageType, source, tags);
            if (actualDamage > 0)
            {
                Log($"DealDamage: target={target.Name} amount={actualDamage} type={damageType} hpNow={(target as IStats)?.CurHP}");
            }
        }

        /// <summary>
        /// Applies direct healing to a target implementing IStats.
        /// </summary>
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

        /// <summary>
        /// Estimates expected damage for UI previews without mutating combat state.
        /// </summary>
        public float PreviewDamage(Entity attacker, Entity target, string abilityId)
        {
            AttackPayloadResource payload = BuildAdHocPayload(abilityId);
            return ComputeExpectedDamage(attacker, target, payload);
        }

        /// <summary>
        /// Removes status stacks from a target and clears runtime movement effects when needed.
        /// </summary>
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

            string stableId = StatusEffectCatalog.NormalizeId(statusId);
            if (!statusMap.TryGetValue(stableId, out StatusRuntime runtime))
            {
                return;
            }

            if (stacks >= runtime.Stacks)
            {
                statusMap.Remove(stableId);
                LogStatus($"Expired {stableId} on {target.Name}.");
            }
            else
            {
                runtime.Stacks -= stacks;
                LogStatus($"Removed {stacks} stacks of {stableId} from {target.Name}; remainingStacks={runtime.Stacks}");
            }

            if (statusMap.Count == 0)
            {
                _activeStatuses.Remove(target);
            }

            if (string.Equals(stableId, StatusEffectCatalog.Knockback, StringComparison.OrdinalIgnoreCase))
            {
                _knockbackVelocities.Remove(target);
            }
        }

        /// <summary>
        /// Returns true when the target currently has at least one stack of the normalized status ID.
        /// </summary>
        public bool HasStatus(Entity target, string statusId)
        {
            if (target == null || string.IsNullOrWhiteSpace(statusId))
            {
                return false;
            }

            string stableId = StatusEffectCatalog.NormalizeId(statusId);
            return _activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap)
                && statusMap.TryGetValue(stableId, out StatusRuntime runtime)
                && runtime.Stacks > 0;
        }

        public bool IsStunned(Entity target) => HasStatus(target, StatusEffectCatalog.Stun);

        public bool IsSilenced(Entity target) => HasStatus(target, StatusEffectCatalog.Silence);

        public float GetMovementSpeedMultiplier(Entity target)
        {
            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                return 1f;
            }

            float multiplier = 1f;
            foreach (StatusRuntime runtime in statusMap.Values)
            {
                float slow = Mathf.Clamp(runtime.Definition?.MovementSlowPercent ?? 0f, 0f, 0.95f);
                if (slow > 0f)
                {
                    multiplier *= 1f - slow * Mathf.Max(1, runtime.Stacks);
                }
            }

            return Mathf.Clamp(multiplier, 0.2f, 1f);
        }

        public float GetAttackSpeedMultiplier(Entity target)
        {
            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                return 1f;
            }

            float multiplier = 1f;
            foreach (StatusRuntime runtime in statusMap.Values)
            {
                float slow = Mathf.Clamp(runtime.Definition?.AttackSlowPercent ?? 0f, 0f, 0.95f);
                if (slow > 0f)
                {
                    multiplier *= 1f - slow * Mathf.Max(1, runtime.Stacks);
                }
            }

            return Mathf.Clamp(multiplier, 0.2f, 1f);
        }

        public float GetPhysicalDamageTakenMultiplier(Entity target)
        {
            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                return 1f;
            }

            float multiplier = 1f;
            foreach (StatusRuntime runtime in statusMap.Values)
            {
                float statusMultiplier = runtime.Definition?.PhysicalDamageTakenMultiplier ?? 1f;
                if (statusMultiplier > 0f)
                {
                    multiplier *= Mathf.Pow(statusMultiplier, Mathf.Max(1, runtime.Stacks));
                }
            }

            return Mathf.Max(0.05f, multiplier);
        }

        public float GetHitChanceMultiplier(Entity target)
        {
            if (!_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                return 1f;
            }

            float multiplier = 1f;
            foreach (StatusRuntime runtime in statusMap.Values)
            {
                float penalty = Mathf.Clamp(runtime.Definition?.BlindHitChancePenalty ?? 0f, 0f, 0.95f);
                if (penalty > 0f)
                {
                    multiplier *= 1f - penalty * Mathf.Max(1, runtime.Stacks);
                }
            }

            return Mathf.Clamp(multiplier, 0.05f, 1f);
        }

        public IReadOnlyList<string> GetActiveStatusDisplayNames(Entity target)
        {
            if (target == null || !_activeStatuses.TryGetValue(target, out Dictionary<string, StatusRuntime> statusMap))
            {
                return Array.Empty<string>();
            }

            return statusMap.Values
                .Where(status => status.Stacks > 0)
                .Select(status => status.Definition?.DisplayName ?? status.Id)
                .ToArray();
        }

        /// <summary>
        /// Returns status-adjusted movement velocity, including roots, stuns, slows, and knockback.
        /// </summary>
        public Vector2 ResolveMovementVelocity(Entity target, Vector2 desiredVelocity)
        {
            if (target == null)
            {
                return desiredVelocity;
            }

            Vector2 finalVelocity = desiredVelocity;
            if (IsStunned(target) || HasStatus(target, "Root"))
            {
                finalVelocity = Vector2.Zero;
            }

            finalVelocity *= GetMovementSpeedMultiplier(target);

            if (_knockbackVelocities.TryGetValue(target, out Vector2 knockback))
            {
                finalVelocity += knockback;
            }

            return finalVelocity;
        }

        /// <summary>
        /// Drains queued attack payloads and resolves their delivery shape handlers.
        /// </summary>
        public void Resolve()
        {
            while (_payloadQueue.Count > 0)
            {
                AttackPayloadPacket packet = _payloadQueue.Dequeue();
                Log($"Resolve: dequeued source={packet.Source?.Name} phase={packet.ComboPhase} remaining={_payloadQueue.Count}");
                ExecuteAttackPayload(packet);
            }
        }

        /// <summary>
        /// Ticks status durations and combat movement effects when passed a frame delta.
        /// </summary>
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

        /// <summary>
        /// Applies a queued attack payload to one concrete target and emits combat feedback.
        /// </summary>
        public float ResolveAttackPayloadHit(AttackPayloadPacket packet, Entity target, out bool isCritical)
        {
            isCritical = false;
            if (packet?.Payload == null || target == null)
            {
                return 0f;
            }

            float damage = ComputeDamage(packet.Source, target, packet.Payload, out isCritical);
            int actualDamage = damage > 0f
                ? ApplyDamageInternal(target, damage, packet.Payload.DamageType, packet.Source, null)
                : 0;

            Log($"ResolveAttackPayloadHit: target={target.Name} damage={actualDamage} crit={isCritical}");
            CombatFeedbackBus.EmitHitResolved(packet.Source, target, actualDamage, isCritical, packet.Payload.DamageType, packet.Payload.ElementType);
            ApplyPayloadEffects(target, packet);
            ApplyUnlockedPassiveEffects(target, packet);

            return actualDamage;
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
            ShowMagicAreaPreview(packet, shapeId);

            IReadOnlyList<Entity> targets = _deliveryHandlers.TryGetValue(shapeId, out Func<AttackPayloadPacket, IReadOnlyList<Entity>> deliveryHandler)
                ? deliveryHandler(packet)
                : Array.Empty<Entity>();

            if (targets.Count == 0)
            {
                Log($"ExecuteAttackPayload: no targets resolved for shape='{shapeId}'.");
            }

            foreach (Entity target in targets)
            {
                ResolveAttackPayloadHit(packet, target, out _);
            }
        }

        private void ApplyPayloadEffects(Entity target, AttackPayloadPacket packet)
        {
            AttackPayloadResource payload = packet.Payload;
            foreach (string effectId in EnumeratePacketEffectIds(packet))
            {
                if (string.IsNullOrWhiteSpace(effectId))
                {
                    continue;
                }

                if (StatusEffectCatalog.TryGet(effectId, out StatusEffectDefinition definition))
                {
                    float duration = payload.EffectDurationSeconds > 0f
                        ? payload.EffectDurationSeconds
                        : definition.DurationSeconds;
                    Log($"ApplyPayloadEffects: applying effect='{definition.StatusEffectId}' duration={duration:0.###}");
                    ApplyStatus(target, definition.StatusEffectId, 1, duration, packet.Source);
                }
                else
                {
                    GD.PushWarning($"CombatManager: unknown effect id '{effectId}' for payload.");
                }
            }
        }

        private void ApplyUnlockedPassiveEffects(Entity target, AttackPayloadPacket packet)
        {
            if (packet?.Source is not Player player || target == null)
            {
                return;
            }

            if (packet.ComboPhase == 3 && player.AbilityPath.HasPassiveAbility(ThirdHitKnockbackPassiveId))
            {
                ApplyStatus(target, StatusEffectCatalog.Knockback, 1, null, packet.Source);
            }
        }

        private void RegisterDefaultHandlers()
        {
            Log("RegisterDefaultHandlers: registering default delivery/effect handlers.");

            _deliveryHandlers["SingleTarget"] = packet =>
            {
                List<Entity> targets = GetEnemyTargetsInShape(packet, MagicShape.ProjectileBolt);
                if (targets.Count > 1)
                {
                    targets = new List<Entity> { targets[0] };
                }
                return targets;
            };

            _deliveryHandlers["Cone"] = packet => GetEnemyTargetsInShape(packet, MagicShape.Cone);
            _deliveryHandlers["Linear"] = packet => GetEnemyTargetsInShape(packet, MagicShape.Linear);
            _deliveryHandlers["ProjectileBolt"] = packet =>
            {
                SpawnMagicProjectile(packet);
                return Array.Empty<Entity>();
            };
            _deliveryHandlers["CircleWaveAwayFromPlayer"] = packet => GetEnemyTargetsInShape(packet, MagicShape.CircleWaveAwayFromPlayer);

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

        private List<Entity> GetEnemyTargetsInShape(AttackPayloadPacket packet, MagicShape shape)
        {
            List<Entity> targets = GetEnemyTargets();
            if (packet == null || targets.Count == 0)
            {
                return new List<Entity>();
            }

            float cellSize = Mathf.Max(1f, packet.Payload?.MagicCellSize ?? 24f);
            Vector2I forward = ToGridDirection(packet.ForwardDirection);
            int rangeBonus = packet.Source is Player player ? player.MagicRangeBonus : 0;
            IReadOnlyList<Vector2I> cells = MagicShapePreview.Calculate(shape, Vector2I.Zero, forward, rangeBonus);
            if (cells.Count == 0)
            {
                return new List<Entity>();
            }

            List<Entity> filtered = new();
            foreach (Entity target in targets)
            {
                Node2D targetNode = FindNodeForEntity(target);
                if (targetNode == null)
                {
                    continue;
                }

                Vector2 local = targetNode.GlobalPosition - packet.OriginPosition;
                if (shape == MagicShape.CircleWaveAwayFromPlayer)
                {
                    float distance = local.Length();
                    if (distance <= cellSize * (2.5f + rangeBonus) && distance >= cellSize * 0.25f)
                    {
                        filtered.Add(target);
                    }

                    continue;
                }

                foreach (Vector2I cell in cells)
                {
                    Vector2 center = new(cell.X * cellSize, cell.Y * cellSize);
                    if (Mathf.Abs(local.X - center.X) <= cellSize * 0.5f
                        && Mathf.Abs(local.Y - center.Y) <= cellSize * 0.5f)
                    {
                        filtered.Add(target);
                        break;
                    }
                }
            }

            return filtered;
        }

        private void ShowMagicAreaPreview(AttackPayloadPacket packet, string shapeId)
        {
            if (packet?.Payload == null || packet.Payload.OverlayMode != AttackOverlayMode.Magic)
            {
                return;
            }

            MagicShape shape = ParseMagicShape(shapeId);
            if (shape == MagicShape.None || shape == MagicShape.ProjectileBolt)
            {
                return;
            }

            Node scene = GameManager.Instance?.GetTree()?.CurrentScene;
            if (scene == null)
            {
                return;
            }

            MagicAreaHighlighter highlighter = scene.GetNodeOrNull<MagicAreaHighlighter>("MagicAreaHighlighter");
            if (highlighter == null)
            {
                highlighter = new MagicAreaHighlighter { Name = "MagicAreaHighlighter" };
                scene.AddChild(highlighter);
            }

            highlighter.ShowPreview(
                ParseElement(packet.Payload.ElementType),
                shape,
                packet.OriginPosition,
                ToGridDirection(packet.ForwardDirection),
                packet.Payload.MagicCellSize,
                packet.Source is Player player ? player.MagicRangeBonus : 0);
        }

        private void SpawnMagicProjectile(AttackPayloadPacket packet)
        {
            if (packet?.Payload == null)
            {
                return;
            }

            Node scene = GameManager.Instance?.GetTree()?.CurrentScene;
            if (scene == null)
            {
                return;
            }

            Vector2 direction = packet.ForwardDirection.LengthSquared() > 0.0001f
                ? packet.ForwardDirection.Normalized()
                : Vector2.Right;
            MagicProjectile projectile = new()
            {
                Name = "MagicProjectile"
            };
            scene.AddChild(projectile);
            projectile.Configure(packet, packet.OriginPosition, direction);
            Log($"SpawnMagicProjectile: origin={packet.OriginPosition} direction={direction} maxDistance={packet.Payload.ProjectileMaxDistance:0.###}");
        }

        private static Vector2I ToGridDirection(Vector2 direction)
        {
            if (direction.LengthSquared() <= 0.0001f)
            {
                return Vector2I.Right;
            }

            return Mathf.Abs(direction.X) >= Mathf.Abs(direction.Y)
                ? new Vector2I(Math.Sign(direction.X), 0)
                : new Vector2I(0, Math.Sign(direction.Y));
        }

        private static MagicShape ParseMagicShape(string shapeId)
        {
            return !string.IsNullOrWhiteSpace(shapeId) && Enum.TryParse(shapeId, true, out MagicShape shape)
                ? shape
                : MagicShape.None;
        }

        private static ElementType ParseElement(string elementId)
        {
            return !string.IsNullOrWhiteSpace(elementId) && Enum.TryParse(elementId, true, out ElementType element)
                ? element
                : ElementType.None;
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
                TickKnockback(delta);
                return;
            }

            List<(Entity target, string status)> expired = new();

            foreach ((Entity target, Dictionary<string, StatusRuntime> statuses) in _activeStatuses.ToArray())
            {
                foreach ((string statusId, StatusRuntime runtime) in statuses.ToArray())
                {
                    TickStatusRuntime(target, runtime, delta);

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

            TickKnockback(delta);
        }

        private int ApplyDamageInternal(Entity target, float amount, string damageType, Entity source, IEnumerable<string> tags)
        {
            if (target is not IStats stats)
            {
                return 0;
            }

            float modifiedAmount = Mathf.Max(1f, amount);
            if (string.Equals(damageType, "Physical", StringComparison.OrdinalIgnoreCase))
            {
                modifiedAmount *= GetPhysicalDamageTakenMultiplier(target);
            }

            int before = stats.CurHP;
            int roundedDamage = Mathf.Max(1, Mathf.RoundToInt(modifiedAmount));
            stats.CurHP = -roundedDamage;
            int actualDamage = Mathf.Max(0, before - stats.CurHP);

            if (actualDamage > 0)
            {
                TryReflectThorns(target, source, actualDamage, damageType, tags);
            }

            return actualDamage;
        }

        private void TickStatusRuntime(Entity target, StatusRuntime runtime, float delta)
        {
            StatusEffectDefinition definition = runtime.Definition;
            if (target == null || definition == null || definition.TickIntervalSeconds <= 0f)
            {
                return;
            }

            runtime.TickTimerSeconds -= delta;
            while (runtime.TickTimerSeconds <= 0f && runtime.RemainingSeconds > 0f)
            {
                runtime.TickTimerSeconds += definition.TickIntervalSeconds;

                if (definition.DamagePerTick > 0f)
                {
                    int damage = ApplyDamageInternal(target, definition.DamagePerTick * Mathf.Max(1, runtime.Stacks), "Status", runtime.Source, new[] { "StatusTick" });
                    LogStatus($"Tick {definition.StatusEffectId}: {damage} damage to {target.Name}");
                    CombatFeedbackBus.EmitHitResolved(runtime.Source, target, damage, false, "Status", definition.StatusEffectId);
                }

                if (definition.ManaDamagePerTick > 0f)
                {
                    TickManaDamage(target, definition, runtime);
                }
            }
        }

        private void TickManaDamage(Entity target, StatusEffectDefinition definition, StatusRuntime runtime)
        {
            if (target is not IStats stats || stats.MaxMana <= 0)
            {
                LogStatus($"Tick {definition.StatusEffectId}: {target?.Name} has no mana resource.");
                return;
            }

            int before = stats.CurMana;
            int manaDamage = Mathf.Max(1, Mathf.RoundToInt(definition.ManaDamagePerTick * Mathf.Max(1, runtime.Stacks)));
            stats.CurMana = -manaDamage;
            int actual = Mathf.Max(0, before - stats.CurMana);
            LogStatus($"Tick {definition.StatusEffectId}: {actual} mana damage to {target.Name}");
        }

        private void TryReflectThorns(Entity defender, Entity attacker, int incomingDamage, string damageType, IEnumerable<string> tags)
        {
            if (defender == null || attacker == null || incomingDamage <= 0)
            {
                return;
            }

            bool preventsReflection = tags?.Any(tag =>
                string.Equals(tag, "Reflected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tag, "StatusTick", StringComparison.OrdinalIgnoreCase)) == true;

            if (preventsReflection || !string.Equals(damageType, "Physical", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!_activeStatuses.TryGetValue(defender, out Dictionary<string, StatusRuntime> statusMap)
                || !statusMap.TryGetValue(StatusEffectCatalog.Thorns, out StatusRuntime runtime))
            {
                return;
            }

            float reflectPercent = Mathf.Clamp(runtime.Definition.ThornsReflectPercent * Mathf.Max(1, runtime.Stacks), 0f, 1f);
            int reflected = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * reflectPercent));
            int actual = ApplyDamageInternal(attacker, reflected, "Reflected", defender, new[] { "Reflected" });
            LogStatus($"Thorns reflected {actual} damage from {defender.Name} to {attacker.Name}.");
            CombatFeedbackBus.EmitHitResolved(defender, attacker, actual, false, "Reflected", StatusEffectCatalog.Thorns);
        }

        private IEnumerable<string> EnumeratePacketEffectIds(AttackPayloadPacket packet)
        {
            if (packet?.Payload?.EffectIds != null)
            {
                foreach (string effectId in packet.Payload.EffectIds)
                {
                    yield return effectId;
                }
            }

            if (packet?.AdditionalEffectIds == null)
            {
                yield break;
            }

            foreach (string effectId in packet.AdditionalEffectIds)
            {
                yield return effectId;
            }
        }

        private void ApplyKnockbackImpulse(Entity target, Entity source, float force)
        {
            if (target == null)
            {
                return;
            }

            Vector2 direction = Vector2.Right;
            if (source != null && target is not null)
            {
                Node2D targetNode = FindNodeForEntity(target);
                Node2D sourceNode = FindNodeForEntity(source);
                if (targetNode != null && sourceNode != null)
                {
                    direction = targetNode.GlobalPosition - sourceNode.GlobalPosition;
                }
            }

            if (direction.LengthSquared() <= 0.0001f)
            {
                direction = Vector2.Right;
            }

            direction = direction.Normalized();
            _knockbackVelocities[target] = direction * Mathf.Max(1f, force);
        }

        private Node2D FindNodeForEntity(Entity entity)
        {
            if (entity == null || GameManager.Instance?.GetTree() == null)
            {
                return null;
            }

            foreach (Node node in GameManager.Instance.GetTree().GetNodesInGroup("Player"))
            {
                if (node is PlayerNode playerNode && ReferenceEquals(entity, GameManager.Instance.GetPlayer()))
                {
                    return playerNode;
                }
            }

            foreach (Node node in GameManager.Instance.GetTree().GetNodesInGroup("DebugEnemy"))
            {
                if (node is TestEnemyNode testEnemy && ReferenceEquals(entity, testEnemy.EnemyModel))
                {
                    return testEnemy;
                }
            }

            return null;
        }

        private void TickKnockback(float delta)
        {
            if (delta <= 0f || _knockbackVelocities.Count == 0)
            {
                return;
            }

            List<Entity> completed = new();
            foreach ((Entity target, Vector2 velocity) in _knockbackVelocities.ToArray())
            {
                Vector2 damped = velocity.MoveToward(Vector2.Zero, 900f * delta);
                if (damped.LengthSquared() <= 0.01f)
                {
                    completed.Add(target);
                }
                else
                {
                    _knockbackVelocities[target] = damped;
                }
            }

            foreach (Entity entity in completed)
            {
                _knockbackVelocities.Remove(entity);
            }
        }

        private static void LogStatus(string message)
        {
            if (!DebugCombatFlow)
            {
                return;
            }

            GD.Print($"[StatusDebug] {message}");
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
