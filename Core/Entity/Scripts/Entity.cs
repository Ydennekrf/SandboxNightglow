

using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    public enum FacingDirection { Down, Left, Right, Up }

    public abstract class Entity : IEntity
    {
        



        //========== Fields and Properties ============//
        protected string _name;
        protected IStateMachine _fsm;
        protected IEntityManager _entity;

        protected Vector2 _desiredVelocity;

        public string Name => _name;

        public FacingDirection Facing {get; set;}


        public Vector2 DesiredVelocity { get{return _desiredVelocity;} set{_desiredVelocity = value;} } 
        public string RequestedAnimation { get; set; } = "Idle_Down";


        public Entity(IEntityManager entity, IStateMachine fsm)
        {
            _entity = entity;
            _fsm = fsm;
            _fsm.SetOwner(this);
        }
        

            



        // notes this will require a collision manager set up and the dialog component created. 


        //======= public Interface Methods =========//
        public void CheckCollision()
        {
            throw new System.NotImplementedException();
        }

        public abstract void Initialize();

        public void SetStats(int ID)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateDialog(int EntityID, int NodeID)
        {
            throw new System.NotImplementedException();
        }

        public BaseState GetCurrentState()
        {
            return _fsm.GetCurrentState();
        }

        public void SetStates(List<BaseState> states)
        {
            _fsm.SetStates(states);
        }
        public void SetInitialState(BaseState state)
        {
            _fsm.SetInitialState(state);
        }

        public void Tick(float delta)
        {
            _fsm.Advance(delta);
        }
        
    }
}