# Run a scene/asset recipe (C#) from .claude/skills/scene-recipe/recipes in the open Unity Editor.
# Usage:
#   run.ps1 -List                        list recipes (with the first comment line)
#   run.ps1 -Recipe stage1_2_door        run recipes/stage1_2_door.cs
#   run.ps1 -Recipe my_recipe -Timeout 180
# A recipe that contains the line "// @helpers" gets recipes/_helpers.cs prepended (shared lambdas).
# Refuses while in play mode or when the active scene has unsaved changes (recipes open and save scenes).
# The combined file is written to %TEMP% (outside Assets, so no import is triggered).
# ASCII only.
param(
    [string]$Recipe,
    [switch]$List,
    [int]$Timeout = 120
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")
$dir = Join-Path $PSScriptRoot "..\recipes"

if ($List -or -not $Recipe) {
    Get-ChildItem $dir -Filter *.cs | Where-Object { $_.Name -ne "_helpers.cs" } | ForEach-Object {
        $first = (Get-Content $_.FullName -Encoding utf8 -TotalCount 1)
        Write-Output ("{0,-28} {1}" -f $_.BaseName, $first)
    }
    return
}

$path = Join-Path $dir ($Recipe.Replace(".cs", "") + ".cs")
if (-not (Test-Path $path)) { Write-Output "[ERROR] no recipe: $path"; exit 2 }

$guard = Invoke-UnityEval -Code 'if (UnityEditor.EditorApplication.isPlaying) return "PLAYING"; var s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); return s.isDirty ? "DIRTY " + s.path : "OK " + s.path;'
if ($null -eq $guard) { Write-Output "[ERROR] editor not reachable"; exit 3 }
if ($guard -eq "PLAYING") { Write-Output "[ERROR] stop play mode first (end.ps1)"; exit 4 }
if ($guard.StartsWith("DIRTY")) { Write-Output "[ERROR] the open scene has unsaved changes ($guard). Ask the user before running a recipe."; exit 5 }
Write-Output "open scene before: $guard"

$body = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
if ($body -match "//\s*@helpers") {
    $helpers = [IO.File]::ReadAllText((Join-Path $dir "_helpers.cs"), [Text.Encoding]::UTF8)
    $body = $helpers + "`n" + $body
}
$tmp = Join-Path $env:TEMP ("ct_recipe_" + [guid]::NewGuid().ToString("N") + ".cs")
[IO.File]::WriteAllText($tmp, $body, (New-Object Text.UTF8Encoding($false)))
try {
    $r = Invoke-UnityEval -File $tmp -Timeout $Timeout
    Write-Output $r
} finally {
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
}
Write-Output "[NEXT] check the references the recipe set (flags, dialogues), then run unity-check -SceneCheck."
