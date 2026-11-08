/*********************************************************************/
// * \file   PuzzleCheck.cs
// * \brief  答え合わせ（入力の仕掛けがそろったら正解）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 答え合わせのしかた
/// </summary>
public enum PuzzleMode
{
    [InspectorName("全部 ON にする")]
    AllOn = 0,

    [InspectorName("決めた順番に ON にする")]
    Order = 1,

    [InspectorName("決めた組み合わせ（値の並び）にする")]
    Pattern = 2,
}

/// <summary>
/// いつ答え合わせをするか
/// </summary>
public enum PuzzleJudgeTiming
{
    [InspectorName("入力が変わるたびに（正解したらすぐ）")]
    Auto = 0,

    [InspectorName("イベントの部品で確かめたとき（レバーを引く・決定を押す）")]
    OnRequest = 1,
}

/// <summary>
/// 答え合わせ
/// </summary>
///
/// ・入力の仕掛け（スイッチ・燭台・ダイヤルなど）の状態を見て、正解なら自分の状態（GimmickState）を ON にする
///   → 扉などは「条件」でこの答え合わせが ON かを見る（ほかの仕掛けと同じつなぎ方）
/// ・全部 ON ／ 決めた順番に ON（間違えたら全部戻す。燭台に順番に火をつける）／ 決めた組み合わせ（ダイヤル・像の向き）
/// ・正解したら入力を固定できる（もう動かせない）。正解したことはセーブする
[RequireComponent(typeof(GimmickState))]
public class PuzzleCheck : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("答え合わせに使う仕掛け（GimmickState）。「決めた順番」なら、ON にする順番に上から並べる")]
    private List<GimmickState> inputs = new List<GimmickState>();

    [SerializeField, Label("答え合わせのしかた")]
    private PuzzleMode mode = PuzzleMode.AllOn;

    [SerializeField]
    [Tooltip("「決めた組み合わせ」の答え。入力と同じ順番に、それぞれの値を並べる（0 = OFF、1 = ON、ダイヤルなら 0, 1, 2 …）")]
    private List<int> answer = new List<int>();

    [SerializeField, Label("答え合わせをするとき")]
    private PuzzleJudgeTiming timing = PuzzleJudgeTiming.Auto;

    [Header("正解したとき")]

    [SerializeField, Label("正解したら入力を固定する")]
    [Tooltip("オン：正解したら、入力のスイッチなどをもう動かせなくする")]
    private bool lockInputs = true;

    [SerializeField, Label("正解したときのイベント")]
    [Tooltip("正解した瞬間に実行する（正解の音・会話など）。扉を開けるのは、扉の「条件」にこの答え合わせを入れる方法がおすすめ")]
    private EventSequence onSolved;

    [SerializeField, Label("正解の SE")]
    private SeData solvedSe;

    [Header("間違えたとき")]

    [SerializeField, Label("間違えたら入力を全部戻す")]
    [Tooltip("「決めた順番」で順番を間違えたとき・「確かめたとき」に違っていたとき、入力を最初の状態に戻す")]
    private bool resetOnWrong = true;

    [SerializeField, Label("間違えたときのイベント")]
    [Tooltip("ダメージ・敵が出る・会話など。空なら何もしない")]
    private EventSequence onWrong;

    [SerializeField, Label("間違えたときの SE")]
    private SeData wrongSe;

    private GimmickState state;
    private int progress;
    private bool judging;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 正解したか </summary>
    public bool Solved => state != null && state.IsOn;

    public IReadOnlyList<GimmickState> Inputs => inputs;
    public PuzzleMode Mode => mode;
    public IReadOnlyList<int> Answer => answer;

    /// <summary> デバッグ用の状態 </summary>
    public string StatusText
    {
        get
        {
            if(Solved) { return "正解済み"; }
            switch(mode)
            {
                case PuzzleMode.AllOn:
                    int on = 0;
                    foreach(GimmickState g in inputs) { if(g != null && g.IsOn) { on++; } }
                    return $"ON {on}/{inputs.Count}";
                case PuzzleMode.Order:
                    return $"順番 {progress}/{inputs.Count}";
                default:
                    var now = new List<string>();
                    foreach(GimmickState g in inputs) { now.Add(g != null ? g.Value.ToString() : "-"); }
                    return $"今 [{string.Join(",", now)}]　答え [{string.Join(",", answer)}]";
            }
        }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        state = GetComponent<GimmickState>();
    }

    private void Start()
    {
        foreach(GimmickState g in inputs) { if(g != null) { g.Changed += OnInputChanged; } }

        if(Solved)
        {
            // セーブから戻った：入力を固定し直す
            if(lockInputs) { foreach(GimmickState g in inputs) { if(g != null) { g.SetLocked(true); } } }
            if(onSolved != null) { onSolved.Restore(gameObject); }
            return;
        }

        // 順番の謎は、途中まで ON にしたまま戻ってくると順番が分からないので、最初からにする
        if(mode == PuzzleMode.Order) { ResetInputs(); }
    }

    private void OnDestroy()
    {
        foreach(GimmickState g in inputs) { if(g != null) { g.Changed -= OnInputChanged; } }
    }

    //=====================================================================
    // 答え合わせ
    //=====================================================================

    private void OnInputChanged(GimmickState input, int value)
    {
        if(Solved || judging) { return; }

        if(mode == PuzzleMode.Order)
        {
            if(value == 0) { return; }    // OFF にしたのは数えない

            int expected = progress < inputs.Count ? progress : -1;
            if(expected >= 0 && inputs[expected] == input)
            {
                progress++;
                if(progress >= inputs.Count) { Solve(); }
            }
            else
            {
                Wrong();
            }
            return;
        }

        if(timing == PuzzleJudgeTiming.Auto && IsCorrect()) { Solve(); }
    }

    /// <summary>
    /// 答え合わせをする（イベントの部品「仕掛けを操作 > 答え合わせをする」から）
    /// </summary>
    /// <returns> 正解か </returns>
    public bool Judge()
    {
        if(Solved) { return true; }

        if(IsCorrect())
        {
            Solve();
            return true;
        }
        Wrong();
        return false;
    }

    /// <summary> 今の入力が正解か </summary>
    public bool IsCorrect()
    {
        if(inputs.Count == 0) { return false; }

        switch(mode)
        {
            case PuzzleMode.AllOn:
                foreach(GimmickState g in inputs) { if(g == null || !g.IsOn) { return false; } }
                return true;

            case PuzzleMode.Order:
                return progress >= inputs.Count;

            default:
                if(answer.Count < inputs.Count) { return false; }
                for(int i = 0; i < inputs.Count; i++)
                {
                    if(inputs[i] == null || inputs[i].Value != answer[i]) { return false; }
                }
                return true;
        }
    }

    private void Solve()
    {
        state.Set(1, true);
        state.SetLocked(true);
        if(lockInputs) { foreach(GimmickState g in inputs) { if(g != null) { g.SetLocked(true); } } }

        SeManager.PlaySe(solvedSe, transform.position);
        if(onSolved != null) { onSolved.Play(gameObject); }
    }

    private void Wrong()
    {
        SeManager.PlaySe(wrongSe, transform.position);
        progress = 0;
        if(resetOnWrong) { ResetInputs(); }
        if(onWrong != null) { onWrong.Play(gameObject); }
    }

    /// <summary>
    /// 入力を最初の状態に戻す（順番も最初から）
    /// </summary>
    public void ResetInputs()
    {
        judging = true;
        foreach(GimmickState g in inputs) { if(g != null) { g.ResetValue(true); } }
        judging = false;
        progress = 0;
    }
}
