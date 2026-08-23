param(
    [string]$Scene = "res://PackedScenes/EthraV1/Core/Debug/SceneFlow/SceneFlowDebugHost.tscn"
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "run-test-scene.ps1") -Scene $Scene
