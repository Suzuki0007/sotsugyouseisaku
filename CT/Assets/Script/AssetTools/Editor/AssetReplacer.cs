/*********************************************************************/
// * \file   AssetReplacer.cs
// * \brief  仮素材の差し替え：古い素材を使っている所（プレハブ・データ・ゲームのシーン）を探して、新しい素材に付け替える
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 仮素材の差し替え
/// </summary>
///
/// ・Tools > 素材 > 仮素材を差し替える（ウィンドウ）、または割り当て表の「replace:古い素材のパス」
/// ・古い素材が画像（Texture2D）なら、その中の絵（Sprite）も全部対象。新しい画像の絵と、名前が同じもの → 並びの順で対応させる
/// ・音・データ・プレハブなど、同じ種類の物どうしならそのまま付け替える
/// ・見る場所：プレハブ（Assets/Prefab・Resources）・データ（Assets/Data・Resources）・Build Settings のシーン
public static class AssetReplacer
{
    /// <summary> 見つかった1か所 </summary>
    public class Hit
    {
        public string place;
        public string component;
        public string property;
        public string oldName;
        public string newName;
    }

    /// <summary>
    /// 古い素材 → 新しい素材の対応を作る（画像なら中の絵ごと）
    /// </summary>
    public static Dictionary<AssetKey, AssetKey> BuildMap(Object oldAsset, Object newAsset, StringBuilder log)
    {
        var map = new Dictionary<AssetKey, AssetKey>();
        if(oldAsset == null || newAsset == null) { return map; }

        string oldPath = AssetDatabase.GetAssetPath(oldAsset);
        string newPath = AssetDatabase.GetAssetPath(newAsset);
        bool oldIsTex = oldAsset is Texture2D || oldAsset is Sprite;
        bool newIsTex = newAsset is Texture2D || newAsset is Sprite;

        if(oldIsTex && newIsTex)
        {
            // 画像：テクスチャどうし ＋ 中の絵どうし
            var oldTex = AssetDatabase.LoadAssetAtPath<Texture2D>(oldPath);
            var newTex = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
            if(oldTex != null && newTex != null) { map[AssetKey.Of(oldTex)] = AssetKey.Of(newTex); }

            List<Sprite> olds = SpritesOf(oldPath);
            List<Sprite> news = SpritesOf(newPath);
            if(oldAsset is Sprite os) { olds = new List<Sprite> { os }; }
            if(newAsset is Sprite ns) { news = new List<Sprite> { ns }; }
            if(news.Count == 0) { log?.AppendLine($"!! 新しい画像に絵（Sprite）がありません：{newPath}（取り込みの種類を Sprite に）"); return map; }

            for(int i = 0; i < olds.Count; i++)
            {
                Sprite target = news.Find(s => s.name == olds[i].name);
                if(target == null && olds.Count == 1) { target = news[0]; }
                if(target == null && i < news.Count) { target = news[i]; }
                if(target == null) { target = news[news.Count - 1]; log?.AppendLine($"  絵の数が足りないので「{olds[i].name}」は最後の絵「{target.name}」にします"); }
                map[AssetKey.Of(olds[i])] = AssetKey.Of(target);
            }
            return map;
        }

        if(oldAsset.GetType() != newAsset.GetType())
        {
            log?.AppendLine($"!! 種類が違います：{oldAsset.GetType().Name} → {newAsset.GetType().Name}");
            return map;
        }
        map[AssetKey.Of(oldAsset)] = AssetKey.Of(newAsset);
        return map;
    }

