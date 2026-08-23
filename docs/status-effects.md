# Status Effects

## Overview

Status effects are reusable combat modifiers applied through the existing attack pipeline:

```text
HitBox -> HurtBox -> CombatManager.ResolveAttackPayloadHit -> status runtime
```

Core runtime state is owned by `CombatManager`, not by debug UI or scene scripts. Debug scenes may choose which statuses to add to a player attack packet, but damage, ticking, expiry, movement modifiers, and reflection are resolved centrally.

## Definitions

Status definitions are in `Core/Combat/Scripts/StatusEffectDefinition.cs`.

Each definition can include:

- `StatusEffectId`
- `DisplayName`
- `Description`
- `DurationSeconds`
- `TickIntervalSeconds`
- `DamagePerTick`
- `ManaDamagePerTick`
- `MovementSlowPercent`
- `AttackSlowPercent`
- `PhysicalDamageTakenMultiplier`
- `KnockbackForce`
- `StunDuration`
- `Silences`
- `BlindHitChancePenalty`
- `ThornsReflectPercent`
- `StackBehavior`

Stable IDs currently supported:

- `status.knockback`
- `status.stun`
- `status.armor_break`
- `status.cold`
- `status.burn`
- `status.poison`
- `status.mana_burn`
- `status.silence`
- `status.blind`
- `status.thorns`

Legacy authored aliases such as `Knockback` and `Slow` are normalized through `StatusEffectCatalog`, but new content should use stable `status.*` IDs.

## Runtime State

`CombatManager` tracks active statuses per `Entity`. Runtime records store the normalized ID, definition, stacks, remaining duration, tick timer, and source entity.

Current query methods:

- `HasStatus(statusId)`
- `IsStunned()`
- `IsSilenced()`
- `GetMovementSpeedMultiplier()`
- `GetAttackSpeedMultiplier()`
- `GetPhysicalDamageTakenMultiplier()`
- `GetHitChanceMultiplier()`
- `GetActiveStatusDisplayNames()`

## Attack Application

`AttackPayloadResource.EffectIds` applies statuses from authored combo payloads. `AttackPayloadPacket.AdditionalEffectIds` allows debug or future ability systems to add extra effects without changing the authored weapon resource.

When a player weapon hit lands, `CombatManager.ResolveAttackPayloadHit()` applies damage, then applies payload and additional status IDs to the target. `HitBox` still guards against duplicate hits on the same target during one activation window.

Combo steps can add status IDs through `ComboPhaseResource.StatusEffectsToApply`; these are appended to a cloned payload before the hit resolves. The debug sword finisher uses this to apply `status.knockback`. See `docs/combat-combos.md`.

## Ticks And Expiry

`CombatManager.Resolve(delta)` ticks active statuses each frame. Tick damage emits combat feedback so existing debug enemies can show floating damage text. Expired statuses are removed and logged through `[StatusDebug]`.

## Status Behavior

- Knockback: applies a short impulse away from the source.
- Stun: stops movement through `ResolveMovementVelocity()` and prevents debug enemies from entering/continuing attack activation.
- Armor Break: increases physical damage taken by 25%.
- Cold: reduces movement speed by 35%; attack speed multiplier is exposed for later integration.
- Burn: deals damage over time and adds physical vulnerability.
- Poison: deals damage over time.
- Mana Burn: drains mana over time; targets without mana log a safe debug fallback.
- Silence: tracks active silence and exposes `IsSilenced()`.
- Blind: reduces hit chance through `CanHit()`.
- Thorns: reflects a portion of physical damage and tags reflected damage to prevent reflection loops.

## Debug Toggles

`CombatDebugSceneRoot` creates a debug-only `Attack Effects` toggle panel under the combat debug HUD. Enabled toggles append stable status IDs to outgoing player attack packets. Thorns is labeled as target-applied: the hit gives the enemy thorns, then subsequent physical hits can reflect damage.

The debug UI does not own status behavior.

## Adding A Status

1. Add a stable `status.*` ID to `StatusEffectCatalog`.
2. Add a `StatusEffectDefinition`.
3. Add it to `CombatPayloadSchema.KnownEffectIds`.
4. Integrate any new runtime modifier in `CombatManager`.
5. Add a debug toggle if it needs manual combat testing.

## Reuse By Equipment And Abilities

Weapons should use `AttackPayloadResource.EffectIds`. Future armor, passive abilities, and skill tree effects can add status IDs to `AttackPayloadPacket.AdditionalEffectIds` or call `CombatManager.ApplyStatus()` directly when they have a target and source.

The first combo passive integration is `passive.combo.third_hit_knockback`: when unlocked, `CombatManager` applies knockback on combo phase 3.

## Manual Test Checklist

- Open `CombatDebugScene`.
- Enable each debug toggle and hit a test enemy.
- Confirm `[StatusDebug] Applied ...` logs appear.
- Confirm active status text appears near the enemy state label.
- Confirm Burn and Poison tick damage.
- Confirm Mana Burn logs a no-mana fallback on test enemies.
- Confirm Armor Break increases later physical hit damage.
- Confirm Cold slows movement.
- Confirm Stun interrupts movement/attack timing.
- Confirm Blind can cause misses.
- Confirm Thorns reflects later physical hits without looping.

## Current Limitations

- Status runtime is not saved or loaded.
- Attack speed slow is exposed but not yet consumed by attack action timing.
- Silence is query-ready, but a magic casting system is not fully wired to it yet.
- Blind uses a simple combat hit chance multiplier rather than a full accuracy/evasion model.
- Debug status indicators are text only.

## Future Follow-Ups

- polished status icons
- save/load active statuses if needed
- resistance/immunity system
- status stacking rules beyond the current simple modes
- elemental damage types
- accuracy/evasion system for Blind
- magic casting integration for Silence
- armor/weapon passive effect integration
- skill tree effect integration
