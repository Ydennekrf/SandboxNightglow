using Godot;

public partial class AttackState : BaseState
{
    private ComboTracker _active;

    public override void Enter(Entity owner, BaseState from)
    {
        // decide which button launched the state
        if (Input.IsActionPressed("attack_magic"))
        {
            _active = owner.GetNode<StateMachine>("StateMachine").MagicTracker;
            SetInputAction("attack_magic");
        }
        else
        {
            _active = owner.GetNode<StateMachine>("StateMachine").MeleeTracker;
            SetInputAction("attack_melee");
        }

        // remember for other states if you need
        owner.GetNode<StateMachine>("StateMachine").LastAttackButton = InputActionName;

        // reset combo if timer expired
        if (_active.TimerRemaining <= 0)
            _active.Reset();

        StartPhase(_active.Current);
    }

    public override BaseState Tick(Entity owner, float delta, BaseState self)
    {
        _active.Current.Execute(delta, owner, this);

        if (_active.Current.QueueNext)
        {
            _active.Index++;
            if (_active.Index < _active.Phases.Count)
                StartPhase(_active.Current);
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

    private void StartPhase(ComboPhase phase)
    {
        phase.Enter(_ownerCache, this);
        _active.TimerRemaining = phase.ComboWindow;
    }
}