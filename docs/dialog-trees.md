# Dialog Trees

## What A Dialog Tree Is

A dialog tree is data that describes one conversation. It has a tree ID, a starting node, and a list of dialog nodes. Each node contains speaker text and player choices. Choices can move to another node, end the dialog, trigger a lightweight action, or be shown only when a simple condition passes.

This is intended as the foundation for future graph/editor tooling, but the current implementation is simple JSON.

## Data Location

Dialog tree JSON files live in:

```text
res://Core/Dialog/Data/
```

The interaction debug example is:

```text
res://Core/Dialog/Data/interaction_debug_merchant.json
```

## Data Format

```json
{
  "treeId": "example_tree",
  "startingNodeId": "start",
  "nodes": [
    {
      "nodeId": "start",
      "speakerName": "NPC Name",
      "text": "Hello there.",
      "graphX": 0,
      "graphY": 0,
      "choices": [
        {
          "choiceText": "Ask a question",
          "nextNodeId": "question_response",
          "conditionId": "debug_true",
          "conditionPayload": "",
          "fromAnchor": "right",
          "toAnchor": "left"
        },
        {
          "choiceText": "Goodbye",
          "endsDialog": true
        }
      ]
    }
  ]
}
```

## Runtime Classes

- `DialogTree`: tree ID, starting node ID, and nodes.
- `DialogNode`: node ID, speaker name, text, optional graph editor position, and choices.
- `DialogChoice`: choice text, optional next node, optional end flag, optional action ID, optional action payload, optional condition ID, optional condition payload, and optional graph connection anchors.
- `MasterRepository`: loads JSON files from `res://Core/Dialog/Data/` into the dialog tree repo.
- `DialogManager`: starts a tree, displays nodes, filters choices by condition, handles choices, branches, actions, and dialog ending.
- `DialogActionRunner`: runs known lightweight dialog actions.
- `DialogConditionRunner`: evaluates known lightweight dialog conditions.

## Creating A New Dialog Tree

1. Add a new `.json` file under `res://Core/Dialog/Data/`.
2. Give it a unique `treeId`.
3. Set `startingNodeId` to one of the node IDs in `nodes`.
4. Add one or more nodes.
5. Add choices that either set `nextNodeId`, set `endsDialog: true`, or both trigger an action and branch/end.
6. Optionally set `graphX` and `graphY`, or use the visual graph editor to place nodes and save those fields.

## Assigning A Tree To An NPC

For the current debug NPC scene:

```text
res://PackedScenes/EthraV1/Core/Entities/Debug/InteractionDebugNpc.tscn
```

Set the exported `DialogTreeId` property on `DebugNpcInteraction` to the JSON tree's `treeId`.

## Branching Choices

A choice branches by setting `nextNodeId`:

```json
{
  "choiceText": "Back",
  "nextNodeId": "start"
}
```

If `nextNodeId` is missing or empty, the dialog ends unless the choice has a valid branch.

## Ending Dialog

A choice ends dialog by setting:

```json
{
  "choiceText": "Goodbye",
  "endsDialog": true
}
```

Nodes with no choices show a generated `Continue` option that closes the dialog.

## Conditional Choices

A choice can be shown only when a condition passes:

```json
{
  "choiceText": "Debug-only visible choice",
  "nextNodeId": "debug_node",
  "conditionId": "debug_true",
  "conditionPayload": ""
}
```

Current condition IDs:

- empty or missing: always show the choice
- `debug_true`: always show the choice
- `debug_false`: always hide the choice
- `npc_friendship_greater_than_stub`: known placeholder for `NPC friendship > value`
- `quest_complete_stub`: known placeholder for `quest is complete`
- `item_in_inventory_stub`: known placeholder for `item is in inventory`

These are intentionally small placeholders so the graph, JSON, editor, and runtime filtering paths can be tested before quest, friendship, flag, or relationship systems exist.

The placeholder conditions are known to validation but return false at runtime until their backing gameplay providers are wired.

## Dialog Actions

Choices can run a known action before branching or ending:

```json
{
  "choiceText": "Open shop",
  "actionId": "store_stub",
  "nextNodeId": "start"
}
```

Currently supported actions:

- `store_stub`: prints `[StoreStub] Opening store screen for NPC: <npc name>`
- `complete_quest_stub`: prints a quest completion stub message with the payload
- `update_friendship_stub`: prints a friendship update stub message with the payload

The actions are intentionally small. They prove dialog choices can trigger future systems without building store UI, quest completion, inventory, pricing, money, or friendship persistence yet.

## Connection Anchors

Choices can set optional graph connection anchors:

```json
{
  "choiceText": "Back",
  "nextNodeId": "start",
  "fromAnchor": "left",
  "toAnchor": "right"
}
```

Supported anchor values are `left`, `right`, `top`, and `bottom`. These only affect the visual graph editor.

## Running The Debug Scene

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1
```

## Visual Editing

The visual graph editor is available as a runtime/debug tool scene:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-dialog-graph-editor.ps1
```

See `docs/dialog-graph-editor.md` for the current workflow, zoom/pan controls, drag layout, conditions, and limitations.

Manual test flow:

1. Walk to the debug merchant.
2. Press interact.
3. Confirm the starting dialog appears.
4. Select `Who are you?`.
5. Select `Back`.
6. Select `Open shop`.
7. Confirm the store stub message appears in the console.
8. Select `Goodbye`.
9. Confirm the dialog closes and player movement unlocks.

## Common Mistakes

- Missing starting node: `startingNodeId` must match a node in `nodes`.
- Choice points to missing node: every `nextNodeId` must match a node in the same tree.
- Dialog tree ID does not match: the NPC `DialogTreeId` must equal the JSON `treeId`.
- Action ID is unknown: unsupported `actionId` values log a warning and do nothing.
- Condition ID is unknown: unsupported `conditionId` values fail editor validation and are hidden at runtime.
- Dialog UI does not open: confirm the scene has `UI/InteractionDialogPanel` and the NPC can find it.

## Future Improvements

- flags
- quest checks
- friendship checks
- localization
- proper store screen
- editor tooling for creating trees
