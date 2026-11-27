/*********************************************************************/
// * \file   RippleEventAction.cs
// * \brief  イベントの部品「波紋を広げる」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「波紋を広げる」（直した場所から、色または光が広がる）
/// </summary>
///
/// ・中心（物・ノア・シグル・置いた位置）から、波紋のプリセットの見せ方（色が戻る／光が灯る）で輪が広がる
/// ・「残す ID」を入れ、プリセットが「ずっと残る」なら、セーブ・ロードのあとも広がった状態で残る
/// ・同じ ID でもう一度広げると作り直す。消すのは部品「波紋を消す」
public class RippleEventAction : EventAction
{
    public enum Center
    {
        [InspectorName("物")]
        Object = 0,

        [InspectorName("ノア")]
        Noah = 1,

        [InspectorName("シグル")]
        Sigle = 2,

        [InspectorName("操作中のキャラ")]
        Current = 3,
    }

    [SerializeField, Label("波紋")]
    [Tooltip("波紋のプリセット（Assets/Resources/Presentation/Ripple/）")]
    private RipplePreset preset;

    [SerializeField, Label("中心")]
    private Center center = Center.Object;

    [SerializeField, Label("中心の物")]
    [Tooltip("中心が「物」のときの物（空ならこの部品の物の位置）")]
    private Transform target;

    [SerializeField, Label("ずらす")]
    [Tooltip("中心からのずれ（マス）")]
    private Vector2 offset = Vector2.zero;

    [SerializeField, Label("広がる秒数", 0.0f)]
    [Tooltip("0 ならプリセットの秒数")]
    private float seconds = 0.0f;

    [SerializeField, Label("残す ID")]
    [Tooltip("空でなければ、セーブ・ロードのあとも残る（プリセットが「ずっと残る」のとき）。同じ場所の波紋は同じ ID に")]
    private string id = "";

    [SerializeField, Label("広がりきるまで待つ")]
    private bool waitUntilDone = false;

    public RipplePreset Preset => preset;
    public string Id => id;

    public override void Execute(GameObject gameObject)
    {
        Spawn(gameObject);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        RippleView v = Spawn(gameObject);
        if(!waitUntilDone || v == null) { yield break; }
        while(v != null && !v.IsExpanded) { yield return null; }
    }

    private RippleView Spawn(GameObject owner)
    {
        if(preset == null) { Debug.LogWarning("波紋を広げる：波紋のプリセットが空です", this); return null; }
        Vector3 pos = transform.position;
        PlayerSwitchManager psm = PlayerSwitchManager.Instance;
        switch(center)
        {
            case Center.Object:  pos = target != null ? target.position : transform.position; break;
            case Center.Noah:    if(psm != null && psm.Noah != null) { pos = psm.Noah.transform.position; } break;
            case Center.Sigle:   if(psm != null && psm.Sigle != null) { pos = psm.Sigle.transform.position; } break;
            case Center.Current: if(psm != null && psm.CurrentPlayer != null) { pos = psm.CurrentPlayer.transform.position; } break;
        }
        pos += (Vector3)offset;
        return RippleSystem.Spawn(preset, pos, seconds > 0.0f ? seconds : preset.Seconds, id, !string.IsNullOrEmpty(id));
    }
}
