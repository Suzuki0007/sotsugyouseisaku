/*********************************************************************/
// * \file   EffectDataCreator.cs
// * \brief  画像やPrefabから EffectData を作成するエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Projectウィンドウで画像 (またはPrefab) を選択して右クリック → Create → Effect →
///   EffectData (選択した画像をまとめて1つのアニメに) : 連番画像 (hit_01.png, hit_02.png...) や、切り分けたスプライトを1つのアニメにする
///   EffectData (画像ファイルごとに作成)               : スプライトシート1枚につき1つ作る (Sprite Editor で切り分けておく)
///   EffectData (選択したPrefabから作成)               : パーティクルなどのPrefabから作る
/// 作成した EffectData は、選んだ素材と同じフォルダに保存される
/// </summary>
public static class EffectDataCreator
{
    private const string MenuCombined = "Assets/Create/Effect/EffectData (選択した画像をまとめて1つのアニメに)";
    private const string MenuEach     = "Assets/Create/Effect/EffectData (画像ファイルごとに作成)";
    private const string MenuPrefab   = "Assets/Create/Effect/EffectData (選択したPrefabから作成)";

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem(MenuCombined, false, 0)]
    private static void CreateCombined()
    {
        List<Sprite> sprites = Selection.objects.SelectMany(GetSprites).Distinct().ToList();
        sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
        if(sprites.Count == 0) { return; }

        string folder = GetFolder(sprites[0]);
        EffectData data = CreateData(folder, MakeName(sprites[0].name), so =>
        {
            SetFrames(so, sprites);
        });

        Finish(new List<Object> { data });
    }

    [MenuItem(MenuEach, false, 1)]
    private static void CreateEach()
    {
        var created = new List<Object>();

        foreach(Texture2D texture in Selection.objects.OfType<Texture2D>())
        {
            List<Sprite> sprites = GetSprites(texture).ToList();
            sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            if(sprites.Count == 0) { continue; }

            created.Add(CreateData(GetFolder(texture), MakeName(texture.name), so => SetFrames(so, sprites)));
        }

        Finish(created);
    }

    [MenuItem(MenuPrefab, false, 2)]
    private static void CreateFromPrefab()
    {
        var created = new List<Object>();

        foreach(GameObject prefab in Selection.objects.OfType<GameObject>())
        {
            if(!PrefabUtility.IsPartOfPrefabAsset(prefab)) { continue; }

            created.Add(CreateData(GetFolder(prefab), MakeName(prefab.name), so =>
            {
                so.FindProperty("displayMode").enumValueIndex = (int)EffectDisplayMode.Prefab;
                so.FindProperty("prefab").objectReferenceValue = prefab;

                // ループ設定のパーティクルなら、ループエフェクトにしておく
                ParticleSystem ps = prefab.GetComponentInChildren<ParticleSystem>(true);
                if(ps != null && ps.main.loop)
                {
                    so.FindProperty("lifetime").enumValueIndex = (int)EffectLifetime.Loop;
                }
            }));
        }

        Finish(created);
    }

    [MenuItem(MenuCombined, true)]
    private static bool ValidateCombined() => Selection.objects.Any(o => o is Sprite || o is Texture2D);

    [MenuItem(MenuEach, true)]
    private static bool ValidateEach() => Selection.objects.OfType<Texture2D>().Any();

    [MenuItem(MenuPrefab, true)]
    private static bool ValidatePrefab() => Selection.objects.OfType<GameObject>().Any(PrefabUtility.IsPartOfPrefabAsset);

    //=====================================================================
    // 内部処理
    //=====================================================================

    /// <summary>
    /// 選択したもの (Sprite か Texture2D) からスプライトを集める
    /// </summary>
    private static IEnumerable<Sprite> GetSprites(Object obj)
    {
        if(obj is Sprite sprite) { return new[] { sprite }; }
        if(obj is Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if(sprites.Length == 0)
            {
                Debug.LogWarning($"「{texture.name}」はスプライトとして読み込まれていません。Inspector で Texture Type を「Sprite (2D and UI)」にしてください", texture);
            }
            return sprites;
        }
        return Enumerable.Empty<Sprite>();
    }

    private static void SetFrames(SerializedObject so, List<Sprite> sprites)
    {
        so.FindProperty("displayMode").enumValueIndex = (int)EffectDisplayMode.SpriteFrames;
        SerializedProperty frames = so.FindProperty("frames");
        frames.arraySize = sprites.Count;
        for(int i = 0; i < sprites.Count; i++)
        {
            frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    private static EffectData CreateData(string folder, string assetName, System.Action<SerializedObject> setup)
    {
        EffectData data = ScriptableObject.CreateInstance<EffectData>();

        // 変数は private なので SerializedObject 経由で設定する
        var so = new SerializedObject(data);
        setup(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 同名のファイルがあれば上書きせず、番号を付ける
        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(folder, assetName + ".asset").Replace('\\', '/'));

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static string GetFolder(Object obj)
    {
        return Path.GetDirectoryName(AssetDatabase.GetAssetPath(obj));
    }

    /// <summary>
    /// "hit_01" → "Fx_hit" のように、末尾の番号を取ってアセット名を決める
    /// </summary>
    private static string MakeName(string sourceName)
    {
        string name = Regex.Replace(sourceName, @"[_\-\s]*\d+$", "");
        if(name.StartsWith("Fx_", System.StringComparison.OrdinalIgnoreCase)) { name = name.Substring(3); }
        if(string.IsNullOrEmpty(name)) { name = sourceName; }
        return "Fx_" + name;
    }

    private static void Finish(List<Object> created)
    {
        if(created.Count == 0) { return; }

        AssetDatabase.SaveAssets();
        Selection.objects = created.ToArray();
        EditorGUIUtility.PingObject(created[0]);

        Debug.Log($"EffectData を {created.Count} 個作成しました");
    }
}
