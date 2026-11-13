# Clean up after a play-mode test started with start.ps1.
#  1. exit play mode, run_in_background off
#  2. restore persistentDataPath/save*.json exactly as backed up (files created by the test are removed)
#  3. delete Assets/__ClaudeTest
#  4. reopen the scene that was open before the test (only if nothing is unsaved)
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/end.ps1 [-KeepTestAssets]
# ASCII only.
param([switch]$KeepTestAssets)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")

if (-not (Wait-UnityReady)) { exit 2 }

# --- 1. stop
$playing = Invoke-UnityEval -Quiet -Code "UnityEditor.EditorApplication.isPlaying"
if ($playing -eq $true) {
    unity command editor.playmode stop --result-only 2>$null | Out-Null
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 500
        $p = Invoke-UnityEval -Quiet -Code "UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode"
        if ($p -eq $false) { break }
    }
}
unity command editor.playmode set_run_in_background --enabled false --result-only 2>$null | Out-Null
Write-Output "play mode stopped, run_in_background restored"

# --- 2. restore saves
$marker = Join-Path $env:TEMP "ct_playtest_backup\latest.json"
if (Test-Path $marker) {
    $info = Get-Content $marker -Raw -Encoding UTF8 | ConvertFrom-Json
    if ((Test-Path $info.backup) -and (Test-Path $info.pdp)) {
        Get-ChildItem -Path $info.pdp -Filter "save*.json" -ErrorAction SilentlyContinue | Remove-Item -Force
        $files = @(Get-ChildItem -Path $info.backup -Filter "save*.json")
        foreach ($f in $files) { Copy-Item $f.FullName $info.pdp -Force }
        Write-Output ("restored {0} save file(s) to {1} (backup kept at {2})" -f $files.Count, $info.pdp, $info.backup)
    } else {
        Write-Output "[WARN] backup or save folder missing; saves NOT restored. Tell the user."
    }
} else {
    Write-Output "[WARN] no marker file; nothing to restore (start.ps1 was not used?)"
}

# --- 3. test assets
if (-not $KeepTestAssets) {
    $r = Invoke-UnityEval -Code "UnityEditor.AssetDatabase.IsValidFolder(`"Assets/__ClaudeTest`") ? (UnityEditor.AssetDatabase.DeleteAsset(`"Assets/__ClaudeTest`") ? `"deleted`" : `"delete failed`") : `"none`""
    Write-Output "Assets/__ClaudeTest: $r"
}

# --- 4. scene back
if ($info -and $info.scene) {
    $code = @"
using UnityEditor.SceneManagement; using UnityEngine.SceneManagement;
string r;
var a = SceneManager.GetActiveScene();
if (a.path == "$($info.scene)") { r = "same"; }
else if (a.isDirty) { r = "DIRTY (left as is)"; }
else { EditorSceneManager.OpenScene("$($info.scene)", OpenSceneMode.Single); r = "reopened"; }
r
"@
    $r = Invoke-UnityEval -Code $code -Timeout 60
    Write-Output "scene: $r ($($info.scene))"
}

if (Test-Path $marker) { Remove-Item $marker -Force }
Write-Output "[RESULT] cleaned up"
exit 0
