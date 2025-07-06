using Godot;

public partial class StunPhaseAction : Node, IPhaseAction
{
    [Export] public float Duration = 1.5f;

    private Entity _owner;
    private BaseState _hurtState;

    public void OnPhaseStart(Entity owner, ComboPhase phase)
    {
        _owner = owner;
        _hurtState = owner.fsm.GetState(StateType.Hurt);
        EventManager.I.Subscribe<HitEvent>(GameEvent.Hit, OnHit);
    }

    public void OnPhaseEnd(Entity o, ComboPhase _)
    {
        EventManager.I.Unsubscribe<HitEvent>(GameEvent.Hit, OnHit);
        _owner     = null;
        _hurtState = null;
    }

    private void OnHit(HitEvent payload)
    {
        if (payload.attacker != _owner) return;

        payload.target.fsm.PushState(_hurtState);

        
        payload.target.CreateTween()
                .TweenCallback(Callable.From(() => payload.target.fsm.PopState()))
                .SetDelay(Duration);
    }
}