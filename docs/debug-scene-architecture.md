# Debug Scene Architecture

## Purpose

Debug scenes are thin manual test harnesses. They place focused test objects, arrange spawn points, trigger smoke checks, and print useful startup messages. They must not become the owner of combat, interaction, enemy, UI, save/load, dialog, quest, or scene-flow implementation.

## Allowed Ownership

Debug scene roots may own:

- scene-specific test object placement
- debug-only input shortcuts
- simple smoke checks and assertions
- test player spawning and test stat setup
- debug logs that explain the test flow

Debug scene roots must not own:

- general hitbox/hurtbox damage rules
- interactable selection rules
- reusable prompt or floating text UI behavior
- enemy state, health, death, or spawn behavior
- dialog, quest, chest, store, save/load, or scene transition implementation
- manager initialization that differs from normal gameplay scenes
- local `GameManager` instances when the project autoload already provides one

## System Ownership

Combat logic belongs in `Core/Managers/CombatManager.cs`, `Core/Combat/Scripts/HitBox.cs`, `Core/Combat/Scripts/HurtBox.cs`, combat payload resources, and combat-capable entity components. Debug roots may trigger test attacks, but they should not implement general damage rules.

Interaction logic belongs in reusable interactable components, `components/Interact/Base/InteractComponent.cs`, prompt source interfaces, and current EthraV1 interactable nodes under `Core/Nodes/Interactable/`. Debug roots may place NPCs, chests, buttons, and doors, but each object should own its own interaction behavior.

Enemy logic belongs in enemy scenes/components, `Core/Nodes/Entity/TestEnemyNode.cs`, `Core/Nodes/Entity/EnemySpawnMarker.cs`, enemy definition resources, and FSM actions/transitions. Debug roots may arrange spawn markers, but spawning and behavior must work when those markers are placed in another scene.

UI feedback logic belongs in the HUD feedback systems:

- `Core/UI/Scripts/InteractionPromptView.cs`
- `Core/UI/Scripts/FloatingTextManager.cs`
- `Core/UI/Scripts/NotificationManager.cs`

Gameplay objects should publish `GameEvent.InteractionPromptChanged`, `GameEvent.FloatingTextRequested`, and `GameEvent.NotificationRequested` through `GameManager.Instance` when available.

## GameManager Strategy

The project uses one authoritative `GameManager` autoload:

```text
GameManager="*res://PackedScenes/EthraV1/Core/UI/game_manager.tscn"
```

`game_manager.tscn` uses `res://Core/Managers/GameManager.cs` and owns exported setup data such as scene folder paths, player scene, repository paths, base stats, and player sprites.

Debug scenes should depend on `GameManager.Instance`. They should not instance `game_manager.tscn` locally. If a scene needs manager services, wait until the autoload has initialized and then use `GameManager.Instance`.

`MasterNode.tscn` is the normal main scene root for world/UI composition. It does not own a child `GameManager`; the autoload owns manager setup.

## Creating A New Debug Scene

1. Add a root script only for harness setup.
2. Use the autoload `GameManager.Instance`; do not add a `GameManager` node.
3. Add a `WorldSceneRoot` if the test needs player/world spawning.
4. Add a normal `UIRoot` with prompt, floating text, and notification children if the test needs HUD feedback.
5. Instance reusable components/scenes for the feature under test.
6. Put feature behavior in the component, manager, or FSM action that would also run in production scenes.
7. Keep debug shortcuts clearly named and optional through exports.
8. Run the build and the matching debug scene script before calling the scene done.

## Warning Signs

- root debug script owns gameplay logic
- scene contains a duplicate `GameManager`
- feature only works in one debug scene
- node paths are hardcoded to one debug scene
- managers are initialized differently per scene
- UI feedback is created as one-off labels instead of published events
- interactables require a debug root script to function
- enemies require a debug root script to spawn, update health, or die

## Manual Test Checklist

- Confirm only one `GameManager` exists at runtime.
- Confirm debug scenes can access `GameManager.Instance`.
- Confirm `CombatDebugScene` still spawns the player and test enemies.
- Confirm hitbox/hurtbox damage works outside the debug root logic.
- Confirm damage floating text appears through `FloatingTextManager`.
- Confirm `InteractionDebugScene` still spawns the player and test objects.
- Confirm prompts appear through `InteractionPromptView`.
- Confirm chest/item pickup feedback publishes floating text and notifications.
- Confirm enemy spawn markers can be placed in another scene and still instance enemies.
- Confirm enemy health bars/state labels are owned by the enemy scene.
- Confirm scene changes do not create duplicate managers or null reference errors.
