using System.Collections.Generic;
using Godot;

namespace ethra.V1.Actions
{
    public sealed class AttackAction : IStateAction
    {
        private const string PlayerAnimLibraryPath = "res://ArtAssets/AnimationLibraries/playerActions.tres";
        private const string FallbackComboPath = "res://Core/Combat/Data/Combos/basic_attack_combo.tres";
        private static readonly bool DebugCombatFlow = true;

        private static AnimationLibrary _playerAnimLibrary;
        private static WeaponComboResource _fallbackCombo;
        private static readonly HashSet<string> _missingAnimationWarnings = new();
        private readonly float _fallbackDuration;

        private string _clipName = "Melee1";
        private string _comboId = string.Empty;
        private float _activeStart;
        private float _activeEnd;
        private float _bufferStart;
        private float _bufferEnd;
        private bool _movementLock = true;
        private float _movementImpulse;
        private Vector2 _phaseForward = Vector2.Down;
        private ComboPhaseResource _activePhaseResource;
        private bool _chargePending;
        private bool _chargeComplete;
        private float _chargeElapsed;
        private bool _payloadQueuedThisPhase;
        private bool _lastActiveWindowOpen;
        private bool _lastBufferWindowOpen;

        public AttackAction(float fallbackDuration = 0.25f)
        {
            _fallbackDuration = fallbackDuration;
        }

        public void Enter(Entity owner, BaseState baseState)
        {
            if (owner is not Player player)
            {
                return;
            }

            if (player.AttackPhase < 1)
            {
                player.AttackPhase = 1;
            }

            player.ComboBufferedInput = null;
            player.ComboAdvanceQueued = false;
            player.ComboCanExitAttack = false;
            _lastActiveWindowOpen = false;
            _lastBufferWindowOpen = false;

            StartPhase(owner, player, player.AttackPhase, player.PendingAttackInput);
        }

        public void Execute(float delta, Entity owner, BaseState baseState)
        {
            if (owner is not Player player)
            {
                return;
            }

            AdvancePhaseTimers(player, delta);
            CaptureBufferedInput(player);

            if (player.AttackTimerRemaining <= 0f)
            {
                if (player.ComboAdvanceQueued && TryGetNextPhase(player, out int nextPhase, out AttackInputType nextInput))
                {
                    Log($"Execute: advancing combo phase {player.AttackPhase} -> {nextPhase} via buffered input={nextInput}.");
                    StartPhase(owner, player, nextPhase, nextInput);
                    return;
                }

                player.ComboCanExitAttack = true;
            }

            owner.RequestedAnimation = _clipName;
            owner.DesiredVelocity = ResolveAttackVelocity(player);
        }

        public void Exit(Entity owner)
        {
            if (owner is not Player player)
            {
                return;
            }

            player.AttackTimerRemaining = 0f;
            player.CurrentAttackOverlay = AttackOverlayMode.None;
            player.CurrentAttackPayload = null;
            player.CurrentComboProfile = null;
            player.CurrentComboPhase = null;
            player.ComboBufferedInput = null;
            player.ComboBufferOpen = false;
            player.ComboAdvanceQueued = false;
            player.ComboCanExitAttack = false;
            player.AttackActiveWindowOpen = false;
            player.CurrentPhaseElapsed = 0f;
            player.CurrentPhaseDuration = 0f;
            player.AttackPhase = 1;
            ResetChargeState();
        }

