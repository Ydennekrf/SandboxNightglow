using Godot;
using System;

namespace ethra.V1
{
    /// <summary>
    /// Logging-first feedback bus for combat presentation hooks.
    /// Current implementation is intentionally lightweight for pixel-art animation-layer workflows.
    /// </summary>
    public static class CombatFeedbackBus
    {
        public static bool DebugLoggingEnabled { get; set; } = true;

        public static event Action<Player, int, string, AttackOverlayMode> AttackPhaseStarted;
        public static event Action<Player, bool, float, float> ActiveWindowChanged;
        public static event Action<Player, bool, float, float> BufferWindowChanged;
        public static event Action<AttackPayloadPacket> PayloadQueued;
        public static event Action<Player, int, int> MagicDenied;
        public static event Action<Entity, Entity, float, bool, string, string> HitResolved;
        public static event Action<Entity, string, float> EffectApplied;

        public static void EmitAttackPhaseStarted(Player player, int phase, string clip, AttackOverlayMode overlay)
        {
            Log($"AttackPhaseStarted: source={player?.Name} phase={phase} clip='{clip}' overlay={overlay}");
            AttackPhaseStarted?.Invoke(player, phase, clip, overlay);
        }

        public static void EmitActiveWindowChanged(Player player, bool isOpen, float elapsed, float duration)
        {
            Log($"ActiveWindowChanged: source={player?.Name} open={isOpen} t={elapsed:0.###}/{duration:0.###}");
            ActiveWindowChanged?.Invoke(player, isOpen, elapsed, duration);
        }

        public static void EmitBufferWindowChanged(Player player, bool isOpen, float elapsed, float duration)
        {
            Log($"BufferWindowChanged: source={player?.Name} open={isOpen} t={elapsed:0.###}/{duration:0.###}");
            BufferWindowChanged?.Invoke(player, isOpen, elapsed, duration);
        }

        public static void EmitPayloadQueued(AttackPayloadPacket packet)
        {
            Log($"PayloadQueued: source={packet?.Source?.Name} phase={packet?.ComboPhase} shape={packet?.Payload?.DeliveryShapeId}");
            PayloadQueued?.Invoke(packet);
        }

        public static void EmitMagicDenied(Player player, int required, int current)
        {
            Log($"MagicDenied: source={player?.Name} required={required} current={current}");
            MagicDenied?.Invoke(player, required, current);
        }

        public static void EmitHitResolved(Entity source, Entity target, float damage, bool crit, string damageType, string element)
        {
            Log($"HitResolved: source={source?.Name} target={target?.Name} damage={damage:0.###} crit={crit} type={damageType} element={element}");
            HitResolved?.Invoke(source, target, damage, crit, damageType, element);
        }

        public static void EmitEffectApplied(Entity target, string effectId, float duration)
        {
            Log($"EffectApplied: target={target?.Name} effect={effectId} duration={duration:0.###}");
            EffectApplied?.Invoke(target, effectId, duration);
        }

        private static void Log(string message)
        {
            if (!DebugLoggingEnabled)
            {
                return;
            }

            GD.Print($"[CombatFeedback] {message}");
        }
    }
}
