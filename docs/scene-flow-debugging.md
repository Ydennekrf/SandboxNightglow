# Scene Flow Debugging

## Overview

The scene flow debug setup is a focused group of standalone Godot scenes for testing scene transitions, simple interactables, and cutscene sequencing without turning those tests into production puzzle or cinematic systems.

Debug scenes live under:

`res://PackedScenes/EthraV1/Core/Debug/SceneFlow/`

## Debug Scenes

- Scene transition test A: `res://PackedScenes/EthraV1/Core/Debug/SceneFlow/SceneFlow_A.tscn`
- Scene transition test B: `res://PackedScenes/EthraV1/Core/Debug/SceneFlow/SceneFlow_B.tscn`
- Scene transition host: `res://PackedScenes/EthraV1/Core/Debug/SceneFlow/SceneFlowDebugHost.tscn`
- Button-door test: `res://PackedScenes/EthraV1/Core/Debug/SceneFlow/ButtonDoorDebugScene.tscn`
- Cutscene test: `res://PackedScenes/EthraV1/Core/Debug/SceneFlow/CutsceneDebugScene.tscn`

Each scene uses `SceneInteractionDebugRoot` to wait for the existing autoload `GameManager`, configure a debug player model, initialize UI, and spawn `Selene` at a named marker. The scenes do not instance their own `GameManager`, which avoids duplicate singleton warnings when run from the Godot project.

