using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class WeaponComboResource : Resource
    {
        [Export] public string ComboId { get; set; } = string.Empty;
        [Export] public string DisplayName { get; set; } = string.Empty;
        [Export] public string WeaponType { get; set; } = string.Empty;
        [Export] public Godot.Collections.Array<int> CompatibleWeaponIds { get; set; } = new();
        [Export] public float ResetTimeSeconds { get; set; } = 0.85f;
        [Export] public bool CanLoop { get; set; } = false;
        [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
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

        public bool HasNextPhase(int phase)
        {
            return Phases != null && phase >= 1 && phase < Phases.Count;
        }
    }
}
