/*********************************************************************/
// * \file   BreakableWall.cs
// * \brief  攻撃で壊せる壁（シグルだけ壊せる・ひびの入った壁など）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 壊せる壁
/// </summary>
///
/// ・決めたキャラ・攻撃の種類の攻撃が、決めた回数当たると壊れる（当たり判定が消え、絵が消える・変わる）
///   例：シグルの弾でしか壊せないひびの入った壁 ／ ノアのゲージ攻撃（パーフェクト）で割れる岩
/// ・壊れたかは仕掛けの状態（GimmickState）の ON。セーブされ、扉などの条件にも使える
/// ・条件に合わない攻撃が当たると「効かない」反応（エフェクト・音）。使えないキャラが近づくと、使えるキャラの目印が出る
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(DamageReceiver))]
[RequireComponent(typeof(GimmickState))]
public class BreakableWall : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("壊せる攻撃")]

    [SerializeField, Label("壊せるキャラ")]
    [Tooltip("この壁を壊せるキャラ（攻撃したキャラで判定する）")]
    private CharacterRestriction breaker = CharacterRestriction.SigleOnly;

    [SerializeField, Label("攻撃の種類")]
    [Tooltip("「すべての攻撃」なら種類を問わない。シグルの弾だけ・ノアのゲージ攻撃だけ、などに絞れる")]
    private DamageKindFilter kind = DamageKindFilter.Any;

    [SerializeField, Label("ゲージの判定")]
    [Tooltip("ゲージ攻撃のとき、判定の結果で絞る（「なし」なら問わない）")]
    private GaugeJudge judge = GaugeJudge.None;

    [SerializeField, Label("壊れるまでの回数", 1)]
    private int requiredHits = 1;

    [Header("壊れたとき")]

    [SerializeField, Label("壊れた絵")]
    [Tooltip("壊れたあとの絵（がれき）。空なら絵を消す")]
    private Sprite brokenSprite;

    [SerializeField, Label("壊れたときのエフェクト")]
    private EffectData breakEffect;

    [SerializeField, Label("壊れたときの SE")]
    private SeData breakSe;

    [SerializeField, Label("壊れたときのイベント")]
    private EventSequence onBroken;

    [Header("当たったとき")]

    [SerializeField, Label("当たったときのエフェクト")]
    [Tooltip("壊せる攻撃が当たるたび（壊れる前）")]
    private EffectData hitEffect;

    [SerializeField, Label("当たったときの SE")]
    private SeData hitSe;

    [SerializeField, Label("効かない攻撃のエフェクト")]
    private EffectData blockedEffect;

    [SerializeField, Label("効かない攻撃の SE")]
    private SeData blockedSe;

    private DamageReceiver receiver;
    private GimmickState state;
    private SpriteRenderer spriteRenderer;
    private int hits;

    public CharacterRestriction Breaker => breaker;
    public bool Broken => state != null && state.IsOn;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        receiver = GetComponent<DamageReceiver>();
        state = GetComponent<GimmickState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer == null) { spriteRenderer = GetComponentInChildren<SpriteRenderer>(); }

        receiver.OnHit += OnHit;
        state.Changed += (s, v) => Apply();
    }

    private void Start()
    {
        Apply();
        if(Broken && onBroken != null) { onBroken.Restore(gameObject); }
    }

    private void OnDestroy()
    {
        if(receiver != null) { receiver.OnHit -= OnHit; }
    }

    private void Update()
    {
        if(Broken) { return; }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        GameObject current = manager != null ? manager.CurrentPlayer : null;
        PartyHintSettings hint = PartyHint.Settings;
        if(current != null && hint != null && !breaker.Allows(current) && Vector2.Distance(current.transform.position, transform.position) <= hint.showDistance + 1.0f)
        {
            PartyHint.Request(transform, breaker, current.transform.position);
        }
    }

    private void OnHit(DamageInfo info)
    {
        if(Broken) { return; }

        bool characterOk = breaker == CharacterRestriction.Both
            || (breaker == CharacterRestriction.NoahOnly && info.character == PlayerCharacter.Noah)
            || (breaker == CharacterRestriction.SigleOnly && info.character == PlayerCharacter.Sigle);
        var rule = new DamageRule { kind = kind, character = CharacterFilter.Any, judge = judge };

        if(!characterOk || !rule.Matches(info))
        {
            EffectManager.Play(blockedEffect, info.point);
            SeManager.PlaySe(blockedSe, info.point);
            return;
        }

        hits++;
        if(hits < Mathf.Max(1, requiredHits))
        {
            EffectManager.Play(hitEffect, info.point);
            SeManager.PlaySe(hitSe, info.point);
            return;
        }

        state.Set(1, true);
        EffectManager.Play(breakEffect, transform.position);
        SeManager.PlaySe(breakSe, transform.position);
        if(onBroken != null) { onBroken.Play(gameObject); }
    }

    /// <summary> 壊れた見た目・当たり判定にする </summary>
    private void Apply()
    {
        bool broken = Broken;
        foreach(Collider2D c in GetComponents<Collider2D>()) { c.enabled = !broken; }
        if(spriteRenderer == null || !broken) { return; }

        if(brokenSprite != null) { spriteRenderer.sprite = brokenSprite; }
        else                     { spriteRenderer.enabled = false; }
    }
}
