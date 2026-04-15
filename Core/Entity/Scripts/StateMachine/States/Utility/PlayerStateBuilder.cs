


using System.Collections.Generic;
using ethra.V1.Actions;
using ethra.V1.Transitions;

namespace ethra.V1
{
    public static class PlayerStateBuilder
    {
        public static List<BaseState> PlayerStates(Entity owner, float moveSpeed)
        {
            List<BaseState> stateStack = [.. PlayerLocomotion(owner, moveSpeed)];

            return stateStack;
        }

        private static List<BaseState> PlayerLocomotion(Entity owner, float moveSpeed)
        {
            BaseState idle = new BaseState
            (
                "Idle",
                owner,
                baseActions: new List<IStateAction>
                {
                    new SetAnimationRequestAction("Idle"),
                    new StopMovingAction()
                },
                baseTransitions: new List<IStateTransition>()
            );

            BaseState walk = new BaseState
            (
                "Walk",
                owner,
                baseActions: new List<IStateAction>
                {
                    new SetAnimationRequestAction("Walk"),
                    new MoveFromInputAction(moveSpeed)
                },
                baseTransitions: new List<IStateTransition>()
            );
            

            idle.Transitions.Add(new MoveInputNonZeroTransition(walk));
            walk.Transitions.Add(new MoveInputZeroTransition(idle));

            return new List<BaseState> {idle, walk};
        }

    }
}