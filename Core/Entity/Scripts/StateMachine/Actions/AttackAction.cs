using System.Collections.Generic;
using Godot;

namespace ethra.V1.Actions
{
    public sealed class AttackAction : IStateAction
    {
        private const string PlayerAnimLibraryPath = "res://ArtAssets/AnimationLibraries/playerActions.tres";
        private const bool DebugCombatFlow = true;

        private static AnimationLibrary _playerAnimLibrary;
        private readonly float _fallbackDuration;

        private string _clipName = "Melee1";
        private float _activeStart;
        private float _activeEnd;
        private float _bufferStart;
        private float _bufferEnd;
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
            owner.DesiredVelocity = Vector2.Zero;
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
            player.ComboBufferedInput = null;
            player.ComboBufferOpen = false;
            player.ComboAdvanceQueued = false;
            player.ComboCanExitAttack = false;
            player.AttackActiveWindowOpen = false;
            player.CurrentPhaseElapsed = 0f;
            player.CurrentPhaseDuration = 0f;
            player.AttackPhase = 1;
        }

        private void StartPhase(Entity owner, Player player, int phase, AttackInputType requestedInput)
        {
            player.AttackPhase = phase;
            player.PendingAttackInput = requestedInput;
            player.ComboBufferedInput = null;
            player.ComboAdvanceQueued = false;
            player.ComboCanExitAttack = false;

            float resolvedDuration = 0f;
            ComboPhaseResource phaseResource = ResolveBestClip(player, out resolvedDuration, out _clipName);

            owner.RequestedAnimation = _clipName;
            owner.DesiredVelocity = Vector2.Zero;
            player.AttackTimerRemaining = resolvedDuration > 0f ? resolvedDuration : _fallbackDuration;
            player.CurrentPhaseDuration = player.AttackTimerRemaining;
            player.CurrentPhaseElapsed = 0f;

            ConfigureWindows(player.CurrentPhaseDuration, phaseResource);
            UpdateWindowFlags(player);
            QueuePayload(player, _clipName);
            CombatFeedbackBus.EmitAttackPhaseStarted(player, phase, _clipName, player.CurrentAttackOverlay);

            Log($"StartPhase: phase={phase} input={requestedInput} clip='{_clipName}' duration={player.CurrentPhaseDuration:0.###} active=[{_activeStart:0.###},{_activeEnd:0.###}] buffer=[{_bufferStart:0.###},{_bufferEnd:0.###}]");
        }

        private void AdvancePhaseTimers(Player player, float delta)
        {
            player.CurrentPhaseElapsed = Mathf.Min(player.CurrentPhaseDuration, player.CurrentPhaseElapsed + delta);
            player.AttackTimerRemaining = Mathf.Max(0f, player.AttackTimerRemaining - delta);
            UpdateWindowFlags(player);
        }

        private void UpdateWindowFlags(Player player)
        {
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

            WeaponItem weapon = GetEquippedMainHandWeapon();
            int phaseCount = weapon?.ComboProfile?.Phases?.Count ?? 0;

            if (phaseCount <= 0)
            {
                return false;
            }

            if (player.AttackPhase >= phaseCount)
            {
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
            ComboPhaseResource phase = weapon?.ComboProfile?.GetPhaseForStep(player.AttackPhase);
            if (phase == null)
            {
                Log($"ResolveComboPhase: no phase found for step={player.AttackPhase}.");
                return null;
            }

            AttackPayloadResource payload = PickPayloadForInput(player, phase);
            if (payload == null)
            {
                Log($"ResolveComboPhase: payload not resolved for phase={player.AttackPhase}.");
                return null;
            }

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
                duration = phase.DurationOverrideSeconds > 0f ? phase.DurationOverrideSeconds : clipDuration;
                Log($"ResolveComboPhase: resolved shared animation='{candidate}' duration={duration:0.###}");
                return phase;
            }

            Log($"ResolveComboPhase: no animation found for shared clip='{phase.SharedAnimationName}'.");
            return null;
        }

        private static AttackPayloadResource PickPayloadForInput(Player player, ComboPhaseResource phase)
        {
            bool wantsMagic = player.PendingAttackInput == AttackInputType.Magic;
            AttackPayloadResource melee = phase.MeleePayload;
            AttackPayloadResource magic = phase.MagicPayload;

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
                    return magic;
                }

                Log($"PickPayloadForInput: insufficient mana for magic (required={manaCost}, current={player.CurMana}), falling back.");
                CombatFeedbackBus.EmitMagicDenied(player, manaCost, player.CurMana);
            }

            Log("PickPayloadForInput: melee payload selected.");
            return melee ?? magic;
        }

        private static void QueuePayload(Player player, string animationName)
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
                ComboPhase = player.AttackPhase,
                Facing = player.Facing,
                AnimationName = animationName,
                OriginPosition = origin,
                ForwardDirection = forward
            };

            gm.Combat.QueueAttackPayload(packet);
            CombatFeedbackBus.EmitPayloadQueued(packet);

            Log($"QueuePayload: queued phase={player.AttackPhase} clip='{animationName}' origin={origin} forward={forward}");
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

            _activeStart = Mathf.Clamp(phase.ActiveWindowStart, 0f, phaseDuration);
            _activeEnd = Mathf.Clamp(phase.ActiveWindowEnd, _activeStart, phaseDuration);
            _bufferStart = Mathf.Clamp(phase.BufferWindowStart, 0f, phaseDuration);
            _bufferEnd = Mathf.Clamp(phase.BufferWindowEnd, _bufferStart, phaseDuration);
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
                    $"Cast{phase}_{facing}",
                    $"Cast{phase}",
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
