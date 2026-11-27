/*********************************************************************/
// * \file   ScreenFrameSettings.cs
// * \brief  いつもかける画面の縁（色調に関係なく、マップの画面にいつもかける縁のぼかし・上下のグラデーション・周辺の暗さ）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// いつもかける画面の縁（Assets/Resources/Presentation/ScreenFrame を1つだけ置く）
/// </summary>
///
/// ・色調が何でも（「通常」でも）、マップを映すシーンにはいつもかける：縁をぼかす・角の丸み・上下のグラデーション・周辺を暗くする
/// ・色調のプリセットにも同じ項目があるときは、強い方を使う（色調で強めることはできる。いつもの値より弱くはならない）
/// ・外したいシーンは、シーンの演出の既定の「いつもの画面の縁を使う」をオフ。イベントの途中は部品「いつもの画面の縁」
/// ・マップのカメラ（FocusCamera）がないシーン（タイトルなど）にはかからない
[CreateAssetMenu(menuName = "CT/演出/いつもかける画面の縁", fileName = "ScreenFrame")]
public class ScreenFrameSettings : ScriptableObject
{
    [SerializeField, Label("使う")]
    [Tooltip("オフにすると、どのシーンにもかけない（色調の「画面の縁」だけになる）")]
    private bool enabled = true;

    [Header("縁")]

    [SerializeField, Label("縁をぼかす幅", 0.0f, 0.25f)]
    [Tooltip("4:3 の端を帯の色へぼかす幅（画面の高さに対する割合）。0.04 で少し")]
    private float edgeFade = 0.04f;

    [SerializeField, Label("角の丸み", 0.0f, 1.0f)]
    private float edgeRound = 0.5f;

    [Header("上下のグラデーション")]

    [SerializeField, Label("上の強さ", 0.0f, 1.0f)]
    [Tooltip("画面の上から色をかぶせる強さ。0 でなし")]
    private float topShade = 0.0f;

    [SerializeField, Label("上の色")]
    private Color topShadeColor = Color.black;

    [SerializeField, Label("上の高さ", 0.05f, 1.0f)]
    private float topShadeHeight = 0.45f;

    [SerializeField, Label("下の強さ", 0.0f, 1.0f)]
    [Tooltip("画面の下から色をかぶせる強さ。0 でなし")]
    private float bottomShade = 0.0f;

    [SerializeField, Label("下の色")]
    private Color bottomShadeColor = Color.white;

    [SerializeField, Label("下の高さ", 0.05f, 1.0f)]
    private float bottomShadeHeight = 0.35f;

    [Header("周辺を暗くする（ビネット）")]

    [SerializeField, Label("強さ", 0.0f, 1.0f)]
    [Tooltip("四隅を暗くする強さ。0.15 で少し、0.3 で中")]
    private float vignette = 0.15f;

    [SerializeField, Label("ぼかし", 0.01f, 1.0f)]
    private float vignetteSmoothness = 0.4f;

    [SerializeField, Label("周辺の色")]
    private Color vignetteColor = Color.black;

    //=====================================================================
    // 公開
    //=====================================================================

    public bool Enabled => enabled;

    private const string ResourcePath = "Presentation/ScreenFrame";
    private static ScreenFrameSettings loaded;
    private static bool searched;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        loaded = null;
        searched = false;
    }

    /// <summary> 設定（なければ null＝いつもの縁はかけない） </summary>
    public static ScreenFrameSettings Current
    {
        get
        {
            if(!searched) { loaded = Resources.Load<ScreenFrameSettings>(ResourcePath); searched = true; }
            return loaded;
        }
    }

    /// <summary>
    /// 色調の値 m に、いつもの縁を重さ w（0〜1）で重ねる。どの項目も強い方を使う（色・形は強い方の値）
    /// </summary>
    public MoodValues Apply(MoodValues m, float w)
    {
        if(!enabled || w <= 0.0001f) { return m; }
        float e = edgeFade * w;
        if(e > m.edgeFade) { m.edgeFade = e; m.edgeRound = edgeRound; }
        float t = topShade * w;
        if(t > m.topShade) { m.topShade = t; m.topShadeColor = topShadeColor; m.topShadeHeight = topShadeHeight; }
        float b = bottomShade * w;
        if(b > m.bottomShade) { m.bottomShade = b; m.bottomShadeColor = bottomShadeColor; m.bottomShadeHeight = bottomShadeHeight; }
        float v = vignette * w;
        if(v > m.vignette) { m.vignette = v; m.vignetteSmoothness = vignetteSmoothness; m.vignetteColor = vignetteColor; }
        return m;
    }

    private void OnValidate()
    {
        if(Application.isPlaying) { PresentationDirector.RefreshMood(); }
    }
}
