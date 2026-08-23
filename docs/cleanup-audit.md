# Cleanup Audit

## Scope

First-pass cleanup focused on clearly unused duplicate manager scenes, editor configurability in debug harnesses, and documenting risky cleanup candidates. No production gameplay systems were rewritten.

## Cleanup Performed

- Removed legacy scene artifacts that created or attached duplicate `GameManager` instances outside the documented autoload path.
- Kept active and compatibility manager code in place when references still existed.
- Converted repeated debug harness constants to exported fields while preserving the previous defaults.

## Removed Files

| File | Reason | Reference check |
| --- | --- | --- |
| `PackedScenes/Masters.tscn` | Legacy manager scene using top-level `res://Managers/*.cs` scripts, including `res://Managers/GameManager.cs`, instead of the documented autoloaded `res://PackedScenes/EthraV1/Core/UI/game_manager.tscn`. | `rg` found no references to `Masters.tscn`, `PackedScenes/Masters`, `res://PackedScenes/Masters`, or `res://Managers/GameManager.cs` outside the deleted scene and legacy code references. |
| `node_2d.tscn` | Root-level scratch scene with `res://Core/Managers/GameManager.cs` attached directly to a `Node2D`, which could create a duplicate manager if launched. | `rg "node_2d\\.tscn|res://node_2d\\.tscn|uid://csprstdhcw5u0"` found only `node_2d.tscn` itself. |
| `components/StateMachine/Actions/Scripts/ParticalCreatorAction.cs` | Empty obsolete prototype action file. | `rg "ParticalCreatorAction"` found no references outside the file. |
| `components/StateMachine/States/StunnedState.cs` | Old state prototype replaced by the current status/combat flow. | `rg "StunnedState"` found only the class and constructor declarations. |
| `components/Inventory/Scripts/Interfaces/IEquippable.cs` | Unused legacy inventory interface. | `rg "IEquippable"` found only its own declaration. |
| `components/Inventory/Scripts/Interfaces/IEquippableAction.cs` | Unused legacy inventory interface. | `rg "IEquippableAction"` found only its own declaration. |
| `components/Interact/DTOs/old/DialogChoice.cs` | Unused old dialog DTO replaced by `Core/Dialog/Scripts/DialogTree.cs` models. | Active `DialogChoice` references resolve to the newer Core dialog class; no path references to this old DTO. |
| `components/Interact/DTOs/old/DialogNode.cs` | Unused old dialog DTO replaced by `Core/Dialog/Scripts/DialogTree.cs` models. | Active `DialogNode` references resolve to the newer Core dialog class or SQL tables; no path references to this old DTO. |
| `components/Interact/DTOs/old/ChoiceAction.cs` | Unused old dialog action DTO. | Hits are SQL table names/comments, not this C# type. |
| `components/Interact/DTOs/old/NodeAction.cs` | Unused old dialog action DTO. | Hits are comments, not active C# references. |

## Hardcoded Values Converted To Exports

- `PackedScenes/EthraV1/Core/Scripts/CombatDebugSceneRoot.cs`
  - Added exported `InitialSpawnName`.
  - Replaced hardcoded debug weapon item ID with exported `DebugWeaponItemId`.
  - Added exported debug player stat defaults.
- `PackedScenes/EthraV1/Core/Scripts/InteractionDebugSceneRoot.cs`
  - Added exported `InitialSpawnName`.
  - Replaced hardcoded debug weapon, socket weapon, crafting material, material quantity, and rune seed IDs with exported values.
  - Added exported debug player stat defaults.
- `PackedScenes/EthraV1/Core/Scripts/CraftingDebugSceneRoot.cs`
  - Added exported `InitialSpawnName`.
  - Added exported debug player stat defaults.
- `PackedScenes/EthraV1/Core/Scripts/MapLayerDebugSceneRoot.cs`
  - Added exported `InitialSpawnName`.
  - Added exported debug player stat defaults.

All exported defaults match the previous hardcoded behavior.

## Manager And GameManager Routing Findings

- The active manager architecture is centered on the `GameManager` autoload in `project.godot`:
  - `GameManager="*res://PackedScenes/EthraV1/Core/UI/game_manager.tscn"`
  - `game_manager.tscn` uses `res://Core/Managers/GameManager.cs`.
