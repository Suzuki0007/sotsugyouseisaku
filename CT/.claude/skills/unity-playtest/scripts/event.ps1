# Event test helpers for play mode (dialogue stepping, state, warp, run, flags, capture).
# Usage (one action per call):
#   event.ps1 -State                       one-line state (dialogue line, input block, party, darkness, enemies, battle)
#   event.ps1 -Advance 3                   advance N dialogue lines (finish typing + next)
#   event.ps1 -UntilIdle                   keep advancing until no dialogue and no event sequence is running
#   event.ps1 -Warp "-128.6,13.0"          warp Noah and Sigle to x,y
#   event.ps1 -Run "Root/Child/Object"     run ItemInteraction (as the current player) or EventSequence on an active object;
#                                          any other Intaractable (switch, peg...) gets its Interact() called (= pressing E in range)
#   event.ps1 -Flag Flag_OP_Done -Value 1  set a GameFlag asset (Assets/Data/Flags/<name>.asset) on/off
#   event.ps1 -Capture name                capture the Game view to %TEMP%\ct_playtest_capture\name.png
# Each eval round trip takes ~1.5 s, so effects shorter than that (flash, shot) cannot be captured.
# ASCII only.
param(
    [switch]$State,
    [int]$Advance = 0,
    [switch]$UntilIdle,
    [string]$Warp,
    [string]$Run,
    [string]$Flag,
    [int]$Value = 1,
    [string]$Capture,
    [int]$MaxSteps = 200
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")

$stateCode = @'
var m = UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>();
var dm = DialogueManager.Instance;
string line = "-";
if (dm != null && dm.IsTalking) {
  var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
  var arr = (DialogueData.Line[])typeof(DialogueManager).GetField("lines", bf).GetValue(dm);
  int idx = (int)typeof(DialogueManager).GetField("index", bf).GetValue(dm);
  line = idx + "/" + arr.Length + " " + arr[idx].DisplayName + ": " + arr[idx].text;
}
var sb = new System.Text.StringBuilder();
sb.Append("scene=").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
sb.Append(" | talk=[").Append(line).Append("]");
sb.Append(" | blocked=").Append(InputBlock.IsBlocked).Append(" seq=").Append(EventSequence.IsBusy).Append(" fader=").Append(SceneFader.IsBusy).Append(" battle=").Append(BattleArea.AnyActive);
if (m != null && m.Noah != null) {
  sb.Append(" | mode=").Append(m.Mode).Append(m.PartnerAbsent ? "(Sigle absent)" : "").Append(" cur=").Append(m.CurrentPlayer != null ? m.CurrentPlayer.name.Replace("(Clone)", "") : "-");
  var na = m.Noah.GetComponent<SpriteAnimator>();
  sb.Append(" noah=").Append(m.Noah.transform.position.ToString("F1")).Append(na != null ? " " + na.Facing : "");
  if (m.Sigle != null) { var sa = m.Sigle.GetComponent<SpriteAnimator>(); sb.Append(" sigle=").Append(m.Sigle.activeSelf ? m.Sigle.transform.position.ToString("F1") + (sa != null ? " " + sa.Facing : "") : "off"); }
  var h = m.PartyHealth; if (h != null) sb.Append(" hp=").Append(h.CurrentHealth).Append("/").Append(h.MaxHealth);
}
if (StageDarkness.Active != null) sb.Append(" | dark=").Append(StageDarkness.Active.Darkness.ToString("F2")).Append(" r=").Append(StageDarkness.Active.Radius.ToString("F1"));
if (ScreenCurtain.Alpha > 0f) sb.Append(" curtain=").Append(ScreenCurtain.Alpha.ToString("F2"));
var enemies = UnityEngine.Object.FindObjectsByType<Enemy>();
if (enemies.Length > 0) { sb.Append(" | enemies="); foreach (var e in enemies) sb.Append(e.transform.position.ToString("F1")).Append(e.IsDead ? "dead" : (e.Paused ? "paused" : "")).Append(" "); }
return sb.ToString();
'@

$advanceCode = @'
var dm = DialogueManager.Instance;
if (dm == null || !dm.IsTalking) return EventSequence.IsBusy ? "busy" : "idle";
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(DialogueManager).GetMethod("CompleteLine", bf).Invoke(dm, null);
typeof(DialogueManager).GetMethod("Next", bf).Invoke(dm, null);
return "advanced";
'@

if ($State) { Invoke-UnityEval -Code $stateCode; return }

if ($Advance -gt 0) {
    for ($i = 0; $i -lt $Advance; $i++) { $r = Invoke-UnityEval -Code $advanceCode; if ($r -ne "advanced") { Write-Output "stopped after $i ($r)"; break } }
    Invoke-UnityEval -Code $stateCode
    return
}

if ($UntilIdle) {
    $idle = 0; $count = 0
    for ($i = 0; $i -lt $MaxSteps; $i++) {
        $r = Invoke-UnityEval -Code $advanceCode
        if ($r -eq "advanced") { $count++; $idle = 0; continue }
        if ($r -eq "idle") { $idle++; if ($idle -ge 2) { break } }
        Start-Sleep -Milliseconds 300
    }
    Write-Output "advanced $count line(s)"
    Invoke-UnityEval -Code $stateCode
    return
}

if ($Warp) {
    $xy = $Warp -split ","
    $code = "var m = UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>(); var p = new UnityEngine.Vector3(" + $xy[0].Trim() + "f, " + $xy[1].Trim() + "f, 0f); m.Warp(p, p); return `"warped`";"
    Invoke-UnityEval -Code $code
    return
}

if ($Run) {
    $code = @'
var go = UnityEngine.GameObject.Find("__PATH__");
if (go == null) return "not found (inactive or wrong path): __PATH__";
var m = UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>();
var who = m != null ? m.CurrentPlayer : null;
var ii = go.GetComponent<ItemInteraction>(); if (ii != null) { ii.Run(who); return "ItemInteraction.Run"; }
var seq = go.GetComponent<EventSequence>(); if (seq != null) { seq.Play(who); return "EventSequence.Play"; }
var any = go.GetComponent<Intaractable>();
if (any != null) { var mi = typeof(Intaractable).GetMethod("Interact", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); mi.Invoke(any, null); return any.GetType().Name + ".Interact (same as pressing E; the player must be in range)"; }
return "no ItemInteraction / EventSequence / Intaractable on " + go.name;
'@
    Invoke-UnityEval -Code ($code.Replace("__PATH__", $Run.Replace('"', '\"')))
    return
}

if ($Flag) {
    $on = if ($Value -ne 0) { "true" } else { "false" }
    $code = "var f = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlag>(`"Assets/Data/Flags/$Flag.asset`"); if (f == null) return `"no flag $Flag`"; GameFlags.Set(f, $on); return `"$Flag = `" + GameFlags.Get(f);"
    Invoke-UnityEval -Code $code
    return
}

if ($Capture) {
    $dir = Join-Path $env:TEMP "ct_playtest_capture"
    New-Item -ItemType Directory -Force $dir | Out-Null
    $out = Join-Path $dir ($Capture + ".png")
    unity command capture game_view --output-path $out --result-only 2>$null | Out-Null
    Write-Output $out
    return
}

Write-Output "Specify one of -State, -Advance N, -UntilIdle, -Warp x,y, -Run path, -Flag name, -Capture name"
