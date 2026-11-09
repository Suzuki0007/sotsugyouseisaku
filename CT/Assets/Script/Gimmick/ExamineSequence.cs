/*********************************************************************/
// * \file   ExamineSequence.cs
// * \brief  調べるたびに変わる（1 回目・2 回目・3 回目… で違うイベント）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 調べるたびに変わる
/// </summary>
///
/// ・調べるたびに、イベントの並びの次を実行する（Ib・魔女の家の「2 回目に調べると……」）
///   例：絵を調べる 1 回目「きれいな絵」→ 2 回目「……目が動いた？」→ 3 回目 絵から手が出てくる
/// ・最後まで行ったら：最後をくり返す／最初に戻る／もう何もしない
/// ・何回調べたかはセーブする（Examine/<シーン>/<ID か階層パス>）。仕掛けの状態にも出せる（条件で使う）
public class ExamineSequence : GimmickInteractable
{
    public enum AfterLast
    {
        [InspectorName("最後をくり返す")]
        RepeatLast = 0,

        [InspectorName("最初に戻る")]
        Loop = 1,

        [InspectorName("もう何もしない")]
        Stop = 2,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("1 回目・2 回目・3 回目… に実行する EventSequence（上から順に）")]
    private List<EventSequence> sequences = new List<EventSequence>();

    [SerializeField, Label("最後まで行ったら")]
    private AfterLast afterLast = AfterLast.RepeatLast;

    [SerializeField, Label("使えるキャラ")]
    private CharacterRestriction user = CharacterRestriction.Both;

    [SerializeField, Label("使えないキャラで調べたとき")]
    private EventSequence wrongCharacter;

    [SerializeField, Label("回数を出す仕掛けの状態")]
    [Tooltip("調べた回数（0, 1, 2 …）を入れる仕掛けの状態（なくてもよい）。扉などの条件で「3 回調べたら」に使う")]
    private GimmickState countState;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する")]
    private string saveId = "";

    private string saveKey;
    private int count;

    public IReadOnlyList<EventSequence> Sequences => sequences;
    public int Count => count;
    protected override CharacterRestriction Restriction => user;

    //=====================================================================
    // 関数
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        saveKey = "Examine/" + gameObject.scene.name + "/" + (string.IsNullOrEmpty(saveId) ? ScenePath(transform) : saveId);
        count = Save.Get(saveKey, 0);
    }

    private void Start()
    {
        if(countState != null) { countState.Set(count, true, false); }
    }

    protected override void OnInteract(GameObject current)
    {
        if(!user.Allows(current)) { RefuseCharacter(wrongCharacter); return; }
        if(sequences.Count == 0) { return; }

        int index = count;
        if(index >= sequences.Count)
        {
            switch(afterLast)
            {
                case AfterLast.Loop:  index %= sequences.Count; break;
                case AfterLast.Stop:  return;
                default:              index = sequences.Count - 1; break;
            }
        }

        count++;
        Save.SetPending(saveKey, count);
        if(countState != null) { countState.Set(count, true); }

        EventSequence seq = sequences[index];
        if(seq != null) { seq.Play(gameObject); }
    }

    private static string ScenePath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent) { path = parent.name + "/" + path; }
        return path;
    }
}
