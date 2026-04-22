using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class WeaponComboResource : Resource
    {
        [Export] public Godot.Collections.Array<ComboPhaseResource> Phases { get; set; } = new();

        public ComboPhaseResource GetPhaseForStep(int phase)
        {
            if (Phases == null || Phases.Count == 0)
            {
                return null;
            }

            int clampedPhase = Mathf.Max(1, phase);
            int idx = Mathf.Min(clampedPhase - 1, Phases.Count - 1);
            return Phases[idx];
        }
    }
}
