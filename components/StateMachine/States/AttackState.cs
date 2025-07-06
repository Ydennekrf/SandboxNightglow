using Godot;

public partial class AttackState : BaseState
{
    private ComboTracker _active;
    private ComboPhase _current;

    public override void Enter(Entity owner, BaseState from)
    {
        
        var fsm = owner.GetNode<StateMachine>("StateMachine");
        // decide which button launched the state
        if (Input.IsActionPressed("Magic"))
        {
            _active = fsm.MagicTracker;
            SetInputAction("Magic");
        }
        else
        {
            _active = fsm.MeleeTracker;
            SetInputAction("Melee");
        }

        // remember for other states if you need
        fsm.LastAttackButton = InputActionName;
        

        // reset combo if timer expired
        if (_active.TimerRemaining <= 0)
            _active.Reset();

        StartPhase(owner, _active.Current);
    }

    public override BaseState Tick(Entity owner, float delta, BaseState self)
    {
        _active.Current.Execute(delta, owner, this);

        if (_active.Current.QueueNext)
        {
            _active.Index++;
            if (_active.Index < _active.Phases.Count)
                StartPhase(owner, _active.Current);
            else
                _active.Reset();
        }
        else if (_active.Current.IsDone)
        {
            _active.Reset();
            return owner.fsm.GetState(StateType.Idle);     // transition out
        }

        return this;
    }

    private void StartPhase(Entity owner,ComboPhase phase)
    {
        phase.Enter(owner, this);
        owner.ActivePhase = phase;
        _active.TimerRemaining = phase.ComboWindow;
    }
}