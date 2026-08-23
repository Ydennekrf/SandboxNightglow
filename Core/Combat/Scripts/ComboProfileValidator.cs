using System;
using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    public enum ComboValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct ComboValidationIssue
    {
        public ComboValidationSeverity Severity { get; }
        public string Message { get; }

        public ComboValidationIssue(ComboValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message ?? string.Empty;
        }
    }

    public static class ComboProfileValidator
    {
        public static List<ComboValidationIssue> Validate(WeaponComboResource combo, string comboLabel = "ComboProfile")
        {
            List<ComboValidationIssue> issues = new();

            if (combo == null)
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Error, $"{comboLabel}: combo profile resource is null."));
                return issues;
            }

            if (combo.Phases == null || combo.Phases.Count == 0)
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Error, $"{comboLabel}: no phases configured."));
                return issues;
            }

            if (string.IsNullOrWhiteSpace(combo.ComboId))
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{comboLabel}: ComboId is empty."));
            }

            for (int i = 0; i < combo.Phases.Count; i++)
            {
                ComboPhaseResource phase = combo.Phases[i];
                string prefix = $"{comboLabel} phase {i + 1}";

                if (phase == null)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Error, $"{prefix}: phase reference is null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(phase.StepId))
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{prefix}: StepId is empty."));
                }

                if (phase.StepIndex > 0 && phase.StepIndex != i + 1)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{prefix}: StepIndex is {phase.StepIndex}, expected {i + 1}."));
                }

                if (phase.AuthoredDurationSeconds > 0f && phase.DurationOverrideSeconds > 0f)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Info, $"{prefix}: DurationOverrideSeconds will take precedence over startup/active/recovery total."));
                }

                if (phase.ComboWindowEndSeconds > 0f && phase.ComboWindowEndSeconds < phase.ComboWindowStartSeconds)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{prefix}: combo window end is before combo window start."));
                }

                if (phase.ChargeSeconds > 0f && phase.MaxChargeSeconds > 0f && phase.MaxChargeSeconds < phase.ChargeSeconds)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{prefix}: MaxChargeSeconds is lower than ChargeSeconds."));
                }

                if (phase.ChargedDamageMultiplier < 1f)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{prefix}: ChargedDamageMultiplier below 1 will make charged hits weaker."));
                }

                ValidateHitboxProfile(issues, phase.HitboxProfile, prefix);

                if (phase.MeleePayload == null && phase.MagicPayload == null)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Error, $"{prefix}: missing payload refs (both melee and magic are null)."));
                    continue;
                }

                if (phase.MeleePayload == null)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{prefix}: melee payload is null (runtime will fallback to magic payload)."));
                }

                if (phase.MagicPayload == null)
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Info, $"{prefix}: magic payload is null (phase is melee-only)."));
                }

                ValidatePayload(issues, phase.MeleePayload, prefix, "MeleePayload");
                ValidatePayload(issues, phase.MagicPayload, prefix, "MagicPayload");
            }

            return issues;
        }

        public static void EmitToGodotLog(WeaponComboResource combo, string comboLabel)
        {
            List<ComboValidationIssue> issues = Validate(combo, comboLabel);
            foreach (ComboValidationIssue issue in issues)
            {
                string msg = $"ComboProfileValidator: {issue.Message}";
                if (issue.Severity == ComboValidationSeverity.Error)
                {
                    GD.PushError(msg);
                }
                else if (issue.Severity == ComboValidationSeverity.Warning)
                {
                    GD.PushWarning(msg);
                }
                else
                {
                    GD.Print(msg);
                }
            }
        }

        private static void ValidatePayload(List<ComboValidationIssue> issues, AttackPayloadResource payload, string phasePrefix, string payloadSlot)
        {
            if (payload == null)
            {
                return;
            }

            string payloadPrefix = $"{phasePrefix} {payloadSlot}";

            if (string.IsNullOrWhiteSpace(payload.DeliveryShapeId))
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{payloadPrefix}: DeliveryShapeId is empty, runtime defaults to 'SingleTarget'."));
            }
            else if (!CombatPayloadSchema.IsKnownDeliveryShape(payload.DeliveryShapeId))
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{payloadPrefix}: unknown DeliveryShapeId '{payload.DeliveryShapeId}'."));
            }

            if (payload.EffectIds == null)
            {
                return;
            }

            for (int i = 0; i < payload.EffectIds.Count; i++)
            {
                string effectId = payload.EffectIds[i]?.Trim();
                if (string.IsNullOrWhiteSpace(effectId))
                {
                    continue;
                }

                if (!CombatPayloadSchema.IsKnownEffectId(effectId))
                {
                    issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{payloadPrefix}: unknown effect id '{effectId}'."));
                }
            }
        }

        private static void ValidateHitboxProfile(List<ComboValidationIssue> issues, HitboxProfileResource profile, string phasePrefix)
        {
            if (profile == null)
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Info, $"{phasePrefix}: HitboxProfile is null; runtime will use PlayerNode fallback hitbox tuning."));
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{phasePrefix}: HitboxProfile ProfileId is empty."));
            }

            if (profile.ShapeType == HitboxShapeType.Circle && profile.Radius <= 0f)
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{phasePrefix}: circle hitbox radius must be greater than zero."));
            }

            if (profile.ShapeType == HitboxShapeType.Rectangle && (profile.Size.X <= 0f || profile.Size.Y <= 0f))
            {
                issues.Add(new ComboValidationIssue(ComboValidationSeverity.Warning, $"{phasePrefix}: rectangle hitbox size must be greater than zero."));
            }
        }
    }
}
