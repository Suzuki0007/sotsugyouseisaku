/*********************************************************************/
// * \file   SpriteAnimSampleSetup.cs
// * \brief  今ある素材からノア・シグルのアニメーションを作って、プレイヤーの Prefab を新しい仕組みに置き換えるエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// メニュー : Tools → Animation → サンプルを作成して割り当てる (ノア・シグル)
///
/// 作るもの (すでにあれば作り直さず、そのまま使う):
///   Assets/Data/Animation/Sample/Anim_Sample_Noah.asset
///     Idle : NoahAnimation に入っていた上下左右の静止画
///     Walk : animation_noah_front2 / back2 / left2 / right2 (各4コマ、4コマ/秒)
///   Assets/Data/Animation/Sample/Anim_Sample_Sigle.asset
///     Idle : SigleAnimation に入っていた上下左右の静止画
///
/// Prefab の変更 (NoahAnimation / SigleAnimation が付いている Prefab):
///   ・SpriteAnimator と CharacterAnimLink を付けて、上のデータを入れる
///   ・Animator を外す (画像を上書きしてしまうため)
///   ・NoahAnimation / SigleAnimation を外す (役目を CharacterAnimLink が引き継ぐ)
///
/// 本番の絵ができたら、SpriteAnimData に画像をドラッグして入れ替えるだけでよい
/// </summary>
public static class SpriteAnimSampleSetup
{
    private const string DataRoot = "Assets/Data/Animation/Sample";
    private const string MenuPath = "Tools/Animation/サンプルを作成して割り当てる (ノア・シグル)";
    private const string PreferredArtFolder = "Assets/res_kari/kari_player";

