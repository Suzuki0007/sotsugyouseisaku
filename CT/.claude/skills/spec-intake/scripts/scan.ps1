# spec-intake: read an instruction file (MUSE.AI etc.) and list every name / coordinate it uses against the project.
# Usage:
#   scan.ps1 -File <spec.md>                          names table (flags / scenes / dialogue IDs / spawn IDs): project name, exists?, conflicts
#   scan.ps1 -File <spec.md> -Out <table.md>          also write the table (Markdown) for the report
#   scan.ps1 -Rect "990,0,180,180" -Map 2160x1440     top-left (x,y,w,h) px -> center px / Unity world (center of the rect)
#   scan.ps1 -Point "1080,1305" -Map 2160x1440 [-Center]   a point: top-left px (or center px with -Center) -> the other + world
# Naming rules (Docs/MUSE*/1_*.md): FLAG_TC_DOOR_EVENT_DONE -> Flag_TC_DoorEventDone (place part: 3 letters or less stays upper case).
# ASCII only (Japanese words are built from char codes).
param(
    [string]$File,
    [string]$Out,
    [string]$Rect,
    [string]$Point,
    [string]$Map = "1440x1080",
    [switch]$Center
)
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$here = (Get-Location).Path
$mw = [double]($Map -split "x")[0]; $mh = [double]($Map -split "x")[1]

function Fmt([double]$v) { return $v.ToString("0.##", [Globalization.CultureInfo]::InvariantCulture) }

if ($Rect) {
    $r = $Rect -split "," | ForEach-Object { [double]$_ }
    $cx = $r[0] + $r[2] / 2; $cy = $r[1] + $r[3] / 2
    Write-Output ("rect TL ({0},{1}) size {2}x{3} on map {4}" -f $r[0], $r[1], $r[2], $r[3], $Map)
    Write-Output ("  center in map px (top-left origin): ({0},{1})" -f (Fmt $cx), (Fmt $cy))
    Write-Output ("  center in px from map center (x right, y down): ({0},{1})" -f (Fmt ($cx - $mw / 2)), (Fmt ($cy - $mh / 2)))
    Write-Output ("  Unity world: ({0},{1})  size {2}x{3} units" -f (Fmt (($cx - $mw / 2) / 100)), (Fmt (($mh / 2 - $cy) / 100)), (Fmt ($r[2] / 100)), (Fmt ($r[3] / 100)))
    Write-Output ("  recipe: TL({0}, {1}, {2}, {3})  or  P({4}, {5})" -f $r[0], $r[1], $r[2], $r[3], (Fmt $cx), (Fmt $cy))
}
if ($Point) {
    $p = $Point -split "," | ForEach-Object { [double]$_ }
    if ($Center) { $tx = $p[0] + $mw / 2; $ty = $p[1] + $mh / 2 } else { $tx = $p[0]; $ty = $p[1] }
    Write-Output ("point on map {0}: top-left px ({1},{2}) / from center ({3},{4}) / Unity world ({5},{6})  recipe P({1}, {2})" -f $Map, (Fmt $tx), (Fmt $ty), (Fmt ($tx - $mw / 2)), (Fmt ($ty - $mh / 2)), (Fmt (($tx - $mw / 2) / 100)), (Fmt (($mh / 2 - $ty) / 100)))
}
if (-not $File) { return }

$specPath = if ([IO.Path]::IsPathRooted($File)) { $File } else { Join-Path $here $File }
$text = [IO.File]::ReadAllText($specPath, [Text.Encoding]::UTF8)
Set-Location $root

# project side
$flags = @{}; Get-ChildItem Assets/Data/Flags -Recurse -Filter *.asset | ForEach-Object { $flags[$_.BaseName.ToLower()] = $_.BaseName }
$places = @{}; foreach ($k in $flags.Keys) { $seg = ($k -split "_"); if ($seg.Count -ge 3) { $places[$seg[1]] = $true } }
$scenes = @{}; Get-ChildItem Assets/Scenes -Filter *.unity | ForEach-Object { $scenes[$_.BaseName.ToLower()] = $_.BaseName }
$dlg = @{}
foreach ($csv in Get-ChildItem Assets/Data/Dialogue -Filter *.csv) {
    foreach ($l in [IO.File]::ReadAllLines($csv.FullName, [Text.Encoding]::UTF8)) {
        $id = ($l -split ",")[0]; if ($id -and -not $id.StartsWith("#")) { $dlg[$id.ToLower()] = $id + " (" + $csv.BaseName + ")" }
    }
}
$spawnIds = @{}
foreach ($sc in Get-ChildItem Assets/Scenes -Filter *.unity) {
    foreach ($m in [regex]::Matches([IO.File]::ReadAllText($sc.FullName), "spawnPointId: (\w+)")) { $spawnIds[$m.Groups[1].Value.ToLower()] = $m.Groups[1].Value + " (" + $sc.BaseName + ")" }
}

