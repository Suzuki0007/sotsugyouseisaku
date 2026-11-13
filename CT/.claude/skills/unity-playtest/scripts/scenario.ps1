# Run a test scenario (Tests/Scenarios/<name>.txt) in play mode and report PASS / FAIL for every "expect" line.
# Usage:
#   scenario.ps1 -Name stage1_2_rescue          (or a full path to a .txt file)
#   scenario.ps1 -Name all                      run every scenario in Tests/Scenarios
#   options: -KeepPlaying (do not stop play mode at the end), -StopOnFail (stop at the first failed expect)
# The first "scene" line starts play mode through start.ps1 (save backup, invincible unless "invincible off" comes first).
# end.ps1 always runs at the end (restores the saves), unless -KeepPlaying.
# During the run the Input System is switched to "all device input goes to the Game view / ignore focus" so "key" lines
# reach the game even when the editor is in the background; the original settings are restored at the end.
# Commands (one per line, '#' = comment, "  #" starts a trailing comment):
#   scene <Scene> | invincible on|off | save on | flag <FlagAsset> on|off | advance all|<n>|battle | wait <sec>|idle|battle|scene <Scene>
#   warp x,y | run <Hierarchy/Path> | key <Key> [ms] | button <GameObjectName> | tighten all | kill | autosave | reload | title
#   capture <name> | eval <C# statements; return value>
#   expect flag <FlagAsset> on|off | expect party <Together|Waiting|Separated> | expect scene <Scene> | expect near x,y [r]
#   expect talk <text part> | expect idle | expect reach all|<name part> | expect no-error | expect prompt <text part>
#   expect selected <GameObjectName> | expect eval <C# bool expression>
# ASCII only (scenario files are UTF-8 and may contain Japanese).
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [switch]$KeepPlaying,
    [switch]$StopOnFail
)
$ErrorActionPreference = "Continue"
. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")
$project = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$scenarioDir = Join-Path $project "Tests\Scenarios"
$ps = "powershell"

if ($Name -eq "all") {
    $total = 0; $failedFiles = @()
    foreach ($f in (Get-ChildItem $scenarioDir -Filter *.txt | Sort-Object Name)) {
        Write-Output ""
        Write-Output ("=================== " + $f.BaseName)
        & $ps -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -Name $f.FullName
        if ($LASTEXITCODE -ne 0) { $failedFiles += $f.BaseName }
        $total++
    }
    Write-Output ""
    Write-Output ("ALL: {0} scenario(s), failed: {1}" -f $total, $(if ($failedFiles.Count) { $failedFiles -join ", " } else { "none" }))
    if ($failedFiles.Count) { exit 1 } else { exit 0 }
}

$file = if (Test-Path $Name) { $Name } else { Join-Path $scenarioDir ($Name + ".txt") }
if (-not (Test-Path $file)) { Write-Output "[ERROR] no scenario: $file"; exit 2 }
$lines = Get-Content $file -Encoding UTF8
$scenarioName = [IO.Path]::GetFileNameWithoutExtension($file)

