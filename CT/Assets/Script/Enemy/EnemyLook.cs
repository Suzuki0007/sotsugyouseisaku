/*********************************************************************/
// * \file   EnemyLook.cs
// * \brief  敵の見た目の変化（体力・受けた攻撃・部位・段階・ひるみ・止まっている・条件）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 見た目が変わるきっかけ
/// </summary>
public enum LookTrigger
{
    [InspectorName("体力がこの割合以下のとき")]
    HealthBelow = 0,

    [InspectorName("この攻撃を受けたとき（○秒）")]
    Hit = 1,

    [InspectorName("この部位が壊れたら")]
    PartBroken = 2,

    [InspectorName("この段階のとき")]
    Phase = 3,

    [InspectorName("ひるんでいる間")]
    Staggered = 4,

    [InspectorName("止められている間（ギミックの命令）")]
    Stopped = 5,

    [InspectorName("条件を満たしている間（フラグ・操作中のキャラなど）")]
    Condition = 6,
}

/// <summary>
/// 見た目の変化（1行分）
/// </summary>
[Serializable]
public class EnemyLookRule
{
    [Label("きっかけ")]
    public LookTrigger trigger = LookTrigger.HealthBelow;

    [Label("体力の割合", 0.0f, 1.0f)]
    public float healthRatio = 0.5f;

    [Label("攻撃の種類")]
    public DamageKindFilter hitKind = DamageKindFilter.Any;

    [Label("攻撃したキャラ")]
    public CharacterFilter hitCharacter = CharacterFilter.Any;

    [Label("ゲージの判定")]
    public GaugeJudge hitJudge = GaugeJudge.None;

    [Label("続く時間（秒）", 0.05f)]
    [Tooltip("「この攻撃を受けたとき」に、見た目が変わっている時間")]
    public float duration = 1.0f;

    [Label("部位の名前")]
    [Tooltip("EnemyPart の「部位の名前」")]
    public string partName = "";

    [Label("段階の番号", 1)]
    [Tooltip("行動パターンの段階の番号（1 から）")]
    public int phase = 2;

    [Label("条件")]
    public GameCondition condition = new GameCondition();

    [Header("変わる見た目")]

    [Label("アニメーション")]
    [Tooltip("この間だけ使う SpriteAnimData（空ならそのまま）")]
    public SpriteAnimData animation;

    [Label("色を変える")]
    public bool useTint = true;

    [Label("色")]
    [Tooltip("絵にかける色（白ならそのまま）")]
    public Color tint = new Color(1.0f, 0.5f, 0.5f, 1.0f);

    [Label("点滅させる")]
    public bool blink = false;

    [Label("変わったときのエフェクト")]
    [Tooltip("この見た目になった瞬間に出す")]
    public EffectData effect;

    [Label("変わったときの SE")]
    public SeData se;

    public bool MatchesHit(DamageInfo info)
    {
        return new DamageRule { kind = hitKind, character = hitCharacter, judge = hitJudge }.Matches(info);
    }
}

/// <summary>
/// 敵の見た目の変化
/// </summary>
///
/// ・Enemy が自動で付ける。設定は敵のデータ（EnemyData）の「見た目の変化」
/// ・条件を満たしているルールのうち、下の行ほど優先して、アニメーションと色を決める
/// ・ギミックの命令で止めたときの色（止まっている間の色）も、ここでまとめてかける
[RequireComponent(typeof(Enemy))]
public class EnemyLook : MonoBehaviour
{
    private Enemy enemy;
    private SpriteRenderer sr;
    private SpriteAnimator anim;
    private Color baseColor = Color.white;

    private bool[] wasActive = new bool[0];
    private float[] hitTimers = new float[0];
    private SpriteAnimData currentAnimation;

    /// <summary> 今効いている見た目のルール（デバッグ表示用。なければ -1） </summary>
    public int ActiveRuleIndex { get; private set; } = -1;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SpriteAnimator>();
        if(sr != null) { baseColor = sr.color; }
    }

    private void Start()
    {
        if(enemy.Health != null) { enemy.Health.OnDamagedInfo += OnDamaged; }
        currentAnimation = enemy.Data != null ? enemy.Data.Animation : null;
    }

    private void OnDestroy()
    {
        if(enemy != null && enemy.Health != null) { enemy.Health.OnDamagedInfo -= OnDamaged; }
    }

    private IReadOnlyList<EnemyLookRule> Rules => enemy.Data != null ? enemy.Data.LookRules : null;

    private void EnsureArrays(int count)
    {
        if(wasActive.Length == count) { return; }
        wasActive = new bool[count];
        hitTimers = new float[count];
    }

    private void OnDamaged(DamageInfo info)
    {
        var rules = Rules;
        if(rules == null) { return; }
        EnsureArrays(rules.Count);

        for(int i = 0; i < rules.Count; i++)
        {
            EnemyLookRule rule = rules[i];
            if(rule != null && rule.trigger == LookTrigger.Hit && rule.MatchesHit(info)) { hitTimers[i] = rule.duration; }
        }
    }

    private void LateUpdate()
    {
        if(enemy.IsDead) { return; }

        var rules = Rules;
        int count = rules != null ? rules.Count : 0;
        EnsureArrays(count);

        SpriteAnimData useAnimation = enemy.Data != null ? enemy.Data.Animation : null;
        Color tint = Color.white;
        bool blink = false;
        ActiveRuleIndex = -1;

        for(int i = 0; i < count; i++)
        {
            EnemyLookRule rule = rules[i];
            if(rule == null) { continue; }

            if(hitTimers[i] > 0.0f) { hitTimers[i] -= Time.deltaTime; }

            bool active = IsActive(rule, i);

            // なった瞬間のエフェクト・SE
            if(active && !wasActive[i])
            {
                EffectManager.Play(rule.effect, transform.position);
                SeManager.PlaySe(rule.se, transform.position);
            }
            wasActive[i] = active;

            if(!active) { continue; }

            // 下の行ほど優先（上書き）
            ActiveRuleIndex = i;
            if(rule.animation != null) { useAnimation = rule.animation; }
            if(rule.useTint) { tint = rule.tint; }
            blink = rule.blink;
        }

        // ギミックの命令で止めているときの色
        if(enemy.PausedTint.HasValue) { tint *= enemy.PausedTint.Value; }

        if(anim != null && useAnimation != null && useAnimation != currentAnimation)
        {
            currentAnimation = useAnimation;
            anim.SetData(useAnimation, false);
        }

        if(sr != null)
        {
            Color c = baseColor * tint;
            if(blink && Mathf.Repeat(Time.time * 10.0f, 1.0f) < 0.5f) { c.a *= 0.3f; }
            sr.color = c;
        }
    }

    private bool IsActive(EnemyLookRule rule, int index)
    {
        switch(rule.trigger)
        {
            case LookTrigger.HealthBelow:
                Health health = enemy.Health;
                return health != null && health.MaxHealth > 0 && (float)health.CurrentHealth / health.MaxHealth <= rule.healthRatio;

            case LookTrigger.Hit:
                return hitTimers[index] > 0.0f;

            case LookTrigger.PartBroken:
                return enemy.IsPartBroken(rule.partName);

            case LookTrigger.Phase:
                return enemy.Brain != null && enemy.Brain.Started && enemy.Brain.PhaseIndex == rule.phase - 1;

            case LookTrigger.Staggered:
                return enemy.Staggered;

            case LookTrigger.Stopped:
                return enemy.Paused;

            case LookTrigger.Condition:
                return rule.condition != null && !rule.condition.IsEmpty && rule.condition.IsMet();
        }
        return false;
    }
}
