/*********************************************************************/
// * \file   AssetWalker.cs
// * \brief  プレハブ・データ・ゲームのシーンの部品を順に見て回る（参照の差し替え・割り当て表で使う共通の土台）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 部品・データを順に見て回る
/// </summary>
///
/// ・順番：プレハブ（Assets/Prefab・Assets/Resources）→ データ（Assets/Data・Assets/Resources の ScriptableObject）→ Build Settings のシーン
///   （プレハブを先に直すと、シーンに置いたプレハブは上書きしていない限りそのまま変わる）
/// ・変えたら保存する（プレハブは保存し直す・データは SetDirty・シーンは保存）。終わったら元のシーンを開き直す
/// ・シーンを開くと、それまでに読んだアセットが片付けられて消えることがある。参照は GUID とファイル内の番号（AssetKey）で比べ、
///   入れる物はシーンを開いたあとに読み直す（AssetKey.Load）
public static class AssetWalker
{
    /// <summary> 見ている場所 </summary>
    public class Place
    {
        /// <summary> prefab / data / scene </summary>
        public string kind;

        /// <summary> プレハブ・データのパス、またはシーンの名前 </summary>
        public string location;

        /// <summary> オブジェクトの階層のパス（データなら空） </summary>
        public string objectPath;

        /// <summary> 部品（データなら ScriptableObject そのもの） </summary>
        public UnityEngine.Object target;

        /// <summary> 場所の説明（「@場所」の照合と報告に使う） </summary>
        public string Label => string.IsNullOrEmpty(objectPath) ? location : location + " " + objectPath;
    }

    /// <summary> どこを見るか </summary>
    [Flags]
    public enum Scope { Prefabs = 1, Data = 2, Scenes = 4, All = 7 }

    /// <summary>
    /// 見て回る。visit が true を返したら「変えた」とみなして保存する
    /// </summary>
    /// <param name="save"> false なら保存しない（試しに見るだけ。シーンを開き直すので変更は消える） </param>
    /// <returns> 変えた数 </returns>
    public static int Walk(Func<Place, bool> visit, bool save, Scope scope = Scope.All, Func<string, bool> sceneFilter = null)
    {
        int changed = 0;
        string startScene = SceneManager.GetActiveScene().path;

        if((scope & Scope.Prefabs) != 0)
        {
            foreach(string path in FindAssets("t:Prefab", "Assets/Prefab", "Assets/Resources"))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int n = VisitObject(root, "prefab", path, visit);
                if(n > 0 && save) { PrefabUtility.SaveAsPrefabAsset(root, path); }
                PrefabUtility.UnloadPrefabContents(root);
                changed += n;
            }
        }

        if((scope & Scope.Data) != 0)
        {
            foreach(string path in FindAssets("t:ScriptableObject", "Assets/Data", "Assets/Resources"))
            {
                foreach(UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if(!(o is ScriptableObject so)) { continue; }
                    if(visit(new Place { kind = "data", location = path, objectPath = "", target = so }))
                    {
                        changed++;
                        if(save) { EditorUtility.SetDirty(so); }
                    }
                }
            }
            if(save) { AssetDatabase.SaveAssets(); }
        }

        if((scope & Scope.Scenes) != 0)
        {
            foreach(EditorBuildSettingsScene bs in EditorBuildSettings.scenes)
            {
                if(!bs.enabled) { continue; }
                string name = System.IO.Path.GetFileNameWithoutExtension(bs.path);
                if(sceneFilter != null && !sceneFilter(name)) { continue; }

                Scene scene = EditorSceneManager.OpenScene(bs.path, OpenSceneMode.Single);
                int n = 0;
                foreach(GameObject root in scene.GetRootGameObjects()) { n += VisitObject(root, "scene", name, visit); }
                if(n > 0 && save)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                changed += n;
            }
            if(!string.IsNullOrEmpty(startScene)) { EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single); }
        }
        return changed;
    }

    /// <summary> シーンに保存していない変更があるか（あれば見て回らない） </summary>
    public static string DirtySceneName()
    {
        for(int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if(s.isDirty) { return s.name; }
        }
        return null;
    }

    private static int VisitObject(GameObject root, string kind, string location, Func<Place, bool> visit)
    {
        int n = 0;
        foreach(Component c in root.GetComponentsInChildren<Component>(true))
        {
            if(c == null || c is Transform) { continue; }
            var place = new Place { kind = kind, location = location, objectPath = PathOf(c.transform), target = c };
            if(visit(place)) { n++; }
        }
        return n;
    }

    /// <summary> 階層のパス（親/子/孫） </summary>
    public static string PathOf(Transform t)
    {
        string path = t.name;
        for(Transform p = t.parent; p != null; p = p.parent) { path = p.name + "/" + path; }
        return path;
    }

    private static IEnumerable<string> FindAssets(string filter, params string[] folders)
    {
        var seen = new HashSet<string>();
        var valid = new List<string>();
        foreach(string f in folders) { if(AssetDatabase.IsValidFolder(f)) { valid.Add(f); } }
        foreach(string guid in AssetDatabase.FindAssets(filter, valid.ToArray()))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if(seen.Add(path)) { yield return path; }
        }
    }
}

/// <summary>
/// アセットの「GUID ＋ ファイル内の番号」（シーンを開いても消えない覚え方）
/// </summary>
public struct AssetKey
{
    public string guid;
    public long localId;

    public bool IsValid => !string.IsNullOrEmpty(guid);

    public static AssetKey Of(UnityEngine.Object o)
    {
        if(o != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string g, out long id)) { return new AssetKey { guid = g, localId = id }; }
        return default;
    }

    public bool Matches(UnityEngine.Object o)
    {
        if(o == null || !IsValid) { return false; }
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string g, out long id) && g == guid && id == localId;
    }

    /// <summary> 読み直す（シーンを開いたあとに使う） </summary>
    public UnityEngine.Object Load()
    {
        if(!IsValid) { return null; }
        string path = AssetDatabase.GUIDToAssetPath(guid);
        foreach(UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if(o != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string g, out long id) && id == localId) { return o; }
        }
        return null;
    }

    public override string ToString() => IsValid ? AssetDatabase.GUIDToAssetPath(guid) + "#" + localId : "(なし)";
}