        private void StartPhase(Entity owner, Player player, int phase, AttackInputType requestedInput)
        {
            player.AttackPhase = phase;
            player.PendingAttackInput = requestedInput;
            player.ComboBufferedInput = null;
            player.ComboAdvanceQueued = false;
            player.ComboCanExitAttack = false;
            _phaseForward = FacingToVector(player.Facing);

            float resolvedDuration = 0f;
            ComboPhaseResource phaseResource = ResolveBestClip(player, out resolvedDuration, out _clipName);
            _activePhaseResource = phaseResource;

            owner.RequestedAnimation = _clipName;
            owner.DesiredVelocity = ResolveAttackVelocity(player);
            player.AttackTimerRemaining = resolvedDuration > 0f ? resolvedDuration : _fallbackDuration;
            player.CurrentPhaseDuration = player.AttackTimerRemaining;
            player.CurrentPhaseElapsed = 0f;

            ConfigureWindows(player.CurrentPhaseDuration, phaseResource);
            ConfigureMovement(phaseResource);
            ConfigureCharge(phaseResource);
            UpdateWindowFlags(player);
            if (!_chargePending)
            {
                QueuePayload(player, _clipName);
                _payloadQueuedThisPhase = true;
            }
            CombatFeedbackBus.EmitComboStepStarted(
                player,
                _comboId,
                phase,
                phaseResource?.Label ?? _clipName,
                phaseResource?.HitboxProfile?.ProfileId ?? string.Empty);
            CombatFeedbackBus.EmitAttackPhaseStarted(player, phase, _clipName, player.CurrentAttackOverlay);

            Log($"StartPhase: phase={phase} input={requestedInput} clip='{_clipName}' duration={player.CurrentPhaseDuration:0.###} active=[{_activeStart:0.###},{_activeEnd:0.###}] buffer=[{_bufferStart:0.###},{_bufferEnd:0.###}]");
        }

        private void AdvancePhaseTimers(Player player, float delta)
        {
            if (_chargePending)
            {
                TickCharge(player, delta);
                return;
            }

            player.CurrentPhaseElapsed = Mathf.Min(player.CurrentPhaseDuration, player.CurrentPhaseElapsed + delta);
            player.AttackTimerRemaining = Mathf.Max(0f, player.AttackTimerRemaining - delta);
            UpdateWindowFlags(player);
        }

        private void UpdateWindowFlags(Player player)
        {
            if (_chargePending)
            {
                player.AttackActiveWindowOpen = false;
                player.ComboBufferOpen = false;
                return;
            }

            float t = player.CurrentPhaseElapsed;
            player.AttackActiveWindowOpen = t >= _activeStart && t <= _activeEnd;
            player.ComboBufferOpen = t >= _bufferStart && t <= _bufferEnd;

            if (player.AttackActiveWindowOpen != _lastActiveWindowOpen)
            {
                _lastActiveWindowOpen = player.AttackActiveWindowOpen;
                CombatFeedbackBus.EmitActiveWindowChanged(player, player.AttackActiveWindowOpen, player.CurrentPhaseElapsed, player.CurrentPhaseDuration);
            }

            if (player.ComboBufferOpen != _lastBufferWindowOpen)
            {
                _lastBufferWindowOpen = player.ComboBufferOpen;
                CombatFeedbackBus.EmitBufferWindowChanged(player, player.ComboBufferOpen, player.CurrentPhaseElapsed, player.CurrentPhaseDuration);
            }
        }

        private void CaptureBufferedInput(Player player)
        {
            if (!player.ComboBufferOpen)
            {
                return;
            }

            bool requestedThisFrame = player.MeleePressed || player.MagicPressed;
            if (!requestedThisFrame)
            {
                return;
            }

            AttackInputType buffered = player.MagicPressed ? AttackInputType.Magic : AttackInputType.Melee;
            player.ComboBufferedInput = buffered;
            player.ComboAdvanceQueued = true;
            Log($"CaptureBufferedInput: buffered={buffered} at t={player.CurrentPhaseElapsed:0.###}/{player.CurrentPhaseDuration:0.###}");
        }

        private bool TryGetNextPhase(Player player, out int nextPhase, out AttackInputType input)
        {
            nextPhase = player.AttackPhase;
            input = player.PendingAttackInput;

            WeaponComboResource combo = ResolveComboProfile(GetEquippedMainHandWeapon());
            int phaseCount = combo?.Phases?.Count ?? 0;

            if (phaseCount <= 0)
            {
                return false;
            }

            if (player.AttackPhase >= phaseCount)
            {
                if (combo?.CanLoop == true)
                {
                    nextPhase = 1;
                    input = player.ComboBufferedInput ?? player.PendingAttackInput;
                    return true;
                }

                return false;
            }

            nextPhase = player.AttackPhase + 1;
            input = player.ComboBufferedInput ?? player.PendingAttackInput;
            return true;
        }

