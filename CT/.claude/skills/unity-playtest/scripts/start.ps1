# Prepare and start a play-mode test.
#  1. back up persistentDataPath/save*.json (the user's slots 1-3)
#  2. optionally open a scene (refuses if any open scene has unsaved changes)
#  3. run_in_background on, enter play mode, Health.PlayerInvincible = true
# Always run end.ps1 afterwards, even if the test failed.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/start.ps1 [-Scene Stage1_1] [-NoInvincible]
# ASCII only.
param(
    [string]$Scene,
    [switch]$NoInvincible
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")

if (-not (Wait-UnityReady)) { exit 2 }

$playing = Invoke-UnityEval -Code "UnityEditor.EditorApplication.isPlaying"
if ($playing -eq $true) {
    Write-Output "[ERROR] The Editor is already in play mode (maybe the user is testing). Stop and ask the user."
    exit 3
}

$markerDir = Join-Path $env:TEMP "ct_playtest_backup"
$marker = Join-Path $markerDir "latest.json"
if (Test-Path $marker) {
    Write-Output "[ERROR] A previous test was not cleaned up ($marker exists). Run end.ps1 first."
    exit 4
}

# --- 1. back up saves
$pdp = Invoke-UnityEval -Code "UnityEngine.Application.persistentDataPath"
if (-not $pdp) { Write-Output "[ERROR] could not read persistentDataPath"; exit 5 }
$pdp = $pdp -replace "/", "\"
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = Join-Path $markerDir $stamp
New-Item -ItemType Directory -Force $backup | Out-Null
$saves = @(Get-ChildItem -Path $pdp -Filter "save*.json" -ErrorAction SilentlyContinue)
foreach ($f in $saves) { Copy-Item $f.FullName $backup }
Write-Output ("backed up {0} save file(s) from {1} to {2}" -f $saves.Count, $pdp, $backup)

$origScene = Invoke-UnityEval -Code "UnityEngine.SceneManagement.SceneManager.GetActiveScene().path"

$info = [ordered]@{ pdp = $pdp; backup = $backup; scene = $origScene; started = $stamp }
($info | ConvertTo-Json) | Set-Content -Path $marker -Encoding UTF8

# --- 2. open scene
if ($Scene) {
    $code = @"
using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.SceneManagement;
string r;
bool dirty = false;
for (int i = 0; i < SceneManager.sceneCount; i++) { if (SceneManager.GetSceneAt(i).isDirty) dirty = true; }
if (dirty) { r = "DIRTY"; }
else {
    string path = null;
    foreach (var g in AssetDatabase.FindAssets("t:Scene $Scene")) {
        var p = AssetDatabase.GUIDToAssetPath(g);
        if (System.IO.Path.GetFileNameWithoutExtension(p) == "$Scene") { path = p; break; }
    }
    if (path == null) { r = "NOTFOUND"; }
    else { EditorSceneManager.OpenScene(path, OpenSceneMode.Single); r = "OK " + path; }
}
r
"@
    $r = Invoke-UnityEval -Code $code -Timeout 60
    Write-Output "open scene: $r"
    if ($r -eq "DIRTY") { Write-Output "[ERROR] An open scene has unsaved changes. Ask the user before switching scenes."; exit 6 }
    if ($r -eq "NOTFOUND") { Write-Output "[ERROR] Scene '$Scene' not found."; exit 6 }
}

# --- 3. play
unity command editor.playmode set_run_in_background --enabled true --result-only 2>$null | Out-Null
unity command editor.playmode start --result-only 2>$null | Out-Null

$ok = $false
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 500
    $p = Invoke-UnityEval -Quiet -Code "UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isCompiling"
    if ($p -eq $true) { $ok = $true; break }
}
if (-not $ok) { Write-Output "[ERROR] play mode did not start within 30s. Run end.ps1."; exit 7 }

if (-not $NoInvincible) {
    Start-Sleep -Milliseconds 500
    $r = Invoke-UnityEval -Code "Health.PlayerInvincible = true; Health.PlayerInvincible"
    Write-Output "PlayerInvincible = $r"
}
$active = Invoke-UnityEval -Code "UnityEngine.SceneManagement.SceneManager.GetActiveScene().name"
Write-Output "[RESULT] playing. active scene: $active"
exit 0
