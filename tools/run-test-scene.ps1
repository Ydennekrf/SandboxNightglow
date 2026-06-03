param(
    [string]$Scene = ""
)

function Import-LocalGodotSettings {
    $localSettings = Join-Path $PSScriptRoot "local.settings.ps1"
    $exampleSettings = Join-Path $PSScriptRoot "local.settings.example.ps1"

    if (Test-Path $localSettings) {
        . $localSettings
    }
    elseif (Test-Path $exampleSettings) {
        Write-Warning "tools/local.settings.ps1 was not found. Falling back to tools/local.settings.example.ps1."
        . $exampleSettings
    }
    else {
        throw "Missing tools/local.settings.ps1 and tools/local.settings.example.ps1."
    }

    if ([string]::IsNullOrWhiteSpace($GodotExe)) {
        throw "GodotExe is not configured."
    }

    if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
        throw "ProjectRoot is not configured."
    }

    if ([string]::IsNullOrWhiteSpace($SolutionFile)) {
        throw "SolutionFile is not configured."
    }

    if ([string]::IsNullOrWhiteSpace($CsprojFile)) {
        throw "CsprojFile is not configured."
    }

    return @{
        GodotExe = $GodotExe
        ProjectRoot = $ProjectRoot
        SolutionFile = $SolutionFile
        CsprojFile = $CsprojFile
        MainScene = $MainScene
        DefaultTestScene = $DefaultTestScene
        CombatDebugScene = $CombatDebugScene
        MovementDebugScene = $MovementDebugScene
    }
}

function Test-GodotExitCode {
    param(
        [object]$ExitCode
    )

    # When launching Godot in a normal window, PowerShell can sometimes leave
    # $LASTEXITCODE unset. Treat an unset code as informational rather than failure.
    if ($null -eq $ExitCode -or "$ExitCode" -eq "") {
        Write-Warning "Godot process returned no exit code. Check the Godot console output above for runtime errors."
        return
    }

    if ([int]$ExitCode -ne 0) {
        throw "Godot exited with code $ExitCode."
    }
}


$ErrorActionPreference = "Stop"

$config = Import-LocalGodotSettings

if ([string]::IsNullOrWhiteSpace($Scene)) {
    $Scene = $config.DefaultTestScene
}

if ([string]::IsNullOrWhiteSpace($Scene)) {
    throw "No scene was provided and DefaultTestScene is not configured."
}

if (-not (Test-Path $config.GodotExe)) {
    throw "Godot executable not found: $($config.GodotExe)"
}

if (-not (Test-Path $config.ProjectRoot)) {
    throw "Project root not found: $($config.ProjectRoot)"
}

Write-Host ""
Write-Host "Launching Godot test scene..." -ForegroundColor Cyan
Write-Host "Project: $($config.ProjectRoot)"
Write-Host "Scene:   $Scene"

$global:LASTEXITCODE = $null
& $config.GodotExe --path $config.ProjectRoot $Scene
Test-GodotExitCode -ExitCode $LASTEXITCODE
