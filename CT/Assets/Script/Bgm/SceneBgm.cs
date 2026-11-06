/*********************************************************************/
// * \file   SceneBgm.cs
// * \brief  シーンごとのBGM設定クラス
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーンごとのBGM設定クラス
/// 各シーンに1つ配置し、基本のBGMと条件付きのBGMを設定する
///
/// 毎フレーム条件をチェックし、満たしているルールのうち
/// 優先度が最も高いものの曲を流す。どれも満たさなければ基本のBGMを流す
/// </summary>
public class SceneBgm : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    /// <summary>
    /// 条件付きBGMのルール
    /// </summary>
    [System.Serializable]
    public class Rule
    {
        [Tooltip("インスペクタ上の名前 (メモ用)")]
        public string label;

        [Tooltip("切り替え条件 (BgmConditionを継承したコンポーネント)")]
        public BgmCondition condition;

        [Tooltip("条件を満たしたときに流す曲 (空なら無音)")]
        public AudioClip clip;

        [Range(0.0f, 1.0f)]
        public float volume = 1.0f;

        [Tooltip("大きいほど優先される")]
        public int priority;

        [Tooltip("切り替えにかける時間(秒)")]
        public float fadeTime = 1.0f;

        [Tooltip("条件を満たしている間のエフェクト (空ならシーンの設定を使う)")]
        public BgmEffectProfile effectProfile;

        [Tooltip("一度でも条件を満たしたら、以降はずっとこの曲にする (ボス戦開始など)")]
        public bool latch;

        [System.NonSerialized]
        public bool latched;

        public bool IsActive()
        {
            if(latched) { return true; }
            if(condition == null) { return false; }

            bool met = condition.Evaluate();
            if(met && latch) { latched = true; }
            return met;
        }
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [Header("基本のBGM")]

    [SerializeField, Tooltip("このシーンで流す曲 (空なら無音)")]
    private AudioClip defaultClip;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float defaultVolume = 1.0f;

    [SerializeField, Tooltip("条件付きBGMから戻るときなどの切り替え時間(秒)")]
    private float defaultFadeTime = 1.0f;

    [SerializeField, Tooltip("条件付きBGMから戻ったとき、基本のBGMを続きから流す")]
    private bool resumeDefault = true;

    [SerializeField, Tooltip("このシーンのエフェクト (空ならエフェクトなし)")]
    private BgmEffectProfile effectProfile;

    [Header("シーン開始時")]

    [SerializeField, Tooltip("シーン開始時のフェードインにかける時間(秒)")]
    private float startFadeInTime = 2.0f;

    [SerializeField, Tooltip("前のシーンと曲が違うとき、オンなら前の曲が消えてからフェードイン / オフならクロスフェード")]
    private bool fadeOutPrevious = true;

    [SerializeField, Tooltip("前のシーンの曲を消す時間(秒)")]
    private float previousFadeOutTime = 0.5f;

    [Header("条件付きBGM")]

    [SerializeField]
    private List<Rule> rules = new List<Rule>();

    [Header("その他")]

    [SerializeField, Tooltip("シーン開始時にBGMフラグをすべてリセットする")]
    private bool clearFlagsOnStart = false;

    // 現在採用しているルール (nullなら基本のBGM)
    private Rule currentRule;
    private bool started;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Start()
    {
        if(clearFlagsOnStart) { BgmFlags.ClearAll(); }

        currentRule = SelectRule();
        Apply(currentRule, true);
        started = true;
    }

    private void Update()
    {
        if(!started) { return; }

        Rule rule = SelectRule();
        if(rule == currentRule) { return; }

        currentRule = rule;
        Apply(rule, false);
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    /// <summary>
    /// 条件を満たしているルールのうち、優先度が最も高いものを返す
    /// </summary>
    private Rule SelectRule()
    {
        Rule best = null;
        foreach(Rule rule in rules)
        {
            if(rule == null) { continue; }
            if(!rule.IsActive()) { continue; }
            if(best == null || rule.priority > best.priority) { best = rule; }
        }
        return best;
    }

    private void Apply(Rule rule, bool isSceneStart)
    {
        // 無音から始まるときは、最初からエフェクトがかかった状態にする
        bool instant = isSceneStart && BgmManager.Instance.CurrentClip == null;

        BgmEffectProfile profile = (rule != null && rule.effectProfile != null) ? rule.effectProfile : effectProfile;
        BgmManager.Instance.SetEffectProfile(profile, instant);

        AudioClip clip   = rule == null ? defaultClip     : rule.clip;
        float     volume = rule == null ? defaultVolume   : rule.volume;
        float     fade   = rule == null ? defaultFadeTime : rule.fadeTime;
        bool      resume = rule == null && resumeDefault && !isSceneStart;

        // シーン開始時はフェードインで始める
        if(isSceneStart)
        {
            if(fadeOutPrevious)
            {
                BgmManager.Instance.PlayAfterFadeOut(clip, volume, startFadeInTime, previousFadeOutTime, resume);
            }
            else
            {
                BgmManager.Instance.Play(clip, volume, startFadeInTime, resume);
            }
            return;
        }

        BgmManager.Instance.Play(clip, volume, fade, resume);
    }

    //=====================================================================
    // 外部から呼ぶ関数
    //=====================================================================

    /// <summary>
    /// latch で固定されたルールを解除する (ボス撃破後に通常BGMへ戻すなど)
    /// </summary>
    public void ResetLatches()
    {
        foreach(Rule rule in rules)
        {
            if(rule != null) { rule.latched = false; }
        }
    }
}
