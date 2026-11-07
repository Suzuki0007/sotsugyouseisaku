/*********************************************************************/
// * \file   BgmProgress.cs
// * \brief  BGMエフェクト用のゲーム進行度
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// BGMエフェクトの基準になるゲーム進行度 (0〜1)
/// 0 = 序盤 (崩れた音) / 1 = 終盤 (整った音)  ※中身は BgmEffectProfile で自由に決める
///
/// 例: BgmProgress.SetAtLeast(0.5f);  // ステージ2クリア時など
/// </summary>
public static class BgmProgress
{
    public static float Value { get; private set; }

    public static event System.Action<float> OnChanged;

    /// <summary>
    /// 進行度を設定する
    /// </summary>
    public static void Set(float value)
    {
        value = Mathf.Clamp01(value);
        if(Mathf.Approximately(value, Value)) { return; }

        Value = value;
        OnChanged?.Invoke(Value);
    }

    /// <summary>
    /// 進行度を加算する
    /// </summary>
    public static void Add(float amount)
    {
        Set(Value + amount);
    }

    /// <summary>
    /// 今より大きいときだけ設定する (戻ってきても後退しない)
    /// </summary>
    public static void SetAtLeast(float value)
    {
        if(value > Value) { Set(value); }
    }
}
