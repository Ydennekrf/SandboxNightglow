# Dialog Graph Editor

## Purpose

`res://PackedScenes/Tools/DialogGraphEditor.tscn` is the runtime visual editor for JSON dialog trees.

It is also wrapped by the `Dialog Graph Editor` editor plugin as a dock in the Godot editor.

## How To Run

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-dialog-graph-editor.ps1
```

The tool lists `.json` files under `res://Core/Dialog/Data/` by default and selects `interaction_debug_merchant.json` when present.

## Editor Dock

The dock plugin lives at:

```text
res://addons/DialogGraphEditor/plugin.cfg
```

It is enabled in `project.godot`. In the Godot editor, look for a `Dialog Graph` dock on the right side. If it is hidden, check `Project > Project Settings > Plugins` and make sure `Dialog Graph Editor` is enabled.

## Layout

- Top toolbar: data folder, JSON file dropdown, refresh/load/save/new/add/auto-layout/validate actions.
- Zoom controls: `-`, percent label, `+`, and `Reset`.
- Main canvas: node cards laid out as a simple graph.
- Link lines: choices with `nextNodeId` draw connections between cards.
- Conditional link lines are tinted and labeled with their `conditionId`.
- Right inspector: edits the tree, selected node, and selected node choices.

## Current Workflow

1. Run the tool scene.
2. Select a node card in the canvas.
3. Edit node fields in the inspector.
4. Click `Apply Node Fields`.
5. Add or edit choices in the inspector.
6. Click `Apply Choice` for each changed choice.
7. Click `Validate`.
8. Click `Save`.

`New` creates an unsaved tree and selects a unique filename such as `new_dialog_tree.json`. Saving a new tree will not overwrite an existing JSON file.

Node cards can be dragged on the canvas. Saved positions are written into the JSON as `graphX` and `graphY` on each node.

Use the zoom buttons or mouse wheel over the canvas to zoom. Right-drag or middle-drag on empty canvas space to pan through the scroll view.

## Supported Editing

- load a JSON dialog tree
- create a new tree
- edit `treeId`
- set the starting node
- add nodes
- auto-layout nodes by branch depth
- drag nodes and save graph positions
- rename nodes and update existing choice references
- delete nodes and safely convert incoming references to end-dialog choices
- edit speaker/text
- add/remove choices
- set choice target node
- set choice connection anchors with `From` and `To` side dropdowns
- mark choices as ending dialog
- set stub actions on choices
- set debug and future-system stub conditions on choices
- save pretty-printed JSON

## Actions

Current action IDs:

- empty: no action
- `store_stub`: logs the store stub message
- `complete_quest_stub`: logs a quest completion stub message
- `update_friendship_stub`: logs a friendship update stub message

Payloads are free text for now. Suggested payload examples:

- `quest=first_delivery`
- `npc=Test NPC;delta=5`

## Conditions

Choice conditions are available as a first scaffold for future quest, flag, and friendship checks.

Current condition IDs:

- empty: always show the choice
- `debug_true`: always show the choice
- `debug_false`: always hide the choice
- `npc_friendship_greater_than_stub`: known placeholder, hidden until a friendship provider is wired
- `quest_complete_stub`: known placeholder, hidden until a quest provider is wired
- `item_in_inventory_stub`: known placeholder, hidden until an inventory provider is wired

Unknown condition IDs fail validation in the editor and are hidden at runtime if they somehow reach the game.

## Limitations

- links are edited in the inspector, including target node and anchor sides
- no real quest, flag, friendship, or localization condition providers yet
- dock layout is basic and reuses the runtime tool UI

## Next Phase Ideas

- create links by dragging from choices to nodes
- file picker and tree list
- stronger validation report panel
- action registry dropdown from runtime action definitions
- condition registry dropdown from runtime condition definitions
