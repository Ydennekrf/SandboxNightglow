

using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public class StateMachine : IStateMachine
    {
        private BaseState _currentState;
        private BaseState _prevState;
        private Entity _owner;

        private List<BaseState> _stateTree = new();

        

        public StateMachine()
        {
        }

		public StateMachine(ICollection<BaseState> states, Entity owner, BaseState initialState)
		{
			Initialize(states, owner, initialState);
		}

		public void Initialize(ICollection<BaseState> states, Entity owner, BaseState initialState)
		{
			_owner = owner;
			_stateTree = states?.ToList() ?? new List<BaseState>();
			_currentState = initialState;
		}

		public void Start()
		{
			_currentState?.Enter(_currentState);
		}


		public void Advance(float time)
        {
            BaseState next = _currentState?.Tick(time, _currentState);
            if(next != null && next != _currentState)
            {
                _currentState.Exit();
                _prevState = _currentState;
                _currentState = next;
                _currentState.Enter(_prevState);
            }
        }

        public BaseState GetState(string id)
        {
            foreach(BaseState state in _stateTree)
            {
                if(state.StateID == id)
                {
                    return state;
                }
                
            }
            return null;
        }

        public BaseState GetCurrentState()
        {
            return _currentState;
        }

        public void SetStates(List<BaseState> states)
        {
            _stateTree = states;
        }

        public void SetInitialState(BaseState state)
        {
            _currentState = state;
        }

        public void SetOwner(Entity owner)
        {
            _owner = owner;
        }

        
    }
}