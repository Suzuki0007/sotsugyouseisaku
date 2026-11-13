# READ-ONLY git summary of the working tree, using the git bundled with Fork.
# Never commits, pushes, switches branches or changes the index.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/finish-task/scripts/changes.ps1 [-Diff] [-Path Assets/Script/Gimmick]
#   -Diff : also print the full diff (can be long; prefer -Path to narrow it)
# ASCII only.
param(
    [switch]$Diff,
    [string]$Path = ""
)

[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
Set-Location $root

# Fork's git: newest version folder under %LOCALAPPDATA%\Fork\gitInstance
$git = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory -ErrorAction SilentlyContinue |
    Sort-Object { try { [version]$_.Name } catch { [version]"0.0" } } -Descending |
    ForEach-Object { Join-Path $_.FullName "cmd\git.exe" } |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $git) {
    $cmd = Get-Command git -ErrorAction SilentlyContinue
    if ($cmd) { $git = $cmd.Source } else { Write-Output "[ERROR] git not found (Fork not installed?)"; exit 2 }
}

$g = @("-c", "core.quotepath=false", "--no-pager")
$pathArgs = @()
if ($Path) { $pathArgs = @("--", $Path) }

Write-Output "== branch"
& $git @g status --short --branch @pathArgs | Select-Object -First 1
# The user only works on these two branches (see CLAUDE.md).
$allowed = @("ueeyama_home", "Uh_School")
$cur = (& $git @g rev-parse --abbrev-ref HEAD).Trim()
if ($allowed -notcontains $cur) {
    Write-Output "[WARN] current branch '$cur' is not one of: $($allowed -join ', '). Tell the user; do not run any git write operation here."
}
Write-Output "== last commits"
& $git @g log --oneline -5
Write-Output "== changed files (status --short)"
& $git @g status --short @pathArgs
Write-Output "== diff stat (vs HEAD, tracked files)"
& $git @g diff HEAD --stat=200 @pathArgs
if ($Diff) {
    Write-Output "== diff (vs HEAD)"
    & $git @g diff HEAD @pathArgs
}
