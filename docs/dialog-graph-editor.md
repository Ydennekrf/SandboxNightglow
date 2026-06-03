# Dialog Graph Editor

## Purpose

`res://PackedScenes/Tools/DialogGraphEditor.tscn` is the phase-1 visual editor for JSON dialog trees.

It is a runtime/debug tool scene, not a full Godot editor dock yet. It gives a first usable workflow for loading, inspecting, editing, validating, and saving dialog tree JSON files.

## How To Run

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-dialog-graph-editor.ps1
```

The tool opens `res://Core/Dialog/Data/interaction_debug_merchant.json` by default.

## Layout

- Top toolbar: data folder, file name, load/save/new/add/validate actions.
- Main canvas: node cards laid out as a simple graph.
- Link lines: choices with `nextNodeId` draw connections between cards.
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

## Supported Editing

- load a JSON dialog tree
- create a new tree
- edit `treeId`
- set the starting node
- add nodes
- rename nodes and update existing choice references
- delete nodes and safely convert incoming references to end-dialog choices
- edit speaker/text
- add/remove choices
- set choice target node
- mark choices as ending dialog
- set `store_stub` as a choice action
- save pretty-printed JSON

## Limitations

- node positions are auto-laid out and are not saved yet
- links are visual only; connections are edited in the inspector
- no drag-and-drop node movement yet
- no file picker yet
- no conditions, flags, quest checks, or localization editing yet
- this is not a docked `EditorPlugin` yet

## Next Phase Ideas

- persistent graph node positions
- drag cards around the canvas
- create links by dragging from choices to nodes
- file picker and tree list
- stronger validation report panel
- Godot editor dock plugin wrapper
- action registry dropdown from runtime action definitions
