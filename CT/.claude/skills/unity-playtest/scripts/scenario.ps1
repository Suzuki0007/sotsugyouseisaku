# Run a test scenario (Tests/Scenarios/<name>.txt) in play mode and report PASS / FAIL for every "expect" line.
# Usage:
#   scenario.ps1 -Name stage1_2_rescue          (or a full path to a .txt file)
#   scenario.ps1 -Name all                      run every scenario in Tests/Scenarios
#   scenario.ps1 -Name a,b,c                    run these one after another
#   scenario.ps1 -Affected                      run the scenarios that match the changed files (affected.tsv); -WhatIf only lists them
#   options: -KeepPlaying (do not stop play mode at the end), -StopOnFail, -Legacy (the old one-eval-per-line runner)
# How it works (2026-11-28): the first "scene" line starts play mode through start.ps1 (save backup, invincible).
# Every later line goes to the Editor in ONE eval: C# expressions (expect eval / within / wait until / eval) become
# lambdas, and Assets/Script/Debug/Editor/ScenarioRunner.cs runs the steps in EditorApplication.update and appends
# results to a file that this script prints. end.ps1 always runs at the end (restores saves and fonts).
# The command list is the same as scenario_legacy.ps1 (the planner guide is in Docs/).
# ASCII only (scenario files are UTF-8 and may contain Japanese).
param(
    [string]$Name,
    [switch]$Affected,
    [switch]$WhatIf,
    [switch]$KeepPlaying,
    [switch]$StopOnFail,
    [switch]$Legacy,
    # save the "expect looks" screenshots as the new baselines (Tests/Baselines/<name>.png) instead of comparing
    [switch]$UpdateBaselines
)
$ErrorActionPreference = "Continue"
$ps = "powershell"
$scenarioDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path "Tests\Scenarios"

if ($Legacy) {
    $a = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $PSScriptRoot "scenario_legacy.ps1"), "-Name", $Name)
    if ($KeepPlaying) { $a += "-KeepPlaying" }; if ($StopOnFail) { $a += "-StopOnFail" }
    & $ps @a; exit $LASTEXITCODE
}

if ($Affected) {
    $pick = & $ps -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "affected.ps1")
    $names = @($pick | Where-Object { $_ -match "^RUN\t" } | ForEach-Object { ($_ -split "`t")[1] })
    $pick | Where-Object { $_ -notmatch "^RUN\t" } | ForEach-Object { Write-Output $_ }
    if ($WhatIf -or $names.Count -eq 0) { if ($names.Count -eq 0) { Write-Output "nothing to run" } else { Write-Output ("would run: " + ($names -join ", ")) }; exit 0 }
    $Name = $names -join ","
}
if (-not $Name) { Write-Output "[ERROR] -Name <scenario>|all|a,b or -Affected"; exit 2 }