- `Core/Managers/GameManager.cs` owns the active manager set: `CombatManager`, `DialogManager`, `EntityManager`, `EventManager`, `GameStateManager`, `InventoryManager`, `CraftingManager`, `WeaponUpgradeManager`, `QuestManager`, `SceneManager`, `UIManager`, `MasterRepository`, and `SaveLoadService`.
- No production manager routing was changed in this pass.
- `CraftingManager`, `WeaponUpgradeManager`, `MasterRepository`, and `SaveLoadService` are constructed by `GameManager` but are not registered through `registerManager(...)`. This appears acceptable for plain services today, but should be made intentional if they gain lifecycle, save, or resolve responsibilities.

## Duplicate GameManager Findings

- Removed:
  - `PackedScenes/Masters.tscn`
  - `node_2d.tscn`
- Kept:
  - `PackedScenes/EthraV1/Core/UI/game_manager.tscn`, the documented autoload scene.
  - `Core/Managers/GameManager.cs`, the active manager script.
  - Top-level `Managers/GameManager.cs`, because older top-level systems still reference the top-level manager set and removing it would require a broader migration.

## Debug Scene Cleanup

- Debug roots remain thin test harnesses relative to production systems: they still seed, spawn, and run debug shortcuts, while combat, inventory, crafting, status, UI feedback, and scene spawning continue to route through `GameManager` managers and reusable components.
- A future cleanup should consider a shared debug harness helper for repeated player creation, stat setup, UI binding, and spawn marker lookup.

## Items Intentionally Not Deleted

- `Managers/` legacy manager folder:
  - References still exist from `Managers/SceneManager.cs`, `Managers/PlayerManager.cs`, `components/Entity/EntityBase/Player.cs`, `components/Interact/Base/ActionExecutor.cs`, and `PackedScenes/Characters/Player/player.tscn`.
  - `docs/world-state.md` explicitly describes the older `WorldStateManager` as a compatibility facade.
- `PackedScenes/Characters/Player/player.tscn` and older `components/` scripts:
  - Still reference top-level components/managers and appear to be legacy-compatible content rather than safely unused code.
- `components/Interact/DTOs/old/DialogGraph.cs` and `components/Interact/DTOs/old/DialogStartDTO.cs`:
  - Still referenced by the older dialog repository/interactable path and are not safe to remove independently.
- `PackedScenes/Zones/test_zone.tscn`:
  - Appears stale and unreferenced, but is currently untracked in the working tree, so it was left untouched as likely user-local work.
- `PackedScenes/Zones/TestZone.tscn`:
  - Part of the older legacy scene stack and should only be removed with a deliberate legacy-stack migration.
- Debug scenes and debug scripts:
  - Current docs and helper scripts reference them as active manual test harnesses.
- Stable ID and save/load data:
  - Left untouched because renames/removals require migration planning.

## Remaining Cleanup Candidates

- Audit whether the legacy `Managers/` and `components/` runtime path can be retired after all active scenes use `Core/Managers` and EthraV1 scenes.
- Extract shared debug harness setup for repeated code across `CombatDebugSceneRoot`, `InteractionDebugSceneRoot`, `CraftingDebugSceneRoot`, `MapLayerDebugSceneRoot`, `SceneInteractionDebugRoot`, and `SceneFlowDebugHost`.
- Normalize debug root exports so all debug scenes expose the same manager/world/player/spawn configuration shape.
- Add an explicit comment or lifecycle registration decision for `CraftingManager`, `WeaponUpgradeManager`, `MasterRepository`, and `SaveLoadService`.

## Second Pass Notes

- Added reusable file-backed debug logging in `Core/Debug/DebugLog.cs`.
- Documented logger usage in `docs/debug-file-logging.md`.
- Instrumented map-layer triggers and controller state changes so bridge route debugging can be reviewed from `user://logs/debug.log`.
- Gave `OnBridge` and `UnderBridge` explicit visual/collision layer state changes in `MapLayerDebugScene.tscn`.

## Risks And Manual Checks

- The worktree already contains many unrelated modified, deleted, and untracked files. This audit only covers the cleanup files listed above.
- Open the updated debug scenes in the Godot editor and confirm the new exported fields appear with expected defaults.
- Confirm no missing-script warnings appear for the deleted duplicate manager scenes.

## Manual Test Checklist

- Start the game and confirm the main menu loads.
- Start a new game and confirm the player spawns.
- Confirm movement still works.
- Confirm interaction still works.
- Confirm combat debug still works.
- Confirm harvestable interaction still works.
- Confirm crafting table opens.
- Confirm inventory/menu opens.
- Confirm no duplicate `GameManager` appears.
- Confirm no missing script errors appear in Godot.
- Confirm exported values appear correctly in the editor for updated debug roots.
