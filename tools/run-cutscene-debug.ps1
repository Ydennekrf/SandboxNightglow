$ErrorActionPreference = "Stop"

$scene = "res://PackedScenes/EthraV1/Core/Debug/SceneFlow/CutsceneDebugScene.tscn"

& (Join-Path $PSScriptRoot "run-test-scene.ps1") -Scene $scene
