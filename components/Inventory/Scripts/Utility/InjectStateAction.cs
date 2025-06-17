using Godot;

/// Persistently modifies a single state while the item is equipped.
/// Implements IEquippableAction (see Section 3).
[GlobalClass]
public partial class InjectStateAction : Resource, IEquippableAction
{
    [Export] public StateType TargetState;
    [Export] public PackedScene ActionScene;   // scene that instantiates an IStateAction

    private IStateAction _runtimeAction;       // cached instance so we can remove it later

    public void OnEquip(Entity user, StateMachine fsm, InventoryItem item)
    {
        var state = fsm.GetState(TargetState);
        if (state is null)
        {
            GD.PushWarning($"[InjectStateAction] State {TargetState} not found.");
            return;
        }

        _runtimeAction = ActionScene.Instantiate() as IStateAction;
        if (_runtimeAction is null)
        {
            GD.PushWarning("[InjectStateAction] ActionScene is not an IStateAction.");
            return;
        }

        state.AddAction(_runtimeAction, runEnter: true);
        GD.Print($"[Inject] Added {_runtimeAction.GetType().Name} to {TargetState}");
    }

    public void OnUnequip(Entity user, StateMachine fsm, InventoryItem item)
    {
        if (_runtimeAction is null) return;

        var state = fsm.GetState(TargetState);
        state?.RemoveAction(_runtimeAction, runExit: true);
        _runtimeAction = null;
        GD.Print($"[Inject] Removed action from {TargetState}");
    }
}