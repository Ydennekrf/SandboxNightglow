# Player State Machine

## Overview

The active EthraV1 player uses the model-driven C# FSM under `Core/Entity/Scripts/StateMachine/`.
`PlayerNode` reads Godot input and animation output, while `Player` owns gameplay request state and the FSM chooses states during `Player.Tick`.

Current player states:

- `Idle`
- `Walk`
- `Run`
- `Dodge`
- `Attack`
- `Harvest`
- `Dialog`

## Harvest State

Harvest starts through the interaction path:

```text
Player InteractionArea/InteractComponent
-> HarvestableInteractable.BeginInteraction(PlayerNode)
-> PlayerNode.RequestHarvest(...)
-> Player FSM transitions to Harvest
-> HarvestAction completes
-> HarvestableInteractable grants inventory reward
```

`HarvestableInteractable` owns harvest data:

- `StableId`
- `ItemId`
- `Quantity`
- `OneShot`
- `HarvestDurationSeconds`
- `AnimationKey`
- `ToolType`
- prompt text and priority

The harvestable does not directly set the player state. It submits a `PlayerHarvestRequest`; the FSM decides when the request can run. Dialog blocks new harvest requests.

`AnimationKey` controls the body animation hook. Current debug examples use `Mining` for ore/tree work and `Harvest` for herbs and chest opening.

`ToolType` controls the temporary harvest tool visual:

- `Pickaxe` shows `HarvestPickaxeSprite` on the player `Sprites/Mining` node.
- `Axe` shows `HarvestAxeSprite` on the same player tool node.
- `None` hides the tool node and is intended for `Harvest` actions such as herbs, chests, and loose pickups.

`GameManager` exposes `HarvestPickaxeSprite` and `HarvestAxeSprite`. `PlayerNode` accepts those via `ApplyHarvestToolSprites`, stows the normal weapon sprites, enables the tool sprite on harvest enter, and restores the previous weapon/tool visibility on harvest exit.

`HarvestAction` stops movement, faces the target when the player node can be found, sets harvest visuals, attempts `<AnimationKey>_<Facing>` then `<AnimationKey>`, and completes after the configured/fallback duration. Missing animations print:

```text
[PlayerFSM] Harvest animation missing or unavailable; completing harvest with fallback timing.
```

On completion, `HarvestableInteractable` calls `InventoryManager.AddItemQuantity`, publishes pickup floating text and a loot notification when UI listeners exist, and prints:

```text
[Harvest] Collected <quantity> x <itemId>
```

One-shot harvestables hide their available visual and stop interacting after success. The exported `StableId` is present for future save/load integration, but harvested state is not persisted yet.

## Dialog State

Dialog starts and ends through `DialogManager` events:

```text
DialogManager.StartDialog
-> GameEvent.DialogStarted
-> PlayerNode marks Player.DialogActive
-> Player FSM transitions to Dialog
```

`DialogAction` stops movement and ignores movement/combat transitions while dialog is active. `DialogManager.EndDialog` or external panel close publishes `DialogEnded`, and the FSM returns to `Idle`.

When dialog shows a node, `DialogManager` publishes `DialogNodeChanged` with tree id, node id, speaker name, `IsPlayerSpeaker`, and animation key. `PlayerNode` requests a dialog animation only when `IsPlayerSpeaker` is true. `DialogAction` tries:

- configured key with facing suffix
- configured key
- `Talk_<Facing>`
- `Talk`
- `Dialog_<Facing>`
- `Dialog`

Missing dialog animations print once and do not block dialog:

```text
[PlayerFSM] Dialog animation missing or unavailable; skipping player speaker animation hook.
```

## Debug Scene

`PackedScenes/EthraV1/Core/Debug/InteractionDebugScene.tscn` includes:

- `CopperOreHarvestable`, using `Mining` plus `Pickaxe`, granting `3001` Copper Ore x2
- `TrainingTreeHarvestable`, using `Mining` plus `Axe`
- `HerbCollectable`, using `Harvest` plus no tool, granting `1001` Small Health Potion x1
- `DebugChest`, using `Harvest` plus no tool before opening
- debug NPC dialog with a `Player` speaker branch

Manual checklist:

- Run `tools/run-interaction-debug.ps1`.
- Walk to `CopperOreHarvestable`.
- Confirm prompt says `Press E to Harvest`.
- Press interact.
- Confirm movement is locked/ignored during Harvest.
- Confirm weapon sprites are stowed during Harvest.
- Confirm Mining shows the pickaxe when `HarvestPickaxeSprite` is assigned.
- Confirm tree Mining shows the axe.
- Confirm herb/chest Harvest shows no tool.
- Confirm missing harvest animation logs once if no animation exists.
- Confirm Copper Ore x2 is added and pickup feedback appears.
- Confirm the one-shot harvestable cannot be collected again.
- Talk to `DebugMerchant`.
- Choose `Player line`.
- Confirm player enters Dialog and the safe dialog animation hook is attempted.
- End dialog and confirm movement resumes.

## Current Limitations

- Harvested state is not saved/restored yet.
- Required tools, skills, and respawn timers are not implemented.
- Final harvest and dialog/talk animations are not authored here.
- Dialog speaker detection currently treats `Player`, `Selene`, or the current player name as player speakers.
- Dialog exits to `Idle`; it does not restore pre-dialog locomotion state.

## Follow-Ups

- Final harvest animations.
- Tool-specific harvest animations.
- Harvest respawn timers.
- Required tools and skills.
- Save/load for harvested node state using `StableId`.
- Dialog emotion animation keys.
- Portrait/body animation sync.
- Player control lock polish.
