/*********************************************************************/
// * \file   GimmickInteractable.cs
// * \brief  調べる仕掛けの共通部分（操作中のキャラだけ・使えるキャラ・使えないときの反応）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 調べる仕掛けの共通部分
/// </summary>
///
/// ・調べられるのは、範囲（Is Trigger の Collider2D）の中にいる「操作しているキャラ」だけ
///   （Intaractable は範囲に入った Player タグの物を覚えるので、ついてくる相棒・待たせている相棒にも反応してしまうため）
/// ・使えるキャラ（両方・ノアだけ・シグルだけ）の判定と、使えないときの反応（イベントか「できない」音）
public abstract class GimmickInteractable : Intaractable
{
    private Collider2D[] areas;

    protected virtual void Awake()
    {
        areas = GetComponents<Collider2D>();
    }

    protected override void OnPlayerExit(GameObject exitedPlayer)
    {
        // ついてくる相棒が出ただけなら、操作しているキャラはまだ中にいる
        GameObject current = CurrentPlayer;
        if(current != null && current != exitedPlayer && IsInside(current)) { player = current; }
    }

    protected sealed override void Interact()
    {
        GameObject current = CurrentPlayer;
        if(current == null || !IsInside(current)) { return; }
        OnInteract(current);
    }

    /// <summary> 操作しているキャラが調べたとき </summary>
    protected abstract void OnInteract(GameObject user);

    /// <summary> 断る（イベントがあれば実行、なければ「できない」音） </summary>
    protected void Refuse(EventSequence sequence)
    {
        if(sequence != null) { sequence.Play(gameObject); }
        else                 { UISound.Play(UISoundType.Cannot); }
    }

    protected static GameObject CurrentPlayer => PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;

    /// <summary> そのキャラが調べる範囲の中にいるか </summary>
    protected bool IsInside(GameObject target)
    {
        if(areas == null) { areas = GetComponents<Collider2D>(); }
        if(areas.Length == 0) { return true; }

        Collider2D targetCollider = target.GetComponent<Collider2D>();
        foreach(Collider2D area in areas)
        {
            if(area == null || !area.enabled || !area.isTrigger) { continue; }
            if(area.OverlapPoint(target.transform.position)) { return true; }
            if(targetCollider != null && area.IsTouching(targetCollider)) { return true; }
        }
        return false;
    }
}