$ev = Join-Path $PSScriptRoot "event.ps1"
function Ev([string[]]$a) { & $ps -NoProfile -ExecutionPolicy Bypass -File $ev @a }
function Esc([string]$s) { return $s.Replace('\', '\\').Replace('"', '\"') }
function E([string]$code, [int]$t = 60) { return Invoke-UnityEval -Code $code -Timeout $t -Quiet }

$results = New-Object System.Collections.ArrayList
$started = $false
$invincible = $true
$inputChanged = $false
$errBase = 0
$stop = $false

function Record([int]$no, [string]$text, [bool]$ok, [string]$detail) {
    $mark = if ($ok) { "PASS" } else { "FAIL" }
    [void]$results.Add([pscustomobject]@{ no = $no; ok = $ok; text = $text; detail = $detail })
    Write-Output ("  [{0}] line {1}: {2}{3}" -f $mark, $no, $text, $(if ($detail) { "  -> " + $detail } else { "" }))
    if (-not $ok) {
        Ev @("-Capture", ("fail_" + $scenarioName + "_" + $no)) | ForEach-Object { Write-Output ("         screenshot: " + $_) }
        if ($StopOnFail) { $script:stop = $true }
    }
}

function WaitUntil([string]$cond, [int]$seconds) {
    $code = "return (" + $cond + ") ? `"yes`" : `"no`";"
    for ($i = 0; $i -lt ($seconds * 2); $i++) {
        if ((E $code) -eq "yes") { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

$playerReady = "UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded && !SceneFader.IsBusy"
$idleCond = "(DialogueManager.Instance == null || !DialogueManager.Instance.IsTalking) && !EventSequence.IsBusy && !SceneFader.IsBusy"
$errScan = @'
var t = System.Type.GetType("UnityEditor.LogEntries,UnityEditor"); var et = System.Type.GetType("UnityEditor.LogEntry,UnityEditor");
int n = (int)t.GetMethod("StartGettingEntries").Invoke(null, null); var entry = System.Activator.CreateInstance(et);
var sb = new System.Text.StringBuilder(); int count = 0;
for (int i = __FROM__; i < n; i++) {
  t.GetMethod("GetEntryInternal").Invoke(null, new object[]{ i, entry });
  int mode = (int)et.GetField("mode").GetValue(entry); string msg = (string)et.GetField("message").GetValue(entry);
  if ((mode & (1|2|16|64|256|2048|131072|1048576)) == 0) continue;
  if (msg.Contains("/api/exec") || msg.Contains("Pipeline") || msg.Contains("Account API")) continue;
  count++; if (count <= 3) sb.Append(msg.Split('\n')[0]).Append(" | ");
}
t.GetMethod("EndGettingEntries").Invoke(null, null);
return __MODE__;
'@

try {
    for ($i = 0; $i -lt $lines.Count -and -not $stop; $i++) {
        $no = $i + 1
        $raw = $lines[$i]
        $cut = $raw.IndexOf("  #"); if ($cut -ge 0) { $raw = $raw.Substring(0, $cut) }
        $line = $raw.Trim()
        if (-not $line -or $line.StartsWith("#")) { continue }
        $sp = $line.IndexOf(" ")
        $cmd = if ($sp -lt 0) { $line } else { $line.Substring(0, $sp) }
        $arg = if ($sp -lt 0) { "" } else { $line.Substring($sp + 1).Trim() }
        Write-Output ("{0,4}: {1}" -f $no, $line)

        switch ($cmd) {
            "scene" {
                if (-not $started) {
                    $startArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $PSScriptRoot "start.ps1"), "-Scene", $arg)
                    if (-not $invincible) { $startArgs += "-NoInvincible" }
                    $out = & $ps @startArgs
                    $out | Select-Object -Last 1 | ForEach-Object { Write-Output ("       " + $_) }
                    if (-not ($out -match "playing")) { Write-Output "[ERROR] could not start play mode"; $stop = $true; break }
                    $started = $true
                    [void](WaitUntil $playerReady 20)
                    Start-Sleep -Milliseconds 800
                    $orig = E 'var s = UnityEngine.InputSystem.InputSystem.settings; string o = s.editorInputBehaviorInPlayMode + "|" + s.backgroundBehavior; s.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; s.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus; return o;'
                    $inputChanged = $true
                    $errBase = [int](E 'var t = System.Type.GetType("UnityEditor.LogEntries,UnityEditor"); int n = (int)t.GetMethod("StartGettingEntries").Invoke(null, null); t.GetMethod("EndGettingEntries").Invoke(null, null); return n;')
                } else {
                    [void](E ("UnityEngine.SceneManagement.SceneManager.LoadScene(`"" + (Esc $arg) + "`"); return `"ok`";"))
                    Start-Sleep -Milliseconds 800
                    [void](WaitUntil ("UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == `"" + (Esc $arg) + "`" && " + $playerReady) 30)
                    Start-Sleep -Milliseconds 800
                }
            }
            "invincible" { $invincible = ($arg -ne "off"); if ($started) { [void](E ("Health.PlayerInvincible = " + $(if ($invincible) { "true" } else { "false" }) + "; return `"ok`";")) } }
            "save"   { Ev @("-SaveOn") | ForEach-Object { Write-Output ("       " + $_) } }
            "flag"   { $p = $arg -split "\s+"; Ev @("-Flag", $p[0], "-Value", $(if ($p[1] -eq "off") { "0" } else { "1" })) | ForEach-Object { Write-Output ("       " + $_) } }
            "advance" {
                if ($arg -eq "battle") {
                    # advance dialogue lines until a battle area starts, then return at once (chase: warp right after this)
                    $adv = 'if (BattleArea.AnyActive) return "battle"; var dm = DialogueManager.Instance; if (dm == null || !dm.IsTalking) return "wait"; var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; typeof(DialogueManager).GetMethod("CompleteLine", bf).Invoke(dm, null); typeof(DialogueManager).GetMethod("Next", bf).Invoke(dm, null); return "advanced";'
                    $n = 0; $got = $false
                    for ($k = 0; $k -lt 120; $k++) { $r = E $adv; if ($r -eq "battle") { $got = $true; break }; if ($r -eq "advanced") { $n++ } else { Start-Sleep -Milliseconds 200 } }
                    Write-Output ("       advanced $n line(s), battle " + $(if ($got) { "started" } else { "NOT started" }))
                }
                elseif ($arg -eq "all" -or -not $arg) { Ev @("-UntilIdle") | Select-Object -First 1 | ForEach-Object { Write-Output ("       " + $_) } }
                else { Ev @("-Advance", $arg) | Select-Object -First 1 | ForEach-Object { Write-Output ("       " + $_) } }
            }
            "wait" {
                $p = $arg -split "\s+", 2
                switch ($p[0]) {
                    "idle"   { if (-not (WaitUntil $idleCond 30)) { Write-Output "       (still busy after 30 s)" } }
                    "battle" { if (-not (WaitUntil "BattleArea.AnyActive" 30)) { Write-Output "       (no battle after 30 s)" } }
                    "scene"  { if (-not (WaitUntil ("UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == `"" + (Esc $p[1]) + "`" && " + $playerReady) 40)) { Write-Output "       (scene not reached)" }; Start-Sleep -Milliseconds 800 }
                    default  { Start-Sleep -Milliseconds ([int]([double]$p[0] * 1000)) }
                }
            }
            "warp"    { Ev @("-Warp", $arg) | ForEach-Object { Write-Output ("       " + $_) } }
            "run"     { Ev @("-Run", $arg) | ForEach-Object { Write-Output ("       " + $_) } }
            "key" {
                $p = $arg -split "\s+"; $ms = if ($p.Count -gt 1) { [int]$p[1] } else { 150 }
                [void](E ("UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key." + $p[0] + ")); return `"down`";"))
                Start-Sleep -Milliseconds $ms
                [void](E 'UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "up";')
                Start-Sleep -Milliseconds 300
            }
            "button" {
                $r = E ("foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>()) { if (b.gameObject.name == `"" + (Esc $arg) + "`" && b.isActiveAndEnabled && b.interactable) { b.onClick.Invoke(); return `"clicked`"; } } return `"not found`";")
                Write-Output ("       " + $r)
                Start-Sleep -Milliseconds 500
            }
            "tighten" {
                $r = E 'var m = typeof(TimpaniPeg).GetMethod("Tighten", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); int n = 0; foreach (var p in UnityEngine.Object.FindObjectsByType<TimpaniPeg>()) { if (!p.IsTightened) { m.Invoke(p, null); n++; } } return "tightened " + n;'
                Write-Output ("       " + $r); Start-Sleep -Milliseconds 1500
            }
            "kill" {
                $r = E 'Health.PlayerInvincible = false; var m = PlayerSwitchManager.Instance; int n = 0; foreach (var go in new[]{ m.Noah, m.Sigle }) { if (go == null) continue; var h = go.GetComponent<Health>(); if (h != null && !h.IsDead) { h.Damage(999); n++; } } return "damaged " + n;'
                Write-Output ("       " + $r)
            }
            "autosave" { Ev @("-AutoSave") | ForEach-Object { Write-Output ("       " + $_) } }
            "reload"   { Ev @("-Reload") | Select-Object -Last 1 | ForEach-Object { Write-Output ("       " + $_) }; Start-Sleep -Milliseconds 800 }
            "title"    { [void](E 'GameSession.ReturnToTitle(); return "ok";'); [void](WaitUntil ("UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == `"Title`" && !SceneFader.IsBusy") 20); Start-Sleep -Milliseconds 800 }
            "capture"  { Ev @("-Capture", $arg) | ForEach-Object { Write-Output ("       " + $_) } }
            "eval"     { Write-Output ("       " + (E $arg)) }
            "expect" {
                $p = $arg -split "\s+", 2
                $what = $p[0]; $rest = if ($p.Count -gt 1) { $p[1] } else { "" }
                switch ($what) {
                    "flag" {
                        $q = $rest -split "\s+"; $want = if ($q[1] -eq "off") { "False" } else { "True" }
                        $v = E ("var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(`"Assets/Data/Flags/" + (Esc $q[0]) + ".asset`"); if (f == null) return `"no flag`"; return GameFlags.Get(f).ToString();")
                        Record $no $line ($v -eq $want) ("is " + $v)
                    }
                    "party" { $v = E 'var m = PlayerSwitchManager.Instance; return m == null ? "none" : m.Mode.ToString();'; Record $no $line ($v -eq $rest) ("is " + $v) }
                    "scene" { $v = E 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'; Record $no $line ($v -eq $rest) ("is " + $v) }
                    "near" {
                        $q = $rest -split "\s+"; $xy = $q[0] -split ","; $rad = if ($q.Count -gt 1) { $q[1] } else { "1.0" }
                        $v = E ("var m = PlayerSwitchManager.Instance; if (m == null || m.CurrentPlayer == null) return `"no player`"; var p = (UnityEngine.Vector2)m.CurrentPlayer.transform.position; float d = UnityEngine.Vector2.Distance(p, new UnityEngine.Vector2(" + $xy[0] + "f, " + $xy[1] + "f)); return (d <= " + $rad + "f ? `"ok `" : `"far `") + p.ToString(`"F1`") + `" d=`" + d.ToString(`"F1`");")
                        Record $no $line ("$v".StartsWith("ok")) $v
                    }
                    "talk" {
                        $v = E 'var dm = DialogueManager.Instance; if (dm == null || !dm.IsTalking) return "(not talking)"; var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var arr = (DialogueData.Line[])typeof(DialogueManager).GetField("lines", bf).GetValue(dm); int idx = (int)typeof(DialogueManager).GetField("index", bf).GetValue(dm); return arr[idx].DisplayName + ": " + arr[idx].text;'
                        Record $no $line ("$v".Contains($rest)) $v
                    }
                    "idle" { $v = E ("return (" + $idleCond + ") ? `"idle`" : `"busy`";"); Record $no $line ($v -eq "idle") $v }
                    "reach" {
                        $v = E ("var m = PlayerSwitchManager.Instance; if (m == null || m.CurrentPlayer == null) return `"no player`"; var list = MapReach.Check(m.CurrentPlayer.transform.position); int ng = 0, n = 0; string first = `"`"; foreach (var e in list) { if (`"" + (Esc $rest) + "`" != `"all`" && !e.name.Contains(`"" + (Esc $rest) + "`")) continue; n++; if (!e.reachable) { ng++; if (first == `"`") first = e.name + `" (`" + e.reason + `")`"; } } return (ng == 0 && n > 0 ? `"ok `" : `"ng `") + (n - ng) + `"/`" + n + (first != `"`" ? `" e.g. `" + first : `"`");")
                        Record $no $line ("$v".StartsWith("ok")) $v
                    }
                    "no-error" {
                        $v = E ($errScan.Replace("__FROM__", "$errBase").Replace("__MODE__", 'count == 0 ? "ok" : count + " error(s): " + sb.ToString()'))
                        Record $no $line ($v -eq "ok") $v
                    }
                    "prompt" { $v = E 'var v = UnityEngine.Object.FindAnyObjectByType<InteractPromptView>(); if (v == null) return "(no view)"; var t = v.GetComponentInChildren<TMPro.TextMeshPro>(true); return t.gameObject.activeSelf ? t.text : "(hidden)";'; Record $no $line ("$v".Contains($rest)) $v }
                    "selected" { $v = E 'var es = UnityEngine.EventSystems.EventSystem.current; var s = es != null ? es.currentSelectedGameObject : null; return s != null ? s.name : "(none)";'; Record $no $line ($v -eq $rest) ("is " + $v) }
                    "eval" { $v = E ("return (" + $rest + ") ? `"True`" : `"False`";"); Record $no $line ($v -eq "True") $v }
                    default { Record $no $line $false "unknown expect" }
                }
            }
            default { Write-Output "       [WARN] unknown command '$cmd' (skipped)" }
        }
    }
}
finally {
    if ($inputChanged) {
        [void](E 'var s = UnityEngine.InputSystem.InputSystem.settings; s.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus; s.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices; return "ok";')
    }
    if ($started -and -not $KeepPlaying) {
        & $ps -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "end.ps1") | Select-Object -Last 1 | ForEach-Object { Write-Output ("       " + $_) }
    }
}

$fail = @($results | Where-Object { -not $_.ok })
Write-Output ""
Write-Output ("RESULT {0}: {1} passed, {2} failed{3}" -f $scenarioName, ($results.Count - $fail.Count), $fail.Count, $(if ($stop -and $fail.Count) { " (stopped at the first failure)" } else { "" }))
foreach ($f in $fail) { Write-Output ("  FAIL line {0}: {1}  -> {2}" -f $f.no, $f.text, $f.detail) }
if ($fail.Count -gt 0) { exit 1 } else { exit 0 }
