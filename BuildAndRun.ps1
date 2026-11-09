<#
.SYNOPSIS
    Builds the Pomodoro Timer app and launches it as a packaged app.

.DESCRIPTION
    Uses the WinApp CLI (0.7+) project-mode run, which builds the project, registers a
    development package identity, and launches the app.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug.

.PARAMETER Wait
    Keep the script attached until the app exits instead of returning immediately.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $Wait
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
    throw "WinApp CLI ('winapp') was not found. Install it (0.7 or later) and try again."
}

$project = Join-Path $PSScriptRoot 'src\PomodoroTimer\PomodoroTimer.csproj'

$runArgs = @('run', $project, '-c', $Configuration)
if (-not $Wait) {
    $runArgs += '--detach'
}

& winapp @runArgs
exit $LASTEXITCODE
