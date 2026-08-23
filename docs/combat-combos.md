# Combat Combos

## Overview

Player weapon combos are authored as Godot resources and resolved through the existing player FSM and combat payload path.

Runtime flow:

```text
WeaponItem.ComboProfile -> WeaponComboResource -> ComboPhaseResource -> AttackAction -> PlayerNode HitBox -> HurtBox -> CombatManager
```

`CombatDebugScene` is only a test harness. It equips a debug weapon, shows combo status, and can append debug status IDs to outgoing attack packets. Combo timing, hitboxes, damage, and effects stay in reusable combat/player code.

## Loading

Weapons load combo profiles from `combo_profile_path` in `Core/Inventory/Data/items_seed.csv`. The path is stored on `WeaponItem.ComboProfilePath` and loaded lazily as `WeaponItem.ComboProfile`.

If the equipped weapon has no valid combo profile, `AttackAction` falls back to:

```text
res://Core/Combat/Data/Combos/basic_attack_combo.tres
```

That fallback also covers no weapon equipped and old weapon data with an empty or invalid combo path.

## Weapon Selection

`AttackAction` resolves the equipped `MainHand` weapon from `InventoryManager.GetEquippedWeapons()`. If that weapon has a `WeaponComboResource` with phases, that combo is used. Otherwise the basic fallback combo is used.

The debug combo example is:

```text
combo.weapon.debug_sword.basic_three_hit
```

It is assigned to item `2003`, `Debug Socket Sword`, and CombatDebugScene equips item `2003`.

Additional authored examples:

- `2004` Ember Scepter: `combo.weapon.ember_scepter.area_magic`, magic cone into `CircleWaveAwayFromPlayer` area nova.
- `2005` Stonebreaker Maul: `combo.weapon.stonebreaker_maul.charge_slam`, sweep into hold-to-charge slam.
- `2006` Gale Knife: `combo.weapon.gale_knife.looping_flurry`, fast mobile combo with `CanLoop`.

## WeaponComboResource Fields

- `ComboId`: stable combo ID.
- `DisplayName`: editor/debug name.
- `WeaponType`: optional compatible weapon type label.
- `CompatibleWeaponIds`: optional numeric item IDs for current CSV-based weapons.
- `ResetTimeSeconds`: intended reset grace after the chain ends. Runtime currently exits the attack state after recovery and starts from step 1 on the next fresh attack.
- `CanLoop`: allows the last step to advance back to step 1 when buffered.
- `Description`: authoring notes.
- `Phases`: ordered `ComboPhaseResource` step list.

## ComboPhaseResource Fields

`ComboPhaseResource` is the current project equivalent of a combo step definition.

- `StepId`: stable step ID.
- `StepIndex`: optional authoring check against array order.
- `InputAction`: input action label, usually `Attack`.
- `DebugLabel`: label shown in debug logs/UI.
- `DamageMultiplier`: multiplies the selected attack payload `BasePower`.
- `SharedAnimationName`: requested animation hook.
- `PreferFacingSuffix`: tries names like `Slash1_Down` before `Slash1`.
- `DurationOverrideSeconds`: overrides animation length and authored total timing.
- `StartupSeconds`, `ActiveSeconds`, `RecoverySeconds`: authored timing when animations are missing or overridden.
- `ComboWindowStartSeconds`, `ComboWindowEndSeconds`: input buffer window for the next step.
- `MovementLock`: keeps player movement locked during the step.
- `MovementImpulse`: optional lunge along facing direction.
- `ChargeSeconds`: hold duration required for a charged release.
- `MaxChargeSeconds`: maximum hold duration before the attack releases automatically. If `0`, `ChargeSeconds` is used.
- `ChargedDamageMultiplier`: extra multiplier applied only when released charged.
- `HitboxProfile`: optional `HitboxProfileResource`.
- `StatusEffectsToApply`: stable `status.*` IDs appended to the payload.
- `ChargedStatusEffectsToApply`: stable `status.*` IDs appended only on charged release.
- `AbilityEffectsToApply`: reserved for future ability effect routing.
- `MeleePayload`, `MagicPayload`: payloads selected by input type.

Legacy fields `ActiveWindowStart`, `ActiveWindowEnd`, `BufferWindowStart`, and `BufferWindowEnd` are still supported for old resources.

## HitboxProfileResource Fields

