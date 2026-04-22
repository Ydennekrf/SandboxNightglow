using System.Text;
using Godot;

namespace ethra.V1
{
    [Tool]
    public partial class ComboProfilePreviewTool : Node
    {
        [Export] public WeaponComboResource ComboProfile { get; set; }
        [Export(PropertyHint.Range, "1,12,1")] public int PreviewPhase { get; set; } = 1;
        [Export] public AttackInputType PreviewInput { get; set; } = AttackInputType.Melee;
        [Export] public bool AutoRefresh { get; set; } = true;
        [Export] public bool RefreshNow { get; set; } = false;

        [Export(PropertyHint.MultilineText)]
        public string PreviewReport { get; set; } = "Assign ComboProfile and enable AutoRefresh.";

        public override void _Process(double delta)
        {
            if (!Engine.IsEditorHint())
            {
                return;
            }

            if (AutoRefresh || RefreshNow)
            {
                RefreshNow = false;
                BuildReport();
            }
        }

        private void BuildReport()
        {
            StringBuilder sb = new();
            string label = ComboProfile?.ResourcePath;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = "(embedded resource)";
            }

            sb.AppendLine($"Combo: {label}");
            sb.AppendLine($"Preview selection: phase={PreviewPhase}, input={PreviewInput}");

            if (ComboProfile == null)
            {
                sb.AppendLine("No combo profile assigned.");
                PreviewReport = sb.ToString();
                return;
            }

            ComboPhaseResource phase = ComboProfile.GetPhaseForStep(PreviewPhase);
            if (phase == null)
            {
                sb.AppendLine("Selected phase did not resolve.");
            }
            else
            {
                AttackPayloadResource payload = PreviewInput == AttackInputType.Magic
                    ? (phase.MagicPayload ?? phase.MeleePayload)
                    : (phase.MeleePayload ?? phase.MagicPayload);

                sb.AppendLine($"Resolved animation: {phase.SharedAnimationName}");
                sb.AppendLine($"Resolved payload: {DescribePayload(payload)}");
                sb.AppendLine($"Windows active=[{phase.ActiveWindowStart:0.###},{phase.ActiveWindowEnd:0.###}] buffer=[{phase.BufferWindowStart:0.###},{phase.BufferWindowEnd:0.###}]");
            }

            var issues = ComboProfileValidator.Validate(ComboProfile, "PreviewCombo");
            sb.AppendLine($"Validation issues: {issues.Count}");
            foreach (var issue in issues)
            {
                sb.AppendLine($"- [{issue.Severity}] {issue.Message}");
            }

            PreviewReport = sb.ToString();
        }

        private static string DescribePayload(AttackPayloadResource payload)
        {
            if (payload == null)
            {
                return "(null payload)";
            }

            string effectList = payload.EffectIds == null || payload.EffectIds.Count == 0
                ? "none"
                : string.Join(",", payload.EffectIds);

            return $"overlay={payload.OverlayMode}, mana={payload.ManaCost}, shape={payload.DeliveryShapeId}, damage={payload.DamageType}, element={payload.ElementType}, basePower={payload.BasePower:0.###}, effects={effectList}, duration={payload.EffectDurationSeconds:0.###}";
        }
    }
}
