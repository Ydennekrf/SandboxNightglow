# New Area Authoring Cheat Sheet

Use this page as the repeatable checklist for building a playable area and connecting a three-zone demo. It describes the systems that exist in the project now and calls out unfinished features instead of hiding them.

## Quick navigation

- [What works today](#what-works-today)
- [Three-zone demo plan](#three-zone-demo-plan)
- [1. Create and import a Tiled map](#1-create-and-import-a-tiled-map)
- [2. Create the Godot area wrapper](#2-create-the-godot-area-wrapper)
- [3. Set up map collisions](#3-set-up-map-collisions)
- [4. Add player spawn points](#4-add-player-spawn-points)
- [5. Connect areas and choose the arrival spawn](#5-connect-areas-and-choose-the-arrival-spawn)
- [6. Set up area audio](#6-set-up-area-audio)
- [7. Make a cutscene](#7-make-a-cutscene)
- [8. Set up an enemy spawner](#8-set-up-an-enemy-spawner)
- [9. Set up enemy loot](#9-set-up-enemy-loot)
- [10. Add an NPC with dialog](#10-add-an-npc-with-dialog)
- [11. Make an NPC walk around](#11-make-an-npc-walk-around)
- [12. Save and load the area](#12-save-and-load-the-area)
- [13. Final validation checklist](#13-final-validation-checklist)
- [Troubleshooting](#troubleshooting)
- [Reference files](#reference-files)

## What works today

| Feature | Status | Authoring path |
|---|---|---|
| Tiled map import | Ready | YATI imports `.tmx`/`.tmj` files as Godot packed scenes |
| Area wrapper and named spawns | Ready | `WorldSceneRoot` plus `Marker2D` spawn points |
| Travel to another area/spawn | Ready | `SceneTransfer` |
| World and trigger collisions | Ready, but must be authored | Tiled tile/object collision or Godot collision nodes |
| Global audio library | Ready | `AudioManager` under the `GameManager` autoload |
| Automatic per-area music | Not built | A small area controller must call `PlayMusic(...)` |
| Node-based cutscenes | Ready | `CutsceneOrchestrator` and child step nodes |
| Enemy spawn marker | Ready with a caller | Something must explicitly call `SpawnEnemy()` |
| Enemy patrol/combat | Ready for `TestEnemyNode` enemies | Definition resource and FSM behavior |
| Enemy loot drops | Not built | `LootTableId` exists, but `DropLoot()` is a stub |
| Static NPC dialog | Ready | `InteractionDebugNpc` and dialog JSON |
| Wandering dialog NPC | Not built | No reusable NPC patrol component exists yet |
| Scene/position/inventory save | Ready | Global four-slot save/load service |
| Defeated enemies, doors, cutscenes | Not automatically persistent | Each needs explicit game-state flags/data |

## Three-zone demo plan

Keep the first showcase deliberately small. Use three globally unique wrapper scene names; the filename without `.tscn` is the scene key.

Example:

```text
DemoVillage.tscn  <->  DemoForest.tscn  <->  DemoRuins.tscn
```

Recommended arrival markers:

| Area | Required markers |
|---|---|
| `DemoVillage` | `NewGameSpawn`, `FromDemoForest` |
| `DemoForest` | `NewGameSpawn`, `FromDemoVillage`, `FromDemoRuins` |
| `DemoRuins` | `NewGameSpawn`, `FromDemoForest` |

Build in this order:

- [ ] Make three very small Tiled maps with walkable ground and boundary collisions.
- [ ] Create one Godot wrapper scene for each imported map.
- [ ] Add the standard scene hierarchy and named spawns to all three wrappers.
- [ ] Add bidirectional transfers: Village ↔ Forest and Forest ↔ Ruins.
- [ ] Prove all four transitions and return trips work before adding content.
- [ ] Add one music call or ambient sound per area.
- [ ] Add one static dialog NPC.
- [ ] Add one working enemy spawn marker and trigger it from an area controller.
- [ ] Add one short cutscene, preferably in the final area.
- [ ] Start a game in a save slot, travel, save, return to menu, and load.
- [ ] Treat real enemy drops and wandering NPCs as follow-up implementation tasks, not demo authoring tasks.

## 1. Create and import a Tiled map

The project uses the enabled YATI importer (`addons/YATI`) for `.tmx` and `.tmj` files. A production example is:

```text
res://PackedScenes/EthraV1/Core/World/PhosphorRegion/PhosphorForest.tmx
```

Checklist:

- [ ] Open the Tiled project at `ArtAssets/Maps/Tiled/EthraProject.tiled-project`.
- [ ] Create an orthogonal map with the same tile size and conventions as a nearby existing map.
- [ ] Keep layer names descriptive: ground, below-player visuals, above-player visuals, and collision/object layers.
- [ ] Author actual physics shapes as described in [Set up map collisions](#3-set-up-map-collisions). A layer named `Collision` is not sufficient by itself.
- [ ] Save the map under `PackedScenes/EthraV1/Core/World/<Region>/` so it is inside the production scene repository tree.
- [ ] Return to Godot and wait for YATI to import the file.
- [ ] Select the `.tmx` in Godot and confirm its importer is **Import from Tiled**.
- [ ] Open or instance the imported map and confirm its visual layers appear.
- [ ] Check the Godot output for YATI import warnings.

Do not edit generated files under `.godot/imported`, and do not hand-edit `.tmx.import` files.

## 2. Create the Godot area wrapper

Do not use the imported map as the complete world scene. Create a `.tscn` wrapper around it so gameplay nodes remain separate from Tiled-generated content.

Use `PhosphorForest.tscn` or `SceneFlow_A.tscn` as a reference. Create this hierarchy:

```text
DemoForest (Node2D, WorldSceneRoot.cs)
├── Map (Node2D)
│   └── DemoForestMap (instance of DemoForest.tmx)
├── Entities (Node2D)
│   ├── Player (Node2D)
│   ├── Enemies (Node2D)
│   ├── NPCs (Node2D)
│   └── SpawnPoints (Node2D)
│       ├── NewGameSpawn (Marker2D)
│       └── FromDemoVillage (Marker2D)
├── Interactables (Node2D)
└── TileSurfaceResolver (Node, optional)
```

Checklist:

- [ ] Create a `Node2D` scene with a globally unique filename.
- [ ] Attach `PackedScenes/EthraV1/Core/Scripts/WorldSceneRoot.cs` to the root.
- [ ] Add the exact standard children shown above.
- [ ] Instance the imported `.tmx` beneath `Map`.
- [ ] Keep enemies under `Entities/Enemies`, NPCs under `Entities/NPCs`, and spawn markers directly under `Entities/SpawnPoints`.
- [ ] Put interactive triggers and transfers under `Interactables`.
- [ ] Save the wrapper somewhere below `res://PackedScenes/EthraV1/Core/`.

`GameManager` recursively registers `.tscn` files below that directory using the filename without `.tscn`. Duplicate filenames can overwrite one another in the repository, so scene filenames must be unique.

## 3. Set up map collisions

### Collision layers used by this project

| Layer number | Name | Typical use |
|---:|---|---|
| 1 | World | Walls, cliffs, water boundaries, solid scenery |
| 2 | Player | Selene's body |
| 3 | NPC | NPC bodies |
| 4 | Enemy | Enemy bodies |
| 5 | PlayerInteract | Player interaction sensor |
| 6 | NPCInteract | NPC interaction area |
| 7 | Interactables | General interactables |
| 8 | Player_Attack | Player attacks |
| 9 | Enemy_Attack | Enemy attacks |
| 10 | Enemy_Aggro | Enemy detection |

Selene is on layer 2 and scans World/NPC/Enemy for body collision. Solid map geometry must therefore be on World layer 1.

### Option A: collision on tiles in Tiled

- [ ] Open the tileset in Tiled's Tile Collision Editor.
- [ ] Draw rectangles or polygons only on tiles that should block movement.
- [ ] Keep shapes close to the visible solid footprint; avoid blocking transparent space.
- [ ] Re-save the tileset and map.
- [ ] Let Godot/YATI reimport them.
- [ ] Inspect the imported `TileMapLayer`/TileSet physics data in Godot.

### Option B: collision object layer in Tiled

- [ ] Add an object layer for walls/boundaries.
- [ ] Draw rectangles or polygons over the solid world geometry.
- [ ] Configure the objects/layer so YATI creates static collision bodies; YATI understands collision/static-body object types and the string properties `collision_layer` and `collision_mask`.
- [ ] Set world blockers to collision layer 1.
- [ ] Reimport and confirm the generated scene contains `StaticBody2D` plus `CollisionShape2D` or `CollisionPolygon2D` nodes.

### Option C: add collision in the Godot wrapper

For a small showcase map, it is acceptable to add `StaticBody2D` and child collision shapes under the wrapper's `Map` node.

- [ ] Set each `StaticBody2D` collision layer to World (1).
- [ ] Use simple rectangles/polygons and avoid many tiny overlapping shapes.
- [ ] Keep transfer triggers as `Area2D`; they should detect the player, not block the player.

### Collision verification

- [ ] Enable **Debug → Visible Collision Shapes** while running the game.
- [ ] Walk every outer boundary, wall, doorway, cliff, and water edge.
- [ ] Confirm the player cannot leave the map or pass through solid art.
- [ ] Confirm the player can still reach every transfer and interaction trigger.
- [ ] Confirm enemies do not start inside a blocker.

For switchable roofs, bridges, tunnels, or interior collision states, use `docs/map-layer-helpers.md` and the `MapLayerDebugScene.tscn` example.

## 4. Add player spawn points

Spawns are exact-name lookups beneath `Entities/SpawnPoints`.

- [ ] Add `NewGameSpawn` to every area as a safe fallback.
- [ ] Add one `Marker2D` for every entrance from another area.
- [ ] Use names that describe the source, such as `FromDemoVillage`.
- [ ] Place each marker a few pixels inside the destination, beyond the transfer trigger, so the player does not immediately transfer back.
- [ ] Point the marker's facing/position toward the playable space if that helps scene readability.
- [ ] Ensure marker names are unique inside that area's `SpawnPoints` node.

To change the new-game starting area, edit the `GameManager` node in `PackedScenes/EthraV1/Core/UI/game_manager.tscn`:

- `NewGameSceneKey`: wrapper filename without `.tscn`.
- `NewGameSpawnName`: normally `NewGameSpawn`.

## 5. Connect areas and choose the arrival spawn

Use `Core/Nodes/Interactable/SceneTransfer.cs`. The clearest examples are `SceneFlow_A.tscn` and `SceneFlow_B.tscn`.

For each exit:

- [ ] Add an `Area2D` beneath the source area's `Interactables` node.
- [ ] Attach `SceneTransfer.cs`.
- [ ] Set its collision layer to 0 and collision mask to Player (2).
- [ ] Add a `CollisionShape2D` large enough to enter reliably.
- [ ] Set `TargetSceneKey` to the destination wrapper's filename without `.tscn`.
- [ ] Set `TargetSpawnName` to the exact destination `Marker2D` name.
- [ ] Enable `RequireInteract` for doors/signposted exits, or disable it for automatic edge transitions.
- [ ] If interaction is required, configure the prompt/verb fields in the Inspector.
- [ ] Repeat in the destination area to create the return trip.

Example wiring:

| Source exit | Target scene key | Target spawn name |
|---|---|---|
| Village east exit | `DemoForest` | `FromDemoVillage` |
| Forest west exit | `DemoVillage` | `FromDemoForest` |
| Forest east exit | `DemoRuins` | `FromDemoForest` |
| Ruins west exit | `DemoForest` | `FromDemoRuins` |

The transition loads the wrapper using `SceneManager.GoToScene(...)`, then defers `GameManager.SpawnPlayerAtMarker(...)` until the new scene exists.

Current limitation: `SceneTransfer` does not record its target spawn name into `GameStateManager`. Saves still capture the player's exact position and normally restore it, but the saved fallback spawn may be `NewGameSpawn` rather than the doorway marker.

## 6. Set up area audio

There is one global `AudioManager`, already owned by the `GameManager` autoload. Do not put another audio manager in an area.

### Add a sound or music definition

- [ ] Put the audio file under an appropriate `ArtAssets/Audio/` subfolder.
- [ ] Open `Core/Audio/Data/MainAudioLibrary.tres` in the Inspector.
- [ ] Add an `AudioSoundDefinitionResource` to `Sounds`.
- [ ] Give it a stable, unique `SoundId`, for example `music.demo_forest` or `sound.demo_forest.birds`.
- [ ] Choose its category, streams, audio bus, volume, pitch settings, looping, maximum instances, positional behavior, and fade duration.
- [ ] For music, enable looping when appropriate and assign at least one real stream.

### Start area music

`WorldSceneRoot` currently has no inspector field that starts music automatically. Add or reuse a small area-owned C# controller and call this from `_Ready()`:

```csharp
GameManager.Instance?.Audio?.PlayMusic("music.demo_forest");
```

Use these calls for other audio:

```csharp
GameManager.Instance?.Audio?.PlaySceneSound("sound.demo_forest.birds", this);
GameManager.Instance?.Audio?.PlayEntity("sound.enemy.attack", enemyNode);
GameManager.Instance?.Audio?.PlayUi("sound.ui.confirm");
```

- [ ] Change music explicitly when the next area loads.
- [ ] Avoid creating ad hoc `AudioStreamPlayer` nodes for sounds that belong in the library.
- [ ] Test the definition in `AudioDebugScene.tscn` if it does not play.

### Optional footsteps by terrain

- [ ] Add a `TileSurfaceResolver` to the wrapper.
- [ ] Point its surface layer paths at the imported ground layers.
- [ ] Add a tile custom-data layer/key named `surface` and assign values such as `grass` in the Godot TileSet data imported/used by the map.
- [ ] Map each surface ID to a sound ID in `Core/Audio/Data/FootstepSurfaceLibrary.tres`.
- [ ] Verify footsteps change when crossing between surfaces.

## 7. Make a cutscene

Use the node-based system in `Core/Cutscenes/`. The complete example is `PackedScenes/EthraV1/Core/Debug/CutsceneNodeDebugScene.tscn`.

### Create the sequence

- [ ] Add a normal `Node` to the area and attach `CutsceneOrchestrator.cs`.
- [ ] Give it a stable `CutsceneId`.
- [ ] Enable `PlayOnReady` only if it should start every time the area loads.
- [ ] Enable player-control lock and restoration.
- [ ] Add ordinary child `Node`s and attach one cutscene step script to each.
- [ ] Put the child nodes in execution order. Tree order, not alphabetical name order, controls playback.
- [ ] Give steps readable names such as `Step_001_Dialog` and `Step_002_Move`.
- [ ] Keep `IsBlocking` enabled when a later step depends on completion.

### Available step nodes

**Dialog — `CutsceneDialogNode.cs`**

- [ ] Set speaker name/ID, body text, optional portrait, and advance behavior.
- [ ] Point `DialogPanelPath` to the area's `UI/InteractionDialogPanel` when fallback lookup is insufficient.

**Spawn — `CutsceneSpawnNode.cs`**

- [ ] Assign the actor `EntityScene`.
- [ ] Set its parent, usually `World/Entities/NPCs`.
- [ ] Set a spawn marker or position.
- [ ] Give it a `SpawnedEntityId` and store it for later steps.

**Movement — `CutsceneMovementNode.cs`**

- [ ] Match `TargetEntityId` to the stored spawned actor ID, or set a direct target path.
- [ ] Assign start/end `Marker2D` paths and movement/idle animation names.
- [ ] Remember movement is straight-line and does not use navigation.

**Action — `CutsceneActionNode.cs`**

- [ ] Select the target actor and animation name.
- [ ] Choose whether to wait for the animation to finish.
- [ ] Set a fallback duration so a missing optional animation cannot hang the sequence.

### Start and test it

- [ ] For an automatic intro, use `PlayOnReady`.
- [ ] For a triggered cutscene, call the orchestrator's `Play()` from an area controller/trigger; no generic cutscene trigger component exists yet.
- [ ] Confirm control locks, steps play in order, dialog advances, and control returns.
- [ ] Do not rely on cutscene completion persisting across save/load unless you add an explicit game-state flag.

Current limitations include no branching, camera/fade/audio/wait/quest/choice steps and no navigation-based movement.

## 8. Set up an enemy spawner

Use:

```text
res://PackedScenes/EthraV1/Core/Entities/Debug/EnemySpawnMarker.tscn
```

Its current enemy example is `TestEnemy.tscn`, configured by an `EnemyDefinitionResource` such as `test_enemy_definition.tres`.

- [ ] Ensure the area has `Entities/Enemies` and `Entities/SpawnPoints`.
- [ ] Instance `EnemySpawnMarker.tscn` under `Entities/SpawnPoints`.
- [ ] Assign `EnemyScene`.
- [ ] Optionally assign `EnemyDefinitionOverride` for this enemy variant.
- [ ] Leave `EnemyParentPath` pointing to the sibling `Enemies` container (`../Enemies`) when using the standard hierarchy.
- [ ] Give each marker a unique `SpawnedEnemyName`.
- [ ] Select `Active`, `Timed`, or `Proximity` spawn mode.
- [ ] Configure delay/proximity, patrol radius, detection range, leash range, and attack range.
- [ ] Keep the marker and its patrol radius away from walls and transfer triggers.

Critical: the marker does not automatically spawn an enemy. An area controller must call `SpawnEnemy()` once for each marker. `InteractionDebugSceneRoot.cs` shows how to enumerate markers and spawn them on ready; `DebugButtonInteraction.cs` shows an interactive call.

- [ ] Add/reuse a scene-root controller that calls each intended marker once.
- [ ] Avoid calling the same marker repeatedly unless respawning is deliberate.
- [ ] Test Active, Timed, or Proximity behavior after the enemy has been instanced.

`TestEnemyNode` patrol is random steering inside a radius, not navigation-aware pathfinding. Collision can confine it, but it may choose unreachable points behind walls.

## 9. Set up enemy loot

Enemy loot is not currently authorable as a working drop system.

- `EnemyDefinitionResource.LootTableId` exists.
- Enemy death calls `TestEnemyNode.DropLoot()` once.
- `DropLoot()` currently only prints a debug message.
- The base loot table/dropper files are empty scaffolds.

For now:

- [ ] Set a stable `LootTableId` only as future metadata if useful.
- [ ] Do not expect an item to appear when the enemy dies.
- [ ] Do not spend demo-authoring time creating loot tables until the dropper/table/pickup integration is implemented.

A future implementation needs to resolve the table ID, roll entries, instance an item pickup scene under the current world, assign item ID/quantity, and decide whether the drop persists across save/load.

## 10. Add an NPC with dialog

Use the current static example:

```text
res://PackedScenes/EthraV1/Core/Entities/Debug/InteractionDebugNpc.tscn
```

### Create dialog data

- [ ] Create a JSON file under `Core/Dialog/Data/`.
- [ ] Give it a unique `treeId`.
- [ ] Set `startingNodeId` to an existing node ID.
- [ ] Add nodes with speaker text.
- [ ] Give each choice either a valid `nextNodeId` or `endsDialog: true`.
- [ ] Validate every referenced node ID.
- [ ] Optionally use the Dialog Graph dock/plugin described in `docs/dialog-graph-editor.md`.

Minimal shape:

```json
{
  "treeId": "demo_village_guide",
  "startingNodeId": "start",
  "nodes": [
    {
      "nodeId": "start",
      "speakerName": "Village Guide",
      "text": "The forest road is east of here.",
      "choices": [
        { "choiceText": "Thanks.", "endsDialog": true }
      ]
    }
  ]
}
```

### Place and configure the NPC

- [ ] Ensure the playable host has a panel at `UI/InteractionDialogPanel`. The current production `MasterNode.tscn` does **not** instance this panel yet; the debug scenes do.
- [ ] For production use, knowingly instance `PackedScenes/EthraV1/Core/UI/InteractionDialogPanel.tscn` under `MasterNode/UI` and name it `InteractionDialogPanel`, or configure each NPC's `DialogPanelPath` to a panel that actually exists.
- [ ] Instance `InteractionDebugNpc.tscn` beneath `Entities/NPCs`.
- [ ] Set `NpcName`.
- [ ] Set `DialogTreeId` to exactly match the JSON `treeId`.
- [ ] Set the interaction verb/prompt and priority.
- [ ] Check `DialogPanelPath`; the safest shared setup is the current scene's `UI/InteractionDialogPanel` fallback once that panel has been added to `MasterNode`.
- [ ] Confirm the NPC interaction `Area2D` overlaps the player's interaction sensor.
- [ ] Walk to the NPC, press the `Interact` action, traverse every choice, and end the conversation.

## 11. Make an NPC walk around

There is no reusable walking/patrol component for dialog NPCs in the current codebase. The supported dialog NPC is static, and enemy patrol code is enemy-specific.

For the three-zone demo:

- [ ] Use static dialog NPCs.
- [ ] Place them where they do not block transfers or narrow passages.
- [ ] Do not attach the enemy FSM/patrol action to an NPC as a shortcut.

Treat a wandering dialog NPC as a small feature task: it should preserve the existing dialog interaction, pause during dialog, use bounded waypoints or navigation, resume afterward, and define saved/restored position behavior.

## 12. Save and load the area

Save/load is global. Do not add a save manager node to each area.

For a new area to restore correctly:

- [ ] Keep its unique wrapper `.tscn` beneath `res://PackedScenes/EthraV1/Core/`.
- [ ] Attach `WorldSceneRoot.cs` and keep the standard containers/paths.
- [ ] Include `NewGameSpawn` as a fallback.
- [ ] Enter the area through `SceneManager`/`SceneTransfer`, not by directly changing the Godot scene.
- [ ] Start a new game by selecting one of the four save slots; this establishes the active slot.
- [ ] Use the HUD Save button, which calls `GameManager.SaveCurrentGame()`.
- [ ] Return to the menu and load the occupied slot.
- [ ] Confirm the saved scene key, player position, inventory, equipment, and game state restore.

Save files are stored at:

```text
user://saves/save_1.json
user://saves/save_2.json
user://saves/save_3.json
user://saves/save_4.json
```

Currently saved: scene/location metadata, exact player position and stats, inventory/equipment, and general game-state data.

Currently not automatically saved: defeated enemies, opened doors/chests, completed cutscenes, spawned cutscene actors, and complete runtime quest progress. Add stable game-state flags/data before relying on any of those across a reload.

## 13. Final validation checklist

Build before runtime testing:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

Then test the complete demo:

- [ ] Start from the main menu in a new save slot.
- [ ] Spawn at the intended `NewGameSpawn` in Zone A.
- [ ] Walk every boundary with visible collision shapes enabled.
- [ ] Travel A → B and arrive at the intended `FromZoneA` marker.
- [ ] Travel B → A and arrive beyond the return trigger.
- [ ] Travel B → C and arrive at the intended `FromZoneB` marker.
- [ ] Travel C → B and arrive beyond the return trigger.
- [ ] Confirm each area's music/ambience starts and old music does not overlap unexpectedly.
- [ ] Speak to every NPC and traverse every dialog branch.
- [ ] Trigger every enemy marker and verify enemies spawn only once as intended.
- [ ] Verify patrol, detection, leash, attack, hurt, and death behavior.
- [ ] Run the cutscene and confirm control is restored afterward.
- [ ] Save while standing in Zone B or C.
- [ ] Return to the main menu and load the slot.
- [ ] Confirm the same area and player position restore.
- [ ] Check the Godot output for missing scene keys, spawns, dialog IDs, audio IDs, and null references.

Do not claim enemy loot or wandering NPCs are working in the showcase until those systems are implemented.

## Troubleshooting

### `Scene '<name>' was not found in the repository`

- Confirm the wrapper is a `.tscn` below `PackedScenes/EthraV1/Core/`.
- Use the filename without `.tscn` as `TargetSceneKey`.
- Check for another `.tscn` with the same filename.
- Restart/run through the normal `GameManager` startup so the repository is populated.

### Player appears at the wrong place or does not appear

- Confirm the root has `WorldSceneRoot.cs`.
- Confirm the marker is directly under `Entities/SpawnPoints`.
- Match `TargetSpawnName` exactly, including capitalization.
- Confirm `Entities/Player` exists.
- Keep a `NewGameSpawn` fallback.

### A painted collision layer does not block the player

- Layer naming does not create physics.
- Confirm the TileSet tiles have collision polygons, or the imported object layer created static bodies/shapes.
- Confirm blockers use World collision layer 1.
- Run with visible collision shapes enabled.

### Transfer does not trigger

- Confirm the transfer is an `Area2D` with an enabled shape.
- Set its collision mask to Player (2).
- If `RequireInteract` is enabled, press the project's `Interact` input while overlapping it.
- Confirm both target scene key and spawn name are non-empty and valid.

### Enemy marker does nothing

- Assign `EnemyScene`.
- Confirm an area controller actually calls `SpawnEnemy()`.
- Confirm `EnemyParentPath` resolves to `Entities/Enemies`.
- For Timed/Proximity modes, remember the instanced enemy may initially be hidden.

### NPC dialog does not open

- Match `DialogTreeId` to the JSON `treeId` exactly.
- Validate `startingNodeId` and every `nextNodeId`.
- Confirm `UI/InteractionDialogPanel` exists.
- Confirm the NPC interaction area overlaps the player interaction sensor.

### Audio ID warns or produces silence

- Confirm the exact sound ID exists in `MainAudioLibrary.tres`.
- Assign at least one real stream to the definition.
- Confirm the audio bus name exists and volume is audible.
- Confirm an area controller actually calls `PlayMusic(...)` or `PlaySceneSound(...)`.

## Reference files

| Purpose | Path |
|---|---|
| Production Tiled wrapper | `PackedScenes/EthraV1/Core/World/PhosphorRegion/PhosphorForest.tscn` |
| Imported Tiled example | `PackedScenes/EthraV1/Core/World/PhosphorRegion/PhosphorForest.tmx` |
| Standard world root | `PackedScenes/EthraV1/Core/Scripts/WorldSceneRoot.cs` |
| Two-way scene flow examples | `PackedScenes/EthraV1/Core/Debug/SceneFlow/SceneFlow_A.tscn`, `SceneFlow_B.tscn` |
| Transfer component | `Core/Nodes/Interactable/SceneTransfer.cs` |
| Audio guide/library | `docs/audio-manager.md`, `Core/Audio/Data/MainAudioLibrary.tres` |
| Cutscene guide/example | `docs/cutscene-nodes.md`, `PackedScenes/EthraV1/Core/Debug/CutsceneNodeDebugScene.tscn` |
| Enemy guide/marker | `docs/enemy-behavior.md`, `PackedScenes/EthraV1/Core/Entities/Debug/EnemySpawnMarker.tscn` |
| Static dialog NPC | `PackedScenes/EthraV1/Core/Entities/Debug/InteractionDebugNpc.tscn` |
| Dialog authoring | `docs/dialog-trees.md`, `docs/dialog-graph-editor.md` |
| Save/load behavior | `docs/save-load.md` |
| Dynamic map layers | `docs/map-layer-helpers.md` |