        private static float ResolveDuration(string clipName)
        {
            _playerAnimLibrary ??= ResourceLoader.Load<AnimationLibrary>(PlayerAnimLibraryPath);
            if (_playerAnimLibrary == null)
            {
                return 0f;
            }

            Animation animation = _playerAnimLibrary.GetAnimation(clipName);
            return animation?.Length ?? 0f;
        }

        private ComboPhaseResource ResolveBestClip(Player player, out float duration, out string clipName)
        {
            _playerAnimLibrary ??= ResourceLoader.Load<AnimationLibrary>(PlayerAnimLibraryPath);

            ComboPhaseResource comboPhase = ResolveComboPhase(player, out string comboClip, out float comboDuration);
            if (comboPhase != null)
            {
                duration = comboDuration;
                clipName = comboClip;
                Log($"ResolveBestClip: combo profile clip='{comboClip}' duration={comboDuration:0.###}");
                return comboPhase;
            }

            foreach (string candidate in BuildFallbackClipCandidates(player))
            {
                float candidateDuration = ResolveDuration(candidate);
                if (candidateDuration > 0f)
                {
                    player.CurrentAttackOverlay = player.PendingAttackInput == AttackInputType.Magic
                        ? AttackOverlayMode.Magic
                        : AttackOverlayMode.Melee;
                    duration = candidateDuration;
                    clipName = candidate;
                    Log($"ResolveBestClip: fallback clip='{candidate}' duration={candidateDuration:0.###}");
                    return null;
                }
            }

            player.CurrentAttackOverlay = AttackOverlayMode.Melee;
            duration = ResolveDuration("Melee1");
            clipName = "Melee1";
            return null;
        }

        private ComboPhaseResource ResolveComboPhase(Player player, out string clipName, out float duration)
        {
            clipName = null;
            duration = 0f;

            WeaponItem weapon = GetEquippedMainHandWeapon();
            WeaponComboResource combo = ResolveComboProfile(weapon);
            ComboPhaseResource phase = combo?.GetPhaseForStep(player.AttackPhase);
            if (phase == null)
            {
                Log($"ResolveComboPhase: no phase found for step={player.AttackPhase}.");
                return null;
            }

            player.CurrentComboProfile = combo;
            player.CurrentComboPhase = phase;
            _comboId = string.IsNullOrWhiteSpace(combo?.ComboId) ? combo?.ResourcePath ?? string.Empty : combo.ComboId;

            AttackPayloadResource payload = PickPayloadForInput(player, phase);
            if (payload == null)
            {
                Log($"ResolveComboPhase: payload not resolved for phase={player.AttackPhase}.");
                return null;
            }

            ApplyPhaseModifiers(payload, phase, charged: false);

            player.CurrentAttackPayload = payload;
            player.CurrentAttackOverlay = payload.OverlayMode;
            Log($"ResolveComboPhase: payload shape={payload.DeliveryShapeId} dmgType={payload.DamageType} element={payload.ElementType} overlay={payload.OverlayMode}");

            foreach (string candidate in BuildPhaseClipCandidates(phase, player.Facing))
            {
                float clipDuration = ResolveDuration(candidate);
                if (clipDuration <= 0f)
                {
                    continue;
                }

                clipName = candidate;
                duration = ResolvePhaseDuration(phase, clipDuration);
                Log($"ResolveComboPhase: resolved shared animation='{candidate}' duration={duration:0.###}");
                return phase;
            }

            string fallbackClip = ResolveFallbackClipName(player);
            float fallbackDuration = ResolveDuration(fallbackClip);
            clipName = fallbackClip;
            duration = ResolvePhaseDuration(phase, fallbackDuration);
            WarnMissingAnimationOnce(phase.SharedAnimationName, fallbackClip);
            return phase;
        }

