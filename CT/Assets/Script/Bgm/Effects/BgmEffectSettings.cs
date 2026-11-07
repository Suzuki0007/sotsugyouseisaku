/*********************************************************************/
// * \file   BgmEffectSettings.cs
// * \brief  BGMにかけるエフェクトの設定値
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// BGMにかけるエフェクトの設定値
/// 初期値は「エフェクトなし (原音のまま)」
/// </summary>
[System.Serializable]
public class BgmEffectSettings
{
    //=====================================================================
    // 定数
    //=====================================================================

    public const float MinFrequency = 10.0f;
    public const float MaxFrequency = 22000.0f;

    //=====================================================================
    // 設定値
    //=====================================================================

    [Header("こもり (Lowpass) : 下げるほど高音が消えてこもる / 22000で無効")]
    [Range(MinFrequency, MaxFrequency)]
    public float lowpassCutoff = MaxFrequency;

    [Range(1.0f, 10.0f), Tooltip("上げるとカットする境目にクセが出る")]
    public float lowpassResonance = 1.0f;

    [Header("薄さ (Highpass) : 上げるほど低音が消えてスカスカになる / 10で無効")]
    [Range(MinFrequency, MaxFrequency)]
    public float highpassCutoff = MinFrequency;

    [Range(1.0f, 10.0f)]
    public float highpassResonance = 1.0f;

    [Header("ゆがみ (Distortion) : 0で無効")]
    [Range(0.0f, 1.0f)]
    public float distortion = 0.0f;

    [Header("揺れ (Chorus) : 0で無効")]
    [Range(0.0f, 1.0f)]
    public float wobble = 0.0f;

    [Range(0.0f, 20.0f), Tooltip("揺れの速さ (Hz)")]
    public float wobbleRate = 0.8f;

    [Range(0.0f, 1.0f), Tooltip("揺れの深さ")]
    public float wobbleDepth = 0.05f;

    [Header("やまびこ (Echo) : 0で無効")]
    [Range(0.0f, 1.0f)]
    public float echo = 0.0f;

    [Range(10.0f, 2000.0f), Tooltip("跳ね返りまでの時間 (ミリ秒)")]
    public float echoDelay = 300.0f;

    [Range(0.0f, 1.0f), Tooltip("跳ね返りの残りやすさ")]
    public float echoDecay = 0.4f;

    [Header("響き (Reverb) : 0で無効")]
    [Range(0.0f, 1.0f)]
    public float reverb = 0.0f;

    [Header("音量")]
    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// エフェクトなしの状態に戻す
    /// </summary>
    public void ResetToClean()
    {
        CopyFrom(Clean);
    }

    public void CopyFrom(BgmEffectSettings other)
    {
        lowpassCutoff     = other.lowpassCutoff;
        lowpassResonance  = other.lowpassResonance;
        highpassCutoff    = other.highpassCutoff;
        highpassResonance = other.highpassResonance;
        distortion        = other.distortion;
        wobble            = other.wobble;
        wobbleRate        = other.wobbleRate;
        wobbleDepth       = other.wobbleDepth;
        echo              = other.echo;
        echoDelay         = other.echoDelay;
        echoDecay         = other.echoDecay;
        reverb            = other.reverb;
        volume            = other.volume;
    }

    /// <summary>
    /// 2つの設定を補間して result に書き込む (result は a と同じでもよい)
    /// 周波数は耳の感覚に合わせて対数で補間する
    /// </summary>
    public static void Lerp(BgmEffectSettings a, BgmEffectSettings b, float t, BgmEffectSettings result)
    {
        t = Mathf.Clamp01(t);
        result.lowpassCutoff     = LerpFrequency(a.lowpassCutoff, b.lowpassCutoff, t);
        result.lowpassResonance  = Mathf.Lerp(a.lowpassResonance, b.lowpassResonance, t);
        result.highpassCutoff    = LerpFrequency(a.highpassCutoff, b.highpassCutoff, t);
        result.highpassResonance = Mathf.Lerp(a.highpassResonance, b.highpassResonance, t);
        result.distortion        = Mathf.Lerp(a.distortion, b.distortion, t);
        result.wobble            = Mathf.Lerp(a.wobble, b.wobble, t);
        result.wobbleRate        = Mathf.Lerp(a.wobbleRate, b.wobbleRate, t);
        result.wobbleDepth       = Mathf.Lerp(a.wobbleDepth, b.wobbleDepth, t);
        result.echo              = Mathf.Lerp(a.echo, b.echo, t);
        result.echoDelay         = Mathf.Lerp(a.echoDelay, b.echoDelay, t);
        result.echoDecay         = Mathf.Lerp(a.echoDecay, b.echoDecay, t);
        result.reverb            = Mathf.Lerp(a.reverb, b.reverb, t);
        result.volume            = Mathf.Lerp(a.volume, b.volume, t);
    }

    private static float LerpFrequency(float a, float b, float t)
    {
        a = Mathf.Clamp(a, MinFrequency, MaxFrequency);
        b = Mathf.Clamp(b, MinFrequency, MaxFrequency);
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(a), Mathf.Log(b), t));
    }

    /// <summary>
    /// エフェクトなしの設定 (書き換え禁止)
    /// </summary>
    public static readonly BgmEffectSettings Clean = new BgmEffectSettings();
}
