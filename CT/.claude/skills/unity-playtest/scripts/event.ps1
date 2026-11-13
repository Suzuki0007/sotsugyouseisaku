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
#   event.ps1 -SaveOn                      direct play only: start a save session (writes go to save_auto.json; backed up by start.ps1)
#   event.ps1 -AutoSave                    autosave now at the current player's position (needs a save session)
#   event.ps1 -Reload                      load check: return to title -> continue from the autosave slot -> print state
#   event.ps1 -CheckInteract               for every interactable around the current player: is the E-key path alive?
#                                          (clears the base 'player' field and checks that Intaractable.Update restores it)
#   event.ps1 -Prompt                      what the key prompt ([E] ...) is showing now
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
    [switch]$SaveOn,
    [switch]$AutoSave,
    [switch]$Reload,
    [switch]$CheckInteract,
    [switch]$Prompt,
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

if ($SaveOn) {
    Invoke-UnityEval -Code 'if (!Save.HasSlot) Save.StartNewGame(); return "save session on (slot " + Save.CurrentSlot + ", writes to " + Save.SlotPath(Save.AutoSlot) + ")";'
    return
}

if ($AutoSave) {
    Invoke-UnityEval -Code 'if (!Save.HasSlot) return "no save session (use -SaveOn first)"; var p = PlayerSwitchManager.Instance.CurrentPlayer.transform.position; GameSession.AutoSaveAt(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, p); return "autosaved at " + p.ToString("F1");'
    return
}

if ($Reload) {
    $before = Invoke-UnityEval -Code 'if (!Save.HasSlot) return "NOSESSION"; return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'
    if ($before -eq "NOSESSION") { Write-Output "no save session: run -SaveOn first (direct play does not save)"; return }
    $meta = Invoke-UnityEval -Code 'var i = SaveSlotInfo.Load(Save.AutoSlot); return "autosave: scene=" + i.SceneName + " exists=" + i.Exists;'
    Write-Output $meta
    Invoke-UnityEval -Code 'GameSession.ReturnToTitle(); return "to title";' | Out-Null
    for ($i = 0; $i -lt 20; $i++) { Start-Sleep -Milliseconds 500; $s = Invoke-UnityEval -Code 'return SceneFader.IsBusy ? "busy" : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;' -Quiet; if ($s -eq "Title") { break } }
    Invoke-UnityEval -Code 'GameSession.Continue(Save.AutoSlot, "Stage1_1"); return "continue";' | Out-Null
    for ($i = 0; $i -lt 30; $i++) { Start-Sleep -Milliseconds 500; $s = Invoke-UnityEval -Code 'return (SceneFader.IsBusy || PlayerSwitchManager.Instance == null || PlayerSwitchManager.Instance.CurrentPlayer == null) ? "busy" : "ready";' -Quiet; if ($s -eq "ready") { break } }
    Start-Sleep -Milliseconds 800
    Write-Output "after reload (check: start position is reachable, no event replays, progress kept):"
    Invoke-UnityEval -Code $stateCode
    return
}

if ($CheckInteract) {
    $clear = @'
var f = typeof(Intaractable).GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var cur = PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;
if (cur == null) return "no current player";
System.Func<UnityEngine.Component, string> Id = c => { var s = c.GetType().Name + ":" + c.name; for (var x = c.transform.parent; x != null; x = x.parent) s = x.name + "/" + s; return s; };
var names = new System.Collections.Generic.List<string>();
foreach (var it in UnityEngine.Object.FindObjectsByType<Intaractable>()) { if (f.GetValue(it) as UnityEngine.GameObject == cur) { f.SetValue(it, null); names.Add(Id(it)); } }
UnityEditor.EditorPrefs.SetString("ct_check_interact", string.Join("\n", names));
return names.Count + " in range";
'@
    # eval calls cannot share variables, so the ids go through EditorPrefs (deleted by the second call)
    Write-Output (Invoke-UnityEval -Code $clear)
    Start-Sleep -Milliseconds 500
    $check = @'
var f = typeof(Intaractable).GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
System.Func<UnityEngine.Component, string> Id = c => { var s = c.GetType().Name + ":" + c.name; for (var x = c.transform.parent; x != null; x = x.parent) s = x.name + "/" + s; return s; };
var ids = UnityEditor.EditorPrefs.GetString("ct_check_interact", "").Split('\n');
var sb = new System.Text.StringBuilder();
foreach (var it in UnityEngine.Object.FindObjectsByType<Intaractable>()) {
  if (System.Array.IndexOf(ids, Id(it)) < 0) continue;
  bool ok = f.GetValue(it) != null;
  sb.Append(it.GetType().Name + " '" + it.name + "': " + (ok ? "OK (E key reaches Interact)" : "NG (Intaractable.Update is not running - a subclass declares Update?)") + "\n");
}
UnityEditor.EditorPrefs.DeleteKey("ct_check_interact");
return sb.Length > 0 ? sb.ToString() : "nothing in range (walk/warp into the trigger first)";
'@
    Invoke-UnityEval -Code $check
    return
}

if ($Prompt) {
    Invoke-UnityEval -Code 'var v = UnityEngine.Object.FindAnyObjectByType<InteractPromptView>(); if (v == null) return "no prompt view"; var t = v.GetComponentInChildren<TMPro.TextMeshPro>(true); return t.gameObject.activeSelf ? "showing: " + t.text + " at " + t.transform.position.ToString("F1") : "hidden";'
    return
}

Write-Output "Specify one of -State, -Advance N, -UntilIdle, -Warp x,y, -Run path, -Flag name, -Capture name, -SaveOn, -AutoSave, -Reload, -CheckInteract, -Prompt"
