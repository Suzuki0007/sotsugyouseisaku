/*********************************************************************/
// * \file   SoundListWindow.cs
// * \brief  音の一覧（Tools > 素材 > 音の一覧）：効果音（SeData）・BGM を並べて試聴し、音量を直し、どこで使っているかを見る
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 音の一覧
/// </summary>
///
/// ・効果音（SeData）と BGM（Assets/BGM の曲）を並べる。▶ で試聴（SeData の音量・ピッチで鳴らす）、■ で止める
/// ・音量はその場で直せる（SeData の Volume。元に戻すは Ctrl+Z）。「自動」で聞こえ方（RMS）を -20dB くらいにそろえる
/// ・「聞こえ方」は音の大きさの目安（-14dB より大きいと赤・-28dB より小さいと青）
/// ・「使っている所」は、シーン・プレハブ・データのどれがその音を使っているか（使っていない音も絞り込める）
public class SoundListWindow : EditorWindow
{
    private enum Tab { Se = 0, Bgm = 1 }

    private Tab tab;
    private string filter = "";
    private bool unusedOnly;
    private Vector2 scroll;

    private readonly List<SeData> ses = new List<SeData>();
    private readonly List<AudioClip> bgms = new List<AudioClip>();
    private readonly Dictionary<string, List<string>> usedBy = new Dictionary<string, List<string>>();
    private readonly Dictionary<AudioClip, float> rmsCache = new Dictionary<AudioClip, float>();
    private readonly HashSet<string> opened = new HashSet<string>();

    private GameObject player;
    private AudioSource source;

    [MenuItem("Tools/素材/音の一覧（試聴・音量）")]
    private static void Open()
    {
        GetWindow<SoundListWindow>("音の一覧");
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Stop();
        if(player != null) { DestroyImmediate(player); }
    }

    //=====================================================================
    // 集める
    //=====================================================================

    private void Refresh()
    {
        ses.Clear();
        bgms.Clear();
        usedBy.Clear();

        foreach(string g in AssetDatabase.FindAssets("t:SeData")) { var d = AssetDatabase.LoadAssetAtPath<SeData>(AssetDatabase.GUIDToAssetPath(g)); if(d != null) { ses.Add(d); } }
        if(AssetDatabase.IsValidFolder("Assets/BGM"))
        {
            foreach(string g in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/BGM" })) { var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)); if(c != null) { bgms.Add(c); } }
        }
        ses.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
        bgms.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));

