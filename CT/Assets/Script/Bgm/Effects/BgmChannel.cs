/*********************************************************************/
// * \file   BgmChannel.cs
// * \brief  BGMの1チャンネル (AudioSource + エフェクト)
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGMの1チャンネル
/// AudioSource と各種エフェクトフィルタを1つのGameObjectにまとめる
/// (フィルタは同じGameObjectのAudioSourceにだけかかるため、チャンネルごとに分ける)
/// BgmManager が自動で生成するので、手動で配置する必要はない
/// </summary>
public class BgmChannel : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    private AudioSource source;

    // 追加した順にかかる (薄さ → ゆがみ → こもり → 揺れ → やまびこ → 響き)
    private AudioHighPassFilter highpass;
    private AudioDistortionFilter distortion;
    private AudioLowPassFilter lowpass;
    private AudioChorusFilter chorus;
    private AudioEchoFilter echo;
    private AudioReverbFilter reverb;

    private float fadeVolume;
    private float effectVolume = 1.0f;
    private float masterVolume = 1.0f;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public AudioSource Source => source;

    /// <summary>
    /// クロスフェード用の音量
    /// </summary>
    public float FadeVolume
    {
        get => fadeVolume;
        set { fadeVolume = value; UpdateVolume(); }
    }

    public float MasterVolume
    {
        get => masterVolume;
        set { masterVolume = value; UpdateVolume(); }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    /// <summary>
    /// チャンネルを生成する
    /// </summary>
    public static BgmChannel Create(Transform parent, string name, AudioMixerGroup outputGroup)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        BgmChannel channel = obj.AddComponent<BgmChannel>();
        channel.Initialize(outputGroup);
        return channel;
    }

    private void Initialize(AudioMixerGroup outputGroup)
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0.0f;// 2Dサウンド
        source.volume = 0.0f;
        source.outputAudioMixerGroup = outputGroup;

        highpass   = gameObject.AddComponent<AudioHighPassFilter>();
        distortion = gameObject.AddComponent<AudioDistortionFilter>();
        lowpass    = gameObject.AddComponent<AudioLowPassFilter>();
        chorus     = gameObject.AddComponent<AudioChorusFilter>();
        echo       = gameObject.AddComponent<AudioEchoFilter>();
        reverb     = gameObject.AddComponent<AudioReverbFilter>();

        // 揺れは1系統だけ使う
        chorus.wetMix2 = 0.0f;
        chorus.wetMix3 = 0.0f;
        chorus.delay = 40.0f;

        // 響きは数値で調整できるユーザー設定にする
        reverb.reverbPreset = AudioReverbPreset.User;
        reverb.dryLevel = 0.0f;
        reverb.decayTime = 2.0f;

        Apply(BgmEffectSettings.Clean);
    }

    //=====================================================================
    // エフェクト反映
    //=====================================================================

    /// <summary>
    /// エフェクトの設定を反映する
    /// 無効な値のフィルタはオフにして負荷を減らす
    /// </summary>
    public void Apply(BgmEffectSettings settings)
    {
        // 薄さ
        highpass.enabled = settings.highpassCutoff > BgmEffectSettings.MinFrequency + 1.0f;
        highpass.cutoffFrequency = settings.highpassCutoff;
        highpass.highpassResonanceQ = settings.highpassResonance;

        // ゆがみ
        distortion.enabled = settings.distortion > 0.001f;
        distortion.distortionLevel = settings.distortion;

        // こもり
        lowpass.enabled = settings.lowpassCutoff < BgmEffectSettings.MaxFrequency - 1.0f;
        lowpass.cutoffFrequency = settings.lowpassCutoff;
        lowpass.lowpassResonanceQ = settings.lowpassResonance;

        // 揺れ
        chorus.enabled = settings.wobble > 0.001f;
        chorus.dryMix = 1.0f - settings.wobble * 0.5f;
        chorus.wetMix1 = settings.wobble;
        chorus.rate = settings.wobbleRate;
        chorus.depth = settings.wobbleDepth;

        // やまびこ
        echo.enabled = settings.echo > 0.001f;
        echo.dryMix = 1.0f;
        echo.wetMix = settings.echo;
        echo.delay = settings.echoDelay;
        echo.decayRatio = settings.echoDecay;

        // 響き (0〜1 をミリベル単位に変換)
        reverb.enabled = settings.reverb > 0.001f;
        reverb.room = Mathf.Lerp(-3000.0f, 0.0f, settings.reverb);
        reverb.reverbLevel = Mathf.Lerp(-3000.0f, 500.0f, settings.reverb);

        effectVolume = settings.volume;
        UpdateVolume();
    }

    private void UpdateVolume()
    {
        if(source == null) { return; }
        source.volume = fadeVolume * effectVolume * masterVolume;
    }
}