        private static AttackPayloadResource PickPayloadForInput(Player player, ComboPhaseResource phase)
        {
            bool wantsMagic = player.PendingAttackInput == AttackInputType.Magic;
            AttackPayloadResource melee = phase.MeleePayload;
            AttackPayloadResource magic = phase.MagicPayload;
            WeaponItem weapon = GetEquippedMainHandWeapon();

            if (wantsMagic && magic != null)
            {
                int manaCost = Mathf.Max(0, magic.ManaCost);
                if (player.CurMana >= manaCost)
                {
                    if (manaCost > 0)
                    {
                        player.CurMana = -manaCost;
                    }

                    Log($"PickPayloadForInput: magic selected (cost={manaCost}, manaRemaining={player.CurMana}).");
                    return BuildSocketedMagicPayload(magic, weapon) ?? ClonePayloadWithOnHitEffects(magic, weapon);
                }

                Log($"PickPayloadForInput: insufficient mana for magic (required={manaCost}, current={player.CurMana}), falling back.");
                CombatFeedbackBus.EmitMagicDenied(player, manaCost, player.CurMana);
            }

            Log("PickPayloadForInput: melee payload selected.");
            return ClonePayloadWithOnHitEffects(melee ?? magic, weapon);
        }

        private static void ApplyPhaseModifiers(AttackPayloadResource payload, ComboPhaseResource phase, bool charged)
        {
            if (payload == null || phase == null)
            {
                return;
            }

            payload.BasePower *= Mathf.Max(0f, phase.DamageMultiplier);
            if (charged)
            {
                payload.BasePower *= Mathf.Max(1f, phase.ChargedDamageMultiplier);
            }

            foreach (string statusId in phase.StatusEffectsToApply)
            {
                if (string.IsNullOrWhiteSpace(statusId))
                {
                    continue;
                }

                payload.EffectIds.Add(statusId);
            }

            if (!charged)
            {
                return;
            }

            foreach (string statusId in phase.ChargedStatusEffectsToApply)
            {
                if (string.IsNullOrWhiteSpace(statusId))
                {
                    continue;
                }

                payload.EffectIds.Add(statusId);
            }
        }

        private static void ApplyChargedPhaseModifiers(AttackPayloadResource payload, ComboPhaseResource phase)
        {
            if (payload == null || phase == null)
            {
                return;
            }

            payload.BasePower *= Mathf.Max(1f, phase.ChargedDamageMultiplier);

            foreach (string statusId in phase.ChargedStatusEffectsToApply)
            {
                if (string.IsNullOrWhiteSpace(statusId))
                {
                    continue;
                }

                payload.EffectIds.Add(statusId);
            }
        }

        private static AttackPayloadResource BuildSocketedMagicPayload(AttackPayloadResource basePayload, WeaponItem weapon)
        {
            GameManager gm = GameManager.Instance;
            WeaponInstanceState instance = gm?.Inventory?.GetEquippedWeaponInstance("MainHand");
            RuneItem elementalRune = gm?.WeaponUpgrades?.GetElementalRune(instance);
            if (basePayload == null || elementalRune == null || !elementalRune.IsElementalRune)
            {
                return null;
            }

            AttackPayloadResource socketed = new()
            {
                OverlayMode = AttackOverlayMode.Magic,
                ManaCost = basePayload.ManaCost,
                DeliveryShapeId = MapMagicShapeToDeliveryShape(elementalRune.Shape),
                DamageType = "Elemental",
                ElementType = elementalRune.Element.ToString(),
                BasePower = Mathf.Max(1f, basePayload.BasePower),
                MagicCellSize = basePayload.MagicCellSize,
                ProjectileSpeed = basePayload.ProjectileSpeed,
                ProjectileMaxDistance = basePayload.ProjectileMaxDistance,
                ProjectileRadius = basePayload.ProjectileRadius,
                EffectDurationSeconds = basePayload.EffectDurationSeconds
            };

            foreach (string effectId in basePayload.EffectIds)
            {
                socketed.EffectIds.Add(effectId);
            }

            AppendOnHitStatuses(socketed, weapon?.Effects);
            AppendOnHitStatuses(socketed, elementalRune.Effects);

            Log($"BuildSocketedMagicPayload: rune={elementalRune.Name} element={socketed.ElementType} shape={socketed.DeliveryShapeId}");
            return socketed;
        }

