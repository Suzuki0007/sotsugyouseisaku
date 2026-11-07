/*********************************************************************/
// * \file   BgmEffectProfile.cs
// * \brief  進行度ごとのBGMエフェクト設定
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 進行度 (0〜1) ごとのエフェクト設定をまとめたアセット
/// キーとキーの間の進行度では、前後のキーの設定を補間する
///
/// 作り方 : Projectウィンドウで右クリック → Create → Audio → BGM Effect Profile
/// </summary>
[CreateAssetMenu(menuName = "Audio/BGM Effect Profile", fileName = "BgmEffectProfile")]
public class BgmEffectProfile : ScriptableObject
{
    //=====================================================================
    // 型
    //=====================================================================

    [System.Serializable]
    public class Key
    {
        [Tooltip("メモ用の名前 (例: 序盤)")]
        public string label;

        [Range(0.0f, 1.0f), Tooltip("この設定になる進行度")]
        public float progress;

        public BgmEffectSettings settings = new BgmEffectSettings();
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("進行度ごとの設定 (並び順は自由)")]
    private List<Key> keys = new List<Key>();

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 進行度に応じたエフェクトを求めて result に書き込む
    /// </summary>
    public void Evaluate(float progress, BgmEffectSettings result)
    {
        Key lower = null;// progress 以下で最も大きいキー
        Key upper = null;// progress 以上で最も小さいキー

        foreach(Key key in keys)
        {
            if(key == null || key.settings == null) { continue; }

            if(key.progress <= progress && (lower == null || key.progress > lower.progress)) { lower = key; }
            if(key.progress >= progress && (upper == null || key.progress < upper.progress)) { upper = key; }
        }

        if(lower == null && upper == null) { result.ResetToClean(); return; }
        if(lower == null) { result.CopyFrom(upper.settings); return; }
        if(upper == null || upper == lower) { result.CopyFrom(lower.settings); return; }

        float t = Mathf.InverseLerp(lower.progress, upper.progress, progress);
        BgmEffectSettings.Lerp(lower.settings, upper.settings, t, result);
    }
}
