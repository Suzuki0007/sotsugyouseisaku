/*********************************************************************/
// * \file   UISound.cs
// * \brief  UI の効果音（選択・決定・戻る・メニュー開閉・セーブ完了）をまとめて鳴らす
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// UI の効果音の種類
/// </summary>
public enum UISoundType
{
    Select,      // 選択を動かした（↑↓←→）
    Decide,      // 決定した
    Cancel,      // 戻った・いいえ・閉じた
    MenuOpen,    // メニューを開いた
    MenuClose,   // メニューを閉じた
    Save,        // セーブが終わった
    Cannot,      // 選べない・できない
}

/// <summary>
/// UI の効果音を鳴らす
/// </summary>
///
/// UISound.Play(UISoundType.Decide) のように使う。音は Assets/Resources/UISounds.asset（UISoundSettings）で決める
/// 画面ごとに別の音にしたいときは UISound.Play(個別のSE, UISoundType.Decide)（個別のSEが空なら共通の音）
public static class UISound
{
    private static UISoundSettings settings;
    private static bool loaded;

    /// <summary>
    /// 共通の UI 効果音を鳴らす
    /// </summary>
    public static void Play(UISoundType type)
    {
        Play(null, type);
    }

    /// <summary>
    /// 個別の SE があればそれを、なければ共通の UI 効果音を鳴らす
    /// </summary>
    public static void Play(SeData overrideSe, UISoundType fallback)
    {
        SeData se = overrideSe != null ? overrideSe : Get(fallback);
        if(se != null) { SeManager.PlaySe(se); }
    }

    /// <summary>
    /// 共通の UI 効果音（設定がなければ null）
    /// </summary>
    public static SeData Get(UISoundType type)
    {
        if(!loaded)
        {
            settings = Resources.Load<UISoundSettings>(UISoundSettings.ResourcePath);
            loaded = true;
        }
        return settings != null ? settings.Get(type) : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        settings = null;
        loaded = false;
    }
}