    [MenuItem(MenuPath)]
    private static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playモード中は実行できません。Playを止めてから実行してください");
            return;
        }

        if(!EditorUtility.DisplayDialog("アニメーションのサンプル",
            "ノア・シグルの Prefab を新しいアニメーションの仕組み (SpriteAnimator) に置き換えます。\n\n" +
            "・SpriteAnimator / CharacterAnimLink を追加\n" +
            "・Animator と NoahAnimation / SigleAnimation を外す\n\n" +
            "実行しますか？", "実行", "やめる"))
        {
            return;
        }

        EnsureFolder(DataRoot);
        var log = new List<string>();

        foreach(string path in AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab == null) { continue; }

            // Prefab の一番上に付いているものだけを置き換える (入れ子の Prefab は元の Prefab 側で置き換わる)
            if(prefab.GetComponent<NoahAnimation>() != null)
            {
                SpriteAnimData data = GetOrCreate("Anim_Sample_Noah", d => BuildNoah(d, prefab.GetComponent<NoahAnimation>()));
                ConvertPrefab(path, typeof(NoahAnimation), data, log);
            }
            else if(prefab.GetComponent<SigleAnimation>() != null)
            {
                SpriteAnimData data = GetOrCreate("Anim_Sample_Sigle", d => BuildIdle(d, prefab.GetComponent<SigleAnimation>()));
                ConvertPrefab(path, typeof(SigleAnimation), data, log);
            }
        }

        AssetDatabase.SaveAssets();

        if(log.Count == 0)
        {
            Debug.Log("【アニメーション】置き換える Prefab がありませんでした (すでに置き換え済みの可能性があります)");
            return;
        }

        Debug.Log("【アニメーション】サンプルを作成して割り当てました。Playして歩くと確認できます\n" + string.Join("\n", log));
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(DataRoot));
    }

    //=====================================================================
    // データの作成
    //=====================================================================

    private static SpriteAnimData GetOrCreate(string assetName, System.Action<SpriteAnimData> build)
    {
        string path = $"{DataRoot}/{assetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<SpriteAnimData>(path);
        if(data != null) { return data; }

        data = ScriptableObject.CreateInstance<SpriteAnimData>();
        build(data);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    /// <summary>
    /// ノア : Idle (静止画) + Walk (スプライトシート)
    /// </summary>
    private static void BuildNoah(SpriteAnimData data, NoahAnimation old)
    {
        var walk = new SpriteAnimClip
        {
            name = "Walk",
            directionMode = SpriteAnimDirectionMode.FourWay,
            down  = LoadSheet("animation_noah_front2"),
            up    = LoadSheet("animation_noah_back2"),
            left  = LoadSheet("animation_noah_left2"),
            right = LoadSheet("animation_noah_right2"),
            fps = 4.0f,// 元の Noah_anim とほぼ同じ速さ
            playMode = SpriteAnimPlayMode.Loop,
        };

        BuildIdle(data, old);

        // 静止画が入っていなかった向きは、歩きの1コマ目で代用する
        SpriteAnimClip idle = data.EditableClips[0];
        if(!SpriteAnimClip.Has(idle.down)  && SpriteAnimClip.Has(walk.down))  { idle.down  = new[] { walk.down[0] }; }
        if(!SpriteAnimClip.Has(idle.up)    && SpriteAnimClip.Has(walk.up))    { idle.up    = new[] { walk.up[0] }; }
        if(!SpriteAnimClip.Has(idle.left)  && SpriteAnimClip.Has(walk.left))  { idle.left  = new[] { walk.left[0] }; }
        if(!SpriteAnimClip.Has(idle.right) && SpriteAnimClip.Has(walk.right)) { idle.right = new[] { walk.right[0] }; }

        if(walk.HasAnyFrames) { data.EditableClips.Add(walk); }
        else { Debug.LogWarning("animation_noah_*2 の画像が見つからなかったため、ノアの Walk は作りませんでした"); }
    }

    /// <summary>
    /// 古いコンポーネントに入っていた上下左右の静止画から Idle を作る
    /// </summary>
    private static void BuildIdle(SpriteAnimData data, Component old)
    {
        var so = new SerializedObject(old);
        Sprite Get(string field) => so.FindProperty(field)?.objectReferenceValue as Sprite;
        Sprite[] One(Sprite s) => s != null ? new[] { s } : new Sprite[0];

        var idle = new SpriteAnimClip
        {
            name = "Idle",
            directionMode = SpriteAnimDirectionMode.FourWay,
            down  = One(Get("downSprite")),
            up    = One(Get("upSprite")),
            left  = One(Get("leftSprite")),
            right = One(Get("rightSprite")),
            playMode = SpriteAnimPlayMode.Loop,
        };

        // 静止画が1枚も入っていなければ、今表示している画像を使う
        if(!idle.HasAnyFrames)
        {
            var sr = old.GetComponent<SpriteRenderer>();
            if(sr != null && sr.sprite != null) { idle.down = new[] { sr.sprite }; idle.directionMode = SpriteAnimDirectionMode.Single; }
        }

        data.EditableClips.Add(idle);
        data.EditableDefaultClip = "Idle";
    }

    /// <summary>
    /// 名前でスプライトシートを探して、切り分けたコマを返す (res_kari の素材を優先)
    /// </summary>
    private static Sprite[] LoadSheet(string textureName)
    {
        string[] paths = AssetDatabase.FindAssets(textureName + " t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => System.IO.Path.GetFileNameWithoutExtension(p) == textureName)
            .OrderByDescending(p => p.StartsWith(PreferredArtFolder))
            .ToArray();
        if(paths.Length == 0) { return new Sprite[0]; }

        return SpriteAnimImporter.LoadSprites(AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]));
    }

    //=====================================================================
    // Prefab の置き換え
    //=====================================================================

    private static void ConvertPrefab(string path, System.Type oldType, SpriteAnimData data, List<string> log)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Component old = root.GetComponent(oldType);
            GameObject go = root;

            SpriteAnimator anim = go.GetComponent<SpriteAnimator>();
            if(anim == null) { anim = go.AddComponent<SpriteAnimator>(); }

            var so = new SerializedObject(anim);
            if(so.FindProperty("data").objectReferenceValue == null)
            {
                so.FindProperty("data").objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if(go.GetComponent<CharacterAnimLink>() == null) { go.AddComponent<CharacterAnimLink>(); }

            var changes = new List<string> { "SpriteAnimator・CharacterAnimLink を追加" };

            Animator animator = go.GetComponent<Animator>();
            if(animator != null)
            {
                Object.DestroyImmediate(animator, true);
                changes.Add("Animator を外した");
            }

            Object.DestroyImmediate(old, true);
            changes.Add($"{oldType.Name} を外した");

            // シーン上の見た目も最初のコマにしておく
            SpriteAnimClip idle = data.DefaultClip;
            Sprite[] frames = idle?.GetFrames(SpriteAnimFacing.Down, false, out _);
            if(SpriteAnimClip.Has(frames) && frames[0] != null) { go.GetComponent<SpriteRenderer>().sprite = frames[0]; }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.Add($"  {path} : {string.Join(" / ", changes)} (データ: {data.name})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) { return; }
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
