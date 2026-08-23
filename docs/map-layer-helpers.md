# Map Layer Helpers

## Purpose

The map layer helper system lets a 2D top-down map change visual depth and collision behavior when the player enters or exits trigger areas. It is intended for houses, roofs, tunnels, bridges, tree canopies, and any layered map section where the player can be above or below authored map details.

The reusable scripts live in `res://Core/World/MapLayers/`:

- `MapLayerController`
- `MapLayerEntry`
- `MapLayerState`
- `MapLayerFadeOperation`
- `MapLayerTrigger`

## Recommended Layer Names

Maps can use names such as:

- `AbovePlayer3`
- `AbovePlayer2`
- `AbovePlayer1`
- `PlayerLayer`
- `BelowPlayer1`
- `BelowPlayer2`
- `Ground`
- `Collision`
- `AboveCollision`
- `BelowCollision`
- `BridgeCollision`
- `BridgeSideCollision`
- `BridgeCover`
- `TunnelCollision`
- `UnderBridgeSideCollision`
- `Roof`
- `RoofCollision`

These names are not global requirements. The controller uses authored layer IDs, so a map can use any node names if each entry points to the right node.

## Visual Layers And Collision Layers

Visual layers are nodes that inherit from `CanvasItem`, such as `TileMapLayer`, `Sprite2D`, `Polygon2D`, `ColorRect`, or `Node2D` parents containing those nodes. They can be shown, hidden, or faded.

Collision layers are nodes that contain collision objects or shapes, such as:

- `StaticBody2D`
- `Area2D`
- `CollisionShape2D`
- `CollisionPolygon2D`

The current implementation toggles separate collision nodes safely. It does not edit TileSet physics data or per-tile collision internals.

## MapLayerController

Add `MapLayerController` to a map scene, usually as a child of the scene root or world root.

Configure:

- `LayerRootPath`: the node under which layer paths are resolved, commonly `Map`.
- `Layers`: `MapLayerEntry` resources with `LayerId`, `LayerPath`, visual/collision flags, and defaults.
- `States`: `MapLayerState` resources with a `StateId` and layer IDs to show, hide, fade, enable collision, or disable collision.

Useful methods:

```csharp
SetLayerVisible(string layerId, bool visible);
SetLayerCollisionEnabled(string layerId, bool enabled);
ApplyMapLayerState(string stateId);
RestoreDefaultMapLayerState();
FadeLayer(string layerId, float targetAlpha, float durationSeconds);
```

## MapLayerTrigger

Add `MapLayerTrigger` to an `Area2D` with a `CollisionShape2D`.

Configure:

- `ControllerPath`: path to the map's `MapLayerController`.
- `EnterStateId`: state applied when the player enters.
- `ExitStateId`: state applied when the player exits.
- `RestoreDefaultOnExit`: restore configured defaults when no exit state is set.

Player detection follows the existing project convention first: `body is PlayerNode`. It can also accept the `"Player"` group as a fallback.

## Roof Or House Transition

Example setup:

1. Add a visual layer node named `Roof`.
2. Add an optional collision node named `RoofCollision` or `InteriorCollision`.
3. Add controller entries:
   - `LayerId = "Roof"`, `LayerPath = "Roof"`, `IsVisualLayer = true`.
   - `LayerId = "InteriorCollision"`, `LayerPath = "InteriorCollision"`, `IsCollisionLayer = true`.
4. Add a state:
   - `StateId = "InsideHouse"`.
   - Fade `Roof` to `0.25`, or put `Roof` in `HiddenLayerIds`.
   - Enable or disable collision layer IDs as needed.
5. Add an `Area2D` with `MapLayerTrigger`.
6. Set `EnterStateId = "InsideHouse"` and `ExitStateId = "DefaultOutdoor"`.

## Bridge Or Tunnel Transition

Use separate layer IDs for the bridge cover, the bridge-top blockers, and the under-bridge valley blockers. Do not model this as one collider that simply turns off.

Example visual layers:

- `BridgeCover`: visual layer drawn above the player when walking under the bridge.
- `AbovePlayer1`: rails, canopy, or other always-above bridge details.
- `UnderBridgeVisual`: shadow or valley detail shown while walking underneath.

Example collision layers:

