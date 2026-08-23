# World State / Game Flags

## Purpose

World state is the shared store for persistent game facts that many systems need to read or write. It is the foundation for save/load, dialog conditions, quest progress references, NPC friendship, explored areas, player location, time of day, and later chest, door, puzzle, and cutscene persistence.

## Why GameStateManager Owns It

`GameStateManager` is already registered as an `ISaveable` manager through `GameManager`, so it is the smallest safe owner for world state. This avoids adding a competing `WorldStateManager` path and keeps persistent facts under the existing manager-based orchestration.

The older top-level `WorldStateManager` remains as a compatibility facade for existing dialog/save code. When `GameManager.Instance.GameState` exists, it delegates to `GameStateManager`.

## What To Store

Store stable, JSON-friendly facts:

- IDs and keys
- primitive values such as bool, int, and string
- collections of IDs
- lightweight DTOs for current location and time of day
- quest state references needed by save/load or dialog checks

## What Not To Store

Do not store:

- Godot `Node` references
- scene instances
- resource instances that can be looked up by ID
- live NPC/player/enemy objects
- temporary UI state
- per-frame runtime-only data

## Flag Naming

Use lowercase dot-separated keys:

- `area.tree_village.explored`
- `cutscene.intro.played`
- `chest.forest_001.opened`
- `door.underroot_gate.opened`

Prefer stable design IDs over scene node names when a fact must survive renames.

## Boolean Flags

Use:

- `SetFlag(string key, bool value = true)`
- `GetFlag(string key)`
- `ClearFlag(string key)`

Examples:

- `area.tree_village.explored`
- `cutscene.intro.played`
- `chest.forest_001.opened`
- `door.underroot_gate.opened`

## Integer Values

Use:

- `SetInt(string key, int value)`
- `GetInt(string key, int defaultValue = 0)`
- `AddInt(string key, int delta)`

Examples:

- `npc.kaz.friendship`
- `quest.wolf_problem.wolves_killed`
- `player.gold`
- `time.day`

## String Values

Use:

- `SetString(string key, string value)`
- `GetString(string key, string defaultValue = "")`

Examples:

- `player.current_scene`
- `player.current_spawn`
- `player.current_area`
- `time.phase`

## Explored Areas

Areas are tracked by stable area ID:

- `MarkAreaExplored(string areaId)`
- `HasAreaBeenExplored(string areaId)`

Marking an area also sets `area.{areaId}.explored`.

## NPC Friendship

Friendship is tracked by stable person/NPC ID:

- `GetFriendship(string personId)`
- `SetFriendship(string personId, int value)`
- `AddFriendship(string personId, int delta)`

The legacy `WorldStateManager.AdjustFriendship` path delegates into this API when the core game manager exists.

## Current Location

Current player location is stored as:

- scene ID/key/path
- spawn ID
- area ID

Use:

- `SetCurrentLocation(string sceneId, string spawnId = "", string areaId = "")`
- `GetCurrentLocation()`

The typed location DTO is also mirrored into string keys:

- `player.current_scene`
- `player.current_spawn`
- `player.current_area`

## Time Of Day

Time is intentionally simple for now:

- current day number
- current phase string such as `Morning`, `Afternoon`, `Evening`, or `Night`

Use:

- `SetTimeOfDay(int day, string phase)`
- `GetTimeOfDay()`

This does not simulate a calendar or clock yet.

## Save / Load Foundation

`GameStateManager.CaptureSnapshot()` returns a `GameStateSave` containing a plain `WorldStateDto`. `RestoreSnapshot()` accepts either `GameStateSave` or `WorldStateDto`.

The DTO can carry:

- boolean flags
- int values
- string values
- explored area IDs
- friendship scores
- current location
- time of day
- quest state references

Full save-file serialization is still a follow-up because broader save/load classes are only partially implemented.

## Quest Integration

Quest runtime logic should stay in `QuestManager`. World state should not duplicate every quest objective unless save/load requires it.

Good integrations:

- store high-level quest facts for dialog, such as `quest.wolf_problem.completed`
- store counters needed by multiple systems, such as `quest.wolf_problem.wolves_killed`
- later add a quest runtime snapshot section that `QuestManager` owns

Avoid splitting the source of truth for active quest runtime state across both managers.

## Dialog Integration

Dialog conditions/actions can use world state for:

- flags
- friendship thresholds
- explored areas
- cutscene completion
- simple quest facts

Example action:

- `SetFlag("cutscene.intro.played")`

Example condition:

- `GetFriendship("kaz") >= 10`

## Events

`GameStateManager` publishes lightweight events when state changes:

- `GameFlagChanged`
- `GameIntChanged`
- `GameStringChanged`
- `AreaExplored`
- `FriendshipChanged`
- `LocationChanged`
- `TimeOfDayChanged`

Use these only when another system needs to react to a change.

## Manual Test Steps

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1
```

In the Godot console, verify:

- `[GameState] Set flag: debug.test_flag = true`
- `[GameState] Friendship changed: test_npc = 5`
- `[GameState] Area explored: debug_area`
- `[GameState] Current location: scene=InteractionDebugScene spawn=Start area=debug_area`
- `[GameState] Time of day: Day 1 - Evening`
- `[GameState] World state restored.`

Manual API checks:

- set/get `debug.test_flag`
- add friendship for `test_npc`
- mark `debug_area` explored
- set current location to `InteractionDebugScene`, `Start`, `debug_area`
- set time to day 1, `Evening`
- capture a snapshot and restore it, then verify the values still read back

## Future Follow-Ups

- full save/load integration
- quest runtime save data
- chest/door/puzzle persistence
- dialog conditions using flags
- area trigger components
- time progression system
- world-state debug UI
