/*********************************************************************/
// * \file   SignalListener.cs
// * \brief  「合図を受けたら」：合図ごとに、イベントを実行・フラグ・相棒の一言
// *
// * \author
/*********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 合図を受けたら（コードを書かずに、出来事に反応する）
/// </summary>
///
/// ・「反応」を並べる：どの合図で・どこから来たときだけ・条件・何回まで・間隔・遅らせる → イベント／フラグ／一言
/// ・このオブジェクトが表示されている間だけ受ける（部屋ごと非表示にすれば止まる）
/// ・反応した回数はセーブしない（シーンに来るたびに数え直す）。1回だけにしたいなら、反応でフラグを立てて条件に入れる
/// ・Hierarchy 右クリック > 合図 > 合図を受けたら で作れる
[AddComponentMenu("合図/合図を受けたら")]
public class SignalListener : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum SourceFilter
    {
        [InspectorName("どこから来ても")]
        Any = 0,

        [InspectorName("このオブジェクトか、その子から来たときだけ")]
        ThisAndChildren = 1,

        [InspectorName("決めた物（とその子）から来たときだけ")]
        Specific = 2,
    }

    [Serializable]
    public class Reaction
    {
        [Label("メモ")]
        [Tooltip("見分けるための名前（動作には影響しない）")]
        public string label = "";

        [Label("合図")]
        [Tooltip("この合図が来たら反応する")]
        public GameSignal signal;

        [Label("送り元")]
        [Tooltip("どこから来た合図に反応するか（この部屋のペグが締まったときだけ、など）")]
        public SourceFilter from = SourceFilter.Any;

        [Label("送り元の物")]
        [Tooltip("送り元が「決めた物」のとき。その物か、その子から来た合図だけに反応する")]
        public Transform sourceObject;

        [Tooltip("この条件を満たしているときだけ反応する（空ならいつも）")]
        public GameCondition condition = new GameCondition();

        [Label("何回まで（0＝何度でも）", 0)]
        [Tooltip("シーンに来てから反応する回数の上限")]
        public int maxCount = 0;

        [Label("次に反応できるまで（秒）", 0.0f)]
        [Tooltip("一度反応したら、この時間は反応しない（同じ合図が続けて来るとき）")]
        public float cooldown = 0.0f;

        [Label("遅らせる（秒）", 0.0f)]
        [Tooltip("合図が来てから、この時間たってから反応する")]
        public float delay = 0.0f;

        [Label("実行するイベント")]
        [Tooltip("反応したときに実行する EventSequence（空なら実行しない）")]
        public EventSequence sequence;

        [Label("立てるフラグ")]
        [Tooltip("反応したときに設定するフラグ（空なら何もしない）")]
        public GameFlag flag;

        [Label("フラグを ON にする")]
        [Tooltip("オン：フラグを ON に ／ オフ：OFF に")]
        public bool flagValue = true;

        [Label("相棒の一言")]
        [Tooltip("反応したときに出す一言（BarkData。条件・回数・確率は一言のデータの設定も見る）")]
        public BarkData bark;

        [NonSerialized] public int count;
        [NonSerialized] public float readyTime;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("合図ごとの反応。上から順に見て、合う物は全部反応する")]
    private List<Reaction> reactions = new List<Reaction>();

    public IReadOnlyList<Reaction> Reactions => reactions;

    //=====================================================================
    // 受ける
    //=====================================================================

    private void OnEnable()
    {
        Signals.Sent += OnSignal;
    }

    private void OnDisable()
    {
        Signals.Sent -= OnSignal;
    }

    private void OnSignal(string id, SignalContext context)
    {
        foreach(Reaction r in reactions)
        {
            if(r == null || !Signals.Matches(r.signal, id)) { continue; }
            if(!FromMatches(r, context)) { continue; }
            if(r.maxCount > 0 && r.count >= r.maxCount) { continue; }
            if(Time.time < r.readyTime) { continue; }
            if(r.condition != null && !r.condition.IsMet()) { continue; }

            r.count++;
            r.readyTime = Time.time + r.cooldown;
            Signals.MarkReceived();

            if(r.delay > 0.0f) { StartCoroutine(Delayed(r, context)); }
            else               { React(r, context); }
        }
    }

    private bool FromMatches(Reaction r, SignalContext context)
    {
        switch(r.from)
        {
            case SourceFilter.ThisAndChildren:
                return context.source != null && context.source.transform.IsChildOf(transform);
            case SourceFilter.Specific:
                return r.sourceObject != null && context.source != null && context.source.transform.IsChildOf(r.sourceObject);
            default:
                return true;
        }
    }

    private IEnumerator Delayed(Reaction r, SignalContext context)
    {
        yield return new WaitForSeconds(r.delay);
        React(r, context);
    }

    private void React(Reaction r, SignalContext context)
    {
        GameObject player = PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;

        if(r.flag != null) { GameFlags.Set(r.flag, r.flagValue); }
        if(r.sequence != null) { r.sequence.Play(player); }
        if(r.bark != null)
        {
            Transform where = context.source != null ? context.source.transform : transform;
            Bark.Play(r.bark, BarkContext.Of(player, where), true);
        }
    }
}