- `ProfileId`: stable hitbox profile ID.
- `ShapeType`: `Rectangle` or `Circle`.
- `DirectionalBehavior`: `FacingDirection` or `FixedOffset`.
- `Offset`: facing-distance on X plus side offset on Y when directional.
- `Size`: rectangle size.
- `Radius`: circle radius.
- `ActiveDurationSeconds`: player hitbox active time for this step.

If a step has no hitbox profile, `PlayerNode` uses its exported fallback `AttackHitBoxOffset` and `AttackHitBoxActiveSeconds`.

## Timing And Input Windows

`AttackAction` starts a step when the Attack state is entered. During each physics tick it advances `CurrentPhaseElapsed`, opens/closes the active hitbox window, and captures melee or magic input only while the combo window is open.

When recovery completes:

- buffered input inside the window advances to the next step
- no buffered input exits the attack state
- no next step exits unless `CanLoop` is enabled

Mashing does not advance outside the configured combo window.

Charge steps pause their active/combo windows while the matching input is held. Releasing before `ChargeSeconds` fires the uncharged attack. Holding through `ChargeSeconds` applies `ChargedDamageMultiplier` and `ChargedStatusEffectsToApply`; holding until `MaxChargeSeconds` auto-releases.

## Animation Safety

The runtime first tries the authored animation name, optionally with facing suffix. If the animation is missing, it keeps the combo step, payload, hitbox, and authored timing, then falls back to an existing attack animation for playback timing when possible.

Missing animation warnings are logged once per requested/fallback pair.

## Damage And Effects

Each step selects `MeleePayload` or `MagicPayload`. The selected payload is cloned before runtime modifiers are applied. This prevents one attack from mutating the authored resource for later attacks.

Step `DamageMultiplier` multiplies payload `BasePower`. `StatusEffectsToApply` is appended to payload `EffectIds` and resolved by `CombatManager.ResolveAttackPayloadHit()`. Charged releases add `ChargedDamageMultiplier` and `ChargedStatusEffectsToApply`.

Weapon and rune on-hit statuses still flow through the same payload path. Elemental rune magic payloads can override magic delivery shape through the existing weapon upgrade system.

The current passive example is intentionally small: if the player has `passive.combo.third_hit_knockback`, `CombatManager` applies knockback on combo phase 3.

## Creating A Weapon Combo

1. Create a `WeaponComboResource` under `Core/Combat/Data/Combos/`.
2. Set a stable `ComboId`.
3. Add ordered `ComboPhaseResource` entries.
4. Give every step a `StepId`, `DebugLabel`, payload, timing, combo window, and optional hitbox profile.
5. Add stable status IDs to `StatusEffectsToApply` when needed.
6. For charge attacks, set `ChargeSeconds`, optional `MaxChargeSeconds`, `ChargedDamageMultiplier`, and optional `ChargedStatusEffectsToApply`.
7. Point a weapon row's `combo_profile_path` in `items_seed.csv` at the resource.
8. Build and run CombatDebugScene.

## CombatDebugScene Test

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-combat-debug.ps1
```

Manual flow:

- confirm `Debug Socket Sword` is equipped
- press Attack once and confirm `Slash 1`
- press Attack inside the combo window and confirm `Slash 2`
- press Attack inside the next window and confirm `Heavy Finisher`
- wait too long and confirm the next attack starts at step 1
- confirm each active window damages enemies at most once per hitbox activation
- confirm finisher applies `status.knockback`
- confirm missing `Slash1`, `Slash2`, or `HeavyFinisher` animations warn once and do not crash

## Current Limitations

- `ResetTimeSeconds` is authored but the current FSM exits after recovery instead of holding an out-of-state grace timer.
- Branching combo routes are not implemented.
- `AbilityEffectsToApply` is reserved; only status IDs are applied directly today.
- Hitbox activation is timer/window driven; animation events can still call `ActivateHitBox()`, but the runtime guards against duplicate activation for the same packet.
- Enemy combos still use their existing enemy attack behavior.

## Future Follow-Ups

- animation events for precise hitbox activation
- weapon-specific combo branches
- charged attacks
- directional combo variations
- aerial/jump attacks if ever needed
- stamina/mana costs
- deeper passive ability combo modifiers
- rune/element combo modifiers
- enemy combo definitions
- editor tooling for combo authoring
