# Audio Manager

`AudioManager` is the single runtime entry point for game audio. It lives as a child of the `GameManager` autoload scene:

```text
res://PackedScenes/EthraV1/Core/UI/game_manager.tscn
```

Sounds are authored through data resources, not exported `AudioStream` slots on the manager.

## Runtime API

```csharp
AudioManager.PlayUi("sound.ui.confirm");
AudioManager.PlayEntity("sound.player.attack", playerNode);
AudioManager.PlaySceneSound("sound.world.chest_open", chestNode);
AudioManager.PlayMusic("music.tree_village");
GameManager.Instance.Audio.PlayFootstep("grass", player.GlobalPosition);
```

Use stable sound IDs. Gameplay code should not load audio files directly or create ad hoc audio player nodes.

## Data Resources

Main audio definitions:

```text
res://Core/Audio/Data/MainAudioLibrary.tres
```

Footstep surface mappings:

```text
res://Core/Audio/Data/FootstepSurfaceLibrary.tres
```

The `AudioManager` node in `game_manager.tscn` has these resources assigned under `Audio Library`.

## Adding A Sound

1. Add audio files under an asset folder, for example:

```text
res://ArtAssets/Audio/Footsteps/Grass/
```

2. Open `MainAudioLibrary.tres`.
3. Add or edit an `AudioSoundDefinitionResource`.
4. Set:

```text
SoundId
DisplayName
Category
BusName
Streams
VolumeDb
PitchScale / RandomPitchMin / RandomPitchMax
Loop
MaxInstances
IsPositional
```

5. Call the sound by its stable ID.

No C# change is required for ordinary new sounds.

## Footsteps

`PlayerNode.PlayFootstepSound()` is intended for animation method tracks. It asks the active world's `TileSurfaceResolver` for the current terrain surface, then calls:

```csharp
AudioManager.PlayFootstep(surfaceId, GlobalPosition);
```

Surface IDs are mapped in `FootstepSurfaceLibrary.tres`, for example:

```text
grass -> sound.footstep.grass
dirt -> sound.footstep.dirt
stone -> sound.footstep.stone
water -> sound.footstep.water
```

Unknown or missing surfaces fall back to `sound.player.footstep`.

## Phosphor Forest World Scene

Phosphor Forest is a production world scene:

```text
res://PackedScenes/EthraV1/Core/World/PhosphorRegion/PhosphorForest.tscn
```

It instances the YATI-imported Phosphor Forest map and includes a `TileSurfaceResolver` configured for the map's `surface` custom data key. It is loadable through `SceneManager.GoToScene("PhosphorForest")`.

## Current Notes

- Empty sound definitions are allowed while audio files are still being imported; they warn once and do nothing.
- Player pooling is handled by `AudioPlayerPool`.
- Category buses still fall back to `Master` when a requested bus is missing.
