/*********************************************************************/
// * \file   SeEventAction.cs
// * \brief  イベントでSEを鳴らすアクションのクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント (EventZone → EventSequence) でSEを鳴らすアクション
/// EventSequence の actions リストに追加して使う
/// </summary>
public class SeEventAction : EventAction
{
    [SerializeField, Tooltip("イベント発動時に鳴らすSE")]
    private SeData seOnEnter;

    [SerializeField, Tooltip("エリアから出たときに鳴らすSE (任意)")]
    private SeData seOnExit;

    [SerializeField, Tooltip("エリアから出たとき、seOnEnter がループSEなら止める")]
    private bool stopLoopOnExit = true;

    [SerializeField, Tooltip("このオブジェクトの位置で鳴らす (3D寄りのSEのとき有効)")]
    private bool playAtThisPosition = true;

    private SeHandle loopHandle;

    public override void Execute(GameObject gameObject)
    {
        if(seOnEnter == null) { return; }

        // 二重に鳴らさないよう、前回のループは止めておく
        SeManager.StopSe(loopHandle);

        SeHandle handle = playAtThisPosition
            ? SeManager.PlaySe(seOnEnter, transform.position)
            : SeManager.PlaySe(seOnEnter);

        loopHandle = seOnEnter.Loop ? handle : SeHandle.None;
    }

    public override void OnExit(GameObject gameObject)
    {
        if(stopLoopOnExit)
        {
            SeManager.StopSe(loopHandle);
            loopHandle = SeHandle.None;
        }

        if(seOnExit == null) { return; }
        if(playAtThisPosition) { SeManager.PlaySe(seOnExit, transform.position); }
        else                   { SeManager.PlaySe(seOnExit); }
    }
}
