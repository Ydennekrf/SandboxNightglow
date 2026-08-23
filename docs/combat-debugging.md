# Combat Debugging

## TestEnemy Scene

`res://PackedScenes/EthraV1/Core/Entities/Debug/TestEnemy.tscn` is a reusable debug-only enemy for manual combat testing.

Use it to check:
- player attacks reducing enemy HP
- reusable `HitBox` overlap against an enemy `HurtBox`
- damage popup numbers
- health bar updates
- defeated/disabled behavior at zero HP
- loot-drop call sites without implementing real loot

The scene is intentionally simple. It does not include patrol, chase, attack AI, pathfinding, animation polish, or production loot behavior.

## How It Works

The root `TestEnemyNode` creates an existing `Enemy` combat model when the scene enters the tree. It configures basic `IStats`, registers the enemy with `GameManager.registeredEnemies`, and binds that model to the child `HurtBox`.

Damage is still applied through the existing combat path:

`HitBox` -> `HurtBox` -> `CombatManager.DealDamage`

For existing payload-driven player attacks, `TestEnemyNode` listens to `CombatFeedbackBus.HitResolved` and asks its `HurtBox` to show the damage popup. A small guard prevents duplicate popup numbers when a `HitBox` already displayed one.

When HP reaches zero, the node enters a debug defeated state, disables its collision/hurtbox, dims the visual, and calls `DropLoot()`. `DropLoot()` is a stub that only prints a debug line.

## Adding TestEnemy To Another Scene

1. Instance `res://PackedScenes/EthraV1/Core/Entities/Debug/TestEnemy.tscn`.
2. Place it under that scene's enemy or entity container.
3. Make sure the project autoload `GameManager` is available.
4. Run the scene and attack the enemy.

Useful exported values:
- `DebugName`
- `MaxHealth`
- `StartingHealth`
- `LootTableId`
- `DisableOnDefeat`
- `ShowDebugLogs`

## Combat Debug Scene

The manual combat debug scene is:

`res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`

It instances:
- `res://PackedScenes/EthraV1/Core/Entities/Selene.tscn`
- `res://PackedScenes/EthraV1/Core/Entities/Debug/TestEnemy.tscn`

Expected startup console output:

```text
[CombatDebug] Manual combat debug scene ready.
[CombatDebug] TestEnemy HP: 100
```

The scene also creates a debug-only `Attack Effects` toggle panel. These toggles append stable status IDs to outgoing player weapon attack packets so each status can be tested through the normal `HitBox` -> `HurtBox` -> `CombatManager` path. See `docs/status-effects.md` for runtime details and the status test checklist.

The scene also shows a small combo debug panel with the current equipped weapon, combo ID, step label, input/window state, hitbox profile, and last hit target. The debug scene equips `Debug Socket Sword` (`2003`), which uses `res://Core/Combat/Data/Combos/debug_sword_combo.tres`. Combo behavior belongs to `AttackAction`, `PlayerNode`, combo resources, and `CombatManager`; the scene only displays feedback and seeds test equipment. See `docs/combat-combos.md`.

To test other authored weapons, change `CombatDebugSceneRoot.DebugWeaponItemId` to `2004` for Ember Scepter area magic, `2005` for Stonebreaker Maul charge slam, or `2006` for Gale Knife looping flurry.

Run it with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-combat-debug.ps1
```

Build first with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

To launch the full game instead:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-game.ps1
```

## Expected Manual Behavior

When attacking the enemy:
- HP decreases on the health bar and label.
- A damage number appears above the enemy.
- Repeated attacks can continue reducing HP.
- A single `HitBox` activation should not damage the same enemy more than once.
- The combo panel should advance through `Slash 1`, `Slash 2`, and `Heavy Finisher` when Attack is pressed inside each combo window.
- Enabled status toggles apply active status text near the enemy state label.
- At zero HP, the enemy dims and disables its body collision and hurtbox.
- The console prints the debug loot-drop stub message.

## Common Failures

Enemy does not take damage:
- Confirm the project autoload `GameManager` exists in the running scene.
- Confirm the enemy is registered in `GameManager.registeredEnemies`.
- Confirm the child `HurtBox` is present and bound by `TestEnemyNode`.
- Confirm the attacking `HitBox` is active and its collision mask includes the enemy hurtbox layer.

Damage popup does not appear:
- Confirm `DamagePopupAnchor` exists.
- Confirm the child `HurtBox` uses `PopupAnchorPath = ../DamagePopupAnchor`.
- If using payload attacks instead of `HitBox`, confirm `CombatFeedbackBus.HitResolved` is emitted.

Hitbox hits multiple times:
- `HitBox` clears its hit target set only on `Activate()`.
- If damage repeats during one swing, check whether the attack is reactivating the hitbox multiple times.

Hurtbox is not detected:
- Confirm the target has a `HurtBox` script, not just a plain `Area2D`.
- Confirm collision layers and masks overlap.
- Confirm a `CollisionShape2D` under the hurtbox is enabled.

Scene fails to load:
- Run `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`.
- Check that `TestEnemy.tscn` and `Selene.tscn` paths still match the debug scene resources.
- Confirm `project.godot` still autoloads `res://PackedScenes/EthraV1/Core/UI/game_manager.tscn`.

## Debug Scene Ownership

`CombatDebugSceneRoot` should stay a test harness. It may create the debug player, equip a debug weapon, trigger configured enemy spawners, and handle H/K damage shortcuts. Combat damage, damage popups, enemy health, enemy state labels, enemy death, and enemy spawning behavior belong in reusable combat/enemy components.

See `docs/debug-scene-architecture.md` for the shared debug-scene rules and GameManager/autoload strategy.

## Debug-Only Notes

`TestEnemy.tscn` is debug scaffolding. It is safe to reuse in test zones, but it is not a production enemy implementation. Production enemies should eventually own their own authored visuals, AI, loot tables, animations, and authored stat resources.
