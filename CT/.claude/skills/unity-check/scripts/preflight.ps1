# Pre-flight check before starting work that touches scenes (recipes, playtests, scenarios).
# Run it once at the start of a task, so questions to the user (unsaved scene, play mode) come first, not in the middle.
# Usage:
#   preflight.ps1                 editor reachable / play mode / unsaved scenes / compile errors / git branch
#   preflight.ps1 -Recipe shosai  also lists the recipes whose name starts with "shosai" and the scenes they open
# Exit code: 0 = OK, 1 = something needs the user (see [ASK] lines), 3 = editor not reachable.
# ASCII only.
param(
    [string]$Recipe
)

. (Join-Path $PSScriptRoot "lib.ps1")
$need = 0

$state = Invoke-UnityEval -Quiet -Code @'
var sb = new System.Text.StringBuilder();
sb.Append(UnityEditor.EditorApplication.isPlaying ? "PLAYING" : "EDIT").Append("\n");
sb.Append(UnityEditor.EditorUtility.scriptCompilationFailed ? "COMPILE-FAILED" : "COMPILE-OK").Append("\n");
for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++) {
  var s = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
  sb.Append(s.isDirty ? "DIRTY " : "SCENE ").Append(s.path).Append("\n");
}
return sb.ToString();
'@
if ($null -eq $state) { Write-Output "[ERROR] editor not reachable (start it with: unity open . -- ask the user first)"; exit 3 }

foreach ($l in ($state -split "`n")) {
    if (-not $l) { continue }
    if ($l -eq "PLAYING") { Write-Output "[ASK] play mode is on (the user may be testing). Ask before stopping it, or run end.ps1 if it is ours."; $need = 1 }
    elseif ($l -eq "COMPILE-FAILED") { Write-Output "[ASK] scripts have compile errors (check.ps1 for details). Ours? fix. Not ours? report."; $need = 1 }
    elseif ($l.StartsWith("DIRTY")) { Write-Output ("[ASK] unsaved changes in " + $l.Substring(6) + " -> ask: save / discard / keep and wait"); $need = 1 }
    elseif ($l.StartsWith("SCENE")) { Write-Output ("[OK] open scene " + $l.Substring(6)) }
    elseif ($l -eq "EDIT") { Write-Output "[OK] edit mode" }
    elseif ($l -eq "COMPILE-OK") { Write-Output "[OK] no compile errors" }
}

# hidden cameras left behind (a temporary capture camera draws over the Main Camera in the Game view)
$stray = Find-StrayCameras -Remove
foreach ($s in $stray) {
    if ($s -match "\(removed\)") { Write-Output "[FIXED] leftover temporary camera removed: $s" }
    else { Write-Output "[ASK] hidden camera in the editor: $s (it may cover the Game view). Ours? destroy it. Not ours? ask."; $need = 1 }
}
if ($stray.Count -eq 0) { Write-Output "[OK] no leftover hidden cameras" }

# git branch (read only)
$git = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
if ($git) {
    $exe = Join-Path $git.FullName "cmd\git.exe"
    $branch = (& $exe rev-parse --abbrev-ref HEAD 2>$null)
    if ($branch -eq "ueeyama_home" -or $branch -eq "Uh_School") { Write-Output "[OK] branch $branch" }
    else { Write-Output "[ASK] branch is '$branch' (the user works only on ueeyama_home / Uh_School)"; $need = 1 }
}

if ($Recipe) {
    $dir = Join-Path $PSScriptRoot "..\..\scene-recipe\recipes"
    $files = @(Get-ChildItem $dir -Filter ($Recipe + "*.cs") | Sort-Object Name)
    if ($files.Count -eq 0) { Write-Output "[WARN] no recipe starts with '$Recipe'" }
    foreach ($f in $files) {
        $txt = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
        $scenes = [regex]::Matches($txt, 'Assets/Scenes/[^"]+?\.unity') | ForEach-Object { $_.Value } | Sort-Object -Unique
        Write-Output ("recipe {0}: opens {1}" -f $f.BaseName, $(if ($scenes) { $scenes -join ", " } else { "(no scene path)" }))
    }
}

if ($need) { Write-Output "PREFLIGHT: ask the user first ([ASK] lines)"; exit 1 }
Write-Output "PREFLIGHT: OK"
exit 0
