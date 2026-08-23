# Enemy Behavior

## Overview

Enemy test behavior is built on the existing C# FSM in `res://Core/Entity/Scripts/StateMachine/`. The debug enemy node creates an `Enemy` combat model, assigns itself as that model's behavior context, then assembles modular states from focused enemy actions and condition transitions.

This keeps scene-specific work in `TestEnemyNode` while reusable behavior remains in FSM actions.

## Scene And Code Locations

- Enemy scene: `res://PackedScenes/EthraV1/Core/Entities/Debug/TestEnemy.tscn`
- Enemy spawn marker scene: `res://PackedScenes/EthraV1/Core/Entities/Debug/EnemySpawnMarker.tscn`
- Enemy debug spawners: `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn` -> `World/Entities/SpawnPoints/ActiveEnemySpawn`, `TimedEnemySpawn`, `ProximityEnemySpawn`
- Enemy node bridge: `res://Core/Nodes/Entity/TestEnemyNode.cs`
- Enemy spawner marker script: `res://Core/Nodes/Entity/EnemySpawnMarker.cs`
- Enemy definition resource: `res://Core/Entity/Scripts/EnemyDefinitionResource.cs`
- Default test enemy definition: `res://Core/Entity/Data/EnemyDefinitions/test_enemy_definition.tres`
- Enemy actions: `res://Core/Entity/Scripts/StateMachine/Actions/Enemy*.cs`
- Enemy transition: `res://Core/Entity/Scripts/StateMachine/Transitions/EnemyConditionTransition.cs`
- Enemy behavior contract: `res://Core/Entity/Scripts/StateMachine/Interfaces/IEnemyBehaviorContext.cs`
- Combat debug scene: `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`

## Spawn Modes

`TestEnemyNode.SpawnModeValue` controls how the enemy becomes active. It is shown in the Inspector as an `Active / Timed / Proximity` dropdown and can be set directly on the enemy scene, through an `EnemyDefinitionResource`, or as a marker override on `EnemySpawnMarker`.

The combat debug scene instances `EnemySpawnMarker.tscn` for each spawn test, matching the existing debug NPC scene pattern where a reusable scene owns the C# script and the parent debug scene only applies per-instance overrides.

- `Active`: starts visible and active immediately.
- `Timed`: starts hidden/non-hostile, waits `SpawnDelaySeconds`, shows `Spawning`, then enters behavior.
- `Proximity`: starts hidden/non-hostile until the player is within `SpawnProximityDistance`.

Spawning disables the enemy body collision, hurtbox, and attack hitbox until activation. The FSM still ticks so timed and proximity spawn checks can complete.

## Idle

`EnemyIdleAction` stops movement, updates the state label to `Idle`, calls the animation hook with `Idle`, and waits `IdleDurationSeconds`. When the timer completes, the FSM transitions to patrol unless detection, attack, hurt, or death takes priority.

## Random Patrol

`EnemyPatrolAction` chooses random destinations inside a circle and moves toward them.

Patrol bounds are configured with:

- `PatrolCenterOffset`: offset from the enemy's initial debug scene position.
- `PatrolRadius`: maximum random destination distance from that center.

The state label shows `Patrol`. This is intentionally simple bounded steering, not navigation/pathfinding.

## Pursue And Leash

`EnemyPursueAction` moves toward the first node in the `Player` group when the player is within `DetectionRange`. The enemy leaves pursue when either the player is no longer detected or the enemy is beyond `LeashRange` from its patrol center.

The state label shows `Pursue`.

## Basic Attack

`EnemyBasicAttackAction` stops movement, faces the player, positions the enemy `AttackHitBox`, and activates it for `AttackActiveDurationSeconds`.

Config values:

- `AttackDamage`
- `AttackRange`
- `AttackCooldownSeconds`
- `AttackDurationSeconds`
- `AttackActiveDurationSeconds`

The existing `HitBox` deduplicates targets during one active window, so one attack window does not damage the player every frame. The player scene's existing hurtbox area is now bound to the reusable `HurtBox` script so enemy attacks use the same combat path.

The state label shows `Attack`.

## Hurt And Die

When combat feedback reports damage against the enemy, `TestEnemyNode` requests `Hurt` unless HP is already zero. `EnemyHurtAction` stops movement for `HurtDurationSeconds`, shows `Hurt`, and then returns to pursue behavior if still alive.

When HP reaches zero, the FSM transitions to `Dead`. `EnemyDieAction` stops movement, deactivates the attack hitbox, disables hurtbox/body collision, dims the enemy, calls the loot stub once, and keeps the label at `Dead`. Extra hits after death are ignored by the existing combat checks and dead-state guards.

