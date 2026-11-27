/*********************************************************************/
// * \file   RippleSystem.cs
// * \brief  波紋の窓口：波紋を広げる・消す・数える。「ずっと残る」波紋をセーブに記録し、シーンに入ったら広がった状態で戻す
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 波紋の窓口（部品「波紋を広げる」「波紋を消す」・残る波紋が使う）
/// </summary>
///
/// ・波紋はシーンごと（シーンを移ると消える）。残す ID を付けた「ずっと残る」波紋は、セーブ（Ripple/&lt;シーン&gt;/…）に記録し、シーンに入ったときに広がった状態で戻す
public static class RippleSystem
{
    private const string Prefix = "Ripple/";

    private static readonly List<RippleView> views = new List<RippleView>();
    private static Transform root;
    private static Sprite ringSprite;
    private static Texture2D ringTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        views.Clear();
        root = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Save.BeforeCommit -= OnBeforeCommit;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Save.BeforeCommit -= OnBeforeCommit;
        Save.BeforeCommit += OnBeforeCommit;
        RestoreSaved(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(mode != LoadSceneMode.Single) { return; }
        views.Clear();
        root = null;
        RestoreSaved(scene);
    }

    /// <summary> 今ある波紋の数（確かめ用） </summary>
    public static int Count
    {
        get { Prune(); return views.Count; }
    }

    /// <summary> 残す ID の波紋があるか </summary>
    public static bool Has(string id)
    {
        Prune();
        for(int i = 0; i < views.Count; i++) { if(views[i].Id == id) { return true; } }
        return false;
    }

    /// <summary>
    /// 波紋を広げる。id を付けて persist なら、セーブに残る（ロードで広がった状態で戻る）
    /// </summary>
    public static RippleView Spawn(RipplePreset preset, Vector3 position, float seconds, string id = "", bool persist = false, bool instant = false)
    {
        if(preset == null) { return null; }
        Prune();
        if(!string.IsNullOrEmpty(id))
        {
            // 同じ ID があれば作り直す
            for(int i = views.Count - 1; i >= 0; i--) { if(views[i].Id == id) { Object.Destroy(views[i].gameObject); views.RemoveAt(i); } }
        }
        var go = new GameObject("波紋 " + (string.IsNullOrEmpty(id) ? preset.name : id));
        go.transform.SetParent(Root(), false);
        go.transform.position = new Vector3(position.x, position.y, 0.0f);
        RippleView v = go.AddComponent<RippleView>();
        v.Id = id;
        v.Persist = persist && !string.IsNullOrEmpty(id) && preset.AfterMode == RipplePreset.After.Stay;
        v.Begin(preset, seconds, instant, Ring());
        views.Add(v);
        if(v.Persist) { WriteSaved(); }
        return v;
    }

    /// <summary> 波紋を消す（id が空ならすべて） </summary>
    public static void Clear(string id, float seconds)
    {
        Prune();
        for(int i = 0; i < views.Count; i++)
        {
            if(string.IsNullOrEmpty(id) || views[i].Id == id) { views[i].Persist = false; views[i].FadeOut(seconds); }
        }
        WriteSaved();
    }

    private static void Prune()
    {
        for(int i = views.Count - 1; i >= 0; i--)
        {
            if(views[i] == null) { views.RemoveAt(i); continue; }
            if(views[i].IsGone) { Object.Destroy(views[i].gameObject); views.RemoveAt(i); }
        }
    }

    private static Transform Root()
    {
        if(root == null) { root = new GameObject("波紋（演出・自動）").transform; }
        return root;
    }

    // 縁の光に使う輪の絵（白い細い輪。実行中に 1 回だけ作る）
    private static Sprite Ring()
    {
        if(ringSprite != null) { return ringSprite; }
        const int size = 256;
        ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "波紋の縁" };
        var px = new Color32[size * size];
        for(int y = 0; y < size; y++)
        {
            for(int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2.0f - 1.0f, dy = (y + 0.5f) / size * 2.0f - 1.0f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1.0f - Mathf.Abs(d - 0.93f) * 18.0f);
                byte b = (byte)(a * 255.0f);
                px[y * size + x] = new Color32(b, b, b, b);
            }
        }
        ringTexture.SetPixels32(px);
        ringTexture.Apply();
        ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }

    //=====================================================================
    // セーブ
    //=====================================================================

    private static string ListKey(string scene) => Prefix + scene + "/list";

    private static void OnBeforeCommit() { WriteSaved(); }

    private static void WriteSaved()
    {
        string scene = SceneManager.GetActiveScene().name;
        if(string.IsNullOrEmpty(scene)) { return; }
        var sb = new System.Text.StringBuilder();
        for(int i = 0; i < views.Count; i++)
        {
            RippleView v = views[i];
            if(v == null || !v.Persist) { continue; }
            Vector3 p = v.transform.position;
            if(sb.Length > 0) { sb.Append(';'); }
            sb.Append(v.Id).Append('|').Append(v.Preset.name).Append('|').Append(p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('|').Append(p.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        Save.SetPending(ListKey(scene), sb.ToString());
    }

    private static void RestoreSaved(Scene scene)
    {
        string list = Save.Get(ListKey(scene.name), "");
        if(string.IsNullOrEmpty(list)) { return; }
        foreach(string item in list.Split(';'))
        {
            string[] f = item.Split('|');
            if(f.Length < 4) { continue; }
            var preset = PresentationDirector.Find<RipplePreset>(f[1]);
            if(preset == null) { continue; }
            float.TryParse(f[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x);
            float.TryParse(f[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y);
            Spawn(preset, new Vector3(x, y, 0.0f), 0.0f, f[0], true, true);
        }
    }
}
