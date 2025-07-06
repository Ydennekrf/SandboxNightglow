using Godot;

public partial class StunnedState : BaseState
{
    private float _timer;
    public StunnedState(float duration) => _timer = duration;

    public override void Enter(Entity owner, BaseState from)
    {
                      // optional: ignore damage during stun
        owner._anim.Play("Stunned");
    }

    public override BaseState Tick(Entity owner, float delta, BaseState baseState)
    {
        _timer -= delta;
        return _timer <= 0 ? owner.fsm.GetState(StateType.Idle) : this;
    }

    public override void Exit(Entity owner) => owner.SetImmune(false);
}