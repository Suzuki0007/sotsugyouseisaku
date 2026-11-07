/*********************************************************************/
// * \file   SpritePreviewGUI.cs
// * \brief  Inspector でスプライトをプレビュー表示する共通処理
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector 内のプレビュー描画 (EffectDataEditor / SpriteAnimDataEditor で共通)
/// </summary>
public static class SpritePreviewGUI
{
    public static readonly Color Background = new Color(0.16f, 0.16f, 0.16f);

    /// <summary>
    /// プレビュー用の枠を確保して、背景を塗る
    /// </summary>
    public static Rect BeginArea(float height)
    {
        Rect rect = GUILayoutUtility.GetRect(10.0f, height, GUILayout.ExpandWidth(true));
        rect = EditorGUI.IndentedRect(rect);
        EditorGUI.DrawRect(rect, Background);
        return rect;
    }

    /// <summary>
    /// スプライトを縦横比を保って枠の中に描く
    /// </summary>
    public static void DrawSprite(Rect area, Sprite sprite, Color tint, bool flip)
    {
        if(sprite == null) { return; }
        Texture2D tex = sprite.texture;
        if(tex == null) { return; }

        Rect sr = sprite.rect;
        Rect uv = new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height);
        if(flip) { uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height); }

        Color prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(Fit(area, sr.width, sr.height), tex, uv, true);
        GUI.color = prev;
    }

    /// <summary>
    /// 縦横比を保って枠に収める
    /// </summary>
    public static Rect Fit(Rect area, float w, float h)
    {
        area = new Rect(area.x + 6, area.y + 6, area.width - 12, area.height - 12);
        float s = Mathf.Min(area.width / Mathf.Max(1.0f, w), area.height / Mathf.Max(1.0f, h));
        // 小さなドット絵はくっきり大きく、大きな画像は縮めて表示
        float rw = w * s, rh = h * s;
        return new Rect(area.center.x - rw * 0.5f, area.center.y - rh * 0.5f, rw, rh);
    }

    public static void CenterLabel(Rect rect, string text)
    {
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };
        GUI.Label(rect, text, style);
    }

    /// <summary>
    /// 枠の左上に小さな文字を出す (「3 / 8」など)
    /// </summary>
    public static void CornerLabel(Rect rect, string text)
    {
        GUI.Label(new Rect(rect.x + 4, rect.y + 2, rect.width - 8, 16), text, EditorStyles.whiteMiniLabel);
    }
}
