/*********************************************************************/
// * \file   EffectSampleSetup.cs
// * \brief  動作確認用の仮エフェクトを作って、弾・敵・攻撃に割り当てるエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 動作確認用の仮エフェクトを作成して割り当てる
///
///   メニュー : Tools → Effect → サンプルエフェクトを作成して割り当てる
///   (このPCでまだ一度も作っていなければ、コンパイル後に1回だけ自動で実行される)
///
/// 作るもの:
///   画像        Assets/Images/Effect/Sample/  (コードで描いた仮の絵)
///   EffectData  Assets/Data/Effect/Sample/
///
/// 割り当て先 (すでに何か入っている欄は上書きしない):
///   PlayerBullet / EnemyBullet の当たった・壁・消えたエフェクト
///   Enemy の死亡・発射エフェクト、EffectTrigger (出現したとき)
///   Player_Sigle の発射エフェクト (SigleShooter が付いている場合)
///   シーン内の AreaAttack の攻撃・ヒットエフェクト
///
/// 本番のエフェクトに差し替えるときは、各欄に別の EffectData を入れ直すだけでよい
/// サンプルが不要になったら、上の2つのフォルダを削除する
/// </summary>
[InitializeOnLoad]
public static class EffectSampleSetup
{
    private const string ImageRoot = "Assets/Images/Effect/Sample";
    private const string DataRoot  = "Assets/Data/Effect/Sample";
    private const string MenuPath  = "Tools/Effect/サンプルエフェクトを作成して割り当てる";

    private static string PrefKey => "EffectSampleSetup.Done." + Application.dataPath;

    //=====================================================================
    // 自動実行 (このPCで1回だけ)
    //=====================================================================

    static EffectSampleSetup()
    {
        EditorApplication.delayCall += TryAutoRun;
    }

    private static void TryAutoRun()
    {
        if(EditorPrefs.GetBool(PrefKey, false)) { return; }
        if(AssetDatabase.IsValidFolder(DataRoot)) { EditorPrefs.SetBool(PrefKey, true); return; }

        // Playモード中・コンパイル中は後回し
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoRun;
            return;
        }

