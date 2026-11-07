/*********************************************************************/
// * \file   EffectEventAction.cs
// * \brief  イベントでエフェクトを出すアクションのクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント (EventZone → EventSequence) でエフェクトを出すアクション
/// EventSequence の actions リストに追加して使う
/// </summary>
[AddComponentMenu("Effect/Effect Event Action")]
public class EffectEventAction : EventAction
{
    private enum Target
    {
        [InspectorName("このオブジェクトの位置")]
        ThisObject,

        [InspectorName("イベントに入ってきたキャラクター")]
        EventObject,

        [InspectorName("指定した場所 (下の欄)")]
        Custom,
    }

    [SerializeField, Tooltip("イベント発動時に出すエフェクト")]
    private EffectData effectOnEnter;

    [SerializeField, Tooltip("エリアから出たときに出すエフェクト (任意)")]
    private EffectData effectOnExit;

    [SerializeField, Tooltip("どこに出すか")]
    private Target spawnAt = Target.ThisObject;

    [SerializeField, Tooltip("「指定した場所」のときに使う位置")]
    private Transform customPoint;

    [SerializeField, Tooltip("位置のずらし")]
    private Vector2 offset = Vector2.zero;

    [SerializeField, Tooltip("出した後も対象にくっついて動く")]
    private bool follow = false;

    [SerializeField, Tooltip("エリアから出たとき、ループエフェクトを止める")]
    private bool stopLoopOnExit = true;

    private EffectHandle loopHandle;

    public override void Execute(GameObject gameObject)
    {
        if(effectOnEnter == null) { return; }

        // 二重に出さないよう、前回のループは止めておく
        EffectManager.Stop(loopHandle);

        EffectHandle handle = PlayAt(effectOnEnter, gameObject);
        loopHandle = effectOnEnter.IsLoop ? handle : EffectHandle.None;
    }

    public override void OnExit(GameObject gameObject)
    {
        if(stopLoopOnExit)
        {
            EffectManager.Stop(loopHandle);
            loopHandle = EffectHandle.None;
        }

        if(effectOnExit != null) { PlayAt(effectOnExit, gameObject); }
    }

    private EffectHandle PlayAt(EffectData data, GameObject eventObject)
    {
        Transform target = transform;
        if(spawnAt == Target.EventObject && eventObject != null) { target = eventObject.transform; }
        else if(spawnAt == Target.Custom && customPoint != null) { target = customPoint; }

        return EffectManager.Play(data, target.position, offset, 0.0f, follow ? target : null, false);
    }
}