    /// <summary> 画像の中の絵（並びは取り込みの順） </summary>
    public static List<Sprite> SpritesOf(string path)
    {
        var list = new List<Sprite>();
        foreach(Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) { if(o is Sprite s) { list.Add(s); } }
        if(list.Count == 0) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(path); if(s != null) { list.Add(s); } }
        return list;
    }

    /// <summary>
    /// 付け替える（dryRun なら探すだけ）。見つかった所を返す
    /// </summary>
    public static List<Hit> Replace(Dictionary<AssetKey, AssetKey> map, bool dryRun)
    {
        var hits = new List<Hit>();
        if(map.Count == 0) { return hits; }

        AssetWalker.Walk(place =>
        {
            var so = new SerializedObject(place.target);
            SerializedProperty it = so.GetIterator();
            bool changed = false;
            while(it.Next(true))
            {
                if(it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) { continue; }
                foreach(KeyValuePair<AssetKey, AssetKey> kv in map)
                {
                    if(!kv.Key.Matches(it.objectReferenceValue)) { continue; }
                    Object nw = kv.Value.Load();
                    hits.Add(new Hit { place = place.Label, component = place.target.GetType().Name, property = it.propertyPath, oldName = it.objectReferenceValue.name, newName = nw != null ? nw.name : "(読めない)" });
                    if(!dryRun && nw != null) { it.objectReferenceValue = nw; changed = true; }
                    break;
                }
            }
            if(changed) { so.ApplyModifiedPropertiesWithoutUndo(); }
            return changed;
        }, !dryRun);
        return hits;
    }

    /// <summary> 結果を文にする </summary>
    public static string Report(List<Hit> hits, bool dryRun)
    {
        var sb = new StringBuilder();
        sb.AppendLine((dryRun ? "見つかった所：" : "付け替えた所：") + hits.Count + " か所");
        foreach(Hit h in hits) { sb.AppendLine($"  {h.place}  {h.component}.{h.property}  {h.oldName} → {h.newName}"); }
        return sb.ToString();
    }
}

/// <summary>
/// 仮素材を差し替えるウィンドウ（Tools > 素材 > 仮素材を差し替える）
/// </summary>
public class AssetReplaceWindow : EditorWindow
{
    private Object oldAsset;
    private Object newAsset;
    private string result = "";
    private Vector2 scroll;

    [MenuItem("Tools/素材/仮素材を差し替える")]
    private static void Open()
    {
        GetWindow<AssetReplaceWindow>("仮素材の差し替え");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("古い素材（仮の絵・音など）を使っている所を、プレハブ・データ・ゲームのシーン（Build Settings）から探して、新しい素材に付け替えます。\n画像どうしなら、中の絵（Sprite）を名前 → 並びの順で対応させます。", MessageType.Info);
        oldAsset = EditorGUILayout.ObjectField("古い素材（仮）", oldAsset, typeof(Object), false);
        newAsset = EditorGUILayout.ObjectField("新しい素材", newAsset, typeof(Object), false);

        using(new EditorGUI.DisabledScope(oldAsset == null || newAsset == null || Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("探す（変えない）")) { Run(true); }
            if(GUILayout.Button("付け替える")) { Run(false); }
            EditorGUILayout.EndHorizontal();
        }
        if(Application.isPlaying) { EditorGUILayout.HelpBox("再生中は使えません", MessageType.Warning); }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(result, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Run(bool dryRun)
    {
        string dirty = AssetWalker.DirtySceneName();
        if(dirty != null) { EditorUtility.DisplayDialog("仮素材の差し替え", $"シーン「{dirty}」に保存していない変更があります。先に保存してください（ゲームのシーンを順に開くため）", "OK"); return; }
        if(!dryRun && !EditorUtility.DisplayDialog("仮素材の差し替え", $"「{oldAsset.name}」を使っている所を「{newAsset.name}」に付け替えて、プレハブ・データ・シーンを保存します。", "付け替える", "やめる")) { return; }

        var log = new System.Text.StringBuilder();
        var map = AssetReplacer.BuildMap(oldAsset, newAsset, log);
        var oldKey = AssetKey.Of(oldAsset);
        var newKey = AssetKey.Of(newAsset);
        var hits = AssetReplacer.Replace(map, dryRun);
        oldAsset = oldKey.Load();
        newAsset = newKey.Load();
        result = log + AssetReplacer.Report(hits, dryRun);
    }
}
