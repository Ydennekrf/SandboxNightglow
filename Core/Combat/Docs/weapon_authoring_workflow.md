# Weapon Combo Content Authoring Workflow (Phase 5)

This document defines the production workflow for authoring new weapons and their combo payloads in the Godot editor.

## 1) Naming + Schema Conventions

Use these conventions consistently so authored resources are predictable and validation warnings remain actionable.

### Resource file naming

- Weapon combo resource path: `res://Core/Combat/Data/Combos/<weapon_id>_combo.tres`
- Example: `res://Core/Combat/Data/Combos/iron_longsword_combo.tres`

### Combo phase naming

- `SharedAnimationName` should use the clip family used by player animation libraries.
- Prefer `Melee1`, `Melee2`, `Melee3`, etc. (or a parallel magic family if needed).
- Keep phase index progression monotonic with `Phases` array order.

### Payload id conventions

`AttackPayloadResource` is embedded as sub-resources inside the combo `.tres`.

Recommended sub-resource id shape:

- `AttackPayloadResource_<overlay>_<phase>`
- Examples: `AttackPayloadResource_melee_1`, `AttackPayloadResource_magic_2`

### Delivery shape schema (`DeliveryShapeId`)

Allowed values are defined in `CombatPayloadSchema`:

- `SingleTarget`
- `Cone`
- `Linear`

Any other value will trigger validation warnings and may skip expected runtime delivery handlers.

### Effect id schema (`EffectIds`)

Allowed values are defined in `CombatPayloadSchema` and wired by `CombatManager`:

- `Knockback`
- `Slow`
- `Stun`
- `Root`

Any unknown effect id will trigger validation warnings and runtime warnings.

### Runtime effect behavior (current)

- `Slow`: reduces movement speed while status is active.
- `Root`: blocks voluntary movement while status is active.
- `Stun`: blocks voluntary movement while status is active and blocks outgoing hits (`CanHit` guard).
- `Knockback`: applies an impulse burst movement (with short decay). If duration is configured above 0, it can also persist as a status id.

## 2) Validation Behavior (automatic checks)

`ComboProfileValidator` checks all phases/payloads for:

- missing phase references,
- missing payload refs (both melee + magic null),
- unknown `DeliveryShapeId`,
- unknown `EffectIds`.

Validation runs automatically when combo profiles are loaded via `WeaponItem` (`LoadComboProfile`) and logs issues through Godot output.

## 3) Editor Preview Tool

Use `ComboProfilePreviewTool` (`res://Core/Combat/Tools/ComboProfilePreviewTool.cs`) to preview authored phase/payload setup in-editor.

### Quick setup

1. Open or create a utility scene.
2. Add a `Node`.
3. Attach script: `res://Core/Combat/Tools/ComboProfilePreviewTool.cs`.
4. Assign `ComboProfile` to your weapon combo `.tres`.
5. Set `PreviewPhase` and `PreviewInput`.
6. Read `PreviewReport` in the Inspector.

`PreviewReport` shows:

- resolved animation name,
- resolved payload summary for selected input,
- active/buffer windows,
- current validation issue list.

You can leave `AutoRefresh` on, or toggle `RefreshNow` for manual refresh.

## 4) Full Step-by-Step: Create a New Weapon in Godot Editor

This is the required end-to-end workflow for adding a production-ready weapon.

1. **Create / import weapon art assets**
   - Ensure weapon draw/stow textures exist and are imported.

2. **Create combo profile resource**
   - In FileSystem, create new resource of type `WeaponComboResource`.
   - Save as `res://Core/Combat/Data/Combos/<weapon_id>_combo.tres`.

3. **Author phases**
   - Add `ComboPhaseResource` entries to `Phases` in intended combo order.
   - For each phase set:
     - `SharedAnimationName`
     - `PreferFacingSuffix`
     - `DurationOverrideSeconds` (if needed)
     - active and buffer windows (`ActiveWindow*`, `BufferWindow*`)

4. **Author payloads per phase**
   - Set `MeleePayload` and optionally `MagicPayload` on each phase.
   - For each payload configure:
     - `OverlayMode` (`Melee` or `Magic`)
     - `ManaCost` (magic only, usually > 0)
     - `DeliveryShapeId` (must match schema)
     - `DamageType`
     - `ElementType`
     - `BasePower`
     - `EffectIds` (must match schema)
     - `EffectDurationSeconds`

5. **Validate with preview tool**
   - Open `res://PackedScenes/Tools/ComboProfilePreviewTool.tscn` (or add the tool script in your own utility scene).
   - Assign your new combo resource.
   - Check multiple `PreviewPhase` + `PreviewInput` combinations.
   - Resolve all `Error` and `Warning` lines in `PreviewReport` before integrating.

6. **Create/update weapon inventory item row**
   - In items data source, add/update the weapon row with:
     - core metadata (id, name, rarity, etc.),
     - texture paths (`weapon_*` fields),
     - `combo_profile_path` -> new combo `.tres` path.

7. **Confirm runtime loading**
   - Run the game and equip weapon through normal flow.
   - Confirm no `ComboProfileValidator` errors in output.

8. **Combat sanity pass in-editor/runtime**
   - Verify all phases advance in order.
   - Verify melee vs magic input picks expected payload.
   - Verify effect IDs apply expected combat behavior (`Slow` speed reduction, `Root` movement lock, `Stun` movement+hit lock, `Knockback` impulse).

9. **Polish pass**
   - Tune `BasePower`, windows, and effect durations.
   - Re-run preview checks after each tuning pass.

## 5) Definition of Done for Weapon Authoring

A weapon is considered complete when:

- combo profile follows naming/schema conventions,
- preview tool resolves expected phase/payload outputs,
- validator logs no errors (and no unresolved warnings),
- runtime equip + combo chain works with expected effects.