# --- several scenarios: run each in its own process (its own play session)
$list = $null
if ($Name -eq "all") { $list = @(Get-ChildItem $scenarioDir -Filter *.txt | Sort-Object Name | ForEach-Object { $_.FullName }) }
elseif ($Name.Contains(",")) { $list = @($Name -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
if ($list) {
    $failedFiles = @(); $sw = [Diagnostics.Stopwatch]::StartNew()
    foreach ($f in $list) {
        Write-Output ""
        Write-Output ("=================== " + [IO.Path]::GetFileNameWithoutExtension($f))
        $a = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $PSCommandPath, "-Name", $f)
        if ($StopOnFail) { $a += "-StopOnFail" }
        if ($UpdateBaselines) { $a += "-UpdateBaselines" }
        & $ps @a
        if ($LASTEXITCODE -ne 0) { $failedFiles += [IO.Path]::GetFileNameWithoutExtension($f) }
    }
    Write-Output ""
    Write-Output ("ALL: {0} scenario(s) in {1} s, failed: {2}" -f $list.Count, [int]$sw.Elapsed.TotalSeconds, $(if ($failedFiles.Count) { $failedFiles -join ", " } else { "none" }))
    if ($failedFiles.Count) { exit 1 } else { exit 0 }
}

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")
$file = if (Test-Path $Name) { $Name } else { Join-Path $scenarioDir ($Name + ".txt") }
if (-not (Test-Path $file)) { Write-Output "[ERROR] no scenario: $file"; exit 2 }
$lines = Get-Content $file -Encoding UTF8
$scenarioName = [IO.Path]::GetFileNameWithoutExtension($file)

function Q([string]$s) { return '@"' + $s.Replace('"', '""') + '"' }

# --- parse: lines before the first "scene" may only be "invincible"; the rest become steps
$invincible = $true; $firstScene = $null; $steps = New-Object System.Collections.ArrayList
for ($i = 0; $i -lt $lines.Count; $i++) {
    $no = $i + 1
    $raw = $lines[$i]
    $cut = $raw.IndexOf("  #"); if ($cut -ge 0) { $raw = $raw.Substring(0, $cut) }
    $line = $raw.Trim()
    if (-not $line -or $line.StartsWith("#")) { continue }
    $sp = $line.IndexOf(" ")
    $cmd = if ($sp -lt 0) { $line } else { $line.Substring(0, $sp) }
    $arg = if ($sp -lt 0) { "" } else { $line.Substring($sp + 1).Trim() }
    if (-not $firstScene) {
        if ($cmd -eq "invincible") { $invincible = ($arg -ne "off"); continue }
        if ($cmd -eq "scene") { $firstScene = $arg; continue }
        Write-Output "[ERROR] line ${no}: the first command must be 'scene' (or 'invincible')"; exit 2
    }
    $cond = "null"; $eval = "null"
    $p = $arg -split "\s+", 2
    if ($cmd -eq "expect" -and $p[0] -eq "eval") { $cond = "() => (" + $p[1] + ")" }
    elseif ($cmd -eq "expect" -and $p[0] -eq "within") { $q = $p[1] -split "\s+", 2; $cond = "() => (" + $q[1] + ")" }
    elseif ($cmd -eq "wait" -and $p[0] -eq "until") { $q = $p[1] -split "\s+", 2; $cond = "() => (" + $q[1] + ")" }
    elseif ($cmd -eq "eval") {
        if ($arg -match "\breturn\b") { $eval = "() => { " + $arg + " }" }
        elseif ($arg.TrimEnd().EndsWith(";")) { $eval = "() => { " + $arg + " return `"ok`"; }" }
        # a method call may return void (eval SetMood(...)): run it as a statement (its value is not printed)
        elseif ($arg.TrimEnd().EndsWith(")")) { $eval = "() => { " + $arg + "; return `"ok`"; }" }
        else { $eval = "() => (object)(" + $arg + ")" }
    }
    [void]$steps.Add(("  new ScenarioRunner.Step({0}, {1}, {2}, {3}, {4}, {5})," -f $no, (Q $line), (Q $cmd), (Q $arg), $cond, $eval))
}
if (-not $firstScene) { Write-Output "[ERROR] no 'scene' line"; exit 2 }

# --- start play mode (save backup, fonts, invincible)
Write-Output ("   scene " + $firstScene)
$startArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $PSScriptRoot "start.ps1"), "-Scene", $firstScene)
if (-not $invincible) { $startArgs += "-NoInvincible" }
$out = & $ps @startArgs
$out | Select-Object -Last 1 | ForEach-Object { Write-Output ("       " + $_) }
if (-not ($out -match "playing")) {
    Write-Output "[ERROR] could not start play mode"; $out | ForEach-Object { Write-Output ("       " + $_) }
    # start.ps1 may have backed up the saves and pressed play already (play can begin late, after a recompile):
    # always clean up, or the next scenario finds the Editor "already playing" (2026-11-28). Not when it refused
    # because the user is playing (exit 3) or a previous run was not cleaned up (exit 4).
    if (-not ($out -match "already in play mode|was not cleaned up")) {
        & $ps -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "end.ps1") | Select-Object -Last 1 | ForEach-Object { Write-Output ("       " + $_) }
    }
    exit 3
}