## Animation Hooks

Each enemy action calls `PlayEnemyAnimation(string animationKey)` through the behavior context. `TestEnemyNode` safely checks an optional `AnimationPlayerPath`.

Missing animation players are ignored. Missing configured animation names log one warning per animation key and do not block the FSM. No final enemy animation assets are required yet.

## State Label

`TestEnemy.tscn` has a `StateLabel` above the health bar. Enemy FSM actions update this label on enter/tick:

- `Spawning`
- `Idle`
- `Patrol`
- `Pursue`
- `Attack`
- `Hurt`
- `Dead`

## Combat Debug Scene

Run the debug scene with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-combat-debug.ps1
```

Build first with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

Expected startup message:

```text
[CombatDebug] Enemy behavior test ready.
```

The debug scene has visible spawn markers under `World/Entities/SpawnPoints`:

- `NewGameSpawn` spawns Selene.
- `ActiveEnemySpawn` tests immediate active spawn.
- `TimedEnemySpawn` tests delayed timed spawn.
- `ProximityEnemySpawn` tests proximity-triggered spawn.

Each enemy spawn marker has `EnemySpawnMarker.cs` attached and references an enemy scene through its exported `EnemyScene` field. Developers can assign a different `Node2D` enemy scene in that field. For `TestEnemyNode` scenes, the marker can also use `EnemyDefinitionOverride` and spawn behavior override fields.

At runtime, the marker instances the enemy under `World/Entities/Enemies`.

Enemy spawn markers are reusable outside `CombatDebugScene`. A normal scene can instance `EnemySpawnMarker.tscn`, assign an `EnemyScene`, and place it under a world/entity container as long as the project autoload `GameManager` is available. The debug scene root only triggers placed markers for the manual test harness; it does not own enemy behavior.

The combat debug scene also has debug-only enemy damage keys:

- `H`: apply configurable debug damage to the test enemy.
- `K`: apply enough debug damage to defeat the test enemy.

These shortcuts are only for manually testing `Hurt` and `Dead` when the inventory/equipment UI is not available in the scene.

## Customizing Enemies

`TestEnemyNode` supports an optional `EnemyDefinitionResource` assigned in the enemy scene. Duplicate `res://Core/Entity/Data/EnemyDefinitions/test_enemy_definition.tres` to create variants, then assign the new resource to a copied enemy scene.

The definition currently controls:

- display/debug name
- HP and core stats
- loot table id
- spawn mode and spawn timing
- idle and patrol timing
- patrol radius and center offset
- pursue speed, detection range, and leash range
- basic attack range, damage, damage type, ability id, cooldown, duration, and active window
- hurt duration
- animation hook keys for each state

Art is still scene-authored: duplicate the enemy scene and replace its visual nodes, sprites, animation player, and collision as needed. The definition keeps gameplay tuning separate from those scene edits.

`EnemySpawnMarker.EnemyScene` can point at any `Node2D` enemy scene. If the spawned scene is a `TestEnemyNode`, marker overrides are applied after the assigned definition so spawn tests can reuse the same enemy scene with different spawn behavior.

## Manual Test Checklist

1. Enemy starts hidden for the timed spawn test.
2. Enemy appears after the configured delay and shows `Spawning`.
3. Proximity enemy stays hidden until Selene moves within its proximity distance.
4. Active enemy starts active immediately.
5. Enemy enters `Idle`, then `Patrol`.
6. Enemy patrol movement stays near its configured patrol center/radius.
7. Moving Selene near the enemy triggers `Pursue`.
8. Moving into attack range triggers `Attack`.
9. Enemy attack damages Selene once per active window, not every frame.
10. Pressing `H` damages the nearest alive test enemy and briefly shows `Hurt`.
11. Pressing `K` defeats the nearest alive test enemy and changes its label to `Dead`.
12. Dead enemy stops movement and attacking without crashing if hit again.

## Current Limitations

- Patrol uses simple random steering, not navigation.
- Detection uses distance to the first node in the `Player` group.
- Attack uses one simple debug hitbox and ad hoc damage.
- Hurt has no knockback yet.
- Enemy visuals are still debug shapes.
- Death does not persist across saves.

## Future Follow-Up Tasks

- Real enemy animations
- Enemy-specific attack data
- Better pathfinding/navigation
- Drops/loot
- Aggro groups
- Knockback
- Status effects
- Difficulty scaling
- Save/load defeated state if needed