        private static AttackPayloadResource ClonePayloadWithOnHitEffects(AttackPayloadResource source, WeaponItem weapon)
        {
            if (source == null)
            {
                return null;
            }

            AttackPayloadResource clone = new()
            {
                OverlayMode = source.OverlayMode,
                ManaCost = source.ManaCost,
                DeliveryShapeId = source.DeliveryShapeId,
                DamageType = source.DamageType,
                ElementType = source.ElementType,
                BasePower = source.BasePower,
                MagicCellSize = source.MagicCellSize,
                ProjectileSpeed = source.ProjectileSpeed,
                ProjectileMaxDistance = source.ProjectileMaxDistance,
                ProjectileRadius = source.ProjectileRadius,
                EffectDurationSeconds = source.EffectDurationSeconds
            };

            foreach (string effectId in source.EffectIds)
            {
                clone.EffectIds.Add(effectId);
            }

            AppendOnHitStatuses(clone, weapon?.Effects);
            return clone;
        }

        private static void AppendOnHitStatuses(AttackPayloadResource payload, IEnumerable<ItemEffects> effects)
        {
            if (payload == null || effects == null)
            {
                return;
            }

            foreach (ItemEffects effect in effects)
            {
                if (effect is not ItemStatusEffect statusEffect || !statusEffect.IsOnHit)
                {
                    continue;
                }

                if (!statusEffect.IsKnownStatus())
                {
                    Log($"AppendOnHitStatuses: skipped unknown status '{statusEffect.StatusId}'.");
                    continue;
                }

                if (!statusEffect.ShouldApply())
                {
                    Log($"AppendOnHitStatuses: chance failed for '{statusEffect.StatusId}'.");
                    continue;
                }

                payload.EffectIds.Add(statusEffect.StatusId);
                Log($"AppendOnHitStatuses: added '{statusEffect.StatusId}'.");
            }
        }

        private static string MapMagicShapeToDeliveryShape(MagicShape shape)
        {
            return shape switch
            {
                MagicShape.ProjectileBolt => "ProjectileBolt",
                MagicShape.Linear => "Linear",
                MagicShape.Cone => "Cone",
                MagicShape.CircleWaveAwayFromPlayer => "CircleWaveAwayFromPlayer",
                _ => "SingleTarget"
            };
        }

        private static void QueuePayload(Player player, string animationName, bool charged = false, float chargeSeconds = 0f)
        {
            if (player.CurrentAttackPayload == null)
            {
                return;
            }

            GameManager gm = GameManager.Instance;
            if (gm?.Combat == null)
            {
                return;
            }

            GetOwnerTransform(player, out Vector2 origin, out Vector2 forward);

            AttackPayloadPacket packet = new AttackPayloadPacket
            {
                Source = player,
                Payload = player.CurrentAttackPayload,
                ComboId = player.CurrentComboProfile?.ComboId ?? string.Empty,
                ComboStepId = player.CurrentComboPhase?.StepId ?? string.Empty,
                ComboStepLabel = player.CurrentComboPhase?.Label ?? animationName,
                ComboPhase = player.AttackPhase,
                IsCharged = charged,
                ChargeSeconds = chargeSeconds,
                Facing = player.Facing,
                AnimationName = animationName,
                HitboxProfile = player.CurrentComboPhase?.HitboxProfile,
                OriginPosition = origin,
                ForwardDirection = forward
            };

            CombatFeedbackBus.EmitPayloadQueued(packet);
            if (packet.Payload.OverlayMode == AttackOverlayMode.Magic)
            {
                gm.Combat.QueueAttackPayload(packet);
            }

            Log($"QueuePayload: prepared hitbox payload phase={player.AttackPhase} clip='{animationName}' origin={origin} forward={forward}");
        }

        private static WeaponItem GetEquippedMainHandWeapon()
        {
            GameManager gm = GameManager.Instance;
            if (gm?.Inventory == null || gm.DB == null)
            {
                return null;
            }

            IReadOnlyDictionary<string, int> equipped = gm.Inventory.GetEquippedWeapons();
            if (equipped == null || !equipped.TryGetValue("MainHand", out int weaponId))
            {
                return null;
            }

            return gm.DB.GetItemFromRepo(weaponId) as WeaponItem;
        }

