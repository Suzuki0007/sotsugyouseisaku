/*********************************************************************/
// * \file   MapAirMaker.cs
// * \brief  空気を動かす物（床をはう霧・光の中のちり）を作るメニューと、霧の絵を作る処理
// *
// * \author
/*********************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 床をはう霧・光の中のちりを作る（Hierarchy 右クリック > マップの見た目）
/// </summary>
///
/// ・床をはう霧：ふちのぼけた霧の絵（GroundFog）。2〜4 枚を少しずつずらして重ねる
/// ・光の中のちり：小さな光の粒がゆっくり舞う（Particle System。光だまり・窓の光の中に置く）
public static class MapAirMaker
{
    public const string FogPath = MapOverlayMaker.Folder + "/床をはう霧.png";
    private const int FogW = 256, FogH = 128;
    private const float FogPpu = 32.0f;   // 1 枚＝8×4 マス（Scale 1）
    public const int FogOrder = 6;         // 床・平らな小物（5）より上、キャラ（100）より下
    public const int DustOrder = 105;      // キャラより手前（光の粒は手前にも舞う）

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("GameObject/マップの見た目/床をはう霧", false, 47)]
    private static void MenuFog(MenuCommand cmd)
    {
        GameObject go = CreateFog("床をはう霧", Pivot(), 1.0f, 0.28f);
        Place(go, cmd);
    }

    [MenuItem("GameObject/マップの見た目/光の中のちり", false, 48)]
    private static void MenuDust(MenuCommand cmd)
    {
        GameObject go = CreateDust("光の中のちり", Pivot(), new Vector2(3.0f, 3.0f), new Color(1.0f, 0.9f, 0.7f, 0.7f));
        Place(go, cmd);
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

    /// <summary> 床をはう霧（scale＝大きさの倍率。1 で 8×4 マス） </summary>
    public static GameObject CreateFog(string name, Vector3 position, float scale, float strength)
    {
        if(NeedsFog(false)) { WriteFog(); }
        var go = new GameObject(name);
        go.transform.position = new Vector3(position.x, position.y, 0.0f);
        go.transform.localScale = new Vector3(scale, scale, 1.0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FogPath);
        sr.sortingOrder = FogOrder;
        sr.color = new Color(0.85f, 0.9f, 1.0f, strength);
        var lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if(lit != null) { sr.sharedMaterial = lit; }
        var f = go.AddComponent<GroundFog>();
        var so = new SerializedObject(f);
        so.FindProperty("strength").floatValue = strength;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    /// <summary> 光の中のちり（size＝舞う範囲〔マス〕、color＝粒の色と濃さ） </summary>
    public static GameObject CreateDust(string name, Vector3 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(position.x, position.y, 0.0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;                       // 始まったときから舞っている
        main.duration = 5.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5.0f, 8.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);   // 2〜4 画素
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y * 3.0f), 8, 60);
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = main.maxParticles / 6.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(size.x, size.y, 0.1f);

        // ふわっと出て、ふわっと消える
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[]{ new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                  new[]{ new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.3f), new GradientAlphaKey(1.0f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // ゆらゆら（ノイズ）
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.2f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sortingOrder = DustOrder;
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Presentation/Materials/Mat_Weather_Unlit.mat");
        if(mat != null) { r.sharedMaterial = mat; }

        ps.Play();
        return go;
    }

    //=====================================================================
    // 霧の絵：横長のもやもや。ふちはだ円に消える
    //=====================================================================

    private static bool NeedsFog(bool force)
    {
        if(!AssetDatabase.IsValidFolder(MapOverlayMaker.Folder)) { AssetDatabase.CreateFolder("Assets/Images", "MapLook"); }
        return force || AssetDatabase.LoadAssetAtPath<Sprite>(FogPath) == null;
    }

    [MenuItem("Tools/マップの見た目/床をはう霧の絵を作り直す")]
    private static void MenuRebuild()
    {
        NeedsFog(true);
        WriteFog();
        Debug.Log("床をはう霧の絵を作り直しました（" + FogPath + "）");
    }

    private static void WriteFog()
    {
        var px = new Color32[FogW * FogH];
        for(int y = 0; y < FogH; y++)
        {
            for(int x = 0; x < FogW; x++)
            {
                float u = (x + 0.5f) / FogW, v = (y + 0.5f) / FogH;
                // 横に長いもやもや（横の周波数を低く）
                float n = Mathf.PerlinNoise(u * 3.0f + 11.3f, v * 5.0f + 4.1f) * 0.65f + Mathf.PerlinNoise(u * 7.0f + 2.7f, v * 11.0f + 9.9f) * 0.35f;
                float dx = (u - 0.5f) * 2.0f, dy = (v - 0.5f) * 2.0f;
                float edge = Mathf.Clamp01(1.0f - (dx * dx + dy * dy));
                edge = edge * edge;
                float a = Mathf.Clamp01((n - 0.3f) * 1.8f) * edge;
                px[y * FogW + x] = new Color(1.0f, 1.0f, 1.0f, a);
            }
        }
        var tex = new Texture2D(FogW, FogH, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(FogPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(FogPath, ImportAssetOptions.ForceSynchronousImport);

        var ti = (TextureImporter)AssetImporter.GetAtPath(FogPath);
        if(ti == null) { return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = FogPpu;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }
}
