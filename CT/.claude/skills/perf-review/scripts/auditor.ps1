# Runs Unity Project Auditor (built into the Editor since 6.4; rules from com.unity.project-auditor-rules)
# on the open Editor, summarizes the report, compares it with the previous run and logs counts.
# Usage:
#   auditor.ps1                 analyze (about 1 min), summarize, compare with PerfLog/auditor_issues.tsv, log PerfLog/auditor.csv
#   auditor.ps1 -Last           summarize the last saved report again (no analysis)
#   options: -Top 12 (kinds per group) / -NoLog (do not update PerfLog)
# Not in play mode. The full report stays in %TEMP%\ct_auditor_report.json (open it in Window > Analysis > Project Auditor > Load).
# Descriptors listed in references/auditor-ignore.md are only counted (Project Auditor's own Ignore is not saved to settings).
# ASCII only.
param(
    [switch]$Last,
    [int]$Top = 12,
    [switch]$NoLog
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$report = Join-Path $env:TEMP "ct_auditor_report.json"
$tsvNew = Join-Path $env:TEMP "ct_auditor_issues.tsv"
$logDir = Join-Path $Script:CtRoot "PerfLog"
$tsvLog = Join-Path $logDir "auditor_issues.tsv"
$csvLog = Join-Path $logDir "auditor.csv"
$ignoreFile = Join-Path $PSScriptRoot "..\references\auditor-ignore.md"

if (-not (Wait-UnityReady)) { exit 2 }
$state = Invoke-UnityEval -Code 'return UnityEditor.EditorApplication.isPlaying ? "playing" : (UnityEditor.EditorApplication.isCompiling ? "compiling" : "ok");' -Quiet
if ($state -ne "ok") { Write-Output "[ERROR] editor is $state (stop play mode / wait for compile)"; exit 1 }

if (-not $Last) {
    $path = $report.Replace('\', '/')
    $start = @"
var p = new Unity.ProjectAuditor.Editor.AnalysisParams(true);
p.Platform = UnityEditor.BuildTarget.StandaloneWindows64;
UnityEditor.EditorPrefs.DeleteKey("ct_auditor_done");
var sw = System.Diagnostics.Stopwatch.StartNew();
p.OnCompleted = r => { r.Save("$path"); UnityEditor.EditorPrefs.SetString("ct_auditor_done", sw.Elapsed.TotalSeconds.ToString("F0") + "s, " + r.NumTotalIssues + " items"); };
new Unity.ProjectAuditor.Editor.ProjectAuditor().AuditAsync(p, null);
return "started";
"@
    Write-Output "analyzing (about 1 min)..."
    Invoke-UnityEval -Code $start -Timeout 600 -Quiet | Out-Null
    $done = ""
    for ($i = 0; $i -lt 180; $i++) {
        $done = Invoke-UnityEval -Code 'return UnityEditor.EditorPrefs.GetString("ct_auditor_done", "");' -Quiet
        if ($done) { break }
        Start-Sleep -Seconds 5
    }
    if (-not $done) { Write-Output "[ERROR] analysis did not finish within 15 min"; exit 1 }
    Write-Output "analysis done: $done"
}
if (-not (Test-Path $report)) { Write-Output "[ERROR] no saved report: run without -Last"; exit 1 }

# ignore list: first column IDs of the table
$ignore = @()
foreach ($l in [System.IO.File]::ReadAllLines($ignoreFile, (New-Object System.Text.UTF8Encoding($false)))) {
    $m = [regex]::Match($l, '^\|\s*((?:PA|UDR)\w+)\s*\|')
    if ($m.Success) { $ignore += $m.Groups[1].Value }
}

$code = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot "auditor_summary.cs"))
$code = $code.Replace("__REPORT__", $report.Replace('\', '/')).Replace("__TSV__", $tsvNew.Replace('\', '/')).Replace("__TOP__", "$Top").Replace("__IGNORE__", ($ignore -join ","))
$sum = Invoke-UnityEval -Code $code -Timeout 120
if (-not $sum -or $sum -match '^error=') { Write-Output "[ERROR] summary failed: $sum"; exit 1 }
$lines = $sum -split "`n"
$kv = @{}
foreach ($p in ($lines[0] -split ';')) { $i = $p.IndexOf('='); if ($i -gt 0) { $kv[$p.Substring(0, $i)] = $p.Substring($i + 1) } }

Write-Output ""
$rulesVer = "?"
$lockHit = Select-String -Path (Join-Path $Script:CtRoot 'Packages\packages-lock.json') -Pattern '"com.unity.project-auditor-rules"' -Context 0,1
if ($lockHit) { $vm = [regex]::Match($lockHit.Context.PostContext[0], '"version":\s*"([^"]+)"'); if ($vm.Success) { $rulesVer = $vm.Groups[1].Value } }
Write-Output "# Project Auditor (com.unity.project-auditor-rules $rulesVer)"
Write-Output ("assets/settings {0} | runtime code {1} | domain reload {2} | Editor code {3} (counted only) | ignored {4} (auditor-ignore.md) | in packages {5} | inventory rows {6}" -f
    $kv.assetIssues, $kv.codeIssues, $kv.reloadIssues, $kv.editorIssues, $kv.ignored, $kv.packageIssues, $kv.inventory)
$lines[1..($lines.Count - 1)] | ForEach-Object { Write-Output $_ }

# Diff with the previous TSV (key = id + path + description; line numbers shift with edits)
function Read-Keys([string]$file) {
    $h = @{}
    if (-not (Test-Path $file)) { return $null }
    $rows = [System.IO.File]::ReadAllLines($file, (New-Object System.Text.UTF8Encoding($false)))
    for ($i = 1; $i -lt $rows.Count; $i++) {
        $c = $rows[$i].Split("`t")
        if ($c.Count -lt 6) { continue }
        $k = $c[0] + "|" + $c[3] + "|" + $c[5]
        $h[$k] = $c[0] + " " + $c[3] + $(if ($c[4] -ne "0") { ":" + $c[4] } else { "" }) + "  " + $c[5]
    }
    return $h
}
$old = Read-Keys $tsvLog
$new = Read-Keys $tsvNew
Write-Output ""
Write-Output "## compared with the previous run (PerfLog/auditor_issues.tsv)"
if ($null -eq $old) { Write-Output "first run: nothing to compare" }
else {
    $added = @($new.Keys | Where-Object { -not $old.ContainsKey($_) })
    $gone = @($old.Keys | Where-Object { -not $new.ContainsKey($_) })
    Write-Output ("new {0} / resolved {1}" -f $added.Count, $gone.Count)
    $added | Select-Object -First 15 | ForEach-Object { Write-Output ("  + " + $new[$_]) }
    if ($added.Count -gt 15) { Write-Output "  + ... $($added.Count - 15) more" }
    $gone | Select-Object -First 8 | ForEach-Object { Write-Output ("  - " + $old[$_]) }
}

if (-not $NoLog) {
    if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force $logDir | Out-Null }
    Copy-Item $tsvNew $tsvLog -Force
    $git = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory -ErrorAction SilentlyContinue |
        Sort-Object { try { [version]$_.Name } catch { [version]"0.0" } } -Descending |
        ForEach-Object { Join-Path $_.FullName "cmd\git.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1
    $commit = "-"; $dirty = "-"
    if ($git) { $commit = (& $git rev-parse --short HEAD 2>$null); $dirty = $(if ((& $git status --porcelain -- . 2>$null | Measure-Object).Count -gt 0) { "yes" } else { "no" }) }
    $row = [ordered]@{ date = (Get-Date).ToString("yyyy-MM-dd HH:mm"); commit = "$commit"; dirty = $dirty }
    foreach ($k in @("playerCompileErrors", "assetIssues", "codeIssues", "reloadIssues", "editorIssues", "ignored", "packageIssues", "inventory", "total")) { $row[$k] = $kv[$k] }
    [pscustomobject]$row | Export-Csv $csvLog -Append -NoTypeInformation -Encoding UTF8
    Write-Output ""
    Write-Output "logged to PerfLog/auditor.csv and PerfLog/auditor_issues.tsv"
}
