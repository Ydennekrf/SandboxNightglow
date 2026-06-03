# Testing Notes

## Preferred Commands

Run these from the repository root in PowerShell.

### Check local setup

```powershell
powershell -ExecutionPolicy Bypass -File ./tools/godot-doctor.ps1
```

### Build C# solution

```powershell
powershell -ExecutionPolicy Bypass -File ./tools/build.ps1
```

### Launch full game

```powershell
powershell -ExecutionPolicy Bypass -File ./tools/run-game.ps1
```

### Launch default test scene

```powershell
powershell -ExecutionPolicy Bypass -File ./tools/run-test-scene.ps1
```

### Launch combat debug scene

```powershell
powershell -ExecutionPolicy Bypass -File ./tools/run-combat-debug.ps1
```

### Launch movement debug scene

```powershell
powershell -ExecutionPolicy Bypass -File ./tools/run-movement-debug.ps1
```

## Important Note About Debug Scenes

The debug scene commands only work after the matching scenes exist:

- `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`
- `res://PackedScenes/EthraV1/Core/Debug/MovementDebugScene.tscn`

If these scenes do not exist yet, Godot will report `Cannot open file` / `Failed loading scene`. That means the script wiring is working, but the scene has not been created yet.

## Reporting Expectations

After a task, Codex should report:

- commands run
- build result
- runtime scene launched, if any
- relevant error output
- files changed
- what was verified
- what still needs manual Godot editor/gameplay verification