- `BridgeSideCollision`: side blockers active when the player is on top of the bridge, preventing them from running off the deck.
- `UnderBridgeSideCollision`: valley or tunnel side blockers active when the player is underneath, preventing them from escaping sideways into the valley walls.

Example `OnBridge` state:

- Hide `BridgeCover`.
- Hide `UnderBridgeVisual`.
- Show `AbovePlayer1` if bridge rails should remain visible.
- Enable `BridgeSideCollision`.
- Disable `UnderBridgeSideCollision`.

Example `UnderBridge` state:

- Show `BridgeCover` so the player walks under a visual layer.
- Show `UnderBridgeVisual`.
- Show `AbovePlayer1` if rails or canopy should remain above the player.
- Disable `BridgeSideCollision`.
- Enable `UnderBridgeSideCollision`.

Use separate trigger gates for entering the top route and the lower route when the bridge has both kinds of traversal. For example:

- north and south bridge approach triggers apply `OnBridge`
- west and east underpass approach triggers apply `UnderBridge`
- small outdoor reset triggers past each exit apply `DefaultOutdoor`

Avoid one large overlapping center trigger for both routes. It cannot reliably know whether the player intended to cross on top or pass underneath.

## Adding States

Add a `MapLayerState` to the controller's `States` array. Use a stable, readable `StateId`, such as:

- `DefaultOutdoor`
- `InsideHouse`
- `UnderBridge`
- `OnBridge`
- `InsideTunnel`
- `UnderCanopy`

State IDs are local authored keys for the map helper. If a state becomes save-facing later, follow `docs/stable-ids.md`.

## Layer IDs

Layer IDs are the controller's lookup keys. They do not have to match node names, but matching them is convenient.

Good examples:

- `Roof`
- `AbovePlayer1`
- `BridgeCover`
- `BridgeSideCollision`
- `UnderBridgeSideCollision`
- `InteriorCollision`

Keep IDs clear and map-local. Avoid hardcoding these names in unrelated gameplay systems.

## Collision Toggling

When a collision layer is disabled, the controller recursively:

- disables `CollisionShape2D`
- disables `CollisionPolygon2D`
- stores and clears `CollisionObject2D.CollisionLayer`
- stores and clears `CollisionObject2D.CollisionMask`

When re-enabled, original collision layer and mask values are restored from the controller cache.

Prefer separate collision nodes for switchable bridge, tunnel, roof, or door behavior. This keeps the helper simple and avoids editing TileSet physics data at runtime.

## Debug Scene

Debug scene:

```text
res://PackedScenes/EthraV1/Core/Debug/MapLayerDebugScene.tscn
```

Run script:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-map-layer-debug.ps1
```

The scene demonstrates:

- a house trigger that fades the roof and changes collision state
- an on-bridge trigger that keeps the player visible and enables bridge-side blockers
- an under-bridge trigger that draws a bridge cover above the player and enables valley/tunnel side blockers

## Current Limitations

- TileSet physics data is not edited.
- TileMapLayer collision is best handled through separate collision nodes for now.
- Trigger priority and overlapping competing states are intentionally simple.
- Fades use basic Tween alpha changes on `CanvasItem.Modulate`.
- The helper does not persist current map-layer state through saves yet.

## Manual Test Checklist

- Start `MapLayerDebugScene`.
- Walk into the house/roof trigger.
- Confirm the roof fades.
- Confirm configured collision changes.
- Walk out of the trigger.
- Confirm roof and default collision state restore.
- Walk into the under-bridge trigger.
- Confirm the player walks under `BridgeCover`.
- Confirm under-bridge side blockers prevent escaping sideways.
- Walk into the on-bridge trigger.
- Confirm the player remains visible on top of the bridge.
- Confirm bridge-side blockers prevent stepping off the bridge sides.
- Confirm collision state changes and the player does not get stuck.
- Confirm no null reference errors occur.
- Place `MapLayerController` and `MapLayerTrigger` in another map scene and confirm they can resolve configured layer IDs.

## Future Follow-Up Tasks

- smoother fades
- camera-aware occlusion
- automatic roof hiding
- tile-based trigger authoring
- persistent area state through `GameStateManager`
- editor tooling for layer state presets
- integration with scene/area IDs
