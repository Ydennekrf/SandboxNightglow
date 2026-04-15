


using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public class BaseState 
    {
       private string _stateID;

       private Entity _owner;

       private List<IStateAction> _actions;
       private List<IStateAction> _runtimeActions;
       private List<IStateTransition> _transitions;

       public string StateID {get{return _stateID;}}
       public Entity Owner {get{return _owner;}}

       public IEnumerable<IStateAction> Actions =>
        _actions.Concat(_runtimeActions);
       public List<IStateTransition> Transitions{get {return _transitions;}}


        public BaseState(string stateId, Entity owner, List<IStateAction> baseActions, List<IStateTransition> baseTransitions)
        {
            _stateID = stateId;
            _owner = owner;
            _actions = baseActions ?? new List<IStateAction>();
            _transitions = baseTransitions ?? new List<IStateTransition>();
            _runtimeActions = new List<IStateAction>();
        }
       public virtual void Enter(BaseState state)
        {
            foreach(var a in Actions) a.Enter(Owner, state);
        }

        public virtual BaseState Tick(float delta, BaseState prevState)
        {
            foreach(var a in Actions) a.Execute(delta, Owner, this);

            foreach(var t in Transitions)
            {
                if(t.ShouldTransition(Owner) && t.Target != this)
                {
                    return t.Target;
                }
            }
            return this;
        }

        public virtual void Exit()
        {
            foreach(var a in Actions) a.Exit(Owner);


        }

        public void AddAction(IStateAction action, bool runOnEnter = false)
        {
            if(action == null) return;

            if (_actions.Contains(action) || _runtimeActions.Contains(action)) return;

            _runtimeActions.Add(action);

            if(runOnEnter && Owner.GetCurrentState() == this)
            {
                action.Enter(Owner, this);
            }
        }


        public void RemoveAction(IStateAction action, bool runOnExit = false)
        {
            if(action == null) return;
            if (!_runtimeActions.Contains(action)) return;

            _runtimeActions.Remove(action);

            if(runOnExit && Owner.GetCurrentState() == this)
            {
                action.Exit(Owner);
            }
        }


    }
}