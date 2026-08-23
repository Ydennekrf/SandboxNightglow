$ErrorActionPreference = "Stop"

$scene = "res://PackedScenes/EthraV1/Core/Debug/CutsceneNodeDebugScene.tscn"

& (Join-Path $PSScriptRoot "run-test-scene.ps1") -Scene $scene
