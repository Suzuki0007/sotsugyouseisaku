/*********************************************************************/
// * \file   Bark.cs
// * \brief  相棒の一言を選んで出す（きっかけの受け口）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一言のきっかけの情報
/// </summary>
public struct BarkContext
{
    /// <summary> きっかけになったキャラ（ダメージを受けた・調べたキャラ） </summary>
    public GameObject subject;

    /// <summary> きっかけになった物（仕掛けなど） </summary>
    public Transform source;

    /// <summary> 倒した敵のデータ </summary>
    public EnemyData enemy;

    /// <summary> 前の値と今の値（待った秒数・止まっていた秒数・体力の割合） </summary>
    public float previous;
    public float value;

    public static BarkContext Of(GameObject subject, Transform source = null)
    {
        return new BarkContext { subject = subject, source = source };
    }
}

/// <summary>
/// 相棒の一言
/// </summary>
///
/// ・きっかけ（調べて断られた・ダメージ・敵を倒した・待たせた など）が起きたら、各所から Fire を呼ぶ
/// ・Resources/Barks/ の BarkData から、そのきっかけで条件を満たすものを選び、吹き出しで出す（BarkView）
/// ・しゃべりすぎないように、一言どうしの間（設定）・一言ごとの待ち時間・確率・回数で絞る
/// ・出している一言より優先度が高いものだけが割り込める
public static class Bark
{
    private static Dictionary<BarkTrigger, List<BarkData>> byTrigger;
    private static BarkData[] allData;
    private static BarkSettings settings;
    private static bool settingsLoaded;

