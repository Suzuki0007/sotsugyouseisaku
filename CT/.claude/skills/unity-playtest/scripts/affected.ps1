# Which test scenarios to run for the uncommitted changes (git status vs HEAD), using data/affected.tsv.
# Usage: affected.ps1 [-Files a,b]   (default: the changed files from git)
# Output: one "RUN<TAB>name<TAB>reason" line per scenario, then notes. scenario.ps1 -Affected runs the RUN lines.
# ASCII only (the .tsv is UTF-8).
param([string[]]$Files)
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$table = Join-Path $PSScriptRoot "..\data\affected.tsv"
$scenarioDir = Join-Path $root "Tests\Scenarios"

if (-not $Files) {
    $gitDir = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory -ErrorAction SilentlyContinue | Sort-Object Name | Select-Object -Last 1
    if (-not $gitDir) { Write-Output "[ERROR] git (Fork) not found"; exit 2 }
    $git = Join-Path $gitDir.FullName "cmd\git.exe"
    Push-Location $root
    $st = & $git -c core.quotepath=false status --porcelain --untracked-files=all -- . 2>$null
    $prefix = (& $git rev-parse --show-prefix 2>$null)
    Pop-Location
    $Files = @()
    foreach ($l in $st) {
        $p = $l.Substring(3).Trim('"')
        if ($p.Contains(" -> ")) { $p = $p.Substring($p.IndexOf(" -> ") + 4) }
        if ($prefix -and $p.StartsWith($prefix)) { $p = $p.Substring($prefix.Length) }
        $Files += $p
    }
}

function ConvertTo-Regex([string]$g) {
    $r = [regex]::Escape($g) -replace '\\\*\\\*/', '(.*/)?' -replace '\\\*\\\*', '.*' -replace '\\\*', '[^/]*'
    return "^" + $r + "$"
}

$rules = @()
foreach ($l in (Get-Content $table -Encoding UTF8)) {
    if (-not $l -or $l.StartsWith("#") -or $l.StartsWith("pattern`t")) { continue }
    $c = $l -split "`t"
    $rules += [pscustomobject]@{ re = (ConvertTo-Regex $c[0]); pat = $c[0]; to = $c[1]; why = $(if ($c.Count -gt 2) { $c[2] } else { "" }) }
}

$allNames = @(Get-ChildItem $scenarioDir -Filter *.txt | ForEach-Object { $_.BaseName })
$sceneUse = @{}
foreach ($f in (Get-ChildItem $scenarioDir -Filter *.txt)) {
    foreach ($l in (Get-Content $f.FullName -Encoding UTF8)) {
        if ($l -match "^\s*(?:wait\s+)?scene\s+(\S+)") { $s = $Matches[1]; if (-not $sceneUse[$s]) { $sceneUse[$s] = @() }; $sceneUse[$s] += $f.BaseName }
    }
}

$run = [ordered]@{}; $notes = @(); $nomap = @()
function Add-Run([string]$n, [string]$why) { if ($allNames -contains $n -and -not $run.Contains($n)) { $run[$n] = $why } }

$Files = @($Files | ForEach-Object { $_ -split "," } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
foreach ($f in $Files) {
    $f = $f -replace "\\", "/"
    # a .meta goes with its asset; a folder's .meta alone means nothing to run
    if ($f.EndsWith(".meta")) { $f = $f.Substring(0, $f.Length - 5); if (-not [IO.Path]::HasExtension($f)) { continue } }
    $hit = $null
    foreach ($r in $rules) { if ($f -match $r.re) { $hit = $r; break } }
    if (-not $hit) { $nomap += $f; continue }
    switch ($hit.to) {
        "-" { }
        "@self" { Add-Run ([IO.Path]::GetFileNameWithoutExtension($f)) ("scenario changed: " + $f) }
        "@all" { foreach ($n in $allNames) { Add-Run $n ($hit.why + ": " + $f) } }
        "@scene" {
            $s = [IO.Path]::GetFileNameWithoutExtension($f)
            $users = @($sceneUse[$s] | Select-Object -Unique)
            if ($users.Count) { foreach ($n in $users) { Add-Run $n ($hit.why + ": " + $f) } } else { $notes += "no scenario uses scene $s ($f)" }
        }
        default { foreach ($n in ($hit.to -split ",")) { Add-Run $n.Trim() ($hit.why + ": " + $f) } }
    }
}

foreach ($k in $run.Keys) { Write-Output ("RUN`t" + $k + "`t" + $run[$k]) }
foreach ($n in $notes) { Write-Output ("NOTE " + $n) }
foreach ($n in $nomap) { Write-Output ("NOMAP " + $n + "   (no row in data/affected.tsv: decide by hand, then add a row)") }
Write-Output ("changed files: {0}, scenarios: {1}" -f $Files.Count, $run.Count)
