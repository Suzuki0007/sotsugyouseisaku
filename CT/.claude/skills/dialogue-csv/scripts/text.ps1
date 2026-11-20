# Dialogue CSV text tools (no Unity needed). Reads Assets/Data/Dialogue/*.csv directly.
# Usage:
#   text.ps1 -Csv <csv> -Snapshot                 save a copy before editing (the "before" of -Diff)
#   text.ps1 -Csv <csv> -Diff [-Head] [-Out f.md] changed dialogues, per ID, "- old / + new" (vs the snapshot, or vs git HEAD with -Head / when no snapshot)
#   text.ps1 -Csv <csv> -Check [-Prefix A,B]      structure (columns, duplicate IDs, joined lines) + tone rules (data/tone.tsv)
#   text.ps1 -Csv <csv>[,<csv2>] -Export -Out f.md [-Prefix F1_TP_,F1_TC_]   full text as Markdown (for Docs/MUSE*/3_*.md)
# -Prefix limits to dialogue IDs starting with one of the prefixes (comma separated).
# Output is UTF-8. ASCII only in this file (Japanese strings come from data/*.tsv or are built from char codes).
param(
    [string[]]$Csv,
    [switch]$Snapshot,
    [switch]$Diff,
    [switch]$Head,
    [switch]$Check,
    [switch]$Export,
    [string]$Out,
    [string[]]$Prefix
)
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
Set-Location $root
$data = Join-Path $PSScriptRoot "..\data"
if ($Prefix) { $Prefix = @($Prefix | ForEach-Object { $_ -split "," } | Where-Object { $_ }) }
if ($Csv) { $Csv = @($Csv | ForEach-Object { $_ -split "," } | Where-Object { $_ }) }

$Q1 = [string][char]0x300C; $Q2 = [string][char]0x300D   # corner brackets
$P1 = [string][char]0xFF08; $P2 = [string][char]0xFF09   # full-width parens
$MemoHead = "# " + [char]0x3010                                  # "# " + black lenticular bracket = staging memo
$BOM = [char]0xFEFF

$speakers = @{}
foreach ($l in (Get-Content (Join-Path $data "speakers.tsv") -Encoding UTF8)) {
    if ($l.StartsWith("#") -or -not $l.Trim()) { continue }
    $c = $l -split "`t"; $speakers[$c[0]] = $c[1]
}

function Read-Dialogues([string[]]$textLines) {
    $list = New-Object System.Collections.ArrayList
    $cur = $null; $memo = New-Object System.Collections.ArrayList; $no = 0
    foreach ($raw in $textLines) {
        $no++
        if ($no -eq 1) { continue }   # header
        if ($raw.StartsWith("#")) {
            # keep only the last staging memo right before the ID (earlier ones usually close the previous scene)
            if ($raw.StartsWith($MemoHead) -and -not $raw.Contains("=====")) { $memo.Clear(); [void]$memo.Add($raw.Substring(2).Trim()) }
            continue
        }
        if (-not $raw.Trim()) { continue }
        $c = $raw.Split(",", 5)
        $line = [pscustomobject]@{ no = $no; cols = $c.Count; chara = $c[1]; expr = $(if ($c.Count -gt 2) { $c[2] } else { "" }); name = $(if ($c.Count -gt 3) { $c[3] } else { "" }); text = $(if ($c.Count -gt 4) { $c[4] } else { "" }); raw = $raw }
        if ($c[0]) {
            $cur = [pscustomobject]@{ id = $c[0]; no = $no; memo = @($memo); lines = (New-Object System.Collections.ArrayList) }
            [void]$list.Add($cur); $memo.Clear()
        } elseif (-not $cur) { continue }
        [void]$cur.lines.Add($line)
    }
    return $list
}

function In-Prefix([string]$id) {
    if (-not $Prefix) { return $true }
    foreach ($p in $Prefix) { if ($id.StartsWith($p)) { return $true } }
    return $false
}

function Speaker($l) {
    if ($l.name) { return $l.name }
    if ($speakers.ContainsKey($l.chara)) { return $speakers[$l.chara] }
    if ($l.chara) { return $l.chara }
    return "?"
}
function Fmt($l) { $e = if ($l.expr) { $P1 + $l.expr + $P2 } else { "" }; return (Speaker $l) + $Q1 + $l.text + $Q2 + $e }

function Read-CsvText([string]$path) { return [IO.File]::ReadAllLines((Join-Path $root $path), [Text.Encoding]::UTF8) }

