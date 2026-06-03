$ErrorActionPreference = "Stop"

$scene = "res://PackedScenes/Tools/DialogGraphEditor.tscn"

& (Join-Path $PSScriptRoot "run-test-scene.ps1") -Scene $scene
