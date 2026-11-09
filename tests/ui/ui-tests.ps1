<#
.SYNOPSIS
    UI tests for the Pomodoro timer, driven through UI Automation with the WinApp CLI (`winapp ui`).

.DESCRIPTION
    Launches the app (or attaches to -AppPid), then verifies that:
      - the timer starts in the "Ready" state with a valid mm:ss value,
      - the remaining time decreases after Start,
      - Reset returns the remaining time to its starting value.
    The starting value is read from the app, so the tests do not depend on saved settings.
    Writes test-results.json and screenshots, and exits 0 when every test passes.

.PARAMETER Target
    'local' runs on this desktop (synthetic input). 'sandbox' runs inside Windows Sandbox.

.EXAMPLE
    .\tests\ui\ui-tests.ps1
#>
param(
    [ValidateSet('sandbox', 'local')][string]$Target = 'local',
    [int]$AppPid = 0,
    [ValidateSet('sandbox', 'local')][string]$AppScope,
    [ValidateNotNullOrEmpty()][string]$WorkflowId = [guid]::NewGuid().ToString('N'),
    [string]$ArtifactDirectory,
    [string]$ResultsPath
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$Project = Join-Path $RepoRoot 'src\PomodoroTimer\PomodoroTimer.csproj'
if (-not $ArtifactDirectory) {
    $ArtifactDirectory = Join-Path $RepoRoot (Join-Path 'ui-test-artifacts' (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
if (-not $ResultsPath) { $ResultsPath = Join-Path $RepoRoot 'test-results.json' }

$ScopeArgs = @(if ($Target -eq 'sandbox') { '--on'; 'sandbox' })
$previousWorkflowId = $env:WINAPP_UI_WORKFLOW_ID
$env:WINAPP_UI_WORKFLOW_ID = $WorkflowId
$results = [Collections.Generic.List[object]]::new()
$screenshots = [Collections.Generic.List[string]]::new()
$uiStarted = $false
$launchedByScript = $false
$hwnd = $null

function Invoke-WinAppChecked {
    param([string[]]$Arguments)
    $global:LASTEXITCODE = $null
    $output = @(& winapp @Arguments)
    $code = $global:LASTEXITCODE
    if ($null -eq $code -or $code -ne 0) {
        throw "winapp $($Arguments -join ' ') failed (exit $code). $($output -join "`n")"
    }
    $output
}

function Invoke-Ui {
    param([string[]]$Arguments, [string]$Window)
    $selector = if ($Window) { @('-w', $Window) } else { @('-a', "$AppPid") }
    Invoke-WinAppChecked (@('ui') + $Arguments + $ScopeArgs + $selector)
}

function Test-UI {
    param([string]$Name, [scriptblock]$Script)
    try {
        & $Script | Out-Null
        $results.Add(@{ name = $Name; status = 'PASS' })
    } catch {
        $results.Add(@{ name = $Name; status = 'FAIL'; detail = "$_" })
    }
}

function Save-Screenshot {
    param([string]$Name)
    $path = Join-Path $ArtifactDirectory $Name
    if (Test-Path -LiteralPath $path) { throw "Choose a new evidence path: $path" }
    Invoke-Ui @('screenshot', '--output', $path) -Window $hwnd | Out-Null
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
        throw "Screenshot was not delivered to the host: $path"
    }
    $screenshots.Add($path)
}

# Reads the clock through UIA and returns its mm:ss text. The element may expose the time
# as plain text or inside its accessible name (e.g. "Remaining time 24:57").
function Get-ClockText {
    $raw = (Invoke-Ui @('get-value', 'TimerClock', '--json')) -join "`n"
    $match = [regex]::Match($raw, '\b(\d{2}):(\d{2})\b')
    if (-not $match.Success) { throw "TimerClock did not expose an mm:ss value: $raw" }
    $match.Value
}

function ConvertTo-Seconds {
    param([string]$ClockText)
    $parts = $ClockText.Split(':')
    [int]$parts[0] * 60 + [int]$parts[1]
}

try {
    $ArtifactDirectory = [IO.Path]::GetFullPath($ArtifactDirectory)
    New-Item -ItemType Directory -Force -Path $ArtifactDirectory | Out-Null
    if ($AppPid -lt 0) { throw 'AppPid must be positive, or zero to launch.' }
    if ($AppPid -gt 0) {
        if ($AppScope -ne $Target) {
            throw 'Reusing a PID requires matching -AppScope and -Target from the same live target.'
        }
    } else {
        $launchOutput = Invoke-WinAppChecked (@('run', $Project) + $ScopeArgs + @('--detach', '--json'))
        $launch = ($launchOutput -join "`n") | ConvertFrom-Json
        if ($launch.Error -or -not $launch.ProcessId -or [int]$launch.ProcessId -le 0) {
            throw "No app ProcessId from winapp run. $($launch.Error)"
        }
        $AppPid = [int]$launch.ProcessId
        $launchedByScript = $true
        if ($Target -eq 'sandbox' -and
            ($launch.ProcessScope -ne 'sandbox' -or $launch.UiTargetArgs -ne "--on sandbox -a $AppPid")) {
            throw 'Launch response is missing the expected Sandbox ProcessScope / UiTargetArgs pair.'
        }
        if ($Target -eq 'local' -and $launch.ProcessScope -and $launch.ProcessScope -ne 'local') {
            throw 'Launch response belongs to a different target.'
        }
    }

    $uiStarted = $true
    Invoke-Ui @('wait-for', 'TimerClock', '-t', '15000') | Out-Null
    $windows = @(Invoke-Ui @('list-windows', '--json') | ConvertFrom-Json)
    $main = $windows | Where-Object { $_.hwnd -and $_.hwnd -ne '0' -and $_.title -ne 'PopupHost' } |
        Select-Object -First 1
    if (-not $main) { throw 'No app window found in the selected target.' }
    $hwnd = [string]$main.hwnd

    $script:initialClock = $null

    Test-UI 'Timer starts in Ready state with an mm:ss value' {
        $script:initialClock = Get-ClockText
        if ((ConvertTo-Seconds $script:initialClock) -le 0) { throw "Unexpected initial time $script:initialClock" }
        Invoke-Ui @('wait-for', 'StatusText', '--value', 'Ready', '--contains', '-t', '3000')
        Invoke-Ui @('wait-for', 'BtnStart', '-p', 'IsEnabled', '--value', 'True', '-t', '3000')
        Save-Screenshot '01-ready.png'
    }

    Test-UI 'Remaining time decreases after Start' {
        if (-not $script:initialClock) { throw 'Initial time was not captured.' }
        $initialSeconds = ConvertTo-Seconds $script:initialClock
        Invoke-Ui @('invoke', 'BtnStart')
        Invoke-Ui @('wait-for', 'StatusText', '--value', 'Running', '--contains', '-t', '3000')

        $deadline = (Get-Date).AddSeconds(6)
        $current = $script:initialClock
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 500
            $current = Get-ClockText
            if ((ConvertTo-Seconds $current) -lt $initialSeconds) { break }
        }
        if ((ConvertTo-Seconds $current) -ge $initialSeconds) {
            throw "Remaining time did not decrease: started at $script:initialClock, still $current"
        }
        Save-Screenshot '02-running.png'
    }

    Test-UI 'Reset returns remaining time to the beginning' {
        if (-not $script:initialClock) { throw 'Initial time was not captured.' }
        Invoke-Ui @('invoke', 'BtnReset')
        Invoke-Ui @('wait-for', 'TimerClock', '--value', $script:initialClock, '--contains', '-t', '3000')
        Invoke-Ui @('wait-for', 'StatusText', '--value', 'Ready', '--contains', '-t', '3000')
        $after = Get-ClockText
        if ($after -ne $script:initialClock) { throw "Expected $script:initialClock after Reset, got $after" }
        Save-Screenshot '03-reset.png'
    }
} catch {
    $results.Add(@{ name = 'Target setup / test execution'; status = 'FAIL'; detail = "$_" })
} finally {
    try {
        if ($uiStarted) { Invoke-WinAppChecked (@('ui', 'yield') + $ScopeArgs) | Out-Null }
    } catch {
        $results.Add(@{ name = 'Release UI workflow'; status = 'FAIL'; detail = "$_" })
    } finally {
        $env:WINAPP_UI_WORKFLOW_ID = $previousWorkflowId
    }

    # Close the app only if this script launched it locally; a sandbox guest is left as is.
    if ($launchedByScript -and $Target -eq 'local' -and $AppPid -gt 0) {
        Stop-Process -Id $AppPid -ErrorAction SilentlyContinue
    }
}

$failed = @($results | Where-Object { $_.status -eq 'FAIL' }).Count
$report = [ordered]@{
    target = $Target; appPid = $AppPid; workflowId = $WorkflowId
    passed = $results.Count - $failed; failed = $failed
    results = @($results.ToArray()); screenshots = @($screenshots.ToArray())
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ResultsPath -Encoding utf8
foreach ($r in $results) {
    $line = "[$($r.status)] $($r.name)"
    if ($r.detail) { $line += " - $($r.detail)" }
    Write-Host $line
}
Write-Host "Passed: $($report.passed) | Failed: $failed | Results: $ResultsPath"
if ($failed) { exit 1 }
exit 0
