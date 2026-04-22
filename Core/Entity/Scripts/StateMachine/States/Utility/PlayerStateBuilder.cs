


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
            float runSpeed = moveSpeed * 1.8f;
            float dodgeSpeed = moveSpeed * 3.0f;

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

            BaseState run = new BaseState
            (
                "Run",
                owner,
                baseActions: new List<IStateAction>
                {
                    new SetAnimationRequestAction("Run"),
                    new MoveFromInputAction(runSpeed)
                },
                baseTransitions: new List<IStateTransition>()
            );

            BaseState dodge = new BaseState
            (
                "Dodge",
                owner,
                baseActions: new List<IStateAction>
                {
                    new DodgeMoveAction(dodgeSpeed)
                },
                baseTransitions: new List<IStateTransition>()
            );
            

            BaseState attack = new BaseState
            (
                "Attack",
                owner,
                baseActions: new List<IStateAction>
                {
                    new AttackAction()
                },
                baseTransitions: new List<IStateTransition>()
            );
            
            idle.Transitions.Add(new DodgePressedTransition(dodge));
            idle.Transitions.Add(new MoveInputNonZeroTransition(walk));
            idle.Transitions.Add(new RunPressedTransition(run));
            idle.Transitions.Add(new AttackPressedTransition(attack));
            walk.Transitions.Add(new DodgePressedTransition(dodge));
            walk.Transitions.Add(new MoveInputZeroTransition(idle));
            walk.Transitions.Add(new RunPressedTransition(run));
            walk.Transitions.Add(new AttackPressedTransition(attack));
            run.Transitions.Add(new DodgePressedTransition(dodge));
            run.Transitions.Add(new MoveInputZeroTransition(idle));
            run.Transitions.Add(new RunReleasedTransition(walk));
            run.Transitions.Add(new AttackPressedTransition(attack));
            dodge.Transitions.Add(new DodgeCompleteTransition(idle, walk, run));
            attack.Transitions.Add(new AttackCompleteTransition(idle, walk, run));

            return new List<BaseState> {idle, walk, run, dodge, attack};

        }

    }
}
