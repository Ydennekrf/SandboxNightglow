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

Write-Host ""
Write-Host "Checking local Godot/C# setup..." -ForegroundColor Cyan

Write-Host ""
Write-Host "Configured paths:"
Write-Host "GodotExe:     $($config.GodotExe)"
Write-Host "ProjectRoot:  $($config.ProjectRoot)"
Write-Host "SolutionFile: $($config.SolutionFile)"
Write-Host "CsprojFile:   $($config.CsprojFile)"
Write-Host "MainScene:    $($config.MainScene)"

Write-Host ""
Write-Host "Path checks:"

if (Test-Path $config.GodotExe) {
    Write-Host "OK: Godot executable found." -ForegroundColor Green
}
else {
    throw "Missing Godot executable: $($config.GodotExe)"
}

if (Test-Path $config.ProjectRoot) {
    Write-Host "OK: Project root found." -ForegroundColor Green
}
else {
    throw "Missing project root: $($config.ProjectRoot)"
}

if (Test-Path $config.SolutionFile) {
    Write-Host "OK: Solution file found." -ForegroundColor Green
}
else {
    throw "Missing solution file: $($config.SolutionFile)"
}

if (Test-Path $config.CsprojFile) {
    Write-Host "OK: .csproj file found." -ForegroundColor Green
}
else {
    throw "Missing .csproj file: $($config.CsprojFile)"
}

Write-Host ""
Write-Host ".NET SDK:"
dotnet --version

Write-Host ""
Write-Host "Godot version:"
& $config.GodotExe --version

Write-Host ""
Write-Host "Doctor check completed." -ForegroundColor Green
