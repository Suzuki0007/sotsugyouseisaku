/*********************************************************************/
// * \file   TimpaniRoom.cs
// * \brief  ティンパニの部屋全体：調律できた数で残りの敵を激しくする、全部調律できたらクリア
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ティンパニの部屋全体
/// </summary>
///
/// ・子のティンパニ（TimpaniUnit）を自動で見つける
/// ・調律できた数に合わせてフラグを立てる（1台目 → 1つ目のフラグ、2台目 → 2つ目…）
///   敵の行動パターンの「段階」が、このフラグで進む（残りの敵が激しくなる）
/// ・全部調律できたら、クリアのフラグを立て、残っている弾を消し、イベントを実行する
/// ・シーンが始まったとき、激しさのフラグは OFF に戻す（やり直し・読み直しで最初から）
public class TimpaniRoom : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("部屋のティンパニ（空なら子のティンパニを全部）")]
    private List<TimpaniUnit> units = new List<TimpaniUnit>();

    [SerializeField]
    [Tooltip("調律できた数ごとに立てるフラグ（1台目で1つ目、2台目で2つ目…）。敵の行動の段階を進める。セーブしないフラグにする")]
    private List<GameFlag> escalationFlags = new List<GameFlag>();

    [SerializeField, Label("全部調律できたら立てるフラグ")]
    [Tooltip("クリアしたことを覚えるフラグ（セーブするフラグにすると、戻ってきても戦闘は始まらない）")]
    private GameFlag clearedFlag;

    [SerializeField, Label("全部調律できたときのイベント")]
    [Tooltip("クリアしたときに実行する（会話・扉を開けるなど）")]
    private EventSequence onAllCalmed;

    [SerializeField, Label("全部調律できたら残りの弾を消す")]
    private bool clearBulletsOnFinish = true;

    private int calmedCount;

    //=====================================================================
    // プロパティ・イベント
    //=====================================================================

    public static TimpaniRoom Active { get; private set; }

    /// <summary> 調律できた数 </summary>
    public int CalmedCount => calmedCount;

    /// <summary> ティンパニの数 </summary>
    public int UnitCount => units.Count;

    /// <summary> 締まったペグの数／全部のペグの数 </summary>
    public int TightenedPegs { get { int n = 0; foreach(TimpaniUnit u in units) { n += u.TightenedCount; } return n; } }
    public int TotalPegs { get { int n = 0; foreach(TimpaniUnit u in units) { n += u.Pegs.Count; } return n; } }

    /// <summary> 全部調律できたとき </summary>
    public static event Action AllCalmed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active = null;
        AllCalmed = null;
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        Active = this;
        if(units.Count == 0) { units.AddRange(GetComponentsInChildren<TimpaniUnit>(true)); }

        // 激しさは最初から（セーブしないフラグを OFF に）
        foreach(GameFlag flag in escalationFlags)
        {
            if(flag != null) { GameFlags.Set(flag, false); }
        }
    }

    private void OnDestroy()
    {
        if(Active == this) { Active = null; }
    }

    private void OnEnable()
    {
        TimpaniUnit.Calmed += OnUnitCalmed;
    }

    private void OnDisable()
    {
        TimpaniUnit.Calmed -= OnUnitCalmed;
    }

    private void OnUnitCalmed(TimpaniUnit unit)
    {
        if(!units.Contains(unit)) { return; }

        calmedCount = 0;
        foreach(TimpaniUnit u in units) { if(u.IsCalmed) { calmedCount++; } }

        // 調律できた数までのフラグを立てる（残りの敵の段階が進む）
        for(int i = 0; i < escalationFlags.Count && i < calmedCount; i++)
        {
            if(escalationFlags[i] != null && !GameFlags.Get(escalationFlags[i])) { GameFlags.Set(escalationFlags[i], true); }
        }

        if(calmedCount < units.Count) { return; }

        if(clearBulletsOnFinish)
        {
            foreach(EnemyBullet bullet in FindObjectsByType<EnemyBullet>())
            {
                if(bullet != null && bullet.gameObject.activeInHierarchy) { bullet.Vanish(); }
            }
        }

        if(clearedFlag != null) { GameFlags.Set(clearedFlag, true); }
        AllCalmed?.Invoke();
        if(onAllCalmed != null) { onAllCalmed.Play(gameObject); }
    }
}
