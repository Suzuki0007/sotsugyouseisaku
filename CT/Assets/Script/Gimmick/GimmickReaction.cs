/*********************************************************************/
// * \file   GimmickReaction.cs
// * \brief  条件で動く物（扉・橋・消える壁など。仕掛けの「出力」）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 条件で動かす物 1 つ（扉が横にずれる・橋が伸びる・床が上がる）
/// </summary>
[Serializable]
public class ReactionMove
{
    [Label("動かす物")]
    public Transform target;

    [Label("動かす距離")]
    [Tooltip("満たしたときに、置いた位置からどれだけずらすか（マス）。満たさなくなったら戻る")]
    public Vector2 offset = new Vector2(0.0f, 2.0f);

    [Label("動く時間（秒）", 0.0f)]
    [Tooltip("0 なら一瞬で動く")]
    public float seconds = 0.6f;

    [NonSerialized] public Vector3 home;
    [NonSerialized] public bool ready;
}

/// <summary>
/// 条件で動く物
/// </summary>
///
/// ・条件（スイッチが ON・答え合わせが済んだ・フラグ・アイテムなど）を満たしたら、表示を切り替え・イベントを実行する
///   例：スイッチ A と B が ON の間だけ開く扉 ／ 燭台を全部つけたら出てくる橋 ／ 一度開いたらずっと開いている扉
/// ・表示の切り替えは、満たしているかでいつも決まる（セーブから戻ったときも、仕掛けの状態から自然に戻る）
/// ・「一度満たしたらずっと」にすると、満たしたことをセーブする（Reaction/<シーン>/<ID か階層パス>）
public class GimmickReaction : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("条件")]
    [Tooltip("「仕掛け」に、スイッチや答え合わせの GimmickState をドラッグする。ほかの条件（フラグ・アイテム・2人の状態など）も使える。すべて満たすとき")]
    private GameCondition condition = new GameCondition();

    [SerializeField, Label("一度満たしたらずっと")]
    [Tooltip("オン：一度条件を満たしたら、あとで条件を満たさなくなっても戻らない（セーブする）／オフ：満たしている間だけ")]
    private bool latch = false;

    [Header("満たしているとき")]

    [SerializeField]
    [Tooltip("満たしているときに表示する物（満たしていないときは隠す）。例：出てくる橋")]
    private List<GameObject> showWhenMet = new List<GameObject>();

    [SerializeField]
    [Tooltip("満たしているときに隠す物（満たしていないときは表示する）。例：閉じた扉")]
    private List<GameObject> hideWhenMet = new List<GameObject>();

    [SerializeField]
    [Tooltip("満たしているときに動かす物（扉が横にずれて開く・橋が伸びる）。満たさなくなったら戻る。当たり判定ごと動くので、敵の通り道も自動で変わる")]
    private List<ReactionMove> moves = new List<ReactionMove>();

    [SerializeField, Label("動き始めの SE")]
    private SeData moveSe;

    [SerializeField, Label("満たしたときのイベント")]
    [Tooltip("満たした瞬間に実行する（扉が開く音・会話など）。セーブから戻ったときは、見た目だけ実行後の状態にする")]
    private EventSequence onMet;

    [SerializeField, Label("満たさなくなったときのイベント")]
    [Tooltip("満たさなくなった瞬間に実行する（扉が閉じる音など）。「一度満たしたらずっと」のときは使わない")]
    private EventSequence onUnmet;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("「一度満たしたらずっと」のときに使う。空なら階層のパスで記録する")]
    private string saveId = "";

    private bool met;
    private bool initialized;
    private string saveKey;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 今、満たしているか </summary>
    public bool IsMet => met;

    /// <summary> 条件（配線の表示・シーンチェック用） </summary>
    public GameCondition Condition => condition;

    public IReadOnlyList<GameObject> ShowWhenMet => showWhenMet;
    public IReadOnlyList<GameObject> HideWhenMet => hideWhenMet;
    public EventSequence OnMetSequence => onMet;
    public IReadOnlyList<ReactionMove> Moves => moves;

    //=====================================================================
    // 更新
    //=====================================================================

    private void Start()
    {
        saveKey = "Reaction/" + gameObject.scene.name + "/" + (string.IsNullOrEmpty(saveId) ? ScenePath(transform) : saveId);

        // 始まったときの状態（演出なし。セーブから戻ったときは実行後の見た目にする）
        bool saved = latch && Save.Get(saveKey, false);
        met = saved || Evaluate();
        if(latch && met && !saved) { Save.SetPending(saveKey, true); }

        // 動かす物：置いた位置を覚え、今の状態の位置へ一瞬で
        foreach(ReactionMove m in moves)
        {
            if(m == null || m.target == null) { continue; }
            m.home = m.target.position;
            m.ready = true;
            SetMovePosition(m, met ? m.home + (Vector3)m.offset : m.home);
        }

        Apply();
        if(met && onMet != null) { onMet.Restore(gameObject); }
        initialized = true;
    }

    private void Update()
    {
        if(!initialized) { return; }
        UpdateMoves();
        if(latch && met) { return; }

        bool now = Evaluate();
        if(now == met) { return; }

        met = now;
        Apply();
        if(moves.Count > 0) { SeManager.PlaySe(moveSe, transform.position); }

        if(met)
        {
            if(latch) { Save.SetPending(saveKey, true); }
            if(onMet != null) { onMet.Play(gameObject); }
        }
        else
        {
            if(onUnmet != null) { onUnmet.Play(gameObject); }
        }
    }

    /// <summary>
    /// 動かす物を、今の状態の位置へ少しずつ動かす
    /// </summary>
    private void UpdateMoves()
    {
        foreach(ReactionMove m in moves)
        {
            if(m == null || m.target == null || !m.ready) { continue; }

            Vector3 goal = met ? m.home + (Vector3)m.offset : m.home;
            Vector3 now = m.target.position;
            if((now - goal).sqrMagnitude < 0.000001f) { continue; }

            float speed = m.seconds > 0.0f ? m.offset.magnitude / m.seconds : float.MaxValue;
            SetMovePosition(m, Vector3.MoveTowards(now, goal, speed * Time.deltaTime));
        }
    }

    private static void SetMovePosition(ReactionMove m, Vector3 position)
    {
        m.target.position = position;
        Rigidbody2D body = m.target.GetComponent<Rigidbody2D>();
        if(body != null) { body.position = position; }
    }

    private bool Evaluate()
    {
        // 何も設定されていない条件は「満たさない」（設定し忘れで扉が開きっぱなしにならないように）
        if(condition == null || condition.IsEmpty) { return false; }
        return condition.IsMet();
    }

    private void Apply()
    {
        foreach(GameObject go in showWhenMet) { if(go != null && go.activeSelf != met) { go.SetActive(met); } }
        foreach(GameObject go in hideWhenMet) { if(go != null && go.activeSelf == met) { go.SetActive(!met); } }
    }

    private static string ScenePath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent) { path = parent.name + "/" + path; }
        return path;
    }
}
