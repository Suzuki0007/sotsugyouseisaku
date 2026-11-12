# Press and release keyboard keys in play mode through the Input System.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/press.ps1 -Key Tab [-HoldMs 150]
#        -Key "W,LeftShift" presses several keys together. Names are UnityEngine.InputSystem.Key values.
# Note: actions that respect Game view focus (UI submit/navigate) may not receive this.
# ASCII only.
param(
    [Parameter(Mandatory = $true)][string]$Key,
    [int]$HoldMs = 150
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")

$keys = ($Key -split ",") | ForEach-Object { "UnityEngine.InputSystem.Key." + $_.Trim() }
$list = $keys -join ", "
$down = "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState($list)); `"down`""
$up = "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); `"up`""

$r1 = Invoke-UnityEval -Code $down
Start-Sleep -Milliseconds $HoldMs
$r2 = Invoke-UnityEval -Code $up
Write-Output "$Key : $r1 -> $r2"