        private void ConfigureWindows(float phaseDuration, ComboPhaseResource phase)
        {
            if (phase == null)
            {
                _activeStart = 0f;
                _activeEnd = phaseDuration;
                _bufferStart = phaseDuration * 0.45f;
                _bufferEnd = phaseDuration;
                return;
            }

            float activeStart = phase.StartupSeconds > 0f ? phase.StartupSeconds : phase.ActiveWindowStart;
            float activeEnd = phase.ActiveSeconds > 0f ? activeStart + phase.ActiveSeconds : phase.ActiveWindowEnd;
            float bufferStart = phase.ComboWindowStartSeconds > 0f ? phase.ComboWindowStartSeconds : phase.BufferWindowStart;
            float bufferEnd = phase.ComboWindowEndSeconds > 0f ? phase.ComboWindowEndSeconds : phase.BufferWindowEnd;

            _activeStart = Mathf.Clamp(activeStart, 0f, phaseDuration);
            _activeEnd = Mathf.Clamp(activeEnd, _activeStart, phaseDuration);
            _bufferStart = Mathf.Clamp(bufferStart, 0f, phaseDuration);
            _bufferEnd = Mathf.Clamp(bufferEnd, _bufferStart, phaseDuration);
        }

        private void ConfigureMovement(ComboPhaseResource phase)
        {
            _movementLock = phase?.MovementLock ?? true;
            _movementImpulse = Mathf.Max(0f, phase?.MovementImpulse ?? 0f);
        }

        private void ConfigureCharge(ComboPhaseResource phase)
        {
            _chargeElapsed = 0f;
            _chargeComplete = false;
            _payloadQueuedThisPhase = false;
            _chargePending = phase != null && phase.ChargeSeconds > 0f;
        }

        private void ResetChargeState()
        {
            _activePhaseResource = null;
            _chargePending = false;
            _chargeComplete = false;
            _chargeElapsed = 0f;
            _payloadQueuedThisPhase = false;
        }

        private void TickCharge(Player player, float delta)
        {
            if (_activePhaseResource == null)
            {
                ReleaseCharge(player, charged: false);
                return;
            }

            _chargeElapsed += Mathf.Max(0f, delta);
            float requiredSeconds = Mathf.Max(0.01f, _activePhaseResource.ChargeSeconds);
            float maxSeconds = _activePhaseResource.MaxChargeSeconds > 0f
                ? Mathf.Max(requiredSeconds, _activePhaseResource.MaxChargeSeconds)
                : requiredSeconds;
            _chargeComplete = _chargeElapsed >= requiredSeconds;

            bool stillHeld = IsChargeInputHeld(player);
            if (!stillHeld || _chargeElapsed >= maxSeconds)
            {
                ReleaseCharge(player, _chargeComplete);
            }
        }

        private void ReleaseCharge(Player player, bool charged)
        {
            _chargePending = false;
            player.CurrentPhaseElapsed = 0f;
            player.AttackTimerRemaining = player.CurrentPhaseDuration;
            UpdateWindowFlags(player);

            if (charged)
            {
                ApplyChargedPhaseModifiers(player.CurrentAttackPayload, _activePhaseResource);
            }

            if (!_payloadQueuedThisPhase)
            {
                QueuePayload(player, _clipName, charged, _chargeElapsed);
                _payloadQueuedThisPhase = true;
            }

            Log($"ReleaseCharge: charged={charged} charge={_chargeElapsed:0.###}s step={_activePhaseResource?.Label}");
        }

        private static bool IsChargeInputHeld(Player player)
        {
            return player.PendingAttackInput == AttackInputType.Magic ? player.MagicHeld : player.MeleeHeld;
        }

        private Vector2 ResolveAttackVelocity(Player player)
        {
            if (_movementImpulse > 0f && player.CurrentPhaseElapsed <= _activeEnd)
            {
                return _phaseForward * _movementImpulse;
            }

            return _movementLock ? Vector2.Zero : player.MoveInput;
        }