function Read-Head([string]$path) {
    $git = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory | Sort-Object Name -Descending | Select-Object -First 1
    $exe = Join-Path $git.FullName "cmd\git.exe"
    $rel = "CT/" + $path.Replace("\", "/")
    $psi = New-Object Diagnostics.ProcessStartInfo $exe, ("show HEAD:" + $rel)
    $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [Text.Encoding]::UTF8; $psi.WorkingDirectory = $root
    $p = [Diagnostics.Process]::Start($psi); $txt = $p.StandardOutput.ReadToEnd(); $p.WaitForExit()
    if ($p.ExitCode -ne 0) { return @() }
    return $txt.TrimStart($BOM) -split "`r?`n"
}

function Snap-Path([string]$path) { return Join-Path $env:TEMP ("ct_dlg_snapshot_" + [IO.Path]::GetFileNameWithoutExtension($path) + ".csv") }

$outBuf = New-Object System.Collections.ArrayList
function Emit([string]$s) { [void]$outBuf.Add($s) }

foreach ($cf in $Csv) {
    $cf = $cf.Replace("\", "/")
    if (-not (Test-Path (Join-Path $root $cf))) { Write-Output "[ERROR] no csv: $cf"; exit 2 }

    if ($Snapshot) {
        Copy-Item (Join-Path $root $cf) (Snap-Path $cf) -Force
        Write-Output ("snapshot saved: " + (Snap-Path $cf))
    }

    if ($Diff) {
        $snap = Snap-Path $cf
        if (-not $Head -and (Test-Path $snap)) { $before = [IO.File]::ReadAllLines($snap, [Text.Encoding]::UTF8); $base = "snapshot" }
        else { $before = Read-Head $cf; $base = "git HEAD" }
        $a = @{}; foreach ($d in (Read-Dialogues $before)) { $a[$d.id] = $d }
        $bList = Read-Dialogues (Read-CsvText $cf)
        $b = @{}; foreach ($d in $bList) { $b[$d.id] = $d }
        Emit ("## " + $cf + " (vs " + $base + ")")
        $n = 0
        foreach ($d in $bList) {
            if (-not (In-Prefix $d.id)) { continue }
            $new = @($d.lines | ForEach-Object { Fmt $_ })
            if (-not $a.ContainsKey($d.id)) { $n++; Emit ""; Emit ("### " + $d.id + "  (NEW)"); $new | ForEach-Object { Emit ("+ " + $_) }; continue }
            $old = @($a[$d.id].lines | ForEach-Object { Fmt $_ })
            if (($old -join "`n") -eq ($new -join "`n")) { continue }
            $n++; Emit ""; Emit ("### " + $d.id)
            # LCS line diff
            $m = $old.Count; $k = $new.Count
            $w = $k + 1
            $t = New-Object 'int[]' (($m + 1) * $w)
            for ($i = $m - 1; $i -ge 0; $i--) {
                for ($j = $k - 1; $j -ge 0; $j--) {
                    if ($old[$i] -eq $new[$j]) { $t[$i * $w + $j] = $t[($i + 1) * $w + $j + 1] + 1 }
                    else { $down = $t[($i + 1) * $w + $j]; $right = $t[$i * $w + $j + 1]; $t[$i * $w + $j] = [math]::Max($down, $right) }
                }
            }
            $i = 0; $j = 0
            while ($i -lt $m -or $j -lt $k) {
                $useNew = $false
                if (-not ($i -lt $m -and $j -lt $k -and $old[$i] -eq $new[$j]) -and $j -lt $k) {
                    if ($i -ge $m) { $useNew = $true } else { $useNew = ($t[$i * $w + $j + 1] -gt $t[($i + 1) * $w + $j]) }
                }
                if ($i -lt $m -and $j -lt $k -and $old[$i] -eq $new[$j]) { Emit ("  " + $old[$i]); $i++; $j++ }
                elseif ($useNew) { Emit ("+ " + $new[$j]); $j++ }
                else { Emit ("- " + $old[$i]); $i++ }
            }
        }
        foreach ($id in $a.Keys) { if (-not $b.ContainsKey($id) -and (In-Prefix $id)) { $n++; Emit ""; Emit ("### " + $id + "  (REMOVED)"); $a[$id].lines | ForEach-Object { Emit ("- " + (Fmt $_)) } } }
        Emit ""; Emit ("changed dialogues: " + $n)
    }

    if ($Check) {
        $list = Read-Dialogues (Read-CsvText $cf)
        $rules = @()
        foreach ($l in (Get-Content (Join-Path $data "tone.tsv") -Encoding UTF8)) {
            if ($l.StartsWith("#") -or -not $l.Trim()) { continue }
            $c = $l -split "`t"; $rules += [pscustomobject]@{ who = $c[0]; kind = $c[1]; pat = $c[2]; msg = $c[3] }
        }
        $issues = 0; $seen = @{}
        Emit ("## check " + $cf)
        foreach ($d in $list) {
            if ($seen.ContainsKey($d.id)) { Emit ("DUP-ID line " + $d.no + ": " + $d.id + " (also line " + $seen[$d.id] + ")"); $issues++ } else { $seen[$d.id] = $d.no }
            if (-not (In-Prefix $d.id)) { continue }
            foreach ($l in $d.lines) {
                if ($l.cols -lt 5) { Emit ("COLUMNS line " + $l.no + " (" + $l.cols + " cols, need 5): " + $l.raw); $issues++; continue }
                if ($l.text -match ",(Noah|Sigle)?,[^,]*,[^,]*,") { Emit ("JOINED? line " + $l.no + " (two rows on one line?): " + $l.text); $issues++ }
                foreach ($r in $rules) {
                    if ($r.who -ne "*" -and $r.who -ne $l.chara) { continue }
                    $hit = if ($r.kind -eq "word") { $l.text.Contains($r.pat) } else { $l.text -match $r.pat }
                    if ($hit) { Emit ("TONE " + $d.id + " line " + $l.no + " [" + (Speaker $l) + "] " + $l.text + "  -> " + $r.msg); $issues++ }
                }
            }
        }
        Emit ("issues: " + $issues)
    }
}

if ($Export) {
    foreach ($cf in $Csv) {
        $cf = $cf.Replace("\", "/")
        foreach ($d in (Read-Dialogues (Read-CsvText $cf))) {
            if (-not (In-Prefix $d.id)) { continue }
            Emit ("### " + $d.id)
            foreach ($mm in $d.memo) { Emit ("*" + $mm + "*") }
            Emit ""
            foreach ($l in $d.lines) { Emit ("- " + (Fmt $l)) }
            Emit ""
        }
    }
}

if ($Out) {
    [IO.File]::WriteAllLines([IO.Path]::Combine($root, $Out), [string[]]$outBuf, (New-Object Text.UTF8Encoding($false)))
    Write-Output ("written: " + $Out + " (" + $outBuf.Count + " lines)")
} else { $outBuf | ForEach-Object { Write-Output $_ } }
