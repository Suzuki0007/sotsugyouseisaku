/*********************************************************************/
// * \file   AnimEventAction.cs
// * \brief  イベントでアニメーションを流すアクション
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント (EventZone → EventSequence) でアニメーションを流すアクション
/// (扉が開く、スイッチが切り替わる、像が光り出す、など)
/// EventSequence の actions リストに追加して使う
///
/// セーブから復元するときは、音やエフェクトを出さずに「流し終わった見た目」にする
/// </summary>
[AddComponentMenu("Animation/Anim Event Action")]
public class AnimEventAction : EventAction
{
    [SerializeField, Tooltip("アニメーションを流す SpriteAnimator (空ならこのオブジェクトのもの)")]
    private SpriteAnimator target;

    [SerializeField, Tooltip("イベント発動時に流す動きの名前")]
    private string clipOnEnter = "Open";

    [SerializeField, Tooltip("エリアから出たときに流す動きの名前 (任意)。設定した場合、セーブからは復元しない (一時的な変化とみなす)")]
    private string clipOnExit = "";

    public override void Execute(GameObject gameObject)
    {
        Play(clipOnEnter);
    }

    public override void OnExit(GameObject gameObject)
    {
        if(string.IsNullOrEmpty(clipOnExit)) { return; }
        Play(clipOnExit);
    }

    public override void Restore(GameObject gameObject)
    {
        // 「出たら戻す」設定のものは一時的な変化なので、復元しない
        if(!string.IsNullOrEmpty(clipOnExit)) { return; }

        SpriteAnimator anim = GetTarget();
        if(anim != null && !anim.JumpToEnd(clipOnEnter)) { WarnMissing(clipOnEnter); }
    }

    private void Play(string clipName)
    {
        SpriteAnimator anim = GetTarget();
        if(anim != null && !anim.Play(clipName, true)) { WarnMissing(clipName); }
    }

    private SpriteAnimator GetTarget()
    {
        if(target == null) { target = GetComponent<SpriteAnimator>(); }
        if(target == null) { Debug.LogWarning($"{name}: SpriteAnimator が設定されていません", this); }
        return target;
    }

    private void WarnMissing(string clipName)
    {
        Debug.LogWarning($"{name}: アニメーションに「{clipName}」という動きがありません", this);
    }
}