$resultFile = Join-Path $env:TEMP ("ct_scenario_" + $scenarioName + ".txt")
if (Test-Path $resultFile) { Remove-Item $resultFile -Force }
$code = "var steps = new ScenarioRunner.Step[] {`n" + ($steps -join "`n") + "`n};`nreturn ScenarioRunner.Begin(" + (Q $scenarioName) + ", steps, " + (Q $resultFile) + ", " + $(if ($StopOnFail) { "true" } else { "false" }) + ", " + $(if ($invincible) { "true" } else { "false" }) + $(if ($UpdateBaselines) { ", true" } else { "" }) + ");"
$sw = [Diagnostics.Stopwatch]::StartNew()
$began = Invoke-UnityEval -Code $code -Timeout 60
$results = New-Object System.Collections.ArrayList
$done = $null
try {
    if ($began -ne "started") {
        Write-Output ("[ERROR] runner did not start: " + $began)
        Write-Output "        (a C# expression in the scenario does not compile? the message above names the line inside the generated code)"
    } else {
        # --- follow the result file
        $seen = 0; $quiet = 0
        while (-not $done) {
            Start-Sleep -Milliseconds 700
            $all = @(); if (Test-Path $resultFile) { $all = @(Get-Content $resultFile -Encoding UTF8) }
            for ($k = $seen; $k -lt $all.Count; $k++) {
                $c = $all[$k] -split "`t"
                switch ($c[0]) {
                    "STEP" { Write-Output ("{0,4}: {1}" -f $c[1], $c[2]) }
                    "INFO" { Write-Output ("       " + $c[2]) }
                    "PASS" { [void]$results.Add([pscustomobject]@{ no = $c[1]; ok = $true; text = $c[2]; detail = $c[3] }); Write-Output ("  [PASS] line {0}: {1}  -> {2}" -f $c[1], $c[2], $c[3]) }
                    "FAIL" { [void]$results.Add([pscustomobject]@{ no = $c[1]; ok = $false; text = $c[2]; detail = $c[3] }); Write-Output ("  [FAIL] line {0}: {1}  -> {2}" -f $c[1], $c[2], $c[3]); Write-Output ("         screenshot: " + (Join-Path $env:TEMP ("ct_playtest_capture\fail_" + $scenarioName + "_" + $c[1] + ".png"))) }
                    "DONE" { $done = $c }
                }
            }
            if ($all.Count -gt $seen) { $quiet = 0 } else { $quiet++ }
            $seen = $all.Count
            # nothing for 3 minutes: the Editor is stuck or play mode was stopped from outside
            if (-not $done -and $quiet -gt 260) { Write-Output "[ERROR] no progress for 3 minutes"; break }
        }
    }
}
finally {
    if (-not $KeepPlaying) {
        & $ps -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "end.ps1") | Where-Object { $_ -match "font assets|RESULT|WARN|ERROR" } | ForEach-Object { Write-Output ("       " + $_) }
    }
}

$fail = @($results | Where-Object { -not $_.ok })
$why = if ($done -and $done[3] -ne "end") { " (" + $done[3] + ")" } else { "" }
if (-not $done) { $why = " (not finished)" }
Write-Output ""
Write-Output ("RESULT {0}: {1} passed, {2} failed in {3} s{4}" -f $scenarioName, ($results.Count - $fail.Count), $fail.Count, [int]$sw.Elapsed.TotalSeconds, $why)
foreach ($f in $fail) { Write-Output ("  FAIL line {0}: {1}  -> {2}" -f $f.no, $f.text, $f.detail) }
if ($fail.Count -gt 0 -or -not $done -or $done[3] -eq "exception") { exit 1 } else { exit 0 }
