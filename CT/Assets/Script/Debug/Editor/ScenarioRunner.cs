/*********************************************************************/
// * \file   ScenarioRunner.cs
// * \brief  テストシナリオ（Tests/Scenarios/*.txt）をエディタの中でまとめて進める（Claude の確認の道具）
// *
// * \author uh
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// テストシナリオの実行役（エディタ専用。プランナーは使わない）
/// </summary>
///
/// ・scenario.ps1 が、シナリオの C# の式をまとめて 1 回の eval で渡し、Begin を呼ぶ
/// ・1 行ずつ EditorApplication.update で進め、結果を 1 行ずつファイルに書く（PowerShell はそれを読むだけ）
/// ・前は 1 つの確認ごとに unity.exe を起動していた（1 回 約 1 秒）。2026-11-28 にまとめて動かす形にした
/// ・エラーは Application.logMessageReceivedThreaded で受ける（コンソールの表示の絞り込みに左右されない）
public static class ScenarioRunner
{
    //===================================================================
    // 外から渡す 1 行
    //===================================================================

    /// <summary> シナリオの 1 行（C# の式は scenario.ps1 がラムダにして渡す） </summary>
    public class Step
    {
        public int line;
        public string text;
        public string cmd;
        public string arg;
        public Func<bool> cond;
        public Func<object> eval;

        public Step(int line, string text, string cmd, string arg, Func<bool> cond = null, Func<object> eval = null)
        {
            this.line = line; this.text = text; this.cmd = cmd; this.arg = arg; this.cond = cond; this.eval = eval;
        }
    }

    //===================================================================
    // 状態
    //===================================================================
    private static List<Step> steps;
    private static string outPath;
    private static string scenarioName;
    private static bool stopOnFail;
    private static bool running;
    private static IEnumerator<double> routine;
    private static double resumeAt;
    private static int passCount, failCount, errorCount;
    private static readonly List<string> errorSamples = new List<string>();
    private static string origInput;
    private static readonly object logLock = new object();

    private const string PlayerReady = "ready";

    /// <summary> 動いている間か </summary>
    public static bool IsRunning => running;

