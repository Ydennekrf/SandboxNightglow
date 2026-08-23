# Interaction Debugging

## Purpose

`res://PackedScenes/EthraV1/Core/Debug/InteractionDebugScene.tscn` is a focused manual scene for testing reusable interaction examples without modifying production gameplay scenes.

It demonstrates:

- an interactable NPC with dialog choices
- a dialog choice that stubs the future store flow
- an interactable chest that does not use dialog
- a reusable pickup popup above the player
- prompt, floating text, and notification events through the shared HUD feedback systems

The scene uses the project autoload `GameManager`. It should not instance a local `GameManager` node.

## Scene And Object Paths

- Debug scene: `res://PackedScenes/EthraV1/Core/Debug/InteractionDebugScene.tscn`
- Debug NPC scene: `res://PackedScenes/EthraV1/Core/Entities/Debug/InteractionDebugNpc.tscn`
- Debug chest scene: `res://PackedScenes/EthraV1/Core/WorldObjects/InteractionDebugChest.tscn`
- Dialog panel scene: `res://PackedScenes/EthraV1/Core/UI/InteractionDialogPanel.tscn`
- NPC script: `res://Core/Nodes/Interactable/DebugNpcInteraction.cs`
- Dialog tree data: `res://Core/Dialog/Data/interaction_debug_merchant.json`
- Chest script: `res://Core/Nodes/Interactable/DebugChestInteraction.cs`
- Pickup popup script: `res://Core/UI/Scripts/PickupPopupLabel.cs`

## How To Run

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1
```

## Current Interaction Flow

The current EthraV1 interaction examples follow the existing Area2D pattern used by interactable nodes such as item pickups and teleports:

1. The debug NPC or chest detects the player through overlap with `PlayerNode` or the player's `InteractionArea`.
2. The interactable checks `Input.IsActionJustPressed("Interact")`.
3. The NPC asks `DialogManager` to start its configured `DialogTreeId`.
4. `DialogManager` loads the JSON tree and opens the debug dialog panel.
5. The chest handles pickup feedback directly and does not open dialog.

The EthraV1 `DialogManager` now handles JSON dialog tree traversal for this debug flow while still using the small `InteractionDialogPanel` UI.

The player scene also has `components/Interact/Base/InteractComponent.cs` attached to its `InteractionArea` for reusable prompt selection around `IInteractable` objects. Current EthraV1 debug/world interactables may still self-detect the player and publish prompt events directly; both paths feed `InteractionPromptView`.

## NPC Dialog Choices

The debug NPC uses `DialogTreeId = interaction_debug_merchant`, defined in `res://Core/Dialog/Data/interaction_debug_merchant.json`. It starts with these choices:

- `Who are you?`
- `Open shop`
- `Goodbye`

`Who are you?` shows another dialog line and offers follow-up choices. `Goodbye` hides the dialog panel.

## Store Stub

The `Open shop` choice intentionally does not create a store UI or inventory. It prints:

```text
[StoreStub] Opening store screen for NPC: <npc name>
```

This proves dialog choices can trigger the future store flow while keeping store inventory and shop UI out of scope.

## Chest Pickup Popup

The debug chest is a one-shot interactable. On first interaction it:

- toggles from closed to opened visual state
- prints `[ChestDebug] Player opened chest and received Test Loot.`
- adds a `PickupPopupLabel` above the player with `Found: Test Loot`

Repeated interactions print that the chest is already open and do not grant another pickup.

## Intentionally Stubbed

- real store screen
- store inventory data
- chest loot tables
- inventory insertion
- save/load state for opened chests
- polished pickup popup visuals

## Debug Scene Ownership

`InteractionDebugSceneRoot` should stay a test harness. It may spawn the test player and run world-state smoke checks. NPC, chest, button, item pickup, door, prompt, dialog, floating text, and notification behavior should live in reusable components/managers.

See `docs/debug-scene-architecture.md` for the shared debug-scene rules and GameManager/autoload strategy.

## Future Follow-Up Tasks

- real store screen
- store inventory data
- chest loot table
- inventory insertion
- save/load opened chest state
- better pickup popup visuals