    private static readonly Dictionary<BarkData, float> lastPlayed = new Dictionary<BarkData, float>();
    private static readonly HashSet<BarkData> playedInScene = new HashSet<BarkData>();
    private static readonly List<string> log = new List<string>();
    private static float globalReadyTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        byTrigger = null;
        allData = null;
        settings = null;
        settingsLoaded = false;
        lastPlayed.Clear();
        playedInScene.Clear();
        log.Clear();
        globalReadyTime = 0.0f;
        Muted = false;
    }

    //=====================================================================
    // 設定・データ
    //=====================================================================

    /// <summary> デバッグ：一言を出さない </summary>
    public static bool Muted { get; set; }

    /// <summary> 設定（Resources/BarkSettings.asset。なければ初期値） </summary>
    public static BarkSettings Settings
    {
        get
        {
            if(!settingsLoaded)
            {
                settings = Resources.Load<BarkSettings>("BarkSettings");
                if(settings == null) { settings = ScriptableObject.CreateInstance<BarkSettings>(); }
                settingsLoaded = true;
            }
            return settings;
        }
    }

    /// <summary> Resources/Barks/ の全データ </summary>
    public static BarkData[] AllData
    {
        get { Load(); return allData; }
    }

    /// <summary> 最近出した一言（デバッグウィンドウ用。新しい順） </summary>
    public static IReadOnlyList<string> Log => log;

    /// <summary> データを読み直す（エディタでデータを足したとき） </summary>
    public static void Reload()
    {
        byTrigger = null;
        allData = null;
        Load();
    }

    private static void Load()
    {
        if(byTrigger != null) { return; }
        byTrigger = new Dictionary<BarkTrigger, List<BarkData>>();
        allData = Resources.LoadAll<BarkData>("Barks");
        foreach(BarkData d in allData)
        {
            if(d == null) { continue; }
            if(!byTrigger.TryGetValue(d.trigger, out List<BarkData> list)) { list = new List<BarkData>(); byTrigger[d.trigger] = list; }
            list.Add(d);
        }
    }

    //=====================================================================
    // きっかけ
    //=====================================================================

    /// <summary>
    /// きっかけが起きた（そのきっかけの一言から1つ選んで出す）
    /// </summary>
    public static bool Fire(BarkTrigger trigger, BarkContext context)
    {
        if(trigger == BarkTrigger.Manual || !CanSpeak()) { return false; }
        Load();
        if(!byTrigger.TryGetValue(trigger, out List<BarkData> list)) { return false; }

        // 自動の一言は、一言どうしの間をあける（優先度で割り込むときは別）
        BarkView view = BarkView.Find();
        int currentPriority = view != null && view.Busy ? view.CurrentPriority : int.MinValue;
        if(currentPriority == int.MinValue && Time.time < globalReadyTime) { return false; }

        int best = int.MinValue;
        var picks = new List<BarkData>();
        foreach(BarkData d in list)
        {
            if(d == null || !MatchesTrigger(d, context) || !CanPlay(d, true)) { continue; }
            if(d.priority <= currentPriority) { continue; }
            if(d.priority > best) { best = d.priority; picks.Clear(); }
            if(d.priority == best) { picks.Add(d); }
        }
        if(picks.Count == 0) { return false; }

        // 確率は選ぶ前にそれぞれで見る（外れたものは今回は出ない）
        for(int i = picks.Count - 1; i >= 0; i--) { if(Random.value > picks[i].chance) { picks.RemoveAt(i); } }
        if(picks.Count == 0) { return false; }

        BarkData chosen = picks[Random.Range(0, picks.Count)];
        return Start(chosen, context, false);
    }

    /// <summary>
    /// イベント・範囲から一言を出す
    /// </summary>
    /// <param name="checkRules"> 条件・回数・待ち時間を見るか（見ないなら必ず出す） </param>
    /// <returns> 出したか </returns>
    public static bool Play(BarkData data, BarkContext context, bool checkRules)
    {
        if(data == null || !CanSpeak()) { return false; }
        if(checkRules && !CanPlay(data, false)) { return false; }
        if(checkRules && Random.value > data.chance) { return false; }

        // 範囲など（条件を見る方）は、出している一言の方が優先度が高ければ割り込まない
        BarkView view = BarkView.Find();
        if(checkRules && view != null && view.Busy && view.CurrentPriority > data.priority) { return false; }
        return Start(data, context, true);
    }

    /// <summary>
    /// その場で決めた行を出す（イベントの部品で、データを作らずに書くとき）
    /// </summary>
    public static bool PlayLines(List<BarkLine> lines, BarkContext context, int priority)
    {
        if(lines == null || lines.Count == 0 || !CanSpeak()) { return false; }
        BarkView.Get().Play(null, lines, context, priority, true);
        AddLog("（イベント）" + Preview(lines));
        return true;
    }

    /// <summary> 一言を出せる状態か（設定・デバッグ） </summary>
    public static bool CanSpeak()
    {
        BarkSettings s = Settings;
        // 相棒（シグル）がまだ仲間になっていない間は、相棒の一言は出さない
        return s.enabled && !Muted && !PlayerSwitchManager.IsPartnerAbsent;
    }

    private static bool Start(BarkData data, BarkContext context, bool fromEvent)
    {
        BarkVariant variant = data.PickVariant();
        if(variant == null) { return false; }

        lastPlayed[data] = Time.time;
        playedInScene.Add(data);
        if(data.repeat == BarkRepeat.OnceEver) { Save.SetPending(data.SaveKey, true); }

        BarkView.Get().Play(data, variant.lines, context, data.priority, fromEvent);
        AddLog(data.name + "：" + Preview(variant.lines));
        return true;
    }

    /// <summary> シーンを読み込んだとき（BarkWatcher から。「シーンに入るたびに1回」を数え直す） </summary>
    internal static void NotifySceneLoaded()
    {
        playedInScene.Clear();
    }

    /// <summary> 一言が終わったとき（BarkView から） </summary>
    internal static void NotifyFinished()
    {
        globalReadyTime = Time.time + Settings.globalCooldown;
    }

    //=====================================================================
    // 判定
    //=====================================================================

    /// <summary>
    /// 今そのデータを出せるか（行・回数・待ち時間・条件）
    /// </summary>
    public static bool CanPlay(BarkData data, bool useCooldown)
    {
        if(data == null || !data.active || !data.HasLines) { return false; }

        switch(data.repeat)
        {
            case BarkRepeat.OncePerScene:
                if(playedInScene.Contains(data)) { return false; }
                break;
            case BarkRepeat.OnceEver:
                if(Save.Get(data.SaveKey, false)) { return false; }
                break;
        }

        if(useCooldown && lastPlayed.TryGetValue(data, out float t) && Time.time < t + data.cooldown) { return false; }
        if(data.condition != null && !data.condition.IsMet()) { return false; }
        return true;
    }

    /// <summary> 理由を返す（一言の一覧・デバッグ用） </summary>
    public static string WhyNot(BarkData data)
    {
        if(data == null) { return "データがない"; }
        if(!data.active) { return "使わない（オフ）"; }
        if(!data.HasLines) { return "文がない"; }
        if(data.repeat == BarkRepeat.OncePerScene && playedInScene.Contains(data)) { return "このシーンでは出した"; }
        if(data.repeat == BarkRepeat.OnceEver && Save.Get(data.SaveKey, false)) { return "もう出した（セーブ済み）"; }
        if(lastPlayed.TryGetValue(data, out float t) && Time.time < t + data.cooldown) { return "待ち時間 あと " + (t + data.cooldown - Time.time).ToString("0") + " 秒"; }
        if(data.condition != null && !data.condition.IsMet()) { return "条件を満たしていない"; }
        return "";
    }

    /// <summary> 出した記録を消す（デバッグ。待ち時間・シーンで1回・ゲームで1回） </summary>
    public static void ResetHistory()
    {
        lastPlayed.Clear();
        playedInScene.Clear();
        globalReadyTime = 0.0f;
        foreach(BarkData d in AllData) { if(d != null && d.repeat == BarkRepeat.OnceEver) { Save.SetPending(d.SaveKey, false); } }
    }

    private static bool MatchesTrigger(BarkData d, BarkContext c)
    {
        switch(d.trigger)
        {
            case BarkTrigger.LongWait:
            case BarkTrigger.Idle:
                // 秒数をまたいだときだけ
                return c.previous < d.seconds && c.value >= d.seconds;
            case BarkTrigger.LowHealth:
                // 割合を下回ったときだけ
                return c.previous > d.healthRatio && c.value <= d.healthRatio;
            case BarkTrigger.EnemyDefeated:
                return d.enemy == null || d.enemy == c.enemy;
        }
        return true;
    }

    //=====================================================================
    // 表示用
    //=====================================================================

    /// <summary> 話すキャラを決める </summary>
    public static GameObject ResolveSpeaker(BarkSpeaker speaker, BarkContext c)
    {
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        if(m == null) { return null; }
        switch(speaker)
        {
            case BarkSpeaker.Partner: return m.Partner;
            case BarkSpeaker.Current: return m.CurrentPlayer;
            case BarkSpeaker.Noah:    return m.Noah;
            case BarkSpeaker.Sigle:   return m.Sigle;
            case BarkSpeaker.Subject: return c.subject != null ? c.subject : m.CurrentPlayer;
            case BarkSpeaker.SubjectPartner:
                GameObject s = c.subject != null ? c.subject : m.CurrentPlayer;
                return s == m.Noah ? m.Sigle : (s == m.Sigle ? m.Noah : m.Partner);
        }
        return null;
    }

    /// <summary> 行の文字数から出している秒数を決める </summary>
    public static float SecondsOf(BarkLine line)
    {
        if(line.seconds > 0.0f) { return line.seconds; }
        BarkSettings s = Settings;
        int length = string.IsNullOrEmpty(line.text) ? 0 : line.text.Length;
        return Mathf.Clamp(length * s.secondsPerChar, s.minSeconds, s.maxSeconds);
    }

    private static string Preview(List<BarkLine> lines)
    {
        var parts = new List<string>();
        foreach(BarkLine l in lines) { if(l != null) { parts.Add(l.text); } }
        return string.Join(" / ", parts);
    }

    private static void AddLog(string text)
    {
        log.Insert(0, Time.time.ToString("0.0") + " 秒  " + text);
        if(log.Count > 20) { log.RemoveAt(log.Count - 1); }
    }
}
