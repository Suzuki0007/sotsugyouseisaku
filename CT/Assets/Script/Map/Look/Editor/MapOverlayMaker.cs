/*********************************************************************/
// * \file   MapOverlayMaker.cs
// * \brief  地面の色むら・流れる雲の影を作るメニューと、そのための絵（くり返してもつながるもやもや）を作る処理
// *
// * \author
/*********************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 地面の色むら・流れる雲の影を作る（Hierarchy 右クリック > マップの見た目）
/// </summary>
///
/// ・絵は Assets/Images/MapLook/ に自動で作る（上下左右につなげても継ぎ目が出ない、もやもやの絵）
/// ・作る大きさは、シーンのマップの範囲（MapBounds の絵）を覆う大きさ。なければシーンビューの真ん中に 40×30 マス
public static class MapOverlayMaker
{
    public const string Folder = "Assets/Images/MapLook";
    public const string TintPath = Folder + "/地面の色むら.png";
    public const string CloudPath = Folder + "/雲の影.png";

    private const int TexSize = 256;
    private const float TintPpu = 16.0f;   // 1 枚＝16 マス
    private const float CloudPpu = 8.0f;   // 1 枚＝32 マス（CloudShadow のくり返しの幅）

    public const int TintOrder = 2;         // 床（1）より上・壁の面（4）より下（隅の影と同じ）
    public const int CloudOrder = 120;      // キャラ（100）・壁の縁（110・111）より上

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("GameObject/マップの見た目/地面の色むら", false, 43)]
    private static void MenuTint(MenuCommand cmd)
    {
        GameObject go = CreateGroundTint(DefaultArea(), 0.4f);
        Place(go, cmd);
    }

    [MenuItem("GameObject/マップの見た目/流れる雲の影", false, 44)]
    private static void MenuCloud(MenuCommand cmd)
    {
        GameObject go = CreateCloudShadow(DefaultArea(), 0.2f);
        Place(go, cmd);
    }

    [MenuItem("Tools/マップの見た目/色むら・雲の影の絵を作り直す")]
    private static void MenuRebuildTextures()
    {
        MakeTextures(true);
        Debug.Log("地面の色むら・雲の影の絵を作り直しました（" + Folder + "）");
    }

    private static void Place(GameObject go, MenuCommand cmd)
    {
        if(cmd.context is GameObject parent) { go.transform.SetParent(parent.transform, true); }
        Undo.RegisterCreatedObjectUndo(go, "作成 " + go.name);
        Selection.activeObject = go;
    }

    /// <summary> シーンの MapBounds の絵の範囲（なければシーンビューの真ん中に 40×30） </summary>
    private static Rect DefaultArea()
    {
        MapBounds mb = Object.FindAnyObjectByType<MapBounds>();
        if(mb != null)
        {
            var sr = mb.GetComponent<SpriteRenderer>();
            if(sr != null) { Bounds b = sr.bounds; return new Rect(b.min.x, b.min.y, b.size.x, b.size.y); }
        }
        Vector3 p = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        return new Rect(p.x - 20.0f, p.y - 15.0f, 40.0f, 30.0f);
    }

    //=====================================================================
    // 作る（レシピからも使う）
    //=====================================================================

    /// <summary>
    /// 地面の色むらを作る（area を覆う。strength＝強さ）
    /// </summary>
    public static GameObject CreateGroundTint(Rect area, float strength)
    {
        MakeTextures(false);
        var go = new GameObject("地面の色むら");
        go.transform.position = new Vector3(area.center.x, area.center.y, 0.0f);
        SpriteRenderer sr = AddTiled(go, TintPath, area.size, TintOrder);
        sr.color = new Color(1.0f, 1.0f, 1.0f, strength);
        var t = go.AddComponent<GroundTint>();
        SetFloat(t, "strength", strength);
        return go;
    }

    /// <summary>
    /// 流れる雲の影を作る（area を覆い、流れてもすき間が出ないよう、くり返しの幅の分だけ大きく並べる）
    /// </summary>
    public static GameObject CreateCloudShadow(Rect area, float strength)
    {
        MakeTextures(false);
        float period = TexSize / CloudPpu;
        var go = new GameObject("流れる雲の影");
        go.transform.position = new Vector3(area.center.x, area.center.y, 0.0f);
        SpriteRenderer sr = AddTiled(go, CloudPath, area.size + Vector2.one * (period * 2.0f), CloudOrder);
        sr.color = new Color(1.0f, 1.0f, 1.0f, strength);
        var c = go.AddComponent<CloudShadow>();
        SetFloat(c, "strength", strength);
        SetFloat(c, "period", period);
        return go;
    }

    private static SpriteRenderer AddTiled(GameObject go, string path, Vector2 size, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingOrder = order;
        var lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if(lit != null) { sr.sharedMaterial = lit; }
        return sr;
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if(p != null) { p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    //=====================================================================
    // 絵（くり返してもつながるもやもや）
    //=====================================================================

    /// <summary>
    /// 色むら・雲の影の絵を作る（force でなければ、ないときだけ）
    /// </summary>
    public static void MakeTextures(bool force)
    {
        if(!AssetDatabase.IsValidFolder(Folder)) { AssetDatabase.CreateFolder("Assets/Images", "MapLook"); }
        if(force || AssetDatabase.LoadAssetAtPath<Sprite>(TintPath) == null) { WriteTint(); Import(TintPath, TintPpu); }
        if(force || AssetDatabase.LoadAssetAtPath<Sprite>(CloudPath) == null) { WriteCloud(); Import(CloudPath, CloudPpu); }
    }

    // 色むら：もやもやの値が真ん中より低い所は黒く、高い所は暖かい白に（どちらも透明に近い所からなめらかに）
    private static void WriteTint()
    {
        var px = new Color32[TexSize * TexSize];
        var warm = new Color(1.0f, 0.93f, 0.8f);
        for(int y = 0; y < TexSize; y++)
        {
            for(int x = 0; x < TexSize; x++)
            {
                float v = Fbm(x, y, 4, 4, 11);
                float d = Mathf.Clamp((v - 0.5f) * 2.6f, -1.0f, 1.0f);
                // 明るい側は弱く（11-27 段階 4：暗い部屋の中では明るい所が白いもやに見えた）
                Color c = d < 0.0f ? new Color(0.0f, 0.0f, 0.0f, -d) : new Color(warm.r, warm.g, warm.b, d * 0.3f);
                px[y * TexSize + x] = c;
            }
        }
        Save(TintPath, px);
    }

    // 雲の影：もやもやの高い所だけを、ふちをぼかした黒い固まりに
    private static void WriteCloud()
    {
        var px = new Color32[TexSize * TexSize];
        for(int y = 0; y < TexSize; y++)
        {
            for(int x = 0; x < TexSize; x++)
            {
                // 格子 5 ＝雲 1 つが 5〜7 マスくらい（画面より小さい。大きいと画面全体が少し暗いだけに見えた）
                float v = Fbm(x, y, 5, 4, 29);
                float a = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(0.48f, 0.62f, v));
                px[y * TexSize + x] = new Color(0.0f, 0.0f, 0.0f, a);
            }
        }
        Save(CloudPath, px);
    }

    private static void Save(string path, Color32[] px)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void Import(string path, float ppu)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if(ti == null) { return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;   // くり返し並べる（Tiled）には Full Rect が要る
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();
    }

    // くり返しの周期をもつ値のノイズを重ねる（period＝1 枚の中の格子の数。上下左右がつながる）
    private static float Fbm(int x, int y, int period, int octaves, int seed)
    {
        float sum = 0.0f, amp = 0.5f, norm = 0.0f;
        int p = period;
        for(int o = 0; o < octaves; o++)
        {
            sum += amp * ValueNoise((float)x / TexSize * p, (float)y / TexSize * p, p, seed + o * 101);
            norm += amp;
            amp *= 0.5f;
            p *= 2;
        }
        return sum / norm;
    }

    private static float ValueNoise(float fx, float fy, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
        float tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3.0f - 2.0f * tx);
        ty = ty * ty * (3.0f - 2.0f * ty);
        float a = Lattice(x0, y0, period, seed), b = Lattice(x0 + 1, y0, period, seed);
        float c = Lattice(x0, y0 + 1, period, seed), d = Lattice(x0 + 1, y0 + 1, period, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    private static float Lattice(int x, int y, int period, int seed)
    {
        x = ((x % period) + period) % period;
        y = ((y % period) + period) % period;
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