        EditorPrefs.SetBool(PrefKey, true);
        Run();
    }

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem(MenuPath)]
    private static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playモード中は作成できません。Playを止めてから実行してください");
            return;
        }

        var log = new List<string>();

        try
        {
            AssetDatabase.DisallowAutoRefresh();
            Dictionary<string, EffectData> fx = CreateAllEffects();
            AssignToPrefabs(fx, log);
            AssignToScenes(fx, log);
        }
        finally
        {
            AssetDatabase.AllowAutoRefresh();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("【エフェクト】サンプルエフェクトを作成しました。Playして敵を撃つと確認できます\n" + string.Join("\n", log));
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(DataRoot));
    }

    //=====================================================================
    // エフェクトの作成
    //=====================================================================

    private static Dictionary<string, EffectData> CreateAllEffects()
    {
        EnsureFolder(ImageRoot);
        EnsureFolder(DataRoot);

        var white  = new Color(1.0f, 1.0f, 1.0f);
        var yellow = new Color(1.0f, 0.9f, 0.35f);
        var red    = new Color(1.0f, 0.3f, 0.3f);
        var gray   = new Color(0.75f, 0.72f, 0.68f);
        var cyan   = new Color(0.45f, 0.95f, 1.0f);
        var blue   = new Color(0.6f, 0.85f, 1.0f);
        var purple = new Color(0.8f, 0.55f, 1.0f);

        var result = new Dictionary<string, EffectData>();

        result["Hit"] = Make("Hit", 6, 64, 24.0f, 1.0f, true,
            (c, t) => DrawHit(c, t, yellow, white));

        result["HitPlayer"] = Make("HitPlayer", 6, 64, 24.0f, 1.0f, true,
            (c, t) => DrawHit(c, t, red, white));

        result["Wall"] = Make("Wall", 5, 64, 16.0f, 0.8f, true,
            (c, t) => DrawPuff(c, t, gray));

        result["Vanish"] = Make("Vanish", 5, 64, 20.0f, 0.7f, false,
            (c, t) => DrawRing(c, t, cyan, 0));

        result["Muzzle"] = Make("Muzzle", 4, 64, 24.0f, 0.8f, false,
            (c, t) => DrawMuzzle(c, t, yellow, white));

        result["Explosion"] = Make("Explosion", 8, 96, 14.0f, 1.3f, true,
            DrawExplosion);

        // 128px (PPU100で1.28) の輪が最大半径 0.6 → 2.5倍で AreaAttack の半径 1.5 くらいになる
        result["AttackRing"] = Make("AttackRing", 6, 128, 24.0f, 2.5f, false,
            (c, t) => DrawAttackRing(c, t, blue));

        result["Spawn"] = Make("Spawn", 7, 64, 16.0f, 1.2f, false,
            (c, t) => DrawRing(c, t, purple, 6));

        return result;
    }

    /// <summary>
    /// 連番画像を描いて保存し、EffectData を作る
    /// </summary>
    private static EffectData Make(string name, int frameCount, int size, float fps, float scale, bool randomRotation,
                                   System.Action<Canvas, float> draw)
    {
        string folder = $"{ImageRoot}/{name}";
        EnsureFolder(folder);

        var sprites = new List<Sprite>();
        for(int i = 0; i < frameCount; i++)
        {
            EditorUtility.DisplayProgressBar("サンプルエフェクトを作成中", $"{name} ({i + 1}/{frameCount})", 0.0f);

            float t = frameCount <= 1 ? 0.0f : i / (float)(frameCount - 1);
            var canvas = new Canvas(size);
            draw(canvas, t);

            string path = $"{folder}/{name}_{i:00}.png";
            File.WriteAllBytes(path, canvas.ToPng());
            sprites.Add(ImportAsSprite(path));
        }

        string dataPath = $"{DataRoot}/Fx_Sample_{name}.asset";
        EffectData data = AssetDatabase.LoadAssetAtPath<EffectData>(dataPath);
        if(data == null)
        {
            data = ScriptableObject.CreateInstance<EffectData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        var so = new SerializedObject(data);
        so.FindProperty("displayMode").enumValueIndex = (int)EffectDisplayMode.SpriteFrames;
        SerializedProperty frames = so.FindProperty("frames");
        frames.arraySize = sprites.Count;
        for(int i = 0; i < sprites.Count; i++) { frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i]; }
        so.FindProperty("fps").floatValue = fps;
        so.FindProperty("scale").floatValue = scale;
        so.FindProperty("randomRotation").boolValue = randomRotation;
        so.FindProperty("lifetime").enumValueIndex = (int)EffectLifetime.Auto;
        so.FindProperty("overrideSorting").boolValue = true;
        so.FindProperty("sortingOrder").intValue = 200;
        so.FindProperty("preloadCount").intValue = 4;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);

        return data;
    }

    private static Sprite ImportAsSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100.0f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    //=====================================================================
    // 割り当て (Prefab)
    //=====================================================================

    private static void AssignToPrefabs(Dictionary<string, EffectData> fx, List<string> log)
    {
        EditPrefab("Assets/Prefab/Bullet/PlayerBullet.prefab", log, root =>
        {
            SetIfEmpty(root.GetComponent<PlayerBullet>(), "hitEffect", fx["Hit"], log);
            SetIfEmpty(root.GetComponent<PlayerBullet>(), "obstacleHitEffect", fx["Wall"], log);
            SetIfEmpty(root.GetComponent<PlayerBullet>(), "vanishEffect", fx["Vanish"], log);
        });

        EditPrefab("Assets/Prefab/Bullet/EnemyBullet.prefab", log, root =>
        {
            SetIfEmpty(root.GetComponent<EnemyBullet>(), "hitEffect", fx["HitPlayer"], log);
            SetIfEmpty(root.GetComponent<EnemyBullet>(), "obstacleHitEffect", fx["Wall"], log);
            SetIfEmpty(root.GetComponent<EnemyBullet>(), "vanishEffect", fx["Vanish"], log);
        });

        EditPrefab("Assets/Prefab/Enemies/Enemy.prefab", log, root =>
        {
            SetIfEmpty(root.GetComponentInChildren<EnemyDead>(true), "deathEffect", fx["Explosion"], log);
            SetIfEmpty(root.GetComponentInChildren<EnemyShooter>(true), "shotEffect", fx["Muzzle"], log);

            // EffectTrigger の使い方の見本として「出現したとき」のエフェクトを付ける
            if(root.GetComponent<EffectTrigger>() == null)
            {
                var trigger = root.AddComponent<EffectTrigger>();
                var so = new SerializedObject(trigger);
                SerializedProperty entries = so.FindProperty("entries");
                entries.arraySize = 1;
                SerializedProperty e = entries.GetArrayElementAtIndex(0);
                e.FindPropertyRelative("name").stringValue = "登場";
                e.FindPropertyRelative("timing").enumValueIndex = (int)EffectTiming.OnStart;
                e.FindPropertyRelative("effect").objectReferenceValue = fx["Spawn"];
                so.ApplyModifiedPropertiesWithoutUndo();
                log.Add("  Enemy : EffectTrigger (出現したとき → Fx_Sample_Spawn) を追加");
            }
        });

        EditPrefab("Assets/Prefab/Players/Player_Sigle.prefab", log, root =>
        {
            SigleShooter shooter = root.GetComponentInChildren<SigleShooter>(true);
            if(shooter == null)
            {
                log.Add("  Player_Sigle : SigleShooter が付いていないためスキップ (付けたら Shot Effect に Fx_Sample_Muzzle を入れてください)");
                return;
            }
            SetIfEmpty(shooter, "shotEffect", fx["Muzzle"], log);
        });
    }

    private static void EditPrefab(string path, List<string> log, System.Action<GameObject> edit)
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            log.Add($"  {path} が見つからないためスキップ");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    //=====================================================================
    // 割り当て (シーン内の AreaAttack)
    //=====================================================================

    private static void AssignToScenes(Dictionary<string, EffectData> fx, List<string> log)
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if(!File.ReadAllText(path).Contains(GetScriptGuid<AreaAttack>())) { continue; }

            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            bool wasDirty = wasLoaded && scene.isDirty;

            if(!wasLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }

            int count = 0;
            foreach(GameObject go in scene.GetRootGameObjects())
            {
                foreach(AreaAttack attack in go.GetComponentsInChildren<AreaAttack>(true))
                {
                    count += SetIfEmpty(attack, "attackEffect", fx["AttackRing"], null) ? 1 : 0;
                    count += SetIfEmpty(attack, "hitEffect", fx["Hit"], null) ? 1 : 0;
                }
            }

            if(count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);

                // 開いていて未保存の変更があるシーンは、勝手に保存しない
                if(wasDirty)
                {
                    log.Add($"  {scene.name} : AreaAttack に割り当て (未保存の変更があったため保存していません。Ctrl+S で保存してください)");
                }
                else
                {
                    EditorSceneManager.SaveScene(scene);
                    log.Add($"  {scene.name} : AreaAttack に割り当て");
                }
            }

            if(!wasLoaded) { EditorSceneManager.CloseScene(scene, true); }
        }
    }

    private static string GetScriptGuid<T>() where T : MonoBehaviour
    {
        foreach(string guid in AssetDatabase.FindAssets($"t:MonoScript {typeof(T).Name}"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if(script != null && script.GetClass() == typeof(T)) { return guid; }
        }
        return "__not_found__";
    }

    //=====================================================================
    // 共通
    //=====================================================================

    /// <summary>
    /// 欄が空のときだけ設定する (プランナーが入れたものは上書きしない)
    /// </summary>
    private static bool SetIfEmpty(Component component, string field, Object value, List<string> log)
    {
        if(component == null) { return false; }

        var so = new SerializedObject(component);
        SerializedProperty p = so.FindProperty(field);
        if(p == null)
        {
            Debug.LogWarning($"{component.GetType().Name} に {field} がありません");
            return false;
        }
        if(p.objectReferenceValue != null) { return false; }

        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        log?.Add($"  {component.gameObject.name} / {component.GetType().Name}.{field} → {value.name}");
        return true;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) { return; }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    //=====================================================================
    // 仮の絵を描く
    //=====================================================================

    /// <summary> 当たった火花 : 中心の光 + 放射状の線 </summary>
    private static void DrawHit(Canvas c, float t, Color main, Color core)
    {
        float a = 1.0f - t;
        for(int k = 0; k < 8; k++)
        {
            float angle = k * 45.0f + (k % 2 == 0 ? 0.0f : 10.0f);
            float len = k % 2 == 0 ? 1.0f : 0.65f;
            c.Ray(angle, Mathf.Lerp(5, 16, t) * len, Mathf.Lerp(14, 30, t) * len, Mathf.Lerp(3.0f, 1.0f, t), main.WithAlpha(a));
        }
        c.Circle(Mathf.Lerp(11, 3, t), main.WithAlpha(a * 0.8f));
        c.Circle(Mathf.Lerp(7, 1, t), core.WithAlpha(a));
    }

    /// <summary> 壁に当たった煙 : 小さな丸が広がって消える </summary>
    private static void DrawPuff(Canvas c, float t, Color color)
    {
        float a = (1.0f - t) * 0.85f;
        for(int k = 0; k < 5; k++)
        {
            float angle = k * 72.0f * Mathf.Deg2Rad;
            float d = Mathf.Lerp(3, 16, t);
            c.Circle(Mathf.Lerp(6, 9, t), color.WithAlpha(a), Mathf.Cos(angle) * d, Mathf.Sin(angle) * d);
        }
    }

    /// <summary> 広がる輪 (sparkles > 0 なら粒も飛ばす) </summary>
    private static void DrawRing(Canvas c, float t, Color color, int sparkles)
    {
        float a = 1.0f - t;
        c.Ring(Mathf.Lerp(5, 28, t), Mathf.Lerp(4.0f, 1.0f, t), color.WithAlpha(a));
        if(t < 0.5f) { c.Circle(Mathf.Lerp(6, 0, t * 2.0f), Color.white.WithAlpha(1.0f - t * 2.0f)); }

        for(int k = 0; k < sparkles; k++)
        {
            float angle = (k * 360.0f / sparkles + 30.0f) * Mathf.Deg2Rad;
            float d = Mathf.Lerp(8, 30, t);
            c.Circle(Mathf.Lerp(3.0f, 1.0f, t), Color.white.WithAlpha(a), Mathf.Cos(angle) * d, Mathf.Sin(angle) * d);
        }
    }

    /// <summary> 発射の光 : 中心から右向きに伸びる (撃った方向に回転して使う) </summary>
    private static void DrawMuzzle(Canvas c, float t, Color main, Color core)
    {
        float a = 1.0f - t * 0.85f;
        c.Ellipse(Mathf.Lerp(14, 20, t), 0, Mathf.Lerp(16, 10, t), Mathf.Lerp(8, 3, t), main.WithAlpha(a));
        c.Ellipse(Mathf.Lerp(10, 16, t), 0, Mathf.Lerp(9, 5, t), Mathf.Lerp(4, 1.5f, t), core.WithAlpha(a));
    }

    /// <summary> 爆発 : 火の玉 + 衝撃の輪 + 最後に煙 </summary>
    private static void DrawExplosion(Canvas c, float t)
    {
        var yellow = new Color(1.0f, 0.95f, 0.5f);
        var orange = new Color(1.0f, 0.5f, 0.15f);
        var smoke  = new Color(0.45f, 0.42f, 0.4f);

        // 煙 (後半)
        if(t > 0.45f)
        {
            float st = (t - 0.45f) / 0.55f;
            for(int k = 0; k < 6; k++)
            {
                float angle = (k * 60.0f + 15.0f) * Mathf.Deg2Rad;
                float d = Mathf.Lerp(14, 30, st);
                c.Circle(Mathf.Lerp(10, 14, st), smoke.WithAlpha((1.0f - st) * 0.7f), Mathf.Cos(angle) * d, Mathf.Sin(angle) * d);
            }
        }

        // 衝撃の輪
        c.Ring(Mathf.Lerp(10, 46, t), Mathf.Lerp(5.0f, 1.0f, t), orange.WithAlpha((1.0f - t) * 0.8f));

        // 火の玉
        float fire = t < 0.35f ? Mathf.Lerp(8, 30, t / 0.35f) : Mathf.Lerp(30, 16, (t - 0.35f) / 0.65f);
        float fireAlpha = t < 0.6f ? 1.0f : 1.0f - (t - 0.6f) / 0.4f;
        c.Circle(fire, Color.Lerp(yellow, orange, t).WithAlpha(fireAlpha));
        c.Circle(fire * 0.55f * (1.0f - t * 0.7f), Color.white.WithAlpha(fireAlpha));
    }

    /// <summary> 範囲攻撃の輪 </summary>
    private static void DrawAttackRing(Canvas c, float t, Color color)
    {
        float a = 1.0f - t;
        float r = Mathf.Lerp(10, 60, Mathf.Sqrt(t));
        c.Ring(r, Mathf.Lerp(8.0f, 2.0f, t), color.WithAlpha(a));
        c.Ring(r * 0.75f, Mathf.Lerp(4.0f, 1.0f, t), Color.white.WithAlpha(a * 0.5f));
        c.Circle(r * 0.9f, color.WithAlpha(a * 0.12f));
    }

    private static Color WithAlpha(this Color c, float a)
    {
        c.a = Mathf.Clamp01(a);
        return c;
    }

    /// <summary>
    /// 仮の絵を描くための簡単なキャンバス (中心が原点、なめらかな縁)
    /// </summary>
    private class Canvas
    {
        private readonly int size;
        private readonly Color[] px;
        private readonly float center;

        public Canvas(int size)
        {
            this.size = size;
            px = new Color[size * size];
            center = (size - 1) * 0.5f;
        }

        public byte[] ToPng()
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        public void Circle(float r, Color color, float ox = 0.0f, float oy = 0.0f)
        {
            Fill(color, (x, y) => r - Mathf.Sqrt((x - ox) * (x - ox) + (y - oy) * (y - oy)));
        }

        public void Ring(float r, float width, Color color)
        {
            Fill(color, (x, y) => width * 0.5f - Mathf.Abs(Mathf.Sqrt(x * x + y * y) - r));
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color color)
        {
            Fill(color, (x, y) =>
            {
                float nx = (x - cx) / rx, ny = (y - cy) / ry;
                return (1.0f - Mathf.Sqrt(nx * nx + ny * ny)) * Mathf.Min(rx, ry);
            });
        }

        public void Ray(float angleDeg, float r0, float r1, float width, Color color)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 a = dir * r0, b = dir * r1;
            Fill(color, (x, y) =>
            {
                var p = new Vector2(x, y);
                float h = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / Mathf.Max(0.0001f, (b - a).sqrMagnitude));
                // 先端に向かって細くする
                float w = width * (1.0f - h * 0.7f);
                return w * 0.5f - (p - (a + (b - a) * h)).magnitude;
            });
        }

        /// <summary>
        /// 内側ほど正になる距離関数で塗る (縁は1ピクセルぶんなめらかに)
        /// </summary>
        private void Fill(Color color, System.Func<float, float, float> inside)
        {
            for(int iy = 0; iy < size; iy++)
            {
                for(int ix = 0; ix < size; ix++)
                {
                    float d = inside(ix - center, iy - center);
                    float cover = Mathf.Clamp01(d + 0.5f);
                    if(cover <= 0.0f) { continue; }
                    Blend(ix, iy, color, color.a * cover);
                }
            }
        }

        private void Blend(int x, int y, Color c, float a)
        {
            int i = y * size + x;
            Color dst = px[i];
            float outA = a + dst.a * (1.0f - a);
            if(outA <= 0.0f) { return; }
            Color o = (c * a + dst * dst.a * (1.0f - a)) / outA;
            o.a = outA;
            px[i] = o;
        }
    }
}
