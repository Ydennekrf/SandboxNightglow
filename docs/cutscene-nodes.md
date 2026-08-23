# Cutscene Nodes

## Overview

The cutscene node system lets a scene author place reusable C# nodes directly in a Godot scene and run them in sequence with `CutsceneOrchestrator`.

Current node types:

- `CutsceneDialogNode`
- `CutsceneSpawnNode`
- `CutsceneMovementNode`
- `CutsceneActionNode`
- `CutsceneOrchestrator`

Runtime scripts live in:

```text
res://Core/Cutscenes/
```

The system is intentionally small. It coordinates existing scene nodes, `DialogManager`, `InteractionDialogPanel`, `PlayerNode`, and authored `PackedScene` instances without creating duplicate managers or replacing the dialog, entity, or scene systems.

## Orchestrator

Add `CutsceneOrchestrator` to a scene, then add cutscene step nodes as children:

```text
CutsceneOrchestrator
- Step_001_Dialog
- Step_002_Spawn
- Step_003_Move
- Step_004_Action
- Step_005_Dialog
```

`CutsceneOrchestrator` exports:

- `CutsceneId`: optional stable ID for events.
- `PlayOnReady`: starts playback after the node enters the scene tree.
- `CanSkip`: allows `Skip()` to cancel playback.
- `DisablePlayerControlDuringCutscene`: finds the first `PlayerNode` in group `Player` and calls `SetCutsceneControlled(true)`.
- `RestorePlayerControlOnEnd`: restores the player control lock when playback ends.
- `StepsRootPath`: optional node whose direct children are the ordered steps.
- `DebugLogging`: prints step execution messages.

## Execution Order

By default, steps execute in Godot scene tree child order under the orchestrator. If `StepsRootPath` is set, the direct children of that node execute in child order instead.

Node names like `Step_001_Dialog` are recommended for readability, but alphabetical sorting is not used.

Each step has:

- `StepId`: optional stable ID or cross-reference label.
- `Description`: editor-facing notes.
- `IsBlocking`: when true, the orchestrator waits for the step to finish before continuing.

## Dialog Node

`CutsceneDialogNode` shows one authored line through the existing `DialogManager` and `InteractionDialogPanel`.

Exports:

- `SpeakerName`
- `SpeakerId`
- `Portrait`
- `PortraitPath`
- `Body`
- `WaitForPlayerInput`
- `AutoAdvanceDelaySeconds`
- `DialogPanelPath`

When executed, the node calls `DialogManager.PlayCutsceneLineAsync(...)` when `GameManager.Instance.Dialog` is available. This publishes the normal dialog start/end events, so existing dialog-state listeners can react consistently. The panel supports an optional portrait texture through its existing scene.

If `DialogPanelPath` is empty, the node tries:

```text
UI/InteractionDialogPanel
```

under the current scene.

## Action Node

`CutsceneActionNode` plays an animation on a target node.

Exports:

- `TargetEntityPath`
- `TargetEntityId`
- `AnimationName`
- `WaitForAnimationComplete`
- `FallbackDurationSeconds`
- `StopBeforePlay`

Target resolution order:

1. `TargetEntityPath`
2. a spawned entity stored in `CutsceneContext` by `TargetEntityId`
3. the first node in a group matching `TargetEntityId`

The action node prefers an `AnimationPlayer` at:

```text
AnimationPlayer
Actions
```

or the first descendant `AnimationPlayer`. If the target is `TestEnemyNode`, it uses `PlayEnemyAnimation(...)` and then waits on that node's configured `AnimationPlayerPath` when possible.

Missing targets or animations log warnings and use `FallbackDurationSeconds` instead of crashing.

## Movement Node

`CutsceneMovementNode` moves a `Node2D` target in a straight line to an end marker.

Exports:

- `TargetEntityPath`
- `TargetEntityId`
- `StartMarkerPath`
- `EndMarkerPath`
- `MoveSpeed`
- `MovementAnimationName`
- `IdleAnimationName`
- `SnapToStartOnBegin`
- `WaitForMovementComplete`

If the target is `CharacterBody2D`, the node sets `Velocity` and calls `MoveAndSlide()`. For other `Node2D` targets, it uses `GlobalPosition.MoveToward(...)`.

This does not perform pathfinding. It is intended for simple, authored straight-line movement between `Marker2D` nodes.

## Spawn Node

`CutsceneSpawnNode` instances an authored `PackedScene`.

Exports:

- `EntityScene`
- `SpawnParentPath`
- `SpawnMarkerPath`
- `SpawnPosition`
- `SpawnedEntityId`
- `AddToGroupName`
- `StoreSpawnedEntityForLaterSteps`

If `SpawnMarkerPath` resolves to a `Marker2D`, the spawned `Node2D` is placed at that marker. Otherwise, `SpawnPosition` is used.

If `SpawnParentPath` is empty, the node tries these fallbacks:

```text
World/Entities/NPCs
World/Entities
CurrentScene
```

When `StoreSpawnedEntityForLaterSteps` is enabled, later movement or action steps can target the entity by setting `TargetEntityId` to the same `SpawnedEntityId`.

## Player Control Lock

The orchestrator uses the existing `PlayerNode.SetCutsceneControlled(...)` method. This clears movement input, zeroes velocity, and prevents player input from driving the player while the cutscene runs.

The orchestrator waits briefly for the player node to appear, which supports debug scenes that spawn the player after `_Ready()`.

## Creating A New Cutscene

1. Add a `CutsceneOrchestrator` node to the scene.
2. Enable `PlayOnReady` or call `Play()` from another scene script.
3. Add child step nodes in the desired order.
4. Set `DialogPanelPath` for dialog steps if the panel is not at `UI/InteractionDialogPanel`.
5. Add `Marker2D` nodes for movement starts and ends.
6. For spawned actors, set `SpawnedEntityId`, then use that value in later `TargetEntityId` fields.
7. Run the scene and check Godot warnings for missing optional or required references.

## Debug Scene

Example scene:

```text
res://PackedScenes/EthraV1/Core/Debug/CutsceneNodeDebugScene.tscn
```

Run it with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-cutscene-node-debug.ps1
```

The scene demonstrates:

- orchestrator play-on-ready
- dialog through `InteractionDialogPanel`
- spawning a debug actor scene
- movement from `ActorStart` to `ActorEnd`
- an action animation named `Nod`
- final dialog after the movement/action sequence

## Manual Test Checklist

- Confirm the orchestrator starts when `PlayOnReady` is enabled.
- Confirm the first dialog line appears with speaker name and text.
- Confirm portrait appears when a portrait texture is configured.
- Confirm dialog advances with `Continue`.
- Confirm the spawn node adds the configured actor under `World/Entities/NPCs`.
- Confirm the movement node moves the actor from `ActorStart` to `ActorEnd`.
- Confirm movement animation is attempted safely.
- Confirm the action node plays `Nod`.
- Confirm the final dialog appears.
- Confirm the cutscene ends.
- Confirm player control is restored.
- Confirm no duplicate `GameManager` appears.
- Confirm no null reference errors occur.

## Current Limitations

- No branching or conditional cutscene flow.
- No timeline editor.
- No pathfinding; movement is straight-line only.
- No camera, fade, sound, wait, quest, or choice nodes yet.
- Non-blocking movement starts its task and does not currently report completion to later steps.
- Spawned scene nodes are context-local and are not persistent save data.

## Future Follow-Ups

- branching cutscenes
- camera movement nodes
- wait/timer nodes
- screen fade nodes
- sound/music nodes
- choice nodes
- quest flag nodes
- save/load cutscene state
- editor gizmos/previews
- timeline-style editor tooling
