/*********************************************************************/
// * \file   SubtitleStyle.cs
// * \brief  映画の字幕（中央・大きな太字）の見た目の設定
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 映画の字幕の見た目（Assets/Resources/Presentation/SubtitleStyle を1つだけ置く）
/// </summary>
///
/// ・部品「字幕」の「字幕の形」が「映画」のときの、字の太さ・大きさ・位置・縁取り・影を決める
/// ・飾らず文字だけを、画面の中央より少し下に大きく出す（映画の字幕のように）
/// ・白い画面でも読めるように、濃い影と薄い縁取りを付ける（白ベースの場面では外さない）
[CreateAssetMenu(menuName = "CT/演出/映画の字幕の見た目", fileName = "SubtitleStyle")]
public class SubtitleStyle : ScriptableObject
{
    [Header("字")]

    [SerializeField, Label("フォント")]
    [Tooltip("字幕のフォント（ttf・otf）。太字の Noto Sans JP Bold が最初の値。空なら会話と同じフォントを太字にして使う")]
    private Font font;

    [SerializeField, Label("字の大きさ", 20.0f, 120.0f)]
    [Tooltip("1920×1080 の画面での字の大きさ。56 くらいが映画の字幕の大きさ（小さすぎると読めない。46 より小さくしない）")]
    private float fontSize = 56.0f;

    [SerializeField, Label("字の間隔", -10.0f, 30.0f)]
    [Tooltip("字と字の間。少し空けると落ち着いて読める")]
    private float characterSpacing = 4.0f;

    [SerializeField, Label("行の間隔", -20.0f, 60.0f)]
    [Tooltip("2行になったときの行と行の間")]
    private float lineSpacing = 10.0f;

    [SerializeField, Label("文字の色")]
    [Tooltip("真っ白より少し温かい白が最初の値")]
    private Color textColor = new Color(0.97f, 0.95f, 0.9f, 1.0f);

    [Header("置き場所（マップの映る 4:3 の範囲に対する割合）")]

    [SerializeField, Label("高さ", 0.05f, 0.5f)]
    [Tooltip("字幕の真ん中の高さ（下の端から）。0.2 で中央より少し下")]
    private float height = 0.2f;

    [SerializeField, Label("1行の幅", 0.3f, 1.0f)]
    [Tooltip("1行に使う幅。これより長い文は折り返す（2行まで）")]
    private float width = 0.78f;

    [Header("読みやすさ（白い画面でも読めるように）")]

    [SerializeField, Label("縁取りの太さ", 0.0f, 0.4f)]
    [Tooltip("字のまわりの縁取り。0.1〜0.15 で細く")]
    private float outlineWidth = 0.12f;

    [SerializeField, Label("縁取りの色")]
    [Tooltip("ふつうは黒に近い色")]
    private Color outlineColor = new Color(0.05f, 0.05f, 0.08f, 1.0f);

    [SerializeField, Label("影の濃さ", 0.0f, 1.0f)]
    [Tooltip("字の後ろのぼけた影の濃さ。白い画面ほど大事")]
    private float shadowStrength = 0.9f;

    [SerializeField, Label("影のぼかし", 0.0f, 1.0f)]
    [Tooltip("影の柔らかさ")]
    private float shadowSoftness = 0.55f;

    [SerializeField, Label("影の広がり", 0.0f, 1.0f)]
    [Tooltip("影を字より太らせる量")]
    private float shadowDilate = 0.4f;

    //=====================================================================
    // 公開
    //=====================================================================

    public float FontSize => fontSize;
    public float CharacterSpacing => characterSpacing;
    public float LineSpacing => lineSpacing;
    public Color TextColor => textColor;
    public float Height => height;
    public float Width => width;

    private const string ResourcePath = "Presentation/SubtitleStyle";

    private static SubtitleStyle loaded;
    private static TMP_FontAsset fontAsset;
    private static Font fontAssetSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        loaded = null;
        fontAsset = null;
        fontAssetSource = null;
    }

    /// <summary> 設定（なければ最初の値の物を作る） </summary>
    public static SubtitleStyle Current
    {
        get
        {
            if(loaded == null) { loaded = Resources.Load<SubtitleStyle>(ResourcePath); }
            if(loaded == null) { loaded = CreateInstance<SubtitleStyle>(); loaded.hideFlags = HideFlags.DontSave; }
            return loaded;
        }
    }

    /// <summary>
    /// 字幕のフォント（実行中にフォントから作る。足りない字は会話のフォントで出す）
    /// フォントのアセットを作って保存しないのは、再生のたびにアセットが書き換わらないようにするため
    /// </summary>
    public TMP_FontAsset FontAsset()
    {
        if(font == null) { return null; }
        if(fontAsset != null && fontAssetSource == font) { return fontAsset; }
        fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        fontAssetSource = font;
        if(fontAsset != null)
        {
            fontAsset.name = font.name + "（字幕・実行中）";
            if(TMP_Settings.defaultFontAsset != null)
            {
                fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            }
        }
        return fontAsset;
    }

    /// <summary> 縁取りと影を入れたマテリアルを作る（呼んだ側が持つ・消す） </summary>
    public Material MakeMaterial(Material baseMaterial)
    {
        if(baseMaterial == null) { return null; }
        var mat = new Material(baseMaterial) { name = "映画の字幕（実行中）" };
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
        if(shadowStrength > 0.001f)
        {
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.0f, 0.0f, 0.0f, shadowStrength));
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, shadowSoftness);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, shadowDilate);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.3f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.3f);
        }
        else
        {
            mat.DisableKeyword(ShaderUtilities.Keyword_Underlay);
        }
        ShaderUtilities.UpdateShaderRatios(mat);
        return mat;
    }

#if UNITY_EDITOR
    /// <summary> エディタの道具から：最初のフォントを入れる </summary>
    public void EditorSetFont(Font f)
    {
        font = f;
    }
#endif
}