    // 再生を始めたときに前の残りを消す（Begin は再生が始まってから呼ばれるので、動いている間は触らない）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        if(running) { return; }
        steps = null; routine = null; outPath = null; scenarioName = null; origInput = null;
        stopOnFail = false; updateBaselines = false; resumeAt = 0;
        passCount = 0; failCount = 0; errorCount = 0;
    }

    //===================================================================
    // 始める・終える
    //===================================================================

    /// <summary> シナリオを始める（すぐ戻る。結果は outPath に 1 行ずつ） </summary>
    public static string Begin(string name, Step[] list, string resultPath, bool stopAtFirstFail, bool invincible, bool saveBaselines = false)
    {
        if(running) { return "already running"; }
        if(!EditorApplication.isPlaying) { return "not playing"; }
        updateBaselines = saveBaselines;
        steps = new List<Step>(list);
        outPath = resultPath;
        scenarioName = name;
        stopOnFail = stopAtFirstFail;
        passCount = 0; failCount = 0; errorCount = 0;
        errorSamples.Clear();
        File.WriteAllText(outPath, "", new UTF8Encoding(false));
        Health.PlayerInvincible = invincible;

        // 本当のキー入力をエディタが裏にあっても届ける（終わったら元に戻す）
        var s = InputSystem.settings;
        origInput = s.editorInputBehaviorInPlayMode + "|" + s.backgroundBehavior;
        s.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        s.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        if(Keyboard.current != null) { InputSystem.EnableDevice(Keyboard.current); }

        Application.logMessageReceivedThreaded += OnLog;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
        routine = Main();
        resumeAt = 0;
        running = true;
        return "started";
    }

    private static void Finish(string reason)
    {
        if(!running) { return; }
        running = false;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        Application.logMessageReceivedThreaded -= OnLog;
        var s = InputSystem.settings;
        s.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus;
        s.backgroundBehavior = InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
        Write("DONE\t" + passCount + "\t" + failCount + "\t" + reason);
    }

    private static void OnPlayMode(PlayModeStateChange c)
    {
        if(c == PlayModeStateChange.ExitingPlayMode) { Finish("play mode stopped"); }
    }

    private static void OnLog(string condition, string stack, LogType type)
    {
        if(type != LogType.Error && type != LogType.Exception && type != LogType.Assert) { return; }
        if(condition.Contains("/api/exec") || condition.Contains("Pipeline") || condition.Contains("Account API")) { return; }
        lock(logLock)
        {
            errorCount++;
            if(errorSamples.Count < 3) { errorSamples.Add(condition.Split('\n')[0]); }
        }
    }

    private static void Tick()
    {
        if(!running) { return; }
        if(EditorApplication.timeSinceStartup < resumeAt) { return; }
        bool more;
        try { more = routine.MoveNext(); }
        catch(Exception e) { Write("FAIL\t0\t(runner)\t" + e.GetType().Name + ": " + e.Message); failCount++; Finish("exception"); return; }
        if(!more) { Finish("end"); return; }
        resumeAt = EditorApplication.timeSinceStartup + Math.Max(0, routine.Current);
    }

    //===================================================================
    // 書き出し
    //===================================================================
    private static void Write(string s)
    {
        File.AppendAllText(outPath, s.Replace("\r", "").Replace("\n", " / ") + "\n", new UTF8Encoding(false));
    }

    private static void Info(Step st, string s) { Write("INFO\t" + st.line + "\t" + s); }

    private static void Record(Step st, bool ok, string detail)
    {
        if(ok) { passCount++; } else { failCount++; }
        Write((ok ? "PASS" : "FAIL") + "\t" + st.line + "\t" + st.text + "\t" + detail);
        if(!ok) { CaptureNow("fail_" + scenarioName + "_" + st.line); }
    }

    //===================================================================
    // 本体
    //===================================================================
    private static IEnumerator<double> Main()
    {
        // 始めてすぐはプレイヤーがまだいないことがある
        foreach(var w in WaitUntil(Ready, 20)) { yield return w; }
        yield return 0.8;
        foreach(var st in steps)
        {
            Write("STEP\t" + st.line + "\t" + st.text);
            IEnumerable<double> sub = null;
            try { sub = Run(st); }
            catch(Exception e) { Record(st, false, "exception: " + e.Message); }
            if(sub != null)
            {
                var it = sub.GetEnumerator();
                while(true)
                {
                    bool more;
                    try { more = it.MoveNext(); }
                    catch(Exception e) { Record(st, false, "exception: " + e.GetType().Name + ": " + e.Message); break; }
                    if(!more) { break; }
                    yield return it.Current;
                }
            }
            if(stopOnFail && failCount > 0) { yield break; }
        }
    }

    // 1 行を実行する（待つ物は yield で秒を返す）
    private static IEnumerable<double> Run(Step st)
    {
        string a = st.arg ?? "";
        switch(st.cmd)
        {
            case "scene":
                SceneManager.LoadScene(a);
                yield return 0.8;
                foreach(var w in WaitUntil(() => SceneManager.GetActiveScene().name == a && Ready(), 30)) { yield return w; }
                yield return 0.8;
                break;
            case "invincible":
                Health.PlayerInvincible = a != "off";
                break;
            case "save":
                if(!Save.HasSlot) { Save.StartNewGame(); }
                Info(st, "save session on (slot " + Save.CurrentSlot + ")");
                break;
            case "flag":
            {
                var p = Split(a);
                var f = AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/" + p[0] + ".asset");
                if(f == null) { Info(st, "no flag " + p[0]); break; }
                GameFlags.Set(f, !(p.Length > 1 && p[1] == "off"));
                Info(st, p[0] + " = " + GameFlags.Get(f));
                break;
            }
            case "advance":
                foreach(var w in Advance(st, a)) { yield return w; }
                break;
            case "wait":
                foreach(var w in WaitCmd(st, a)) { yield return w; }
                break;
            case "warp":
            {
                var xy = a.Split(',');
                var pos = new Vector3(F(xy[0]), F(xy[1]), 0f);
                PlayerSwitchManager.Instance.Warp(pos, pos);
                yield return 0.3;
                break;
            }
            case "run":
                Info(st, RunObject(a));
                yield return 0.3;
                break;
            case "key":
            {
                var p = Split(a);
                int ms = p.Length > 1 ? int.Parse(p[1]) : 150;
                var key = (Key)Enum.Parse(typeof(Key), p[0]);
                var kb = Keyboard.current;
                if(kb == null) { Info(st, "no keyboard"); break; }
                InputSystem.QueueStateEvent(kb, new KeyboardState(key));
                yield return ms / 1000.0;
                if(Keyboard.current != null) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); }
                yield return 0.3;
                break;
            }
            case "button":
            {
                string r = "not found";
                foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>())
                {
                    if(b.gameObject.name == a && b.isActiveAndEnabled && b.interactable) { b.onClick.Invoke(); r = "clicked"; break; }
                }
                Info(st, r);
                yield return 0.5;
                break;
            }
            case "tighten":
            {
                var m = typeof(TimpaniPeg).GetMethod("Tighten", BindingFlags.NonPublic | BindingFlags.Instance);
                int n = 0;
                foreach(var peg in UnityEngine.Object.FindObjectsByType<TimpaniPeg>()) { if(!peg.IsTightened) { m.Invoke(peg, null); n++; } }
                Info(st, "tightened " + n);
                yield return 1.5;
                break;
            }
            case "kill":
            {
                Health.PlayerInvincible = false;
                var m = PlayerSwitchManager.Instance; int n = 0;
                foreach(var go in new[] { m.Noah, m.Sigle })
                {
                    if(go == null) { continue; }
                    var h = go.GetComponent<Health>();
                    if(h != null && !h.IsDead) { h.Damage(999); n++; }
                }
                Info(st, "damaged " + n);
                break;
            }
            case "autosave":
            {
                if(!Save.HasSlot) { Info(st, "no save session (save on first)"); break; }
                var p = PlayerSwitchManager.Instance.CurrentPlayer.transform.position;
                GameSession.AutoSaveAt(SceneManager.GetActiveScene().name, p);
                Info(st, "autosaved at " + p.ToString("F1"));
                break;
            }
            case "reload":
                if(!Save.HasSlot) { Info(st, "no save session (save on first)"); break; }
                GameSession.ReturnToTitle();
                yield return 0.5;
                foreach(var w in WaitUntil(() => !SceneFader.IsBusy && SceneManager.GetActiveScene().name == "Title", 10)) { yield return w; }
                GameSession.Continue(Save.AutoSlot, "Stage1_1");
                yield return 0.5;
                foreach(var w in WaitUntil(() => !SceneFader.IsBusy && PlayerSwitchManager.Instance != null && PlayerSwitchManager.Instance.CurrentPlayer != null, 15)) { yield return w; }
                yield return 0.8;
                Info(st, "after reload: " + SceneManager.GetActiveScene().name);
                break;
            case "title":
                GameSession.ReturnToTitle();
                yield return 0.5;
                foreach(var w in WaitUntil(() => SceneManager.GetActiveScene().name == "Title" && !SceneFader.IsBusy, 20)) { yield return w; }
                yield return 0.8;
                break;
            case "capture":
                Info(st, CaptureNow(a));
                yield return 0.2;
                break;
            case "eval":
            {
                object r = st.eval != null ? st.eval() : null;
                Info(st, r == null ? "(null)" : r.ToString());
                break;
            }
            case "expect":
                foreach(var w in Expect(st, a)) { yield return w; }
                break;
            default:
                Info(st, "[WARN] unknown command '" + st.cmd + "' (skipped)");
                break;
        }
    }

    //===================================================================
    // advance・wait
    //===================================================================
    private static IEnumerable<double> Advance(Step st, string a)
    {
        if(a == "battle")
        {
            int n = 0; bool got = false;
            double end = Now + 40;
            while(Now < end)
            {
                if(BattleArea.AnyActive) { got = true; break; }
                // 戦闘が始まった瞬間に止まりたいので、ここは待たずに送る（前の実行役と同じ間隔 0.2 秒）
                if(AdvanceOnce()) { n++; yield return 0.2; } else { yield return 0.2; }
            }
            Info(st, "advanced " + n + " line(s), battle " + (got ? "started" : "NOT started"));
            yield break;
        }
        if(a == "all" || a == "")
        {
            int count = 0, idle = 0;
            for(int i = 0; i < 200; i++)
            {
                bool sent = false;
                foreach(var w in AdvanceAndSettle(r => sent = r)) { yield return w; }
                if(sent) { count++; idle = 0; continue; }
                if(!EventSequence.IsBusy) { idle++; if(idle >= 2) { break; } }
                yield return 0.3;
            }
            Info(st, "advanced " + count + " line(s)");
            yield break;
        }
        int want = int.Parse(a), done = 0;
        for(int i = 0; i < want; i++)
        {
            // イベントが動いていて会話がまだ出ていなければ、出るまで待つ（最大 5 秒。11-28：full_run の advance 4 が
            // 振動の演出の途中で呼ばれて 0 文で終わっていた。前の実行役は遅かったので会話が出てから送れていた）
            if(DialogueManager.Instance == null || !DialogueManager.Instance.IsTalking)
            {
                foreach(var w in WaitUntil(() => (DialogueManager.Instance != null && DialogueManager.Instance.IsTalking) || !EventSequence.IsBusy, 5)) { yield return w; }
            }
            bool sent = false;
            foreach(var w in AdvanceAndSettle(r => sent = r)) { yield return w; }
            if(!sent) { break; }
            done++;
        }
        Info(st, "advanced " + done + " / " + want);
    }

    private static IEnumerable<double> WaitCmd(Step st, string a)
    {
        var p = a.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        string head = p.Length > 0 ? p[0] : "1";
        switch(head)
        {
            case "idle":
            {
                bool ok = false;
                foreach(var w in WaitUntil(Idle, 30, r => ok = r)) { yield return w; }
                if(!ok) { Info(st, "(still busy after 30 s)"); }
                break;
            }
            case "battle":
            {
                bool ok = false;
                foreach(var w in WaitUntil(() => BattleArea.AnyActive, 30, r => ok = r)) { yield return w; }
                if(!ok) { Info(st, "(no battle after 30 s)"); }
                break;
            }
            case "until":
            {
                var q = p.Length > 1 ? p[1].Split(new[] { ' ' }, 2) : new string[0];
                double sec = q.Length > 0 ? F(q[0]) : 10;
                bool ok = false;
                foreach(var w in WaitUntil(st.cond ?? (() => false), sec, r => ok = r)) { yield return w; }
                if(!ok) { Info(st, "(not true after " + sec + " s)"); }
                break;
            }
            case "scene":
            {
                string name = p.Length > 1 ? p[1].Trim() : "";
                bool ok = false;
                foreach(var w in WaitUntil(() => SceneManager.GetActiveScene().name == name && Ready(), 40, r => ok = r)) { yield return w; }
                if(!ok) { Info(st, "(scene not reached)"); }
                yield return 0.8;
                break;
            }
            default:
                yield return F(head);
                break;
        }
    }

    //===================================================================
    // expect
    //===================================================================
    private static IEnumerable<double> Expect(Step st, string a)
    {
        var p = a.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        string what = p.Length > 0 ? p[0] : "";
        string rest = p.Length > 1 ? p[1].Trim() : "";
        switch(what)
        {
            case "flag":
            {
                var q = Split(rest);
                var f = AssetDatabase.LoadAssetAtPath<GameFlag>("Assets/Data/Flags/" + q[0] + ".asset");
                if(f == null) { Record(st, false, "no flag"); break; }
                bool want = !(q.Length > 1 && q[1] == "off");
                bool v = GameFlags.Get(f);
                Record(st, v == want, "is " + v);
                break;
            }
            case "party":
            {
                var m = PlayerSwitchManager.Instance;
                string v = m == null ? "none" : m.Mode.ToString();
                Record(st, v == rest, "is " + v);
                break;
            }
            case "scene":
            {
                string v = SceneManager.GetActiveScene().name;
                Record(st, v == rest, "is " + v);
                break;
            }
            case "near":
            {
                var q = Split(rest);
                var xy = q[0].Split(',');
                float rad = q.Length > 1 ? F(q[1]) : 1f;
                var m = PlayerSwitchManager.Instance;
                if(m == null || m.CurrentPlayer == null) { Record(st, false, "no player"); break; }
                Vector2 pos = m.CurrentPlayer.transform.position;
                float d = Vector2.Distance(pos, new Vector2(F(xy[0]), F(xy[1])));
                Record(st, d <= rad, (d <= rad ? "ok " : "far ") + pos.ToString("F1") + " d=" + d.ToString("F1"));
                break;
            }
            case "talk":
            {
                string v = TalkLine();
                Record(st, v.Contains(rest), v);
                break;
            }
            case "idle":
                Record(st, Idle(), Idle() ? "idle" : "busy");
                break;
            case "reach":
            {
                var m = PlayerSwitchManager.Instance;
                if(m == null || m.CurrentPlayer == null) { Record(st, false, "no player"); break; }
                int ng = 0, n = 0; string first = "";
                foreach(var e in MapReach.Check(m.CurrentPlayer.transform.position))
                {
                    if(rest != "all" && !e.name.Contains(rest)) { continue; }
                    n++;
                    if(!e.reachable) { ng++; if(first == "") { first = e.name + " (" + e.reason + ")"; } }
                }
                bool ok = ng == 0 && n > 0;
                Record(st, ok, (ok ? "ok " : "ng ") + (n - ng) + "/" + n + (first != "" ? " e.g. " + first : ""));
                break;
            }
            case "no-error":
            {
                int c; string sample;
                lock(logLock) { c = errorCount; sample = string.Join(" | ", errorSamples); }
                Record(st, c == 0, c == 0 ? "ok" : c + " error(s): " + sample);
                break;
            }
            case "prompt":
            {
                var v = UnityEngine.Object.FindAnyObjectByType<InteractPromptView>();
                string s = "(no view)";
                if(v != null) { var t = v.GetComponentInChildren<TMPro.TextMeshPro>(true); s = t.gameObject.activeSelf ? t.text : "(hidden)"; }
                Record(st, s.Contains(rest), s);
                break;
            }
            case "selected":
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                var sel = es != null ? es.currentSelectedGameObject : null;
                string s = sel != null ? sel.name : "(none)";
                Record(st, s == rest, "is " + s);
                break;
            }
            case "eval":
            {
                bool v = st.cond != null && st.cond();
                Record(st, v, v ? "True" : "False");
                break;
            }
            case "looks":
            {
                // expect looks <name> [許す割合 %]：ゲームビューを撮り、Tests/Baselines/<name>.png（基準）と比べる
                var q = Split(rest);
                string name = q[0];
                float tol = q.Length > 1 ? F(q[1]) : 5f;
                string shot = CaptureNow("looks_" + name);
                double end = Now + 5;
                long last = -1;
                // 書き終わるまで待つ（大きさが 2 回続けて同じ）
                while(Now < end)
                {
                    yield return 0.2;
                    if(!File.Exists(shot)) { continue; }
                    long len = new FileInfo(shot).Length;
                    if(len > 0 && len == last) { break; }
                    last = len;
                }
                Record(st, LooksCompare(name, shot, tol, out string detail), detail);
                break;
            }
            case "within":
            {
                var q = rest.Split(new[] { ' ' }, 2);
                double sec = F(q[0]);
                double t0 = Now;
                bool ok = false;
                foreach(var w in WaitUntil(st.cond ?? (() => false), sec, r => ok = r)) { yield return w; }
                Record(st, ok, ok ? "true after " + (Now - t0).ToString("F1") + " s" : "not true within " + sec + " s");
                break;
            }
            default:
                Record(st, false, "unknown expect");
                break;
        }
    }

    //===================================================================
    // 小道具
    //===================================================================
    private static double Now => EditorApplication.timeSinceStartup;

    private static float F(string s) { return float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture); }

    private static string[] Split(string s) { return s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); }

    private static bool Ready()
    {
        return SceneManager.GetActiveScene().isLoaded && !SceneFader.IsBusy;
    }

    private static bool Idle()
    {
        return (DialogueManager.Instance == null || !DialogueManager.Instance.IsTalking) && !EventSequence.IsBusy && !SceneFader.IsBusy;
    }

    // 条件が真になるまで待つ（0.1 秒ごと）
    private static IEnumerable<double> WaitUntil(Func<bool> c, double seconds, Action<bool> done = null)
    {
        double end = Now + seconds;
        while(true)
        {
            bool v = false;
            try { v = c(); } catch { v = false; }
            if(v) { done?.Invoke(true); yield break; }
            if(Now >= end) { done?.Invoke(false); yield break; }
            yield return 0.1;
        }
    }

    private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

    private static bool AdvanceOnce()
    {
        var dm = DialogueManager.Instance;
        if(dm == null || !dm.IsTalking) { return false; }
        typeof(DialogueManager).GetMethod("CompleteLine", NonPublic).Invoke(dm, null);
        typeof(DialogueManager).GetMethod("Next", NonPublic).Invoke(dm, null);
        return true;
    }

    // 今の文の位置（会話・何文目）。送ったあと本当に次の文へ変わったかを見るのに使う
    private static string LineKey()
    {
        var dm = DialogueManager.Instance;
        if(dm == null || !dm.IsTalking) { return "-"; }
        var arr = typeof(DialogueManager).GetField("lines", NonPublic).GetValue(dm);
        int idx = (int)typeof(DialogueManager).GetField("index", NonPublic).GetValue(dm);
        return (arr != null ? arr.GetHashCode() : 0) + ":" + idx;
    }

    // 1 文送り、次の文に変わる（または会話が終わる）まで待つ。
    // 続けてすぐ送ると、切り替わりの途中で送った分が飛ばされる（11-28：full_run の advance 4 がずれた）
    private static IEnumerable<double> AdvanceAndSettle(Action<bool> done)
    {
        string before = LineKey();
        if(!AdvanceOnce()) { done(false); yield break; }
        foreach(var w in WaitUntil(() => LineKey() != before, 1.5)) { yield return w; }
        yield return 0.15;
        done(true);
    }

    private static string TalkLine()
    {
        var dm = DialogueManager.Instance;
        if(dm == null || !dm.IsTalking) { return "(not talking)"; }
        var arr = (DialogueData.Line[])typeof(DialogueManager).GetField("lines", NonPublic).GetValue(dm);
        int idx = (int)typeof(DialogueManager).GetField("index", NonPublic).GetValue(dm);
        return arr[idx].DisplayName + ": " + arr[idx].text;
    }

    private static string RunObject(string path)
    {
        var go = GameObject.Find(path);
        if(go == null) { return "not found (inactive or wrong path): " + path; }
        var m = PlayerSwitchManager.Instance;
        var who = m != null ? m.CurrentPlayer : null;
        var ii = go.GetComponent<ItemInteraction>(); if(ii != null) { ii.Run(who); return "ItemInteraction.Run"; }
        var seq = go.GetComponent<EventSequence>(); if(seq != null) { seq.Play(who); return "EventSequence.Play"; }
        var any = go.GetComponent<Intaractable>();
        if(any != null) { typeof(Intaractable).GetMethod("Interact", NonPublic).Invoke(any, null); return any.GetType().Name + ".Interact"; }
        return "no ItemInteraction / EventSequence / Intaractable on " + go.name;
    }

    //===================================================================
    // 画像の比較（expect looks）
    //===================================================================
    private const int LookW = 192, LookH = 108;      // 比べる升目（画面の大きさが違っても同じ升目に縮める）
    private const float CellDiff = 0.12f;            // 1 マスの色の差がこれより大きいと「違うマス」
    private static bool updateBaselines;

    /// <summary> 基準の画像の置き場所（プロジェクトの Tests/Baselines） </summary>
    public static string BaselineDir => Path.Combine(Directory.GetCurrentDirectory(), "Tests", "Baselines");

    private static bool LooksCompare(string name, string shot, float tolPercent, out string detail)
    {
        if(!File.Exists(shot)) { detail = "no screenshot"; return false; }
        var cur = Shrink(File.ReadAllBytes(shot));
        Directory.CreateDirectory(BaselineDir);
        string basePath = Path.Combine(BaselineDir, name + ".png");
        if(updateBaselines || !File.Exists(basePath))
        {
            if(!updateBaselines) { UnityEngine.Object.DestroyImmediate(cur); detail = "no baseline: run with -UpdateBaselines (" + basePath + ")"; return false; }
            File.WriteAllBytes(basePath, cur.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(cur);
            detail = "baseline saved";
            return true;
        }
        var bas = new Texture2D(2, 2);
        bas.LoadImage(File.ReadAllBytes(basePath));
        if(bas.width != LookW || bas.height != LookH) { var b2 = Shrink(File.ReadAllBytes(basePath)); UnityEngine.Object.DestroyImmediate(bas); bas = b2; }
        var a = cur.GetPixels(); var b = bas.GetPixels();
        var diff = new Texture2D(LookW, LookH, TextureFormat.RGBA32, false);
        var outPx = new Color[a.Length];
        int bad = 0;
        for(int i = 0; i < a.Length; i++)
        {
            float d = (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b)) / 3f;
            bool off = d > CellDiff;
            if(off) { bad++; }
            // 違うマスを赤く、ほかは今の画像を暗く
            outPx[i] = off ? new Color(1f, 0.1f, 0.1f, 1f) : new Color(a[i].r * 0.45f, a[i].g * 0.45f, a[i].b * 0.45f, 1f);
        }
        float pct = 100f * bad / a.Length;
        bool ok = pct <= tolPercent;
        string diffPath = Path.Combine(Path.GetDirectoryName(shot), "looks_" + name + "_diff.png");
        diff.SetPixels(outPx); diff.Apply();
        File.WriteAllBytes(diffPath, diff.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(diff); UnityEngine.Object.DestroyImmediate(cur); UnityEngine.Object.DestroyImmediate(bas);
        detail = pct.ToString("F1") + "% of the screen differs (allowed " + tolPercent + "%)" + (ok ? "" : ", see " + diffPath);
        return ok;
    }

    // 画像を比べる升目の大きさに縮める（マスの中を何点か拾って平均）
    private static Texture2D Shrink(byte[] png)
    {
        var src = new Texture2D(2, 2);
        src.LoadImage(png);
        var dst = new Texture2D(LookW, LookH, TextureFormat.RGBA32, false);
        var px = new Color[LookW * LookH];
        for(int y = 0; y < LookH; y++)
        {
            for(int x = 0; x < LookW; x++)
            {
                Color c = Color.clear;
                for(int sy = 0; sy < 3; sy++)
                {
                    for(int sx = 0; sx < 3; sx++)
                    {
                        c += src.GetPixelBilinear((x + (sx + 0.5f) / 3f) / LookW, (y + (sy + 0.5f) / 3f) / LookH);
                    }
                }
                px[y * LookW + x] = c / 9f;
            }
        }
        dst.SetPixels(px); dst.Apply();
        UnityEngine.Object.DestroyImmediate(src);
        return dst;
    }

    // ゲームビューを撮る（次のフレームの終わりに書かれる）
    private static string CaptureNow(string name)
    {
        string dir = Path.Combine(Path.GetTempPath(), "ct_playtest_capture");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, name + ".png");
        if(File.Exists(file)) { File.Delete(file); }
        ScreenCapture.CaptureScreenshot(file);
        return file;
    }
}
