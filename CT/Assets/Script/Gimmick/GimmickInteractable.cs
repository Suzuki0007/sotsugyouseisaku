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
///   （Intaractable も操作キャラだけを見るようにしたが、念のため調べた瞬間にも範囲の中にいるかを確かめる）
/// ・使えるキャラ（両方・ノアだけ・シグルだけ）の判定と、使えないときの反応（イベントか「できない」音）
public abstract class GimmickInteractable : Intaractable, IInteractPrompt
{
    private Collider2D[] areas;

    /// <summary> キーの案内を出すか（範囲に操作中のキャラがいて、そのキャラが使えるとき） </summary>
    public virtual bool PromptVisible => player != null && Restriction.Allows(player) && CanPrompt;

    /// <summary> キーの案内の文 </summary>
    public virtual string PromptLabel => "調べる";

    /// <summary> 今 E で何か起きるか（もう使えない仕掛けは false にする） </summary>
    protected virtual bool CanPrompt => true;

    protected virtual void Awake()
    {
        areas = GetComponents<Collider2D>();
    }

    protected sealed override void Interact()
    {
        GameObject current = CurrentPlayer;
        if(current == null || !IsInside(current)) { return; }
        OnInteract(current);
    }

    /// <summary> 操作しているキャラが調べたとき </summary>
    protected abstract void OnInteract(GameObject user);

    /// <summary> 使えるキャラ（使えないキャラで近づくと、使えるキャラの目印を出す） </summary>
    protected virtual CharacterRestriction Restriction => CharacterRestriction.Both;

    protected virtual void LateUpdate()
    {
        CharacterRestriction r = Restriction;
        if(r == CharacterRestriction.Both) { return; }

        GameObject current = CurrentPlayer;
        if(current == null || r.Allows(current)) { return; }

        PartyHintSettings s = PartyHint.Settings;
        if(s != null && Vector2.Distance(current.transform.position, transform.position) <= s.showDistance)
        {
            PartyHint.Request(transform, r, current.transform.position);
        }
    }

    /// <summary> 断る（イベントがあれば実行、なければ「できない」音） </summary>
    protected void Refuse(EventSequence sequence)
    {
        if(sequence != null) { sequence.Play(gameObject); }
        else                 { UISound.Play(UISoundType.Cannot); }
        Bark.Fire(BarkTrigger.CannotUse, BarkContext.Of(CurrentPlayer, transform));
    }

    /// <summary> 使えないキャラで調べたので断る（イベントか「できない」音 ＋ 相棒の一言） </summary>
    protected void RefuseCharacter(EventSequence sequence)
    {
        if(sequence != null) { sequence.Play(gameObject); }
        else                 { UISound.Play(UISoundType.Cannot); }
        Bark.Fire(BarkTrigger.WrongCharacter, BarkContext.Of(CurrentPlayer, transform));
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
