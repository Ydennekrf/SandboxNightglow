# Quest System

## Responsibility

The quest system owns quest definition lookup, starting quests, tracking active runtime state, updating objective progress from game events, publishing quest progress/completion events, and exposing read-only entries for quest log UI.

It does not own quest markers, rewards, prerequisites, or save/load persistence yet.

## Data Location

Quest JSON files live in:

```text
res://Core/Quest/Data/
```

`GameManager.QuestDefinitionDataFolderPath` points at that folder and `MasterRepository.FillQuestDefinitionRepo` loads every `.json` file there during startup.

## Quest Definition Format

```json
{
  "questId": "quest_test_speak_to_npc",
  "title": "Go Speak to the Test NPC",
  "description": "A simple quest used to verify dialog-driven quest starts.",
  "startingObjectiveId": "speak_to_test_npc",
  "objectives": [],
  "rewards": []
}
```

`startingObjectiveId` is optional. If it is empty, the first objective in `objectives` is used.

## Objective Definition Format

```json
{
  "objectiveId": "speak_to_test_npc",
  "objectiveType": "SpeakToNpc",
  "targetId": "test_npc",
  "requiredCount": 1,
  "areaId": "",
  "timeLimitSeconds": 0,
  "personId": "",
  "actionId": "",
  "goldAmount": 0,
  "description": "Speak to the Test NPC."
}
```

Only fields needed by the current objective type must be filled in.

## Runtime State

`QuestRuntimeState` stores:

- `QuestId`
- `Status`: `NotStarted`, `Active`, `Completed`, or `Failed`
- `CurrentObjectiveId`
- per-objective `CurrentCount` and `IsCompleted`

Runtime quest state is currently memory-only.

## Quest Log UI

The first quest log UI lives in the player menu:

```text
res://PackedScenes/EthraV1/Core/UI/player_menu.tscn
```

`QuestLogPanel` subscribes to:

- `GameEvent.QuestStarted`
- `GameEvent.QuestUpdated`
- `GameEvent.QuestCompleted`

It reads display-friendly entries from `QuestManager.GetQuestLogEntries(...)` and shows active and completed quests with the current objective text and progress.

## Dialog Quest Start

Dialog choices can start quests with:

```json
{
  "actionId": "start_quest",
  "actionPayload": "quest_test_speak_to_npc"
}
```

`DialogActionRunner` routes this to `GameManager.Instance.Quest.StartQuest(...)`.

Expected debug output:

```text
[Quest] Started quest: quest_test_speak_to_npc
```

If the quest is already active or completed, the manager prints a safe status message instead of restarting it.

## SpeakToNpc Objectives

`DialogManager.StartDialog` publishes `GameEvent.NpcSpokenTo` with an `NpcSpokenToQuestEvent`.

`QuestManager` checks active quests. A `SpeakToNpc` objective completes when:

- the objective is the current objective
- `objective.TargetId` matches the spoken-to NPC id

Expected debug output:

```text
[Quest] Objective completed: speak_to_test_npc
[Quest] Completed quest: quest_test_speak_to_npc
```

## CollectItem Objectives

`InventoryManager.AddItem` publishes `GameEvent.PickupItem` with an `ItemCollectedQuestEvent` after an item is successfully added.

`QuestManager` checks active quests. A `CollectItem` objective progresses when:

- the objective is the current objective
- `objective.TargetId` matches the collected item id as text

The interaction debug chest grants item `3001` Copper Ore, which can complete `quest_test_collect_item`.

## Debug Scene

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1
```

Scene:

```text
res://PackedScenes/EthraV1/Core/Debug/InteractionDebugScene.tscn
```

## Manual Test Steps

1. Run the interaction debug scene.
2. Interact with `DebugMerchant`.
3. Choose `Start speak quest`.
4. Confirm `[Quest] Started quest: quest_test_speak_to_npc`.
5. Open the player menu with the inventory toggle and confirm the quest appears in the Quest Log.
6. Walk to `TestNpc` and interact.
7. Confirm `[Quest] Objective completed: speak_to_test_npc`.
8. Confirm `[Quest] Completed quest: quest_test_speak_to_npc`.
9. Confirm the quest log updates to `Completed`.
10. Optional: interact with `DebugMerchant` and choose `Start collect quest`.
11. Open the debug chest.
12. Confirm the collect objective and quest complete.

## Current Limitations

- Quest runtime state is not saved or loaded.
- Only the current objective progresses; branching is not implemented.
- Rewards are data-stubbed only.
- Quest log UI is a first-pass read-only panel.
- No quest marker UI exists yet.
- Timed objectives and failure conditions are not implemented.
- NPC ids currently come from the dialog interaction `NpcName` export.

## Future Objective Types

- `CollectItem`
- `KillEnemy`
- `ExploreArea`
- `CompletePuzzle`
- `ReachLocationWithinTime`
- `ReachFriendship`
- `PerformActionCount`
- `PayGold`

## Future Follow-Up Tasks

- quest journal filters/details
- quest markers
- save/load quest progress
- quest rewards
- quest prerequisites
- branching quest outcomes
- failure conditions
- timed objectives
- editor tooling
