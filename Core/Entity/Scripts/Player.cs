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
        public AttackInputType PendingAttackInput { get; set; } = AttackInputType.Melee;
        public AttackOverlayMode CurrentAttackOverlay { get; set; } = AttackOverlayMode.None;
        public AttackPayloadResource CurrentAttackPayload { get; set; } = null;
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
    }
}
