# Save/Load

## Current Support

The current save/load slice supports four manual save slots, JSON save files, save-slot metadata, player snapshot data, `InventoryManager` snapshots, and `GameStateManager` snapshots.

New games are started by choosing a save slot and entering a player name. The selected slot becomes the active slot for that play session. Loaded saves also remember their slot, so the HUD `Save` button writes back to the loaded slot.

Runtime quest progress is not serialized yet. Save metadata can show the current active quest title when the quest log exposes one, otherwise it uses the `GameStateManager` main-story quest metadata fallback.

## Save File Location

Save files are written under:

```text
user://saves/
```

Slot files use this shape:

```text
user://saves/save_1.json
user://saves/save_2.json
user://saves/save_3.json
user://saves/save_4.json
```

## Four-Slot Behavior

Only slots 1 through 4 are valid. Attempts to save or load outside that range log a `[SaveLoad]` error.

The main menu load view shows four generated slot buttons. Occupied slots show metadata. Empty slots show `Empty Slot`. Clicking an empty slot logs:

```text
[SaveLoad] No save found for slot X.
```

## Save Slot Metadata

Each occupied slot records:

- `PlayerName`
- `GameTimePlayedSeconds`
- formatted `GameTimePlayed` as `HH:MM:SS`
- `SceneName`
- `CurrentMainQuestName`

Metadata is built from the `GameStateManager` world-state snapshot and player snapshot. Safe fallbacks are used when a full system does not exist yet:

- player name: `Player`
- time played: `00:00:00`
- scene: current scene key or `Unknown location`
- quest: `No active main quest`

## Saved Data

The top-level `SaveGame` JSON model stores:

- `SaveVersion`
- `SlotNumber`
- `SavedAt`
- `Metadata`
- `Player`
- `Inventory`
- `GameState`
- `Quest`

## Player Snapshot

The player snapshot stores JSON-friendly values only:

- player name
- scene id
- spawn id
- position as `{ X, Y }`
- core combat stats

No player node, scene node, or live resource references are stored.

## Inventory Snapshot

`InventoryManager` already owns an `InventorySave` snapshot. The save file stores:

- item ids
- quantities
- equipped armor item ids by slot
- equipped trinket item ids by slot
- equipped weapon item ids by slot

Restore clears current inventory/equipment, rebuilds item counts, and reapplies equipment when the item data is available. Armor and trinket runtime stat effects are removed and reapplied through source-tracked player equipment modifiers so load does not double-apply bonuses.

Socketed weapons are persisted as unique weapon instances in `InventorySave.WeaponInstances`. Stackable items still save as item IDs and quantities, but weapon rows also carry an instance ID plus socketed upgrade and elemental rune IDs so two copies of the same weapon can load with different rune state.

## Game State Snapshot

`GameStateManager` owns persistent world-state metadata and snapshots. The saved `WorldStateDto` includes:

- player name
- game time played
- current scene/location/spawn/area
- current scene display name
- main story quest id/name fallback
- boolean flags
- integer values
- string values
- explored area ids
- NPC friendship scores
- simple day/time phase values
- compatibility NPC state list

## GameManager Coordination

`GameManager` owns the active manager stack and creates `SaveLoadService`.

New game:

1. `MainMenu` opens a simple new-game setup view.
2. The player enters a name and selects one of the four slots.
3. `GameManager.StartNewGameInSlot(slot, playerName)` stores the active slot.
4. The player model is created with the selected name.
5. Gameplay starts in the normal new-game scene.

Saving:

1. Captures current location through `GameStateManager`.
2. Asks `SaveLoadService` to build a `SaveGame`.
3. Captures `InventoryManager` and `GameStateManager` snapshots.
4. Captures player metadata and position.
5. Writes JSON to the requested slot file.

Loading:

1. `SaveLoadService` reads the requested JSON file.
2. `GameManager.RestoreLoadedSave` restores the game-state snapshot.
3. A player model is created and populated from the player snapshot.
4. The saved scene is loaded through `SceneManager`.
5. The player node is spawned at the saved position, or the saved spawn marker if no position is available.
6. `InventoryManager` restores its snapshot after the player node exists.
7. Gameplay HUD is shown.

## SaveLoadService JSON

`SaveLoadService` reads and writes JSON with `System.Text.Json`. It uses indented JSON and string enum conversion. The service validates slot numbers before saving or loading.

Useful debug output includes:

```text
[SaveLoad] Saving slot 1...
[SaveLoad] Saved slot 1 to: user://saves/save_1.json
[SaveLoad] Loading slot 1...
[SaveLoad] Loaded scene: LabScene
[SaveLoad] Restored player position: x,y
[SaveLoad] Restored inventory snapshot.
[SaveLoad] Restored game state snapshot.
[SaveLoad] Refreshed save slot metadata.
```

## Scene And Position Restore

`SceneManager` records the current scene key when `GoToScene` succeeds. During load, `GameManager` asks `SceneManager` to load the saved scene id. Once the world scene exists, the player is spawned into the world root and moved to the saved `{ X, Y }` position.

If the saved position is unavailable, the loader falls back to the saved spawn id, then `NewGameSpawn`, then `Vector2.Zero`.

## Main Menu Display

`MainMenu` generates simple controls in code:

- `New Game` shows a player-name input and four slot buttons.
- `Load Game` shows four save-slot buttons.

Both views read slot metadata through `GameManager.GetSaveSlotInfos()`, which delegates to `SaveLoadService`.

The generated display is intentionally simple for this slice.

## HUD Save Button

The HUD has a simple `Save` button. It calls `GameManager.SaveCurrentGame()`, which saves to the active slot selected at new-game start or set by loading a save.

If no active slot is set, the button logs:

```text
[SaveLoad] No active save slot. Start a new game from a slot or load an existing save first.
```

## Manual Testing

1. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-game.ps1
```

3. In game:

- click `New Game`
- enter a player name
- select save slot 1, 2, 3, or 4
- move the player
- click the HUD `Save` button to save the active slot
- optionally press `F5` to save slot 1
- press `F6`, `F7`, or `F8` to save slots 2, 3, or 4
- press `Escape` to return to the main menu
- click `Load Game`
- confirm occupied slots show metadata
- click a populated slot
- confirm the scene loads and the player returns to the saved position

## Known Limitations

- Runtime quest state is not fully saved/restored yet.
- The save-slot UI is intentionally simple and generated in code.
- There is no overwrite confirmation.
- There is no save deletion UI.
- There is no autosave.
- Chests, doors, puzzle objects, defeated enemies, and other world actors are only persisted if they write to `GameStateManager`.
- There is no save thumbnail or screenshot.
- There is no save-version migration beyond the current `SaveVersion` field.

## Future Follow-Ups

- polished save slot UI
- save deletion and overwrite confirmation
- autosave
- quest runtime save integration
- chest, door, and puzzle persistence
- defeated enemy persistence
- richer time-of-day persistence
- save file version migration
- save thumbnails/screenshots
