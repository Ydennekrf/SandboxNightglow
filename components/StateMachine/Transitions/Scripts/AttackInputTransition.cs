using Godot;

public partial class AttackInputTransition : Node, IStateTransition
{
    [Export] public NodePath TargetStatePath;          // drag & drop AttackState
    public BaseState Target => GetNode<BaseState>(TargetStatePath);


    /* Optional: stamina cost check */
    [Export] public int StaminaCost = 0;

    private bool PlayerHasWeapon(Entity owner)
        {
            var wc = owner.GetNode<WeaponComponent>("WeaponComponent");
            return wc != null && wc._current != null;   // Current is the equipped scene
        }

    public bool ShouldTransition(Entity owner)
    {
        bool pressedMelee = Input.IsActionJustPressed("Melee");
        bool pressedMagic = Input.IsActionJustPressed("Magic");
        if (!pressedMelee && !pressedMagic) return false;

        /* ── block magic if weapon lacks MagicTracker ── */
        if (pressedMagic)
        {
                  // safer than hard path
            if (owner.fsm.MagicTracker == null || owner.fsm.MagicTracker.Phases.Count == 0)
                return false;
        }

        if (pressedMelee)
        {
                  // safer than hard path
            if (owner.fsm.MeleeTracker == null || owner.fsm.MeleeTracker.Phases.Count == 0)
                return false;
        }

        return true; 
    }
}