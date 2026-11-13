# asset-audit: re-survey the Unity project and check every row of the asset checklist (Docs xlsx) against it.
# Usage:
#   audit.ps1                 survey + report only (the workbook is not changed)
#   audit.ps1 -Apply          survey + write the result column; rows that became resolved move to the "review" progress
#   audit.ps1 -SetKeys        fill empty key cells from data/keys.tsv (adds the key/result columns if missing) and save
#   audit.ps1 -SetKeys -ForceKeys   overwrite key cells with data/keys.tsv (only when the team has not edited the keys)
#   audit.ps1 -NewSlots       also list empty asset slots that no row's key covers (candidates for new rows)
#   audit.ps1 -NoSurvey       reuse the last survey (%TEMP%\ct_asset_survey.txt)
# Key syntax (column "key", several separated by ';', optional '@scope' = substring of "where object"):
#   slot:Component.field[@scope]   resolved when every such slot is filled
#   nouse:pathPart[@scope]         resolved when no sprite / slot value / animation frame uses it
#   has:target:Component           resolved when the prefab (file name) or scene ("SceneName") has the component
#   chara:Asset:Expression         resolved when Data/Characters/Asset has a sprite for the expression
#   anim:namePart:Clip             resolved when an animation data whose path contains namePart has frames for Clip
#   item:Asset                     resolved when Resources/Items/.../Asset has an icon (not a built-in sprite)
# Refuses while Unity is playing or the open scene has unsaved changes (the survey opens every build scene).
# ASCII only.
param([switch]$Apply, [switch]$SetKeys, [switch]$ForceKeys, [switch]$NoSurvey, [switch]$NewSlots)
# "Continue": the unity CLI writes hints to stderr, which "Stop" would turn into a terminating error
$ErrorActionPreference = "Continue"
$skill = Split-Path -Parent $PSScriptRoot
$cfg = Get-Content (Join-Path $skill "data\config.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$A = $cfg.audit
. (Join-Path $skill "..\unity-check\scripts\lib.ps1")

# ---------------------------------------------------------------- survey
$surveyFile = Join-Path $env:TEMP "ct_asset_survey.txt"
if (-not $NoSurvey) {
    Write-Output "survey: opening every build scene in the editor (about 30 s)..."
    $r = Invoke-UnityEval -File (Join-Path $PSScriptRoot "survey.cs") -Timeout 400
    if (-not ("$r".StartsWith("wrote"))) { Write-Output "[ERROR] survey: $r"; exit 2 }
}
if (-not (Test-Path $surveyFile)) { Write-Output "[ERROR] no survey file"; exit 2 }

$slots = New-Object System.Collections.ArrayList
$uses = New-Object System.Collections.ArrayList
$comps = New-Object System.Collections.ArrayList
$anims = New-Object System.Collections.ArrayList
$charas = New-Object System.Collections.ArrayList
$items = New-Object System.Collections.ArrayList
foreach ($line in (Get-Content $surveyFile -Encoding UTF8)) {
    $c = $line -split "`t"
    switch ($c[0]) {
        "SLOT"   { [void]$slots.Add([pscustomobject]@{ where = $c[1]; comp = $c[2]; obj = $c[3]; field = $c[5]; val = $c[6] })
                   if ($c[6] -ne "EMPTY") { [void]$uses.Add([pscustomobject]@{ scope = $c[1] + " " + $c[3]; text = $c[6] }) } }
        "SPRITE" { [void]$uses.Add([pscustomobject]@{ scope = $c[1] + " " + $c[2]; text = $c[3] }) }
        "UIIMG"  { [void]$uses.Add([pscustomobject]@{ scope = $c[1] + " " + $c[2]; text = $c[3] }) }
        "COMP"   { [void]$comps.Add([pscustomobject]@{ where = $c[1]; type = $c[2] }) }
        "ANIM"   { [void]$anims.Add([pscustomobject]@{ path = $c[1]; text = ($c[2..($c.Count - 1)] -join "`t") })
                   [void]$uses.Add([pscustomobject]@{ scope = "DATA " + $c[1]; text = ($c[2..($c.Count - 1)] -join "`t") }) }
        "CHARA"  { [void]$charas.Add([pscustomobject]@{ path = $c[1]; text = ($c[3..($c.Count - 1)] -join "`t") }) }
        "ITEM"   { [void]$items.Add([pscustomobject]@{ path = $c[1]; icon = $c[3] }) }
    }
}
Write-Output ("survey: slots {0}, uses {1}, components {2}, animations {3}, characters {4}, items {5}" -f $slots.Count, $uses.Count, $comps.Count, $anims.Count, $charas.Count, $items.Count)

function Has([string]$s, [string]$part) { return $s.IndexOf($part, [StringComparison]::OrdinalIgnoreCase) -ge 0 }

# returns @{ ok; found; text }
function Test-Key([string]$key) {
    $key = $key.Trim()
    $scope = ""
    $at = $key.IndexOf("@")
    if ($at -ge 0) { $scope = $key.Substring($at + 1); $key = $key.Substring(0, $at) }
    $colon = $key.IndexOf(":")
    if ($colon -lt 0) { return @{ ok = $false; found = $false; text = "bad key '$key'" } }
    $type = $key.Substring(0, $colon); $arg = $key.Substring($colon + 1)
    switch ($type) {
        "slot" {
            $dot = $arg.LastIndexOf("."); $comp = $arg.Substring(0, $dot); $field = $arg.Substring($dot + 1)
            $hits = @($slots | Where-Object { $_.comp -eq $comp -and ($_.field -eq $field -or $_.field.StartsWith($field + ".Array")) -and ($scope -eq "" -or (Has ($_.where + " " + $_.obj) $scope)) })
            if ($hits.Count -eq 0) { return @{ ok = $false; found = $false; text = "$arg$(if($scope){'@'+$scope}) ?" } }
            $empty = @($hits | Where-Object { $_.val -eq "EMPTY" }).Count
            return @{ ok = ($empty -eq 0); found = $true; text = "$arg empty $empty/$($hits.Count)" }
        }
        "nouse" {
            $hits = @($uses | Where-Object { (Has $_.text $arg) -and ($scope -eq "" -or (Has $_.scope $scope)) })
            return @{ ok = ($hits.Count -eq 0); found = $true; text = "$arg used $($hits.Count)" }
        }
        "has" {
            $c2 = $arg.LastIndexOf(":"); $target = $arg.Substring(0, $c2); $comp = $arg.Substring($c2 + 1)
            $where = @($comps | Where-Object { Has $_.where $target })
            if ($where.Count -eq 0) { return @{ ok = $false; found = $false; text = "$target ?" } }
            $hit = @($where | Where-Object { $_.type -eq $comp }).Count
            return @{ ok = ($hit -gt 0); found = $true; text = "$comp on $target $(if($hit -gt 0){'yes'}else{'no'})" }
        }
        "chara" {
            $c2 = $arg.IndexOf(":"); $name = $arg.Substring(0, $c2); $expr = $arg.Substring($c2 + 1)
            $ch = @($charas | Where-Object { $_.path.EndsWith("/" + $name + ".asset") })
            if ($ch.Count -eq 0) { return @{ ok = $false; found = $true; text = "$name (no profile)" } }
            $ok = Has $ch[0].text ($expr + ":OK")
            return @{ ok = $ok; found = $true; text = "$name $expr $(if($ok){'ok'}else{'missing'})" }
        }
        "anim" {
            $c2 = $arg.IndexOf(":"); $name = $arg.Substring(0, $c2); $clip = $arg.Substring($c2 + 1)
            $an = @($anims | Where-Object { Has $_.path $name })
            $ok = @($an | Where-Object { Has $_.text ("`t" + $clip + ":OK") -or $_.text.StartsWith($clip + ":OK") }).Count -gt 0
            return @{ ok = $ok; found = ($an.Count -gt 0); text = "$name $clip $(if($ok){'ok'}else{'missing'})" }
        }
        "item" {
            $it = @($items | Where-Object { $_.path.EndsWith("/" + $arg + ".asset") })
            if ($it.Count -eq 0) { return @{ ok = $false; found = $false; text = "$arg ?" } }
            $ok = $it[0].icon -ne "NOICON" -and -not (Has $it[0].icon "unity_builtin")
            return @{ ok = $ok; found = $true; text = "$arg icon $(if($ok){'ok'}else{'missing'})" }
        }
    }
    return @{ ok = $false; found = $false; text = "unknown key type '$type'" }
}

# ---------------------------------------------------------------- workbook
$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
$changed = 0
try {
    $wb = $xl.Workbooks.Open($cfg.output)
    if ($wb.ReadOnly -and ($Apply -or $SetKeys)) { Write-Output "[ERROR] the workbook is open somewhere (read-only). Close it first."; $wb.Close($false); exit 3 }
    $ws = $wb.Worksheets.Item($cfg.sheetList)
    $lastCol = $ws.UsedRange.Columns.Count + $ws.UsedRange.Column - 1
    $col = @{}
    for ($c = 1; $c -le $lastCol; $c++) { $h = [string]$ws.Cells.Item(1, $c).Value2; if ($h) { $col[$h] = $c } }
    foreach ($need in @($A.idHeader, $A.progressHeader)) { if (-not $col.ContainsKey($need)) { Write-Output "[ERROR] column '$need' not found"; exit 4 } }

    $hdrChanged = $false
    foreach ($h in @($A.keyHeader, $A.resultHeader)) {
        if (-not $col.ContainsKey($h)) {
            if (-not ($Apply -or $SetKeys)) { $col[$h] = -1; continue }
            $lastCol++; $col[$h] = $lastCol
            $cell = $ws.Cells.Item(1, $lastCol); $cell.Value2 = $h; $cell.Font.Bold = $true; $cell.Interior.Color = 0x5A3A1F; $cell.Font.Color = 0xFFFFFF; $cell.WrapText = $true
            $ws.Columns.Item($lastCol).ColumnWidth = if ($h -eq $A.keyHeader) { 40 } else { 34 }
            $ws.Columns.Item($lastCol).WrapText = $true
            if ($h -eq $A.keyHeader) { [void]$cell.AddComment([string]$A.keyNote); $cell.Comment.Shape.Width = 420; $cell.Comment.Shape.Height = 150 }
            $hdrChanged = $true
        }
    }

    # rows
    $lastRow = $ws.Cells.Item($ws.Rows.Count, $col[$A.idHeader]).End(-4162).Row
    if ($SetKeys) {
        $keys = @{}
        foreach ($l in (Get-Content (Join-Path $skill "data\keys.tsv") -Encoding UTF8 | Select-Object -Skip 1)) { $p = $l -split "`t"; if ($p.Count -ge 2) { $keys[$p[0]] = $p[1] } }
        $set = 0
        for ($r = 2; $r -le $lastRow; $r++) {
            $id = [string]$ws.Cells.Item($r, $col[$A.idHeader]).Value2
            $kc = $ws.Cells.Item($r, $col[$A.keyHeader])
            if ($keys.ContainsKey($id) -and (-not [string]$kc.Value2 -or ($ForceKeys -and [string]$kc.Value2 -ne $keys[$id]))) { $kc.Value2 = $keys[$id]; $set++ }
        }
        Write-Output "set keys: $set"
        $changed += $set
    }

    $today = (Get-Date).ToString("yyyy-MM-dd")
    $nRes = 0; $nNot = 0; $nMan = 0; $nMoved = 0
    $report = New-Object System.Collections.ArrayList
    $coveredSlots = New-Object System.Collections.Generic.HashSet[string]
    for ($r = 2; $r -le $lastRow; $r++) {
        $id = [string]$ws.Cells.Item($r, $col[$A.idHeader]).Value2
        if (-not $id) { continue }
        $name = if ($col.ContainsKey($A.nameHeader)) { [string]$ws.Cells.Item($r, $col[$A.nameHeader]).Value2 } else { "" }
        $progress = [string]$ws.Cells.Item($r, $col[$A.progressHeader]).Value2
        $keyText = if ($col[$A.keyHeader] -gt 0) { [string]$ws.Cells.Item($r, $col[$A.keyHeader]).Value2 } else { "" }
        if (-not $keyText) {
            $nMan++
            if ($Apply -and $col[$A.resultHeader] -gt 0) { $ws.Cells.Item($r, $col[$A.resultHeader]).Value2 = $A.manual }
            continue
        }
        $allOk = $true; $anyMissing = $false; $details = @()
        foreach ($k in ($keyText -split ";")) {
            if (-not $k.Trim()) { continue }
            if ($k.Trim().StartsWith("slot:")) { $s = $k.Trim().Substring(5); $at = $s.IndexOf("@"); if ($at -ge 0) { $s = $s.Substring(0, $at) }; [void]$coveredSlots.Add($s) }
            $t = Test-Key $k
            if (-not $t.ok) { $allOk = $false; $details += $t.text }
            if (-not $t.found) { $anyMissing = $true }
        }
        $result = if ($allOk) { "$($A.resolved) $today" } elseif ($anyMissing) { "$($A.notFound): " + ($details -join " / ") } else { "$($A.notYet): " + ($details -join " / ") }
        if ($allOk) { $nRes++ } else { $nNot++ }
        [void]$report.Add(("{0,-7} {1} | {2} | {3}" -f $id, $(if ($allOk) { "OK " } else { "-- " }), $name, $result))
        if ($Apply) {
            $rc = $ws.Cells.Item($r, $col[$A.resultHeader])
            if ([string]$rc.Value2 -ne $result) { $rc.Value2 = $result; $changed++ }
            # font color only (the row stripes and category colors come from format.ps1)
            $rc.Font.Color = if ($allOk) { 0x006100 } elseif ($anyMissing) { 0x0000C0 } else { 0x00659C }
            $rc.Font.Bold = $allOk
            if ($allOk -and ($A.progressMovable -contains $progress)) { $ws.Cells.Item($r, $col[$A.progressHeader]).Value2 = $A.progressReview; $nMoved++; $changed++ }
        }
    }

    Write-Output ""
    $report | Sort-Object | ForEach-Object { Write-Output $_ }
    Write-Output ""
    Write-Output ("rows with keys: resolved {0}, not yet {1}; rows without keys (check by hand): {2}; moved to review: {3}" -f $nRes, $nNot, $nMan, $nMoved)

    if ($NewSlots) {
        Write-Output ""
        Write-Output "empty slots not covered by any key (excluding samples and data/ignore.tsv):"
        $ignore = New-Object System.Collections.Generic.HashSet[string]
        foreach ($l in (Get-Content (Join-Path $skill "data\ignore.tsv") -Encoding UTF8 | Select-Object -Skip 1)) { $p = $l -split "`t"; if ($p[0]) { [void]$ignore.Add($p[0]) } }
        $slots | Where-Object { $_.val -eq "EMPTY" -and -not (Has ($_.where + $_.obj) "Sample") -and -not $_.field.Contains(".Array") } |
            Where-Object { -not $coveredSlots.Contains($_.comp + "." + $_.field) -and -not $ignore.Contains($_.comp + "." + $_.field) } |
            Group-Object { $_.comp + "." + $_.field + "  @" + $_.where } | Sort-Object Name |
            ForEach-Object { Write-Output ("  {0}  x{1}  e.g. {2}" -f $_.Name, $_.Count, $_.Group[0].obj) }
    }

    if ($Apply -or ($SetKeys -and ($changed -gt 0 -or $hdrChanged))) {
        $wb.Save()
        Write-Output "saved: $changed change(s)"
    } else {
        Write-Output "(workbook not changed; use -Apply to write results)"
    }
    $wb.Close($false)
}
finally {
    $xl.Quit()
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($xl)
}
