

using System.Collections.Generic;

namespace ethra.V1
{
    public interface IStateMachine
    {
        void SetOwner(Entity owner);

        void SetStates(List<BaseState> states);
        void SetInitialState(BaseState state);

        void Start();

        void Advance(float delta);

        BaseState GetState(string stateID);
        BaseState GetCurrentState();
    }
}