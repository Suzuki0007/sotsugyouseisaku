/*********************************************************************/
// * \file   LightPreset.cs
// * \brief  光のプリセット：場面の「時間帯」を、全体の光（Global Light 2D）と場面の光の強さで決める
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 光のプリセット（場面の時間帯を 2D の光で決める）
/// </summary>
///
/// ・全体の光（Global Light 2D）の色と強さ、シーンに置いた「場面の光」のグループごとの点け消しと強さを決める
/// ・部品「光を変える」やシーンの既定で選ぶ。画面全体の色の加工はしない（それは色調）
/// ・明かりに反応する絵（Sprite-Lit-Default）だけが変わる。UI は変わらない
/// ・置き場所は Assets/Resources/Presentation/Light/（セーブから名前で戻すため）
[CreateAssetMenu(menuName = "CT/演出/光のプリセット", fileName = "Light_")]
public class LightPreset : ScriptableObject
{
    [System.Serializable]
    public class SceneLightSetting
    {
        [Label("グループ名")]
        [Tooltip("シーンの「場面の光」に付けたグループ名（月・ランタン・窓 など）")]
        public string group = "";

        [Label("点ける")]
        [Tooltip("オフにするとこのグループの光を消す")]
        public bool on = true;

        [Label("強さ（元の何倍）", 0.0f, 4.0f)]
        [Tooltip("シーンに置いたときの強さに掛ける。1 でそのまま")]
        public float intensity = 1.0f;
    }

    [Header("説明（メモ）")]
    [SerializeField]
    [Tooltip("どんな場面で使うか（メモ。動きには関係ない）")]
    [TextArea(1, 3)]
    private string memo = "";

    [Header("全体の光（Global Light 2D）")]

    [SerializeField, Label("色")]
    [Tooltip("全体の光の色（白＝昼。青みで夜、橙で夕日）")]
    private Color color = Color.white;

    [SerializeField, Label("強さ", 0.0f, 2.0f)]
    [Tooltip("全体の光の強さ（1＝昼。夜 0.45）")]
    private float intensity = 1.0f;

    [Header("場面の光（グループごと）")]

    [SerializeField]
    [Tooltip("書いていないグループは、シーンに置いたときのまま")]
    private List<SceneLightSetting> sceneLights = new List<SceneLightSetting>();

    //=====================================================================
    // 公開
    //=====================================================================

    public string Memo => memo;
    public Color LightColor => color;
    public float Intensity => intensity;

    /// <summary>
    /// グループの強さ（元の何倍か。0 で消す）。書いていなければ -1（置いたときのまま）
    /// </summary>
    public float FactorOf(string group)
    {
        if(string.IsNullOrEmpty(group)) { return -1.0f; }
        for(int i = 0; i < sceneLights.Count; i++)
        {
            SceneLightSetting s = sceneLights[i];
            if(s != null && s.group == group) { return s.on ? s.intensity : 0.0f; }
        }
        return -1.0f;
    }

    public IReadOnlyList<SceneLightSetting> SceneLights => sceneLights;

#if UNITY_EDITOR
    public void EditorSet(string memoText, Color c, float i, List<SceneLightSetting> lights)
    {
        memo = memoText;
        color = c;
        intensity = i;
        sceneLights = lights ?? new List<SceneLightSetting>();
    }
#endif
}