## Running

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-scene-flow-debug.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-button-door-debug.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-cutscene-debug.ps1
```

`run-scene-flow-debug.ps1` defaults to `SceneFlowDebugHost.tscn` and accepts an optional scene path:

Most testing should start from the default host scene so `SceneManager` has the persistent `World` container it expects.

## Scene Transition Test

`SceneFlowDebugHost` is the persistent test root. It includes the `World` container and UI, waits for the autoload `GameManager`, creates/sets a debug player model, then calls:

`GameManager.Scene.GoToScene("SceneFlow_A")`

After the scene is loaded, it calls:

`GameManager.SpawnPlayerAtMarker("NewGameSpawn")`

`SceneFlow_A` and `SceneFlow_B` are world scenes loaded by `SceneManager` from `MasterRepository` scene keys. Each includes:

- `World` with `WorldSceneRoot`
- `World/Entities/SpawnPoints/NewGameSpawn`
- `World/Entities/Player`
- `UI` with `InteractionDialogPanel`

The transition areas use the existing `SceneTransfer` interactable.

`SceneTransfer` uses exported `TargetSceneKey` and `TargetSpawnName`, then calls `GameManager.Scene.GoToScene(TargetSceneKey)` and deferred `GameManager.SpawnPlayerAtMarker(TargetSpawnName)`. For the debug flow, it has optional `RequireInteract` and `TransitionMessage` exports.

`SceneFlow_A` prints:

`[SceneFlowDebug] Transitioning from SceneFlow_A to SceneFlow_B`

`SceneFlow_B` prints:

`[SceneFlowDebug] Loaded SceneFlow_B`

## Player Spawn

For scene-flow transitions, spawn names are configured on each `SceneTransfer` node:

- `SceneFlowDebugHost.InitialSpawnName`: initial spawn in `SceneFlow_A`.
- `SceneFlow_A/Interactables/TransitionToB.TargetSpawnName`: spawn used after loading `SceneFlow_B`.
- `SceneFlow_B/Interactables/ReturnToA.TargetSpawnName`: spawn used after loading `SceneFlow_A`.

`GameManager.SpawnPlayerAtMarker(...)` resolves the active `WorldSceneRoot` loaded inside the persistent root `World` container, then asks `GameManager.Scene.SpawnPlayerNode(...)` to instance and bind `Selene`.

## Button-Door Test

`ButtonDoorDebugScene` contains:

- `Button`, an `Area2D` with `DebugButtonDoorButton`
- `Door`, a `Node2D` with `DebugDoor`

`DebugButtonDoorButton` uses an exported `DoorPath`. When the player enters the button area and presses `Interact`, it prints:

`[ButtonDoorDebug] Button pressed.`

Then it calls `DebugDoor.OpenDoor()`.

`DebugDoor.OpenDoor()` disables the configured collision shape, moves the door by `OpenOffset`, optionally hides its visual, and prints:

`[ButtonDoorDebug] Door opened.`

To create another simple button-door interaction, instance or recreate the same node shape, assign `DoorPath`, and configure the door collision/visual paths.

## Cutscene Debug Controller

`CutsceneDebugScene` uses `CutsceneDebugController`.

The controller runs a small hardcoded sequence:

1. Move Character A to `CharacterAMarker`.
2. Show Character A: "We made it."
3. Move Character B to `CharacterBMarker`.
4. Show Character B: "This path leads deeper into the woods."
5. Move Character A to `CharacterASecondMarker`.
6. Play animation hook `nod`.
7. Show Character A: "Then we keep moving."
8. End the cutscene.

This is deliberately not a full timeline editor. The sequence is code-first for now, but it is shaped around reusable step types: move actor, show dialog line, play animation hook, wait, end.

## Movement Steps

Movement uses simple `GlobalPosition.MoveToward(...)` interpolation each process frame. Each movement step logs:

`[CutsceneDebug] Moving CharacterA to marker: MarkerName`

or:

`[CutsceneDebug] Moving CharacterB to marker: MarkerName`

## Dialog Steps

Dialog lines use the existing `InteractionDialogPanel`. The controller shows a single `Continue` choice and waits until it is pressed before advancing.

This avoids requiring final dialog tree data for a throwaway sequencing test while still exercising the real in-game dialog panel.

## Animation Hooks

Animation hooks call `PlayAnimationHook(actor, animationName)`. The method logs:

`[CutsceneDebug] Play animation hook: nod`

It safely checks for an `AnimationPlayer` named `AnimationPlayer` or `Actions`. Missing animation players or missing animation names do not fail the cutscene.

## Player Control Lock

When the cutscene starts, the controller calls:

`PlayerNode.SetCutsceneControlled(true)`

When the cutscene completes, it calls:

`PlayerNode.SetCutsceneControlled(false)`

This stops movement and combat input during the cutscene, prevents the normal player physics loop from overriding scripted movement, and still reuses the existing player node behavior after the cutscene ends.

## Manual Test Checklist

Scene transition:

- Start `SceneFlow_A`.
- For the manager/repository test, start `SceneFlowDebugHost`.
- Move to the blue transition marker.
- Press `Interact`.
- Confirm `SceneFlow_B` loads.
- Confirm the player appears near `SceneFlow_B`'s `ReturnFromASpawn`.
- Move to the orange return marker.
- Press `Interact`.
- Confirm `SceneFlow_A` loads again.

Button-door:

- Start `ButtonDoorDebugScene`.
- Move onto the red button.
- Press `Interact`.
- Confirm `[ButtonDoorDebug] Button pressed.`
- Confirm `[ButtonDoorDebug] Door opened.`
- Confirm the door moves upward and no longer blocks the path.

Cutscene:

- Start `CutsceneDebugScene`.
- Confirm player control is disabled while the cutscene is active.
- Confirm Character A moves to its marker.
- Press `Continue` through each dialog line.
- Confirm Character B moves between dialog lines.
- Confirm Character A moves again before the final line.
- Confirm `[CutsceneDebug] Play animation hook: nod`.
- Confirm `[CutsceneDebug] Cutscene complete.`
- Confirm player control returns after completion.

## Current Limitations

- Scene transition debug now uses the production `SceneManager.GoToScene(...)` and scene repository key path.
- Button-door state is not saved.
- Button-door interaction is direct `NodePath` wiring, not an event-channel puzzle framework.
- Cutscene steps are hardcoded in C#.
- Dialog lines are shown directly through `InteractionDialogPanel`, not dialog tree JSON.
- Movement is straight-line interpolation and does not use navigation/pathfinding.
- Animation hooks are safe stubs until real actor animations exist.

## Future Follow-Up Tasks

- Data-driven cutscene definitions
- Visual cutscene editor
- Proper animation integration
- Branching cutscenes
- Puzzle event channels
- Save/load persistent door states
- Scene transition fade effects
