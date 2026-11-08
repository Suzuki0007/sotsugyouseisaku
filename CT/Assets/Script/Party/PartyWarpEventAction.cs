/*********************************************************************/
// * \file   PartyWarpEventAction.cs
// * \brief  イベントでノア・シグルを決めた場所へ移すアクション（離れ離れになる場面など）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// キャラを移す
/// </summary>
///
/// 例：崩れた床で離れ離れになる → シグルを別のエリアへ移す ／ 扉の向こうへ移す演出 ／ 合流の場面で 2人を並べる
/// ・「暗転する」なら、暗転している間に移す（「順番に実行」では明転し終わるまで次を待つ）
/// ・一緒のときに 1 人だけ移すと、相棒がついてきて戻ってしまうので、先に「2人の状態を変える」で待機・別行動にしておく
public class PartyWarpEventAction : EventAction
{
    public enum Who
    {
        [InspectorName("ノア")]
        Noah = 0,

        [InspectorName("シグル")]
        Sigle = 1,

        [InspectorName("操作しているキャラ")]
        Current = 2,

        [InspectorName("相棒（操作していない方）")]
        Partner = 3,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("移すキャラ")]
    private Who who = Who.Sigle;

    [SerializeField, Label("移す場所")]
    [Tooltip("空のオブジェクトを置いて入れる")]
    private Transform destination;

    [SerializeField, Label("暗転する")]
    private bool fade = true;

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        Move();
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        if(!fade)
        {
            Move();
            yield break;
        }

        bool moved = false;
        SceneFader.Instance.FadeThrough(() => { Move(); moved = true; });
        while(!moved || SceneFader.IsBusy) { yield return null; }
    }

    private void Move()
    {
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        if(m == null || destination == null || m.Noah == null || m.Sigle == null) { return; }

        GameObject target;
        switch(who)
        {
            case Who.Noah:    target = m.Noah; break;
            case Who.Sigle:   target = m.Sigle; break;
            case Who.Current: target = m.CurrentPlayer; break;
            default:          target = m.Partner; break;
        }
        if(target == null) { return; }

        Vector3 noahPos = m.Noah.transform.position;
        Vector3 siglePos = m.Sigle.transform.position;
        if(target == m.Noah) { noahPos = destination.position; }
        else                 { siglePos = destination.position; }
        m.Warp(noahPos, siglePos);

        // 操作しているキャラを移したら、カメラもすぐ追いつかせる
        FocusCamera cam = FindAnyObjectByType<FocusCamera>();
        if(cam != null && target == m.CurrentPlayer) { cam.SetTarget(target.transform, false); }
    }

    private void OnDrawGizmosSelected()
    {
        if(destination == null) { return; }
        Gizmos.color = new Color(0.4f, 0.8f, 1.0f, 0.9f);
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.3f);
    }
}
