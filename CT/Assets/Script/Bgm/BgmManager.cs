/*********************************************************************/
// * \file   BgmManager.cs
// * \brief  BGM再生管理クラス (クロスフェード・エフェクト対応)
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGMを再生するクラス
/// シーンに配置しなくても、初回アクセス時に自動生成され、シーンをまたいで存続する
/// 同じ曲を指定した場合は途切れずにそのまま流れ続ける
///
/// エフェクトは BgmEffectProfile を設定すると、BgmProgress の値に応じて自動で変化する
/// </summary>
public class BgmManager : MonoBehaviour
{
    //=====================================================================
    // シングルトン
    //=====================================================================

    private static BgmManager instance;

    /// <summary>
    /// インスタンス (存在しなければ自動生成)
    /// </summary>
    public static BgmManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new GameObject(nameof(BgmManager)).AddComponent<BgmManager>();
            }
            return instance;
        }
    }

    /// <summary>
    /// インスタンスを生成せずに存在確認する (終了時の生成を防ぐ)
    /// </summary>
    public static bool HasInstance => instance != null;

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("全体の音量")]
    [Range(0.0f, 1.0f)]
    private float masterVolume = 1.0f;

    [SerializeField, Tooltip("出力先のAudioMixerGroup (任意)")]
    private AudioMixerGroup outputGroup;

    [SerializeField, Tooltip("エフェクトが目標の状態に近づく速さ (秒)")]
    private float effectSmoothTime = 1.5f;

    // クロスフェード用に2つのチャンネルを交互に使う
    private BgmChannel[] channels;
    private int activeIndex;

    // 現在再生中(フェードイン中も含む)の曲と音量
    private AudioClip currentClip;
    private float currentVolume = 1.0f;

    // 途中から再開するための再生位置の記録
    private readonly Dictionary<AudioClip, float> resumeTimes = new Dictionary<AudioClip, float>();

    private Coroutine fadeRoutine;

    // エフェクト
    private BgmEffectProfile effectProfile;
    private BgmEffectSettings overrideSettings;
    private readonly BgmEffectSettings targetEffect = new BgmEffectSettings();
    private readonly BgmEffectSettings currentEffect = new BgmEffectSettings();

    //=====================================================================
    // プロパティ
    //=====================================================================

    public AudioClip CurrentClip => currentClip;

    public BgmEffectProfile EffectProfile => effectProfile;

    /// <summary>
    /// 現在かかっているエフェクト (読み取り用)
    /// </summary>
    public BgmEffectSettings CurrentEffect => currentEffect;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            if(channels == null) { return; }
            foreach(BgmChannel channel in channels) { channel.MasterVolume = masterVolume; }
        }
    }

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        channels = new BgmChannel[2];
        for(int i = 0; i < channels.Length; i++)
        {
            channels[i] = BgmChannel.Create(transform, $"Channel{i}", outputGroup);
            channels[i].MasterVolume = masterVolume;
        }
    }

    private void OnDestroy()
    {
        if(instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        UpdateEffect(false);
    }

    //=====================================================================
    // 再生関数
    //=====================================================================

    /// <summary>
    /// BGMを再生する (再生中の曲からクロスフェードで切り替える)
    /// </summary>
    /// <param name="clip">     再生する曲 (nullなら停止) </param>
    /// <param name="volume">   曲ごとの音量 </param>
    /// <param name="fadeTime"> 切り替えにかける時間(秒) </param>
    /// <param name="resume">   以前に途中まで流した曲なら、続きから再生する </param>
    public void Play(AudioClip clip, float volume = 1.0f, float fadeTime = 1.0f, bool resume = false)
    {
        if(clip == null) { Stop(fadeTime); return; }

        // 同じ曲なら流し続ける (音量だけ合わせる)
        if(clip == currentClip)
        {
            if(!Mathf.Approximately(volume, currentVolume))
            {
                currentVolume = volume;
                StartFade(fadeTime, false);
            }
            return;
        }

        RememberCurrentTime();

        currentClip = clip;
        currentVolume = volume;

        // 使っていない方のチャンネルで新しい曲を流す
        activeIndex = 1 - activeIndex;
        AudioSource next = channels[activeIndex].Source;
        channels[activeIndex].FadeVolume = 0.0f;
        next.clip = clip;
        next.time = (resume && resumeTimes.TryGetValue(clip, out float time) && time < clip.length) ? time : 0.0f;
        next.Play();

        StartFade(fadeTime, false);
    }

    /// <summary>
    /// BGMを停止する
    /// </summary>
    /// <param name="fadeTime"> フェードアウトにかける時間(秒) </param>
    public void Stop(float fadeTime = 1.0f)
    {
        if(currentClip == null) { return; }

        RememberCurrentTime();
        currentClip = null;
        StartFade(fadeTime, true);
    }

    /// <summary>
    /// 記録している再生位置を消す
    /// </summary>
    public void ClearResumeTimes()
    {
        resumeTimes.Clear();
    }

    //=====================================================================
    // エフェクト関数
    //=====================================================================

    /// <summary>
    /// エフェクトのプロファイルを設定する (nullならエフェクトなし)
    /// </summary>
    /// <param name="profile"> 使うプロファイル </param>
    /// <param name="instant"> trueなら補間せずにすぐ反映する </param>
    public void SetEffectProfile(BgmEffectProfile profile, bool instant = false)
    {
        effectProfile = profile;
        UpdateEffect(instant);
    }

    /// <summary>
    /// プロファイルを無視して、指定したエフェクトをかける (演出用)
    /// ClearEffectOverride() で元に戻る
    /// </summary>
    public void SetEffectOverride(BgmEffectSettings settings, bool instant = false)
    {
        overrideSettings = settings;
        UpdateEffect(instant);
    }

    public void ClearEffectOverride(bool instant = false)
    {
        overrideSettings = null;
        UpdateEffect(instant);
    }

    /// <summary>
    /// 目標のエフェクトを求め、現在のエフェクトを近づけて反映する
    /// </summary>
    private void UpdateEffect(bool instant)
    {
        if(channels == null) { return; }

        if(overrideSettings != null)    { targetEffect.CopyFrom(overrideSettings); }
        else if(effectProfile != null)  { effectProfile.Evaluate(BgmProgress.Value, targetEffect); }
        else                            { targetEffect.ResetToClean(); }

        if(instant || effectSmoothTime <= 0.0f)
        {
            currentEffect.CopyFrom(targetEffect);
        }
        else
        {
            // 時間に依存しない滑らかな追従 (Time.timeScaleの影響を受けない)
            float t = 1.0f - Mathf.Exp(-Time.unscaledDeltaTime * 3.0f / effectSmoothTime);
            BgmEffectSettings.Lerp(currentEffect, targetEffect, t, currentEffect);
        }

        foreach(BgmChannel channel in channels) { channel.Apply(currentEffect); }
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private void RememberCurrentTime()
    {
        if(currentClip == null) { return; }

        AudioSource active = channels[activeIndex].Source;
        if(active.clip == currentClip && active.isPlaying)
        {
            resumeTimes[currentClip] = active.time;
        }
    }

    private void StartFade(float fadeTime, bool stopAll)
    {
        if(fadeRoutine != null) { StopCoroutine(fadeRoutine); }
        fadeRoutine = StartCoroutine(FadeRoutine(fadeTime, stopAll));
    }

    /// <summary>
    /// アクティブなチャンネルを目標音量へ、それ以外を0へ補間する
    /// (Time.timeScaleの影響を受けない)
    /// </summary>
    private IEnumerator FadeRoutine(float fadeTime, bool stopAll)
    {
        float[] startVolumes = new float[channels.Length];
        for(int i = 0; i < channels.Length; i++) { startVolumes[i] = channels[i].FadeVolume; }

        float time = 0.0f;
        while(time < fadeTime)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / fadeTime);

            for(int i = 0; i < channels.Length; i++)
            {
                float target = (!stopAll && i == activeIndex) ? currentVolume : 0.0f;
                channels[i].FadeVolume = Mathf.Lerp(startVolumes[i], target, t);
            }
            yield return null;
        }

        for(int i = 0; i < channels.Length; i++)
        {
            bool isActive = !stopAll && i == activeIndex;
            channels[i].FadeVolume = isActive ? currentVolume : 0.0f;
            if(!isActive)
            {
                channels[i].Source.Stop();
                channels[i].Source.clip = null;
            }
        }

        fadeRoutine = null;
    }
}
