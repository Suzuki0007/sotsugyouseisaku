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
        PlayInternal(clip, volume, fadeTime, fadeTime, false, resume);
    }

    /// <summary>
    /// 流れている曲をフェードアウトしきってから、新しい曲をフェードインで再生する
    /// (無音から始まるときは、すぐにフェードインを始める)
    /// </summary>
    /// <param name="clip">        再生する曲 (nullなら停止) </param>
    /// <param name="volume">      曲ごとの音量 </param>
    /// <param name="fadeInTime">  フェードインにかける時間(秒) </param>
    /// <param name="fadeOutTime"> 前の曲のフェードアウトにかける時間(秒) </param>
    /// <param name="resume">      以前に途中まで流した曲なら、続きから再生する </param>
    public void PlayAfterFadeOut(AudioClip clip, float volume = 1.0f, float fadeInTime = 2.0f, float fadeOutTime = 0.5f, bool resume = false)
    {
        PlayInternal(clip, volume, fadeInTime, fadeOutTime, true, resume);
    }

    private void PlayInternal(AudioClip clip, float volume, float fadeInTime, float fadeOutTime, bool waitFadeOut, bool resume)
    {
        if(clip == null) { Stop(fadeOutTime); return; }

        // 同じ曲なら流し続ける (音量だけ合わせる)
        if(clip == currentClip)
        {
            if(!Mathf.Approximately(volume, currentVolume))
            {
                currentVolume = volume;

                // フェードイン待ちで止めてしまわないように、再生されていなければ再生する
                AudioSource active = channels[activeIndex].Source;
                if(!active.isPlaying) { active.Play(); }

                StartFade(fadeInTime, fadeInTime, 0.0f, false);
            }
            return;
        }

        RememberCurrentTime();

        // 前の曲が聞こえているときだけ、消えるのを待つ
        float delay = (waitFadeOut && IsAudible()) ? Mathf.Max(0.0f, fadeOutTime) : 0.0f;

        currentClip = clip;
        currentVolume = volume;

        // 使っていない方のチャンネルで新しい曲を準備する (再生はフェードイン開始時)
        activeIndex = 1 - activeIndex;
        BgmChannel next = channels[activeIndex];
        next.Source.Stop();
        next.FadeVolume = 0.0f;
        next.Source.clip = clip;
        next.Source.time = (resume && resumeTimes.TryGetValue(clip, out float time) && time < clip.length) ? time : 0.0f;

        StartFade(fadeInTime, fadeOutTime, delay, false);
    }

    /// <summary>
    /// いずれかのチャンネルから音が聞こえているか
    /// </summary>
    private bool IsAudible()
    {
        foreach(BgmChannel channel in channels)
        {
            if(channel.Source.isPlaying && channel.FadeVolume > 0.001f) { return true; }
        }
        return false;
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
        StartFade(fadeTime, fadeTime, 0.0f, true);
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
            float t = 1.0f - Mathf.Exp(-DeltaTime * 3.0f / effectSmoothTime);
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

    /// <summary>
    /// 1フレームの経過時間 (Time.timeScaleの影響を受けない)
    /// シーン読み込み直後の長いフレームでフェードが一瞬で終わらないように上限をつける
    /// </summary>
    private static float DeltaTime => Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);

    private const float MaxFadeStep = 1.0f / 30.0f;

    private void StartFade(float fadeInTime, float fadeOutTime, float fadeInDelay, bool stopAll)
    {
        if(fadeRoutine != null) { StopCoroutine(fadeRoutine); }
        fadeRoutine = StartCoroutine(FadeRoutine(fadeInTime, fadeOutTime, fadeInDelay, stopAll));
    }

    /// <summary>
    /// アクティブなチャンネルを目標音量へ、それ以外を0へ補間する
    /// アクティブなチャンネルは fadeInDelay 秒待ってから再生・フェードインを始める
    /// </summary>
    private IEnumerator FadeRoutine(float fadeInTime, float fadeOutTime, float fadeInDelay, bool stopAll)
    {
        float[] startVolumes = new float[channels.Length];
        for(int i = 0; i < channels.Length; i++) { startVolumes[i] = channels[i].FadeVolume; }

        AudioSource activeSource = channels[activeIndex].Source;
        float totalTime = Mathf.Max(fadeOutTime, fadeInDelay + fadeInTime);

        float time = 0.0f;
        while(time < totalTime)
        {
            time += DeltaTime;

            for(int i = 0; i < channels.Length; i++)
            {
                bool isActive = !stopAll && i == activeIndex;
                if(isActive)
                {
                    if(time < fadeInDelay) { continue; }

                    // 待ち時間が終わったら再生を始める
                    if(!activeSource.isPlaying && activeSource.clip != null) { activeSource.Play(); }

                    float t = fadeInTime > 0.0f ? Mathf.Clamp01((time - fadeInDelay) / fadeInTime) : 1.0f;
                    channels[i].FadeVolume = Mathf.Lerp(startVolumes[i], currentVolume, t);
                }
                else
                {
                    float t = fadeOutTime > 0.0f ? Mathf.Clamp01(time / fadeOutTime) : 1.0f;
                    channels[i].FadeVolume = Mathf.Lerp(startVolumes[i], 0.0f, t);
                }
            }
            yield return null;
        }

        if(!stopAll && !activeSource.isPlaying && activeSource.clip != null) { activeSource.Play(); }

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
