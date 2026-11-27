/*********************************************************************/
// * \file   MapLightSpriteMaker.cs
// * \brief  絵で形を決める明かり（光だまり・窓の光）を作るメニューと、その絵（やわらかい丸・窓枠の型抜き）を作る処理
// *
// * \author
/*********************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 光だまり・窓の光を作る（Hierarchy 右クリック > マップの見た目）
/// </summary>
///
/// ・光だまり：やわらかい丸い光（影を作らない絵の明かり＝軽い）。見せ場を照らす・部屋の明るい所を作る
/// ・窓の光：窓枠の形に切り抜いた光が床に落ちる（型抜き）。斜めに差して、窓の格子の影も見える
/// ・大きさは Transform の Scale、色・明るさは Light 2D の Color・Intensity で変える
/// ・絵は Assets/Images/MapLook/ に自動で作る
public static class MapLightSpriteMaker
{
    public const string PoolPath = MapOverlayMaker.Folder + "/光だまり.png";
    public const string WindowPath = MapOverlayMaker.Folder + "/窓の光.png";
    private const int Size = 128;
    private const float Ppu = 64.0f;   // 1 枚＝2 マス（Scale 1 で 2×2 マス）

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("GameObject/マップの見た目/光だまり（絵の明かり）", false, 45)]
    private static void MenuPool(MenuCommand cmd)
    {
        GameObject go = CreatePool("光だまり", Pivot(), new Color(1.0f, 0.86f, 0.62f), 0.9f, 3.0f);
        Place(go, cmd);
    }

    [MenuItem("GameObject/マップの見た目/窓の光（型抜き）", false, 46)]
    private static void MenuWindow(MenuCommand cmd)
    {
        GameObject go = CreateWindow("窓の光", Pivot(), new Color(1.0f, 0.95f, 0.82f), 0.8f, 1.5f);
        Place(go, cmd);
    }

    [MenuItem("Tools/マップの見た目/光だまり・窓の光の絵を作り直す")]
    private static void MenuRebuild()
    {
        MakeTextures(true);
        Debug.Log("光だまり・窓の光の絵を作り直しました（" + MapOverlayMaker.Folder + "）");
    }

    private static Vector3 Pivot()
    {
        Vector3 p = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        p.z = 0.0f;
        return p;
    }

    private static void Place(GameObject go, MenuCommand cmd)
    {
        if(cmd.context is GameObject parent) { go.transform.SetParent(parent.transform, true); }
        Undo.RegisterCreatedObjectUndo(go, "作成 " + go.name);
        Selection.activeObject = go;
    }

    //=====================================================================
    // 作る（レシピからも使う）
    //=====================================================================

    /// <summary> やわらかい丸い光（scale＝大きさの倍率。1 で 2×2 マス） </summary>
    public static GameObject CreatePool(string name, Vector3 position, Color color, float intensity, float scale)
    {
        MakeTextures(false);
        return CreateSpriteLight(name, position, PoolPath, color, intensity, new Vector3(scale, scale, 1.0f));
    }

    /// <summary> 窓枠の形の光（scale＝大きさの倍率。1 で 2×2 マス） </summary>
    public static GameObject CreateWindow(string name, Vector3 position, Color color, float intensity, float scale)
    {
        MakeTextures(false);
        return CreateSpriteLight(name, position, WindowPath, color, intensity, new Vector3(scale, scale, 1.0f));
    }

    private static GameObject CreateSpriteLight(string name, Vector3 position, string spritePath, Color color, float intensity, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(position.x, position.y, 0.0f);
        go.transform.localScale = scale;
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Sprite;
        l.lightCookieSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        l.color = color;
        l.intensity = intensity;
        l.shadowsEnabled = false;   // 影を作らない＝軽い
        return go;
    }

    //=====================================================================
    // 絵
    //=====================================================================

    /// <summary> 光だまり・窓の光の絵を作る（force でなければ、ないときだけ） </summary>
    public static void MakeTextures(bool force)
    {
        if(!AssetDatabase.IsValidFolder(MapOverlayMaker.Folder)) { AssetDatabase.CreateFolder("Assets/Images", "MapLook"); }
        if(force || AssetDatabase.LoadAssetAtPath<Sprite>(PoolPath) == null) { Write(PoolPath, PoolPixel); }
        if(force || AssetDatabase.LoadAssetAtPath<Sprite>(WindowPath) == null) { Write(WindowPath, WindowPixel); }
    }

    // やわらかい丸：真ん中が明るく、ふちへなめらかに消える
    private static float PoolPixel(float u, float v)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2.0f;
        float a = 1.0f - Mathf.Clamp01(d);
        return a * a * (3.0f - 2.0f * a);
    }

    // 窓の光：左上から差した 2×2 の窓（右へ傾いた四角）。格子の影の線と、ふちのぼかし
    private static float WindowPixel(float u, float v)
    {
        // 上ほど左へずらす（斜めに差す光）
        float x = u - (v - 0.5f) * 0.35f;
        float left = 0.22f, right = 0.78f, bottom = 0.12f, top = 0.88f, soft = 0.04f;
        float inside = Edge(x, left, soft) * (1.0f - Edge(x, right, soft)) * Edge(v, bottom, soft) * (1.0f - Edge(v, top, soft));
        // 格子（縦・横の真ん中に細い影）
        float barX = 1.0f - (1.0f - Edge(Mathf.Abs(x - 0.5f), 0.025f, 0.012f));
        float barY = 1.0f - (1.0f - Edge(Mathf.Abs(v - 0.5f), 0.025f, 0.012f));
        float bars = Mathf.Min(barX, barY);
        // 窓から遠い（下の）ほど少し弱く
        return inside * bars * Mathf.Lerp(0.75f, 1.0f, v);
    }

    private static float Edge(float x, float at, float soft)
    {
        return Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(at - soft, at + soft, x));
    }

    private static void Write(string path, System.Func<float, float, float> pixel)
    {
        var px = new Color32[Size * Size];
        for(int y = 0; y < Size; y++)
        {
            for(int x = 0; x < Size; x++)
            {
                float a = Mathf.Clamp01(pixel((x + 0.5f) / Size, (y + 0.5f) / Size));
                px[y * Size + x] = new Color(a, a, a, a);
            }
        }
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if(ti == null) { return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = Ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }
}
