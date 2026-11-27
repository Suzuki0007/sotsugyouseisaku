/*********************************************************************/
// * \file   CinemaLookSettings.cs
// * \brief  会話のシネマ演出の見た目（黒帯・黒いぼかし・会話の枠）の設定。全部のシーンで共通
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 会話のシネマ演出の見た目（黒帯の太さ・黒いぼかしの高さと濃さ・会話の枠の大きさと位置）
/// </summary>
///
/// ・`Resources/CinemaLookSettings.asset` を1つ置くと、全部のシーンの会話がこの値を使う（DialogueManager の同じ欄より先）
/// ・Tools > 会話 > シネマ演出の見た目 で調整する（再生中に動かすと、出ている会話にすぐ反映される）
[CreateAssetMenu(menuName = "CT/シネマ演出の見た目", fileName = "CinemaLookSettings")]
public class CinemaLookSettings : ScriptableObject
{
    [Label("黒帯の太さ", 0.0f, 0.3f)]
    [Tooltip("上下それぞれの黒帯の太さ（マップの映る範囲の高さに対する割合）")]
    public float barHeight = 0.10f;

    [Label("黒いぼかしの高さ", 0.0f, 0.5f)]
    [Tooltip("下の端から、黒いぼかしが届く高さ（マップの映る範囲の高さに対する割合）")]
    public float fadeHeight = 0.13f;

    [Label("黒いぼかしの濃さ", 0.0f, 1.0f)]
    [Tooltip("黒いぼかしのいちばん濃い所の濃さ。0 でぼかしなし")]
    public float fadeStrength = 0.8f;

    [Label("会話の枠の大きさ", 0.3f, 1.0f)]
    [Tooltip("会話の枠の大きさ（1 で今までの枠と同じ 1600×260）。見た目はそのまま全体を縮める")]
    public float boxScale = 0.8f;

    [Label("会話の枠の位置（黒帯から上へ）", 0.0f, 600.0f)]
    [Tooltip("下の黒帯の上の端から、会話の枠の下の端までの高さ（1920×1080 の基準で）")]
    public float boxOffset = 70.0f;

    //=====================================================================
    // 読み込み
    //=====================================================================

    private static CinemaLookSettings cached;
    private static bool loaded;

    /// <summary> 設定（Resources にあれば。なければ null） </summary>
    public static CinemaLookSettings Current
    {
        get
        {
            if(!loaded) { cached = Resources.Load<CinemaLookSettings>("CinemaLookSettings"); loaded = true; }
            return cached;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        cached = null;
        loaded = false;
    }
}
