using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ethra.V1
{
    public enum AttackInputType
    {
        Melee,
        Magic
    }

    public enum AttackOverlayMode
    {
        None,
        Melee,
        Magic
    }

    public partial class Player : CombatEntity, IPlayer
    {
        private IInventory _inventory;
        private Vector2 _moveInput;
        private readonly Dictionary<string, List<EquipmentStatModifier>> _equipmentModifiersBySource = new();

        public Vector2 MoveInput
        {
            get => _moveInput;
            set
            {
                _moveInput = value;

                if (_moveInput.LengthSquared() < 0.001f)
                    return;

                if (Mathf.Abs(_moveInput.X) > Mathf.Abs(_moveInput.Y))
                    Facing = _moveInput.X > 0 ? FacingDirection.Right : FacingDirection.Left;
                else
                    Facing = _moveInput.Y > 0 ? FacingDirection.Down : FacingDirection.Up;
            }
        }

        public bool AttackPressed { get; set; } = false;
        public bool MeleePressed { get; set; } = false;
        public bool MagicPressed { get; set; } = false;
        public bool AttackHeld { get; set; } = false;
        public bool MeleeHeld { get; set; } = false;
        public bool MagicHeld { get; set; } = false;
        public AttackInputType PendingAttackInput { get; set; } = AttackInputType.Melee;
        public AttackOverlayMode CurrentAttackOverlay { get; set; } = AttackOverlayMode.None;
        public AttackPayloadResource CurrentAttackPayload { get; set; } = null;
        public WeaponComboResource CurrentComboProfile { get; set; } = null;
        public ComboPhaseResource CurrentComboPhase { get; set; } = null;
        public AttackInputType? ComboBufferedInput { get; set; } = null;
        public bool ComboBufferOpen { get; set; } = false;
        public bool ComboAdvanceQueued { get; set; } = false;
        public bool ComboCanExitAttack { get; set; } = false;
        public bool AttackActiveWindowOpen { get; set; } = false;
        public float CurrentPhaseElapsed { get; set; } = 0f;
        public float CurrentPhaseDuration { get; set; } = 0f;

        public bool RunPressed { get; set; } = false;
        public bool DodgePressed { get; set; } = false;
        public float DodgeTimerRemaining { get; set; } = 0f;
        public float AttackTimerRemaining { get; set; } = 0f;
        public int AttackPhase { get; set; } = 1;
        public bool DialogActive { get; private set; }
        public PlayerHarvestRequest ActiveHarvestRequest { get; set; }
        public float HarvestTimerRemaining { get; set; }
        public bool HarvestComplete { get; set; }
        public PlayerProgression Progression { get; } = new();
        public AbilityPathState AbilityPath { get; } = new();
        public int MagicRangeBonus => GetEquipmentModifierTotal("magic_range_bonus");

        private PlayerHarvestRequest _pendingHarvestRequest;
        private bool _dialogAnimationRequested;
        private string _dialogAnimationKey = "Dialog";

        public bool HasPendingHarvestRequest => _pendingHarvestRequest != null;

        public Player(IEntityManager entity, ICombat combat, IInventory inventory, IStateMachine fsm) : base(entity, combat, fsm)
        {
            _inventory = inventory;
        }

        public override void Initialize()
        {
            var gm = GameManager.Instance;
            float moveSpeed = gm != null ? gm.MoveSpeed : 120f;

            var states = PlayerStateBuilder.PlayerStates(this, moveSpeed);
            var idle = states.First(s => s.StateID == "Idle");

            SetStates(states);
            SetInitialState(idle);
            _fsm.Start();
        }

        public void BuildStateMachine(float moveSpeed)
        {
            List<BaseState> states = PlayerStateBuilder.PlayerStates(this, moveSpeed);
            BaseState idle = states.First(s => s.StateID == "Idle");
            SetStates(states);
            SetInitialState(idle);
        }

        public bool RequestHarvest(PlayerHarvestRequest request)
        {
            if (request == null || DialogActive || _pendingHarvestRequest != null || ActiveHarvestRequest != null)
            {
                return false;
            }

            _pendingHarvestRequest = request;
            MoveInput = Vector2.Zero;
            RunPressed = false;
            DodgePressed = false;
            AttackPressed = false;
            MeleePressed = false;
            MagicPressed = false;
            DesiredVelocity = Vector2.Zero;
            return true;
        }

        public PlayerHarvestRequest ConsumePendingHarvestRequest()
        {
            PlayerHarvestRequest request = _pendingHarvestRequest;
            _pendingHarvestRequest = null;
            return request;
        }

        public void CompleteActiveHarvestRequest()
        {
            PlayerHarvestRequest request = ActiveHarvestRequest;
            ActiveHarvestRequest = null;
            HarvestComplete = true;
            request?.Completed?.Invoke(request);
        }

        public void SetDialogActive(bool active)
        {
            DialogActive = active;
            if (active)
            {
                _pendingHarvestRequest = null;
                MoveInput = Vector2.Zero;
                RunPressed = false;
                DodgePressed = false;
                AttackPressed = false;
                MeleePressed = false;
                MagicPressed = false;
                DesiredVelocity = Vector2.Zero;
            }
        }

        public void RequestDialogAnimation(string animationKey)
        {
            if (!DialogActive)
            {
                return;
            }

            _dialogAnimationKey = string.IsNullOrWhiteSpace(animationKey) ? "Dialog" : animationKey;
            _dialogAnimationRequested = true;
        }

        public bool ConsumeDialogAnimationRequest(out string animationKey)
        {
            animationKey = _dialogAnimationKey;
            if (!_dialogAnimationRequested)
            {
                return false;
            }

            _dialogAnimationRequested = false;
            return true;
        }

        public void GainExperience(int amount)
        {
            Progression.GainExperience(amount);
        }

        public void ApplyEquipmentModifiers(string sourceId, IEnumerable<ItemEffects> effects)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            RemoveEquipmentModifiers(sourceId);

            List<EquipmentStatModifier> modifiers = BuildEquipmentModifiers(effects);
            if (modifiers.Count == 0)
            {
                return;
            }

            foreach (EquipmentStatModifier modifier in modifiers)
            {
                ApplyEquipmentModifierDelta(modifier.StatKey, modifier.Amount);
            }

            _equipmentModifiersBySource[sourceId] = modifiers;
        }

        public void RemoveEquipmentModifiers(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)
                || !_equipmentModifiersBySource.TryGetValue(sourceId, out List<EquipmentStatModifier> modifiers))
            {
                return;
            }

            foreach (EquipmentStatModifier modifier in modifiers)
            {
                ApplyEquipmentModifierDelta(modifier.StatKey, -modifier.Amount);
            }

            _equipmentModifiersBySource.Remove(sourceId);
        }

        public int GetEquipmentModifierTotal(string statKey)
        {
            string normalized = NormalizeEquipmentStatKey(statKey);
            return _equipmentModifiersBySource.Values
                .SelectMany(modifiers => modifiers)
                .Where(modifier => NormalizeEquipmentStatKey(modifier.StatKey) == normalized)
                .Sum(modifier => modifier.Amount);
        }

        private static List<EquipmentStatModifier> BuildEquipmentModifiers(IEnumerable<ItemEffects> effects)
        {
            List<EquipmentStatModifier> modifiers = new();
            if (effects == null)
            {
                return modifiers;
            }

            foreach (ItemEffects effect in effects)
            {
                switch (effect)
                {
                    case PlusStat plus:
                        modifiers.Add(new EquipmentStatModifier(plus.StatKey, plus.EffectPower));
                        break;
                    case MinusStat minus:
                        modifiers.Add(new EquipmentStatModifier(minus.StatKey, -minus.EffectPower));
                        break;
                }
            }

            return modifiers;
        }

        private void ApplyEquipmentModifierDelta(string statKey, int delta)
        {
            switch (NormalizeEquipmentStatKey(statKey))
            {
                case "maxhp":
                    MaxHP = Mathf.Max(1, MaxHP + delta);
                    CurHP = Mathf.Min(CurHP, MaxHP) - CurHP;
                    break;
                case "maxmana":
                    MaxMana = Mathf.Max(0, MaxMana + delta);
                    CurMana = Mathf.Min(CurMana, MaxMana) - CurMana;
                    break;
                case "strength":
                    Strength += delta;
                    break;
                case "dexterity":
                    Dexterity += delta;
                    break;
                case "intelligence":
                    Intelligence += delta;
                    break;
                case "spirit":
                    Spirit += delta;
                    break;
                case "vitality":
                    Vitality += delta;
                    break;
                case "luck":
                    Luck += delta;
                    break;
                case "magic_range_bonus":
                    break;
                default:
                    GD.Print($"Player equipment modifier '{statKey}' is tracked but has no direct stat binding yet.");
                    break;
            }
        }

        private static string NormalizeEquipmentStatKey(string statKey)
        {
            return (statKey ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "str" => "strength",
                "dex" => "dexterity",
                "int" => "intelligence",
                "spi" => "spirit",
                "vit" => "vitality",
                "luk" => "luck",
                "magicrange" => "magic_range_bonus",
                "magic_range" => "magic_range_bonus",
                "magicrangebonus" => "magic_range_bonus",
                _ => (statKey ?? string.Empty).Trim().ToLowerInvariant()
            };
        }

        private sealed record EquipmentStatModifier(string StatKey, int Amount);
    }
}