        // 使っている所：ゲームのシーン・プレハブ・データが直接使っている物（シーンを開かずに調べる）
        var users = new List<string>();
        foreach(EditorBuildSettingsScene s in EditorBuildSettings.scenes) { if(s.enabled) { users.Add(s.path); } }
        foreach(string folder in new[] { "Assets/Prefab", "Assets/Resources", "Assets/Data" })
        {
            if(!AssetDatabase.IsValidFolder(folder)) { continue; }
            foreach(string g in AssetDatabase.FindAssets("t:Prefab t:ScriptableObject", new[] { folder })) { users.Add(AssetDatabase.GUIDToAssetPath(g)); }
        }
        foreach(string user in users)
        {
            foreach(string dep in AssetDatabase.GetDependencies(user, false))
            {
                if(dep == user) { continue; }
                if(!usedBy.TryGetValue(dep, out var list)) { list = new List<string>(); usedBy[dep] = list; }
                if(!list.Contains(user)) { list.Add(user); }
            }
        }
    }

    private List<string> UsersOf(Object o)
    {
        return usedBy.TryGetValue(AssetDatabase.GetAssetPath(o), out var list) ? list : new List<string>();
    }

    //=====================================================================
    // 画面
    //=====================================================================

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        tab = (Tab)GUILayout.Toolbar((int)tab, new[] { "効果音（SeData）", "BGM" }, EditorStyles.toolbarButton, GUILayout.Width(220));
        GUILayout.Space(8);
        GUILayout.Label("絞り込み", GUILayout.Width(50));
        filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField, GUILayout.Width(180));
        unusedOnly = GUILayout.Toggle(unusedOnly, "使っていない物だけ", EditorStyles.toolbarButton, GUILayout.Width(120));
        GUILayout.FlexibleSpace();
        if(GUILayout.Button("■ 止める", EditorStyles.toolbarButton, GUILayout.Width(70))) { Stop(); }
        if(GUILayout.Button("読み直す", EditorStyles.toolbarButton, GUILayout.Width(70))) { Refresh(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("▶ で試聴（効果音は SeData の音量・ピッチで鳴る）。音量はその場で直せます（Ctrl+Z で戻せる）。「聞こえ方」は大きさの目安：赤＝大きい（-14dB より上）・青＝小さい（-28dB より下）。ほかの音と聞き比べて、大きすぎ・小さすぎを直してください。", MessageType.None);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if(tab == Tab.Se) { foreach(SeData d in ses) { if(Visible(d)) { DrawSe(d); } } }
        else              { foreach(AudioClip c in bgms) { if(Visible(c)) { DrawBgm(c); } } }
        EditorGUILayout.EndScrollView();
    }

    private bool Visible(Object o)
    {
        if(o == null) { return false; }
        string path = AssetDatabase.GetAssetPath(o);
        if(!string.IsNullOrEmpty(filter) && path.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) { return false; }
        if(unusedOnly && UsersOf(o).Count > 0) { return false; }
        return true;
    }

    private void DrawSe(SeData d)
    {
        var so = new SerializedObject(d);
        SerializedProperty clips = so.FindProperty("clips");
        AudioClip first = clips.arraySize > 0 ? clips.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip : null;
        string path = AssetDatabase.GetAssetPath(d);

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("▶", GUILayout.Width(24))) { PlaySe(d); }
        if(GUILayout.Button(d.name, EditorStyles.label, GUILayout.Width(200))) { EditorGUIUtility.PingObject(d); Selection.activeObject = d; }

        so.Update();
        SerializedProperty vol = so.FindProperty("volume");
        EditorGUI.BeginChangeCheck();
        float v = EditorGUILayout.Slider(vol.floatValue, 0.0f, 1.0f, GUILayout.Width(160));
        if(EditorGUI.EndChangeCheck()) { vol.floatValue = v; so.ApplyModifiedProperties(); }
        if(GUILayout.Button(new GUIContent("自動", "聞こえ方を -20dB くらいにそろえる（大きい音だけ下げる）"), GUILayout.Width(40)) && first != null)
        {
            vol.floatValue = AssetMapApplier.AutoVolume(first);
            so.ApplyModifiedProperties();
        }

        DrawRms(first);
        GUILayout.Label(first != null ? first.length.ToString("0.0") + " 秒" : "音なし", GUILayout.Width(50));
        GUILayout.Label((clips.arraySize > 1 ? clips.arraySize + " 個" : "") + (so.FindProperty("loop").boolValue ? " ループ" : ""), GUILayout.Width(70));
        DrawUsers(d, path);
        EditorGUILayout.EndHorizontal();
        DrawUserList(path);
    }

    private void DrawBgm(AudioClip c)
    {
        string path = AssetDatabase.GetAssetPath(c);
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("▶", GUILayout.Width(24))) { PlayClip(c, 1.0f, 1.0f, true); }
        if(GUILayout.Button(c.name, EditorStyles.label, GUILayout.Width(260))) { EditorGUIUtility.PingObject(c); Selection.activeObject = c; }
        GUILayout.Label((c.length / 60.0f).ToString("0") + " 分 " + (c.length % 60.0f).ToString("00") + " 秒", GUILayout.Width(80));
        GUILayout.Label(Path.GetExtension(path), GUILayout.Width(40));
        GUILayout.Label("音量は使う所（SceneBgm）で決める", EditorStyles.miniLabel, GUILayout.Width(190));
        DrawUsers(c, path);
        EditorGUILayout.EndHorizontal();
        DrawUserList(path);
    }

    private void DrawRms(AudioClip clip)
    {
        float rms = Rms(clip);
        var style = new GUIStyle(EditorStyles.label);
        if(rms > -14.0f) { style.normal.textColor = new Color(0.9f, 0.3f, 0.3f); }
        else if(rms < -28.0f && rms > -200.0f) { style.normal.textColor = new Color(0.35f, 0.6f, 1.0f); }
        GUILayout.Label(new GUIContent(rms > -200.0f ? rms.ToString("0") + "dB" : "-", "聞こえ方（RMS）"), style, GUILayout.Width(45));
    }

    private void DrawUsers(Object o, string path)
    {
        List<string> users = UsersOf(o);
        string label = users.Count == 0 ? "使っていない" : "使っている所 " + users.Count;
        bool open = opened.Contains(path);
        bool now = EditorGUILayout.Foldout(open, label, true);
        if(now != open) { if(now) { opened.Add(path); } else { opened.Remove(path); } }
    }

    private void DrawUserList(string path)
    {
        if(!opened.Contains(path) || !usedBy.TryGetValue(path, out var users)) { return; }
        EditorGUI.indentLevel += 2;
        foreach(string u in users)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(u, EditorStyles.miniLabel);
            if(GUILayout.Button("選ぶ", EditorStyles.miniButton, GUILayout.Width(40))) { var a = AssetDatabase.LoadMainAssetAtPath(u); EditorGUIUtility.PingObject(a); Selection.activeObject = a; }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel -= 2;
    }

    //=====================================================================
    // 鳴らす
    //=====================================================================

    /// <summary> 聞こえ方（RMS、dB）。測れなければ -999 </summary>
    private float Rms(AudioClip c)
    {
        if(c == null) { return -999.0f; }
        if(rmsCache.TryGetValue(c, out float r)) { return r; }
        r = -999.0f;
        if(c.loadType == AudioClipLoadType.DecompressOnLoad && c.length < 30.0f)
        {
            var data = new float[c.samples * c.channels];
            if(c.GetData(data, 0))
            {
                double sum = 0; foreach(float x in data) { sum += x * x; }
                r = (float)(20.0 * System.Math.Log10(System.Math.Max(1e-6, System.Math.Sqrt(sum / System.Math.Max(1, data.Length)))));
            }
        }
        rmsCache[c] = r;
        return r;
    }

    private void PlaySe(SeData d)
    {
        var so = new SerializedObject(d);
        SerializedProperty clips = so.FindProperty("clips");
        if(clips.arraySize == 0) { return; }
        var clip = clips.GetArrayElementAtIndex(Random.Range(0, clips.arraySize)).objectReferenceValue as AudioClip;
        float pitch = Random.Range(so.FindProperty("pitchMin").floatValue, so.FindProperty("pitchMax").floatValue);
        PlayClip(clip, so.FindProperty("volume").floatValue, pitch, so.FindProperty("loop").boolValue);
    }

    /// <summary> 鳴らす（音量・ピッチつき）。見えない AudioSource で鳴らし、鳴らせなければ Unity の試聴で（音量は 1） </summary>
    public void PlayClip(AudioClip clip, float volume, float pitch, bool loop)
    {
        if(clip == null) { return; }
        Stop();
        if(player == null)
        {
            player = EditorUtility.CreateGameObjectWithHideFlags("__SoundListPlayer", HideFlags.HideAndDontSave, typeof(AudioSource));
            source = player.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.0f;
        }
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();
        if(!source.isPlaying) { PreviewClip(clip, loop); }
    }

    /// <summary> 鳴っているか（確かめ用） </summary>
    public bool IsPlaying => source != null && source.isPlaying;

    public void Stop()
    {
        if(source != null) { source.Stop(); }
        StopPreview();
    }

    private static void PreviewClip(AudioClip clip, bool loop)
    {
        System.Type t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo m = t != null ? t.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null) : null;
        if(m != null) { m.Invoke(null, new object[] { clip, 0, loop }); }
    }

    private static void StopPreview()
    {
        System.Type t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        MethodInfo m = t != null ? t.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public) : null;
        if(m != null) { m.Invoke(null, null); }
    }

    /// <summary> 確かめ用：数（効果音・BGM）と、使っていない物の数 </summary>
    public string Summary()
    {
        int unusedSe = 0; foreach(SeData d in ses) { if(UsersOf(d).Count == 0) { unusedSe++; } }
        int unusedBgm = 0; foreach(AudioClip c in bgms) { if(UsersOf(c).Count == 0) { unusedBgm++; } }
        return $"SeData {ses.Count}（使っていない {unusedSe}）・BGM {bgms.Count}（使っていない {unusedBgm}）";
    }

    /// <summary> 確かめ用：名前で探して鳴らす </summary>
    public bool PlayByName(string name)
    {
        SeData d = ses.Find(x => x.name == name);
        if(d != null) { PlaySe(d); return true; }
        AudioClip c = bgms.Find(x => x.name == name);
        if(c != null) { PlayClip(c, 1.0f, 1.0f, true); return true; }
        return false;
    }
}
