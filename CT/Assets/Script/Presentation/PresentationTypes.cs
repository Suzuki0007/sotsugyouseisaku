/*********************************************************************/
// * \file   PresentationTypes.cs
// * \brief  演出の基盤（色調・光・天候・回想）で共通に使う型
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary> 変わり方 </summary>
public enum PresentationCurve
{
    [InspectorName("一定の速さ（直線）")]
    Linear = 0,

    [InspectorName("なめらか（ゆっくり始まりゆっくり終わる）")]
    Smooth = 1,
}

/// <summary>
/// 演出の層（上の層に値があれば、そちらが勝つ）
/// </summary>
///
/// ・色調・光：シーンの既定 ＜ 範囲 ＜ イベント ＜ 回想
/// ・天候：シーンの既定 ＜ イベント ＜ 範囲（室内に入ると止む） ＜ 回想
public enum PresentationLayer
{
    Base = 0,
    Area = 1,
    Event = 2,
    Flashback = 3,
}

/// <summary> 回想の入り方・戻り方 </summary>
public enum FlashbackTransition
{
    [InspectorName("色が抜けてから差す（おすすめ）")]
    GrayThenTint = 0,

    [InspectorName("ゆっくり溶ける")]
    Dissolve = 1,

    [InspectorName("すぐ")]
    Instant = 2,
}

/// <summary> 補間の計算 </summary>
public static class PresentationEase
{
    public static float Apply(PresentationCurve curve, float t)
    {
        t = Mathf.Clamp01(t);
        return curve == PresentationCurve.Smooth ? t * t * (3.0f - 2.0f * t) : t;
    }

    /// <summary> 品質「軽い」か（重い効果を外す・粒を減らす） </summary>
    public static bool IsLowQuality => QualitySettings.GetQualityLevel() == 0;
}
