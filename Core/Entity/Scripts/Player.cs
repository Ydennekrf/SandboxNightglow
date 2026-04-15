
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ethra.V1
{
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
                {
                    Facing = _moveInput.X > 0 ? FacingDirection.Right : FacingDirection.Left;
                }
                else
                {
                    Facing = _moveInput.Y > 0 ? FacingDirection.Down : FacingDirection.Up;
                }
                    
            }
        }

        public bool AttackPressed { get; set; } = false;

        public Player(IEntityManager entity, ICombat combat, IInventory inventory, IStateMachine fsm): base (entity, combat, fsm)
        {
            _inventory = inventory;
   
        }

        public override void Initialize()
        {
            // Build player states here — leaf responsibility
            var gm = GameManager.Instance; // see patch #6 below


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