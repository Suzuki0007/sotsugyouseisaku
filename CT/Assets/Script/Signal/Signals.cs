/*********************************************************************/
// * \file   Signals.cs
// * \brief  合図を送る・受ける窓口（static）と、合図に付ける情報
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 合図に付ける情報（どこで・誰が・どれくらい）
/// </summary>
public struct SignalContext
{
    /// <summary> 合図を出した物（ペグ・敵・イベントのオブジェクトなど。ないこともある） </summary>
    public GameObject source;

    /// <summary> 起きた場所 </summary>
    public Vector2 position;

    /// <summary> 数（残りの数・ダメージ・体力など。合図ごとに意味が決まっている） </summary>
    public float value;

    public static SignalContext From(GameObject source, float value = 0.0f)
    {
        return new SignalContext
        {
            source = source,
            position = source != null ? (Vector2)source.transform.position : Vector2.zero,
            value = value,
        };
    }
}

/// <summary>
/// 合図の窓口
/// </summary>
///
/// ・送る：Signals.Send(合図のアセット, 情報) か、標準の合図は Signals.Send("ID", 情報)
/// ・受ける：Signals.Sent（ID と情報）。受ける側は自分の合図の ID と比べる（SignalListener・相棒の一言）
/// ・合図はセーブしない。シーンをまたいでも残らない（その瞬間だけ）
/// ・デバッグウィンドウ「合図」用に、最近の合図を記録する（時刻・ID・出した物・受けた数）
public static class Signals
{
    /// <summary> 合図が送られた（ID・情報） </summary>
    public static event Action<string, SignalContext> Sent;

    /// <summary> 記録の1件 </summary>
    public struct Entry
    {
        public float time;
        public string id;
        public string source;
        public int received;
    }

    private const int LogSize = 40;
    private static readonly List<Entry> log = new List<Entry>();
    private static int receivedCount;
    private static int depth;

    /// <summary> 最近の合図（新しいものが後ろ） </summary>
    public static IReadOnlyList<Entry> Log => log;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Sent = null;
        log.Clear();
        receivedCount = 0;
        depth = 0;
    }

    /// <summary> 合図を送る（アセット） </summary>
    public static void Send(GameSignal signal, SignalContext context)
    {
        if(signal == null) { return; }
        Send(signal.Id, context);
    }

    /// <summary> 合図を送る（ID。標準の合図をコードから出すとき） </summary>
    public static void Send(string id, SignalContext context)
    {
        if(string.IsNullOrEmpty(id)) { return; }

        // 合図を受けて合図を出す…が続きすぎたら止める（受けた側が同じ合図を出し直すなどの間違い）
        if(depth > 8)
        {
            Debug.LogWarning($"合図「{id}」：合図の中で合図が続きすぎたので止めました（受けた側が同じ合図を出していないか確かめてください）");
            return;
        }

        int before = receivedCount;
        depth++;
        try { Sent?.Invoke(id, context); }
        finally { depth--; }

        if(log.Count >= LogSize) { log.RemoveAt(0); }
        log.Add(new Entry { time = Time.time, id = id, source = context.source != null ? context.source.name : "-", received = receivedCount - before });
    }

    /// <summary> 受けた側が呼ぶ（記録の「受けた数」用） </summary>
    public static void MarkReceived() { receivedCount++; }

    /// <summary> ID が合図のアセットと同じか </summary>
    public static bool Matches(GameSignal signal, string id) => signal != null && signal.Id == id;
}
