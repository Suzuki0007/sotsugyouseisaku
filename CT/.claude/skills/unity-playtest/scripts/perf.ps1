# Performance measurement in play mode (uses PerfMonitor in Assets/Script/Debug/PerfMonitor.cs).
# Start the play session with start.ps1 first and finish with end.ps1.
# Usage (one action per call):
#   perf.ps1 -Measure -Label timpani_battle -Seconds 20   record N seconds, append to PerfLog/perf.csv, compare with the last run of the same label
#   perf.ps1 -Begin -Label op_event                        start recording (then drive the game with event.ps1 ...)
#   perf.ps1 -End                                          stop recording, append + compare (label from -Begin)
#   perf.ps1 -Snapshot                                     GC + unload unused assets, then memory and loaded object counts
#   perf.ps1 -LeakCheck -Times 3                           title -> continue (event.ps1 -Reload) N times, snapshot each time, report growth
#                                                          (needs a save session: event.ps1 -SaveOn, then -AutoSave)
#   perf.ps1 -History -Label timpani_battle                last 5 rows of that label
#   perf.ps1 -Overlay -Value 0                             hide (1 = show) the FPS / memory overlay (F3 in game)
#   perf.ps1 -Profile                                      3 s with the Editor profiler: game-only GC alloc, top scripts by GC and by time
#   add -NoLog to -Measure / -End to print only (no CSV row); -SkipProfile to skip the 3 s profiler pass after -Measure / -End
# Numbers are from the Editor (editor overhead included). Use them to compare before / after, not as absolute values.
# In the Editor the GC counters of PerfMonitor include the Editor's own allocations; the game-only GC is the profiler's
# PlayerLoop (gameGcKBPerFrame).
# ASCII only.
param(
    [switch]$Measure,
    [switch]$Begin,
    [switch]$End,
    [switch]$Snapshot,
    [switch]$LeakCheck,
    [switch]$History,
    [switch]$Overlay,
    [string]$Label = "",
    [int]$Seconds = 20,
    [int]$Times = 3,
    [int]$Value = 1,
    [switch]$NoLog,
    [switch]$Profile,
    [switch]$SkipProfile,
    [int]$ProfileSeconds = 3
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")

$logDir = Join-Path $Script:CtRoot "PerfLog"
$perfCsv = Join-Path $logDir "perf.csv"
$leakCsv = Join-Path $logDir "leak.csv"

# Columns of perf.csv (fixed order)
$cols = @("date", "branch", "commit", "dirty", "label", "scene", "seconds", "frames", "avgFps", "p50ms", "p95ms", "p99ms", "maxms", "spikes",
          "workP50ms", "workP95ms", "workMaxms", "scriptP50ms", "scriptP95ms", "gcKBPerFrame", "gcMaxKB", "allocFramePct", "gcCollections",
          "drawCallsAvg", "drawCallsMax", "memStartMB", "memEndMB", "gcUsedMB",
          "gameGcKBPerFrame", "gameGcMaxKB", "gameAllocFramePct", "playerLoopMs", "profiledFrames")

# Compare rules: key, relative threshold, absolute threshold (both must be exceeded to warn), higher is worse.
# In the Editor, gcKBPerFrame / gcCollections / maxms / spikes include the Editor itself (its GC pauses too),
# so the game-only GC comes from the profiler (gameGcKBPerFrame) and spikes use loose thresholds.
$rules = @(
    @{ k = "p95ms"; rel = 0.15; abs = 1.0 },
    @{ k = "p99ms"; rel = 0.20; abs = 2.0 },
    @{ k = "maxms"; rel = 1.00; abs = 30.0 },
    @{ k = "spikes"; rel = 1.00; abs = 3 },
    @{ k = "workP50ms"; rel = 0.15; abs = 0.5 },
    @{ k = "workP95ms"; rel = 0.15; abs = 1.0 },
    @{ k = "scriptP95ms"; rel = 0.20; abs = 0.3 },
    @{ k = "gameGcKBPerFrame"; rel = 0.50; abs = 0.5 },
    @{ k = "gameAllocFramePct"; rel = 0.30; abs = 10 },
    @{ k = "drawCallsAvg"; rel = 0.20; abs = 10 },
    @{ k = "memEndMB"; rel = 0.10; abs = 30.0 }
)

function Get-Git {
    $g = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory -ErrorAction SilentlyContinue |
        Sort-Object { try { [version]$_.Name } catch { [version]"0.0" } } -Descending |
        ForEach-Object { Join-Path $_.FullName "cmd\git.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1
    return $g
}

# Read-only git info: branch, short commit, whether there are uncommitted changes
function Get-GitInfo {
    $git = Get-Git
    if (-not $git) { return @{ branch = "-"; commit = "-"; dirty = "-" } }
    $b = (& $git rev-parse --abbrev-ref HEAD 2>$null)
    $c = (& $git rev-parse --short HEAD 2>$null)
    $d = (& $git status --porcelain -- . 2>$null | Measure-Object).Count
    return @{ branch = "$b"; commit = "$c"; dirty = $(if ($d -gt 0) { "yes" } else { "no" }) }
}

function ConvertFrom-KV([string]$text) {
    $h = [ordered]@{}
    foreach ($p in ($text -split ";")) {
        $i = $p.IndexOf("=")
        if ($i -gt 0) { $h[$p.Substring(0, $i)] = $p.Substring($i + 1) }
    }
    return $h
}

function Test-Playing {
    $r = Invoke-UnityEval -Code 'return UnityEngine.Application.isPlaying && PerfMonitor.Instance != null ? "ok" : (UnityEngine.Application.isPlaying ? "nomonitor" : "notplaying");' -Quiet
    if ($r -eq "ok") { return $true }
    if ($r -eq "nomonitor") { Write-Output "[ERROR] PerfMonitor not found (it is created after the first scene load)" } else { Write-Output "[ERROR] not in play mode: run start.ps1 first" }
    return $false
}

function Write-Result([string]$summary) {
    $kv = ConvertFrom-KV $summary
    if ($kv.Contains("error")) { Write-Output "[ERROR] $($kv['error'])"; return }
    $gi = Get-GitInfo
    $row = [ordered]@{}
    foreach ($c in $cols) { $row[$c] = "" }
    $row["date"] = (Get-Date).ToString("yyyy-MM-dd HH:mm")
    $row["branch"] = $gi.branch; $row["commit"] = $gi.commit; $row["dirty"] = $gi.dirty
    foreach ($k in $kv.Keys) { if ($row.Contains($k)) { $row[$k] = $kv[$k] } }

    Write-Output ("{0} ({1}) {2}s {3} frames | fps {4} | frame p50 {5} p95 {6} p99 {7} max {8} ms | spikes {9}" -f
        $row.label, $row.scene, $row.seconds, $row.frames, $row.avgFps, $row.p50ms, $row.p95ms, $row.p99ms, $row.maxms, $row.spikes)
    Write-Output ("  game work (PlayerLoop - wait) p50 {0} p95 {1} max {2} ms | scripts p50 {3} p95 {4} ms | draw calls avg {5} max {6}" -f
        $row.workP50ms, $row.workP95ms, $row.workMaxms, $row.scriptP50ms, $row.scriptP95ms, $row.drawCallsAvg, $row.drawCallsMax)
    if ($row.gameGcKBPerFrame -ne "") {
        Write-Output ("  game GC alloc {0} KB/frame (max {1} KB, {2}% of frames allocate; profiler, {3} frames, PlayerLoop {4} ms)" -f
            $row.gameGcKBPerFrame, $row.gameGcMaxKB, $row.gameAllocFramePct, $row.profiledFrames, $row.playerLoopMs)
    }
    Write-Output ("  all GC incl. editor {0} KB/frame, {1} collections | mem {2} -> {3} MB, gc used {4} MB (editor included)" -f
        $row.gcKBPerFrame, $row.gcCollections, $row.memStartMB, $row.memEndMB, $row.gcUsedMB)

    # Compare with the last row of the same label
    $prev = $null
    if (Test-Path $perfCsv) { $prev = Import-Csv $perfCsv -Encoding UTF8 | Where-Object { $_.label -eq $row.label } | Select-Object -Last 1 }
    if ($prev) {
        Write-Output ("  compare with {0} ({1}{2}):" -f $prev.date, $prev.commit, $(if ($prev.dirty -eq "yes") { "+changes" } else { "" }))
        $warn = 0
        foreach ($r in $rules) {
            $a = 0.0; $b = 0.0
            if (-not [double]::TryParse($prev.($r.k), [ref]$a)) { continue }
            if (-not [double]::TryParse([string]$row[$r.k], [ref]$b)) { continue }
            $diff = $b - $a
            $relv = if ($a -ne 0) { $diff / [math]::Abs($a) } else { 0 }
            $mark = "  "
            if ($diff -gt $r.abs -and ($a -eq 0 -or $relv -gt $r.rel)) { $mark = "!!"; $warn++ }
            elseif ($diff -lt -$r.abs -and ($a -eq 0 -or -$relv -gt $r.rel)) { $mark = "++" }
            Write-Output ("    {0} {1,-14} {2,10} -> {3,10}" -f $mark, $r.k, $a, $b)
        }
        if ($warn -gt 0) { Write-Output "  [WARN] $warn value(s) got worse (!! = worse, ++ = better). Measure again before concluding (editor noise)." }
        else { Write-Output "  [OK] no value got clearly worse" }
    } else {
        Write-Output "  (first run of this label: nothing to compare)"
    }

    if (-not $NoLog) {
        if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force $logDir | Out-Null }
        [pscustomobject]$row | Export-Csv $perfCsv -Append -NoTypeInformation -Encoding UTF8
        Write-Output "  logged to PerfLog/perf.csv"
    }
}

# Records ProfileSeconds of play mode with the Editor profiler (game only), then reads it with perf_profile.cs.
# The profiler adds overhead, so it runs after the PerfMonitor recording, not during it.
function Invoke-Profile {
    Invoke-UnityEval -Code 'UnityEditorInternal.ProfilerDriver.ClearAllFrames(); UnityEditorInternal.ProfilerDriver.profileEditor = false; UnityEditorInternal.ProfilerDriver.enabled = true; return "on";' -Quiet | Out-Null
    Start-Sleep -Seconds $ProfileSeconds
    return (Invoke-UnityEval -File (Join-Path $PSScriptRoot "perf_profile.cs") -Timeout 120)
}

function Get-Snapshot {
    Invoke-UnityEval -Code 'return PerfMonitor.CollectNow();' -Quiet | Out-Null
    Start-Sleep -Milliseconds 1500
    return (Invoke-UnityEval -Code 'return PerfMonitor.Snapshot();')
}

if ($Overlay) {
    $b = if ($Value -ne 0) { "true" } else { "false" }
    Invoke-UnityEval -Code ('PerfMonitor.OverlayVisible = ' + $b + '; return "overlay " + PerfMonitor.OverlayVisible;')
    return
}

if ($History) {
    if (-not (Test-Path $perfCsv)) { Write-Output "no PerfLog/perf.csv yet"; return }
    $rows = Import-Csv $perfCsv -Encoding UTF8
    if ($Label) { $rows = $rows | Where-Object { $_.label -eq $Label } }
    $rows | Select-Object -Last 5 | Format-Table date, commit, dirty, label, scene, avgFps, p95ms, maxms, spikes, workP95ms, scriptP95ms, gameGcKBPerFrame, drawCallsAvg -AutoSize | Out-String -Width 250
    return
}

if ($Snapshot) {
    if (-not (Test-Playing)) { return }
    Get-Snapshot
    return
}

if ($Measure -or $Begin) {
    if (-not $Label) { Write-Output "[ERROR] -Label is required (same label = compared with the previous run)"; return }
    if (-not (Test-Playing)) { return }
    $safe = $Label -replace '[^A-Za-z0-9_\-]', '_'
    $r = Invoke-UnityEval -Code ('return PerfMonitor.BeginRecord("' + $safe + '");')
    Write-Output $r
    if ($Begin) { return }
    Start-Sleep -Seconds $Seconds
    $End = $true
}

if ($End) {
    $s = Invoke-UnityEval -Code 'return PerfMonitor.EndRecord();'
    if (-not $s) { return }
    $details = @()
    if (-not $SkipProfile -and $s -notmatch "error=") {
        $p = Invoke-Profile
        if ($p -and $p -notmatch "^error=") {
            $lines = $p -split "`n"
            $s = $s + ";" + $lines[0]
            $details = $lines[1..($lines.Count - 1)]
        } else { Write-Output "  (profiler: $p)" }
    }
    Write-Result $s
    $details | Where-Object { $_ -ne "" } | ForEach-Object { Write-Output $_ }
    return
}

if ($Profile) {
    if (-not (Test-Playing)) { return }
    $p = Invoke-Profile
    $p
    return
}

if ($LeakCheck) {
    if (-not (Test-Playing)) { return }
    $ev = Join-Path $PSScriptRoot "event.ps1"
    $snaps = @()
    $first = Get-Snapshot
    Write-Output "0: $first"
    $snaps += ,(ConvertFrom-KV $first)
    for ($i = 1; $i -le $Times; $i++) {
        $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Reload
        if (($out -join "`n") -match "no save session") { Write-Output "[ERROR] no save session: run event.ps1 -SaveOn and -AutoSave first"; return }
        Start-Sleep -Seconds 2
        $s = Get-Snapshot
        Write-Output "${i}: $s"
        $snaps += ,(ConvertFrom-KV $s)
    }
    # Growth: compare cycle 1 with the last cycle (cycle 0 may differ because the first load warms caches)
    $base = $snaps[[math]::Min(1, $snaps.Count - 1)]
    $last = $snaps[$snaps.Count - 1]
    Write-Output "growth from cycle 1 to cycle ${Times}:"
    $bad = 0
    foreach ($k in @("totalUsedMB", "gcUsedMB", "textures", "audio", "materials", "gameObjects")) {
        $a = [double]$base[$k]; $b = [double]$last[$k]; $d = $b - $a
        $lim = switch ($k) { "totalUsedMB" { 20 } "gcUsedMB" { 5 } default { 0 } }
        $mark = if ($d -gt $lim) { "!!" } else { "  " }
        if ($d -gt $lim) { $bad++ }
        Write-Output ("  {0} {1,-12} {2,10} -> {3,10} ({4:+0.#;-0.#;0})" -f $mark, $k, $a, $b, $d)
    }
    if ($bad -gt 0) { Write-Output "  [WARN] something keeps growing across reloads: possible leak (check with the Memory Profiler)" }
    else { Write-Output "  [OK] nothing keeps growing" }
    if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force $logDir | Out-Null }
    $gi = Get-GitInfo
    $n = 0
    foreach ($s in $snaps) {
        $row = [ordered]@{ date = (Get-Date).ToString("yyyy-MM-dd HH:mm"); commit = $gi.commit; dirty = $gi.dirty; cycle = $n }
        foreach ($k in @("scene", "totalUsedMB", "gcUsedMB", "gcHeapMB", "textures", "audio", "materials", "gameObjects")) { $row[$k] = $s[$k] }
        [pscustomobject]$row | Export-Csv $leakCsv -Append -NoTypeInformation -Encoding UTF8
        $n++
    }
    Write-Output "  logged to PerfLog/leak.csv"
    return
}

Write-Output "Specify one of -Measure -Label x, -Begin -Label x, -End, -Snapshot, -LeakCheck, -History, -Overlay -Value 0|1"
