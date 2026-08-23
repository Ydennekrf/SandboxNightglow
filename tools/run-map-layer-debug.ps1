$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "run-test-scene.ps1") -Scene "res://PackedScenes/EthraV1/Core/Debug/MapLayerDebugScene.tscn"
