# Recompile the running Editor and show warnings/errors from the console.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-check/scripts/check.ps1 [-SceneCheck] [-Tail 30] [-Strict]
# ASCII only.
param(
    [switch]$SceneCheck,
    [switch]$Strict,
    [int]$Tail = 30,
    [int]$Timeout = 300
)

. (Join-Path $PSScriptRoot "lib.ps1")

Write-Output "== wait for editor"
if (-not (Wait-UnityReady)) { exit 2 }

Write-Output "== recompile"
$rcArgs = @("recompile", "--timeout", "$Timeout")
if ($Strict) { $rcArgs += "--strict" }
& unity @rcArgs
$compileExit = $LASTEXITCODE
# The Editor may already be compiling on its own (HTTP 400). Wait and try once more.
if ($compileExit -ne 0) {
    Write-Output "[INFO] recompile failed once (Editor busy?). Waiting and retrying."
    Start-Sleep -Seconds 5
    if (-not (Wait-UnityReady)) { exit 2 }
    & unity @rcArgs
    $compileExit = $LASTEXITCODE
}

# The domain may reload after compiling; wait again before asking for the console.
if (-not (Wait-UnityReady)) { exit 2 }

Write-Output "== console (warn and error, last $Tail, first line of each)"
Show-UnityConsole -Level warn -Tail $Tail

if ($SceneCheck) {
    Write-Output "== scene check (scenes currently open in the Editor)"
    $r = Invoke-UnityEval -File ".claude/skills/unity-check/scripts/scene_check.cs" -Timeout 120
    if ($null -ne $r) { Write-Output $r }
}

if ($compileExit -ne 0) { Write-Output "[RESULT] compile FAILED (exit $compileExit)"; exit 1 }
Write-Output "[RESULT] compile OK"
exit 0
