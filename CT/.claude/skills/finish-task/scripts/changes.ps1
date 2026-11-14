# READ-ONLY git summary of the working tree, using the git bundled with Fork.
# Never commits, pushes, switches branches or changes the index.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/finish-task/scripts/changes.ps1 [-Diff] [-Path Assets/Script/Gimmick]
#   -Diff : also print the full diff (can be long; prefer -Path to narrow it)
#   -Log  : also print who changed the files (git log, last 15; use with -Path <file>)
# Always lists changed C# files whose \author is another team member.
# ASCII only.
param(
    [switch]$Diff,
    [switch]$Log,
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

$g = @("-c", "core.quotepath=false", "-c", "core.safecrlf=false", "--no-pager")
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

# Changed C# files written by other team members (non-empty \author line).
# The user's own files leave \author empty. Mention these in the commit message (rules/team-code.md).
Write-Output "== changed files by other authors (\author)"
$changed = @(& $git @g diff HEAD --name-only --relative @pathArgs) + @(& $git @g ls-files --others --exclude-standard @pathArgs)
$found = 0
foreach ($f in ($changed | Where-Object { $_ -like "*.cs" } | Sort-Object -Unique)) {
    if (-not (Test-Path -LiteralPath $f)) { continue }
    $line = Get-Content -LiteralPath $f -TotalCount 12 -Encoding UTF8 | Where-Object { $_ -match '\\author' } | Select-Object -First 1
    if (-not $line) { continue }
    $name = ($line -replace '^.*\\author', '').Trim()
    if ($name -and $name -notmatch '^uh\b') { Write-Output ("  {0}  <- {1}" -f $f, $name); $found++ }
}
if ($found -eq 0) { Write-Output "  (none)" } else { Write-Output "  -> explain these changes to the author in the commit message" }

if ($Log) {
    Write-Output "== history (who changed it)"
    & $git @g log -15 --date=short --format="%h %ad %an  %s" @pathArgs
}
if ($Diff) {
    Write-Output "== diff (vs HEAD)"
    & $git @g diff HEAD @pathArgs
}
