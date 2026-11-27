/*********************************************************************/
// * \file   CinemaEventAction.cs
// * \brief  イベントの部品「シネマ演出」（上下の黒い帯を出し、カメラを寄せる／元に戻す）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「シネマ演出」（映画のように上下に黒い帯を出し、カメラを寄せる）
/// </summary>
///
/// ・「始める」で帯が上下から出てきて、カメラが寄る先へ寄る。「終える」で帯が引っ込み、カメラが元に戻る
/// ・セリフは、この部品のあとに部品「字幕」を並べる（下の帯の中に出る）
/// ・「始める」を置いたら、同じ流れの最後に必ず「終える」を置く（シーンチェックで確かめられる）
/// ・プレイヤーを止めたいときは、EventSequence の Lock Player をオンにする
/// ・ロードしても帯は戻らない（イベントの途中でセーブしない）
public class CinemaEventAction : EventAction
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum Mode
    {
        [InspectorName("始める（帯を出してカメラを寄せる）")]
        Begin = 0,

        [InspectorName("終える（帯を引っ込めてカメラを戻す）")]
        End = 1,
    }

    public enum FocusTarget
    {
        [InspectorName("今のまま（操作中のキャラを追う）")]
        None = 0,

        [InspectorName("操作中のキャラ")]
        CurrentPlayer = 1,

        [InspectorName("ノア")]
        Noah = 2,

        [InspectorName("シグル")]
        Sigle = 3,

        [InspectorName("物（下の「寄る物」）")]
        Object = 4,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("する事")]
    [Tooltip("始める：帯を出してカメラを寄せる／終える：帯を引っ込めてカメラを元に戻す（字幕も消える）")]
    private Mode mode = Mode.Begin;

    [SerializeField, Label("帯の太さ", 0.0f, 0.3f)]
    [Tooltip("上下それぞれの帯の太さ（マップの映る範囲の高さに対する割合）。会話の帯と同じ 0.1 が基本（字幕は帯の上のぼかしの中に出る）")]
    private float barSize = 0.10f;

    [SerializeField, Label("寄る強さ", 1.0f, 3.0f)]
    [Tooltip("1 で寄らない。1.3 で 1.3 倍に大きく映る。寄りすぎると絵が粗く見えるので 1.5 くらいまでがおすすめ")]
    private float zoom = 1.3f;

    [SerializeField, Label("寄る先")]
    [Tooltip("カメラを向ける相手。動いても追いかける。今のまま＝ふだんどおり操作中のキャラを追う")]
    private FocusTarget focusTarget = FocusTarget.None;

    [SerializeField, Label("寄る物")]
    [Tooltip("寄る先が「物」のときに向ける物（キャラ・扉・空のオブジェクトなど）")]
    private Transform focusObject;

    [SerializeField, Label("動く時間（秒）", 0.0f)]
    [Tooltip("帯が出る・カメラが寄るまでの時間。0 なら一瞬")]
    private float moveTime = 0.8f;

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("順番に実行するとき、帯が出きる（引っ込みきる）まで次の部品へ進まない")]
    private bool waitUntilDone = true;

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        CinemaMode cinema = CinemaMode.Get();
        if(cinema == null) { return; }

        if(mode == Mode.End)
        {
            cinema.End(moveTime);
            return;
        }

        cinema.Begin(barSize, zoom, moveTime, FindTarget(), null);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }

        float t = 0.0f;
        while(t < moveTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // 寄る先（ノア・シグルは生成されるので、実行のときに探す）
    private Transform FindTarget()
    {
        PlayerSwitchManager party = PlayerSwitchManager.Instance;
        switch(focusTarget)
        {
            case FocusTarget.CurrentPlayer: return party != null && party.CurrentPlayer != null ? party.CurrentPlayer.transform : null;
            case FocusTarget.Noah:          return party != null && party.Noah != null ? party.Noah.transform : null;
            case FocusTarget.Sigle:         return party != null && party.Sigle != null ? party.Sigle.transform : null;
            case FocusTarget.Object:        return focusObject;
            default:                        return null;
        }
    }

    /// <summary> 始める部品か（シーンチェック用） </summary>
    public bool IsBegin => mode == Mode.Begin;

    /// <summary> 寄る先が「物」なのに空か（シーンチェック用） </summary>
    public bool MissingObject => mode == Mode.Begin && focusTarget == FocusTarget.Object && focusObject == null;
}
