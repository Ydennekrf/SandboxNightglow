# Debug File Logging

`Core/Debug/DebugLog.cs` provides a small reusable file-backed logger for debug scenes, managers, components, and tools.

Default output:

```text
user://logs/debug.log
```

Use it from C#:

```csharp
DebugLog.Info("MapLayer", "Applied UnderBridge state.", this);
DebugLog.Warning("Inventory", "Missing debug seed item.", this);
DebugLog.Error("SaveLoad", "Failed to restore test save.", this);
```

Useful switches:

- `DebugLog.Enabled`: turns file logging on or off.
- `DebugLog.MirrorToGodotOutput`: also prints log entries to the Godot console.
- `DebugLog.LogFilePath`: changes the output path.
- `DebugLog.Clear()`: clears the current log file.
- `DebugLog.ResolvedLogFilePath`: returns the OS path for the configured log file.

Current map-layer instrumentation writes:

- controller cache rebuilds
- cached layers and states
- trigger enter/exit events
- applied map-layer states

This is intended for short-lived debug evidence, not permanent gameplay telemetry.