function To-Flag([string]$s) {
    $parts = @(($s -split "_") | Where-Object { $_ })
    if ($parts[0] -ieq "flag") { $parts = @($parts | Select-Object -Skip 1) }
    if ($parts.Count -lt 2) { return "Flag_" + ($parts -join "") }
    $place = $parts[0]; if ($place.Length -gt 3) { $place = $place.Substring(0, 1).ToUpper() + $place.Substring(1).ToLower() } else { $place = $place.ToUpper() }
    $rest = (@($parts | Select-Object -Skip 1) | ForEach-Object { if ($_ -cmatch "^[A-Z0-9]+$") { $_.Substring(0, 1) + $_.Substring(1).ToLower() } else { $_.Substring(0, 1).ToUpper() + $_.Substring(1) } }) -join ""
    return "Flag_" + $place + "_" + $rest
}

$rows = New-Object System.Collections.ArrayList
function Add-Row($kind, $spec, $proj, $state) { [void]$rows.Add([pscustomobject]@{ kind = $kind; spec = $spec; proj = $proj; state = $state }) }

foreach ($m in ([regex]::Matches($text, "\b(?:FLAG|Flag|flag)_[A-Za-z0-9_]+") | ForEach-Object { $_.Value } | Sort-Object -Unique)) {
    $proj = if ($m -cmatch "^Flag_[A-Z][A-Za-z]*_[A-Z][A-Za-z0-9]*$") { $m } else { To-Flag $m }
    $state = if ($flags.ContainsKey($proj.ToLower())) { "EXISTS (same meaning? reuse : rename the new one)" } elseif ($proj -cne $m) { "new (renamed)" } else { "new" }
    $place = ($proj -split "_")[1]
    if (-not $flags.ContainsKey($proj.ToLower()) -and -not $places.ContainsKey($place.ToLower())) { $state += "; place part '$place' is not used yet (missing place? e.g. Flag_TC_...)" }
    Add-Row "flag" $m $proj $state
}
foreach ($m in ([regex]::Matches($text, "\bStage_?\d+_?[A-Za-z0-9_]*") | ForEach-Object { $_.Value.TrimEnd("_") } | Sort-Object -Unique)) {
    $state = if ($scenes.ContainsKey($m.ToLower())) { "EXISTS" } else { "not a scene yet (new scene? name it Stage<floor>_<n>)" }
    Add-Row "scene" $m $m $state
}
foreach ($m in ([regex]::Matches($text, "\bF\d+_[A-Za-z0-9]+(?:_[A-Za-z0-9]+)*") | ForEach-Object { $_.Value } | Sort-Object -Unique)) {
    $state = if ($dlg.ContainsKey($m.ToLower())) { "EXISTS " + $dlg[$m.ToLower()] } else { "new" }
    Add-Row "dialogue" $m $m $state
}
foreach ($m in ([regex]::Matches($text, "\bFrom[A-Z][A-Za-z0-9_]+") | ForEach-Object { $_.Value } | Sort-Object -Unique)) {
    $state = if ($spawnIds.ContainsKey($m.ToLower())) { "EXISTS " + $spawnIds[$m.ToLower()] } else { "new" }
    Add-Row "spawn" $m $m $state
}

# coordinate hints ("top-left" / "center" in English or Japanese)
$jpTL = [string][char]0x5DE6 + [char]0x4E0A
$jpCenter = [string][char]0x4E2D + [char]0x5FC3
$times = [string][char]0x00D7
$hasTL = ($text -match "(?i)top-?left") -or $text.Contains($jpTL)
$hasCenter = ($text -match "(?i)\bcenter\b") -or $text.Contains($jpCenter)
$sizes = [regex]::Matches($text, "\b(\d{3,4})\s*[x" + $times + "]\s*(\d{3,4})\b") | ForEach-Object { $_.Value } | Sort-Object -Unique

$md = New-Object System.Collections.ArrayList
[void]$md.Add("| kind | spec | project | state |")
[void]$md.Add("|---|---|---|---|")
foreach ($r in $rows) { [void]$md.Add(("| {0} | ``{1}`` | ``{2}`` | {3} |" -f $r.kind, $r.spec, $r.proj, $r.state)) }
$md | ForEach-Object { Write-Output $_ }
Write-Output ""
Write-Output ("coordinates: top-left mentioned={0}, center mentioned={1}, sizes: {2}" -f $hasTL, $hasCenter, ($sizes -join ", "))
if ($hasTL -and $hasCenter) { Write-Output "[CHECK] both origins appear: confirm which one each table uses" }
elseif (-not $hasTL -and -not $hasCenter) { Write-Output "[CHECK] origin not stated: ask, or assume center (MUSE rule 2)" }
$exists = @($rows | Where-Object { $_.state.StartsWith("EXISTS") -and $_.kind -eq "flag" })
if ($exists.Count) { Write-Output ("[CHECK] {0} flag(s) already exist: same meaning? reuse : rename" -f $exists.Count) }
if ($Out) { $op = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $here $Out }; [IO.File]::WriteAllLines($op, [string[]]$md, (New-Object Text.UTF8Encoding($false))); Write-Output "written: $op" }
