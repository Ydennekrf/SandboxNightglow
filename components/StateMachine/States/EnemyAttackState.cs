// EnemyAttackState.cs
using Godot;

public partial class EnemyAttackState : BaseState
{
    private ComboTracker _tracker;
    private ComboPhase   _phase;

    [Export] public NodePath WeaponPath = "WeaponHolder/Weapon";

    public override void Enter(Entity owner, BaseState from)
    {
        var fsm = owner.GetNode<StateMachine>("StateMachine");
        _tracker = fsm.MeleeTracker;      // always melee for AI
        _tracker.Reset();                 // start from first phase

        StartPhase(owner, _tracker.Current);
    }

    public override BaseState Tick(Entity owner, float dt, BaseState self)
    {
        _phase.Execute(dt, owner, this);

        // single-phase AI: leave when anim finishes
        if (_phase.IsDone)
            return owner.fsm.GetState(StateType.Chase);   // or Idle

        return this;
    }

    /* ───────────────────────────────────────────── */
    private void StartPhase(Entity owner, ComboPhase phase)
    {
        owner.ActivePhase = phase;        // 1) mark current
        phase.Enter(owner, this);         // 2) play clip / effects

        // 3) let the weapon move+enable hitbox
        var weapon = owner.GetNodeOrNull<WeaponBase>(WeaponPath);
        weapon?.ActivateHitBox();
        _phase = phase;
    }
}