        private static void GetOwnerTransform(Player player, out Vector2 origin, out Vector2 forward)
        {
            origin = Vector2.Zero;
            forward = player.Facing switch
            {
                FacingDirection.Up => Vector2.Up,
                FacingDirection.Down => Vector2.Down,
                FacingDirection.Left => Vector2.Left,
                FacingDirection.Right => Vector2.Right,
                _ => Vector2.Down
            };

            Node root = GameManager.Instance;
            if (root?.GetTree() == null)
            {
                return;
            }

            PlayerNode playerNode = root.GetTree().GetFirstNodeInGroup("Player") as PlayerNode;
            if (playerNode == null)
            {
                return;
            }

            origin = playerNode.GlobalPosition;
        }

        private static List<string> BuildPhaseClipCandidates(ComboPhaseResource phase, FacingDirection facing)
        {
            var clips = new List<string>();
            string baseName = phase?.SharedAnimationName?.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return clips;
            }

            if (phase.PreferFacingSuffix)
            {
                clips.Add($"{baseName}_{facing}");
            }

            clips.Add(baseName);

            if (!phase.PreferFacingSuffix)
            {
                clips.Add($"{baseName}_{facing}");
            }

            return clips;
        }

        private static List<string> BuildFallbackClipCandidates(Player player)
        {
            string facing = player.Facing.ToString();
            int phase = Mathf.Max(1, player.AttackPhase);

            if (player.PendingAttackInput == AttackInputType.Magic)
            {
                return new List<string>
                {
                    $"Magic{phase}_{facing}",
                    $"Magic{phase}",
                    $"Magic_{facing}",
                    "Magic",
                    $"Cast{phase}_{facing}",
                    $"Cast{phase}",
                    $"Melee1_{facing}",
                    "Melee1"
                };
            }

            return new List<string>
            {
                $"Melee{phase}_{facing}",
                $"Melee{phase}",
                $"Attack{phase}_{facing}",
                $"Attack{phase}",
                $"Attack_{facing}",
                "Attack",
                "Melee1"
            };
        }

        private static WeaponComboResource ResolveComboProfile(WeaponItem weapon)
        {
            WeaponComboResource weaponCombo = weapon?.ComboProfile;
            if (weaponCombo?.Phases?.Count > 0)
            {
                return weaponCombo;
            }

            _fallbackCombo ??= ResourceLoader.Load<WeaponComboResource>(FallbackComboPath);
            return _fallbackCombo;
        }

        private static float ResolvePhaseDuration(ComboPhaseResource phase, float animationDuration)
        {
            if (phase == null)
            {
                return animationDuration;
            }

            if (phase.DurationOverrideSeconds > 0f)
            {
                return phase.DurationOverrideSeconds;
            }

            if (animationDuration > 0f)
            {
                return animationDuration;
            }

            return phase.AuthoredDurationSeconds;
        }

        private static string ResolveFallbackClipName(Player player)
        {
            foreach (string candidate in BuildFallbackClipCandidates(player))
            {
                if (ResolveDuration(candidate) > 0f)
                {
                    return candidate;
                }
            }

            return "Melee1";
        }

        private static Vector2 FacingToVector(FacingDirection facing)
        {
            return facing switch
            {
                FacingDirection.Up => Vector2.Up,
                FacingDirection.Down => Vector2.Down,
                FacingDirection.Left => Vector2.Left,
                FacingDirection.Right => Vector2.Right,
                _ => Vector2.Down
            };
        }

        private static void WarnMissingAnimationOnce(string requestedAnimation, string fallbackAnimation)
        {
            string key = $"{requestedAnimation}->{fallbackAnimation}";
            if (!_missingAnimationWarnings.Add(key))
            {
                return;
            }

            GD.PushWarning($"[Combo] Animation '{requestedAnimation}' was not found; using '{fallbackAnimation}' timing/fallback safely.");
        }

        private static void Log(string message)
        {
            if (!DebugCombatFlow)
            {
                return;
            }

            GD.Print($"[AttackAction] {message}");
        }
    }
}
