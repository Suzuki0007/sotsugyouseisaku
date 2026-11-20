# Take a top-down picture of a whole map with the areas drawn on it (MapShot.Capture).
# Usage:
#   mapshot.ps1 -Scene Stage1_4            -> Temp/MapShot/Stage1_4.png (+ legend and spawn coordinates)
#   mapshot.ps1 -Scene Stage1_3,Stage1_4 -Ppu 100
# Opens the scene only when it is not the active one, and goes back to the previous scene afterwards.
# Refuses when a scene has unsaved changes or in play mode (never saves scenes).
# Attach the PNG to the report (SendUserFile) after building or moving things in a room.
# ASCII only.
param(
    [Parameter(Mandatory = $true)][string[]]$Scene,
    [int]$Ppu = 50
)
. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")
$Scene = @($Scene | ForEach-Object { $_ -split "," } | Where-Object { $_ })

$guard = Invoke-UnityEval -Code 'if (UnityEditor.EditorApplication.isPlaying) return "PLAYING"; for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++) { var d = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i); if (d.isDirty) return "DIRTY " + d.path; } return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;'
if ($null -eq $guard) { Write-Output "[ERROR] editor not reachable"; exit 3 }
if ($guard -eq "PLAYING") { Write-Output "[ERROR] stop play mode first"; exit 4 }
if ($guard.StartsWith("DIRTY")) { Write-Output "[ERROR] unsaved changes ($guard). Ask the user first."; exit 5 }
$original = $guard

foreach ($s in $Scene) {
    $code = @'
var path = "Assets/Scenes/__S__.unity";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != path) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
return MapShot.Capture("Temp/MapShot/__S__.png", __PPU__);
'@
    $r = Invoke-UnityEval -Code ($code.Replace("__S__", $s).Replace("__PPU__", "$Ppu")) -Timeout 180
    Write-Output $r
}

if ($original) {
    [void](Invoke-UnityEval -Code ('var p = "' + $original + '"; if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != p) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(p, UnityEditor.SceneManagement.OpenSceneMode.Single); return "ok";'))
    Write-Output "back to $original"
}
