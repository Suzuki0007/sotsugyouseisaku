/*********************************************************************/
// * \file   DebugWindow.cs
// * \brief  動作確認用のデバッグウィンドウ
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > デバッグ
/// ・再生中：アイテムの増減、フラグの切り替え、出現位置へのワープ、別シーンへの移動、回復・無敵
/// ・再生前：「再生開始時に持たせるアイテム・ON にするフラグ」を決めておける（自分の PC だけに保存）
/// ・セーブの確定・削除、セーブファイルの場所を開く
/// </summary>
public class DebugWindow : EditorWindow
{
    //=====================================================================
    // 変数
    //=====================================================================

    private Vector2 scroll;
    private bool foldItems = true;
    private bool foldFlags = true;
    private bool foldWarp = true;
    private bool foldPlayer = true;
    private bool foldSave = true;
    private bool foldStart = true;

    private string warpScene = "";
    private string warpSpawn = "";

    private List<ItemData> items;
    private List<GameFlag> flags;

    [MenuItem("Tools/デバッグ", false, 1)]
    private static void Open()
    {
        GetWindow<DebugWindow>("デバッグ");
    }

    private void OnEnable()
    {
        Reload();
        EditorApplication.projectChanged += Reload;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= Reload;
    }

    private void OnInspectorUpdate()
    {
        if(Application.isPlaying) { Repaint(); }
    }

    private void Reload()
    {
        items = LoadAll<ItemData>();
        flags = LoadAll<GameFlag>();
        flags.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        Repaint();
    }

    //=====================================================================
    // 表示
    //=====================================================================

    private void OnGUI()
    {
        if(!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("再生中は、アイテム・フラグ・ワープ・回復などをここで操作できます。\n再生前は「再生開始時の状態」を決めておけます。", MessageType.Info);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if(Application.isPlaying)
        {
            DrawItems();
            DrawFlags();
            DrawWarp();
            DrawPlayer();
        }

        DrawStartState();
        DrawSave();

        EditorGUILayout.EndScrollView();
    }

    //=====================================================================
    // アイテム
    //=====================================================================

    private void DrawItems()
    {
        foldItems = EditorGUILayout.BeginFoldoutHeaderGroup(foldItems, "アイテム");
        if(foldItems)
        {
            InventoryManager inventory = InventoryManager.Instance;
            if(inventory == null)
            {
                EditorGUILayout.HelpBox("InventoryManager がありません。", MessageType.Warning);
            }
            else
            {
                foreach(ItemData item in items)
                {
                    if(item == null) { continue; }

                    using(new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(item.DisplayName, GUILayout.MinWidth(100));
                        EditorGUILayout.LabelField("×" + inventory.GetCount(item), GUILayout.Width(40));
                        if(GUILayout.Button("−", GUILayout.Width(24))) { inventory.RemoveItem(item); }
                        if(GUILayout.Button("＋", GUILayout.Width(24))) { inventory.AddItem(item); }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //=====================================================================
    // フラグ
    //=====================================================================

    private void DrawFlags()
    {
        foldFlags = EditorGUILayout.BeginFoldoutHeaderGroup(foldFlags, "フラグ");
        if(foldFlags)
        {
            if(flags.Count == 0) { EditorGUILayout.LabelField("フラグがありません"); }

            foreach(GameFlag flag in flags)
            {
                if(flag == null) { continue; }

                bool value = flag.Value;
                bool next = EditorGUILayout.ToggleLeft($"{flag.Id}{(flag.Saved ? "" : "（一時）")}", value);
                if(next != value) { flag.Value = next; }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //=====================================================================
    // ワープ
    //=====================================================================

    private void DrawWarp()
    {
        foldWarp = EditorGUILayout.BeginFoldoutHeaderGroup(foldWarp, "ワープ");
        if(foldWarp)
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;

            EditorGUILayout.LabelField("このシーンの出現位置", EditorStyles.boldLabel);
            PlayerSpawnPoint[] points = FindObjectsByType<PlayerSpawnPoint>();
            if(points.Length == 0) { EditorGUILayout.LabelField("（PlayerSpawnPoint がありません）"); }

            using(new EditorGUI.DisabledScope(manager == null))
            {
                foreach(PlayerSpawnPoint point in points)
                {
                    if(GUILayout.Button(string.IsNullOrEmpty(point.SpawnPointId) ? point.name : point.SpawnPointId))
                    {
                        manager.Warp(point.NoahPoint.position, point.SiglePoint.position);
                    }
                }

                if(GUILayout.Button("シーンビューの中心へ") && SceneView.lastActiveSceneView != null)
                {
                    Vector3 pivot = SceneView.lastActiveSceneView.pivot;
                    manager.Warp(pivot, pivot + Vector3.left * 0.5f);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("別のシーンへ移動", EditorStyles.boldLabel);

            List<string> scenes = SceneLookup.BuildSceneNames();
            int sceneIndex = Mathf.Max(0, scenes.IndexOf(warpScene));
            if(scenes.Count > 0)
            {
                sceneIndex = EditorGUILayout.Popup("シーン", sceneIndex, scenes.ToArray());
                warpScene = scenes[sceneIndex];

                var spawnOptions = new List<string> { "（デフォルトの位置）" };
                spawnOptions.AddRange(SceneLookup.SpawnPointIds(SceneLookup.FindScenePath(warpScene)));
                int spawnIndex = Mathf.Max(0, spawnOptions.IndexOf(warpSpawn));
                spawnIndex = EditorGUILayout.Popup("出現位置", spawnIndex, spawnOptions.ToArray());
                warpSpawn = spawnIndex == 0 ? "" : spawnOptions[spawnIndex];

                using(new EditorGUI.DisabledScope(SceneFader.Instance == null || SceneFader.IsBusy))
                {
                    if(GUILayout.Button("移動"))
                    {
                        Save.Commit();
                        PlayerSpawnPoint.PendingSpawnPointId = warpSpawn;
                        SceneFader.Instance.FadeAndLoad(warpScene);
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //=====================================================================
    // プレイヤー
    //=====================================================================

    private void DrawPlayer()
    {
        foldPlayer = EditorGUILayout.BeginFoldoutHeaderGroup(foldPlayer, "プレイヤー");
        if(foldPlayer)
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            Health health = manager != null && manager.Noah != null ? manager.Noah.GetComponent<Health>() : null;

            if(health == null)
            {
                EditorGUILayout.LabelField("（プレイヤーの Health が見つかりません）");
            }
            else
            {
                EditorGUILayout.LabelField("体力", $"{health.CurrentHealth} / {health.MaxHealth}");
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("全回復")) { health.Heal(health.MaxHealth); }
                    if(GUILayout.Button("1 ダメージ")) { health.Damage(1); }
                }
                Health.PlayerInvincible = EditorGUILayout.ToggleLeft("無敵（ダメージを受けない。シーンが変わっても続く）", Health.PlayerInvincible);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //=====================================================================
    // 再生開始時の状態
    //=====================================================================

    private void DrawStartState()
    {
        foldStart = EditorGUILayout.BeginFoldoutHeaderGroup(foldStart, "再生開始時の状態（自分の PC だけ）");
        if(foldStart)
        {
            DebugStartState.Enabled = EditorGUILayout.ToggleLeft("再生開始時に、下のアイテムとフラグを与える", DebugStartState.Enabled);

            using(new EditorGUI.DisabledScope(!DebugStartState.Enabled))
            {
                EditorGUILayout.LabelField("持たせるアイテム（個数）", EditorStyles.boldLabel);
                foreach(ItemData item in items)
                {
                    if(item == null) { continue; }
                    int count = DebugStartState.GetItemCount(item);
                    int next = EditorGUILayout.IntField(item.DisplayName, count);
                    if(next != count) { DebugStartState.SetItemCount(item, Mathf.Max(0, next)); }
                }

                EditorGUILayout.LabelField("ON にするフラグ", EditorStyles.boldLabel);
                foreach(GameFlag flag in flags)
                {
                    if(flag == null) { continue; }
                    bool on = DebugStartState.IsFlagOn(flag);
                    bool next = EditorGUILayout.ToggleLeft(flag.Id, on);
                    if(next != on) { DebugStartState.SetFlag(flag, next); }
                }

                if(GUILayout.Button("すべて外す")) { DebugStartState.ClearAll(); }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //=====================================================================
    // セーブ
    //=====================================================================

    private void DrawSave()
    {
        foldSave = EditorGUILayout.BeginFoldoutHeaderGroup(foldSave, "セーブ");
        if(foldSave)
        {
            string path = Path.Combine(Application.persistentDataPath, "save.json");
            EditorGUILayout.LabelField("セーブファイル", File.Exists(path) ? "あり" : "なし");

            bool load = EditorPrefs.GetBool(AutoSave.EditorLoadOnPlayKey, true);
            bool nextLoad = EditorGUILayout.ToggleLeft("再生するときにセーブを読み込む（オフ：毎回まっさらな状態で始める）", load);
            if(nextLoad != load) { EditorPrefs.SetBool(AutoSave.EditorLoadOnPlayKey, nextLoad); }
            if(!nextLoad)
            {
                EditorGUILayout.HelpBox("オフの間にチェックポイントを通ると、それまでのセーブは今回の内容で上書きされます。", MessageType.Warning);
            }

            using(new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if(GUILayout.Button("今の状態をセーブ（チェックポイントと同じ）"))
                {
                    Save.Commit();
                    Save.Write();
                }
            }

            if(GUILayout.Button("セーブファイルを消す"))
            {
                if(EditorUtility.DisplayDialog("セーブファイルを消す", "セーブファイルを削除します。元に戻せません。\n（再生中の所持品やフラグは、再生を止めるまでそのままです）", "消す", "やめる"))
                {
                    if(Application.isPlaying) { Save.Clear(); }
                    if(File.Exists(path)) { File.Delete(path); }
                }
            }

            if(GUILayout.Button("セーブファイルの場所を開く"))
            {
                EditorUtility.RevealInFinder(File.Exists(path) ? path : Application.persistentDataPath);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static List<T> LoadAll<T>() where T : Object
    {
        var list = new List<T>();
        foreach(string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if(asset != null) { list.Add(asset); }
        }
        return list;
    }
}

/// <summary>
/// 再生開始時に与えるアイテム・フラグ（EditorPrefs に保存するので、自分の PC だけで有効）
/// </summary>
[InitializeOnLoad]
public static class DebugStartState
{
    private static readonly string Prefix = "CT.DebugStart." + PlayerSettings.productName + ".";

    static DebugStartState()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(Prefix + "Enabled", false);
        set => EditorPrefs.SetBool(Prefix + "Enabled", value);
    }

    public static int GetItemCount(ItemData item) => EditorPrefs.GetInt(Prefix + "Item." + Guid(item), 0);

    public static void SetItemCount(ItemData item, int count)
    {
        if(count <= 0) { EditorPrefs.DeleteKey(Prefix + "Item." + Guid(item)); }
        else           { EditorPrefs.SetInt(Prefix + "Item." + Guid(item), count); }
    }

    public static bool IsFlagOn(GameFlag flag) => EditorPrefs.GetBool(Prefix + "Flag." + Guid(flag), false);

    public static void SetFlag(GameFlag flag, bool on)
    {
        if(on) { EditorPrefs.SetBool(Prefix + "Flag." + Guid(flag), true); }
        else   { EditorPrefs.DeleteKey(Prefix + "Flag." + Guid(flag)); }
    }

    public static void ClearAll()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:ItemData"))  { EditorPrefs.DeleteKey(Prefix + "Item." + guid); }
        foreach(string guid in AssetDatabase.FindAssets("t:GameFlag"))  { EditorPrefs.DeleteKey(Prefix + "Flag." + guid); }
    }

    private static string Guid(Object asset)
    {
        return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
    }

    //=====================================================================
    // 再生開始時に与える
    //=====================================================================

    private static double waitUntil;

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if(state != PlayModeStateChange.EnteredPlayMode || !Enabled) { return; }

        // InventoryManager ができるまで待つ（最大5秒）
        waitUntil = EditorApplication.timeSinceStartup + 5.0;
        EditorApplication.update -= TryApply;
        EditorApplication.update += TryApply;
    }

    private static void TryApply()
    {
        if(!Application.isPlaying) { EditorApplication.update -= TryApply; return; }

        if(InventoryManager.Instance == null)
        {
            if(EditorApplication.timeSinceStartup > waitUntil)
            {
                Debug.LogWarning("[デバッグ] InventoryManager が見つからないため、開始時のアイテムを与えられませんでした");
                EditorApplication.update -= TryApply;
            }
            return;
        }

        EditorApplication.update -= TryApply;

        var given = new List<string>();

        foreach(string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            int count = EditorPrefs.GetInt(Prefix + "Item." + guid, 0);
            if(count <= 0) { continue; }

            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if(item == null) { continue; }

            int lack = count - InventoryManager.Instance.GetCount(item);
            if(lack > 0) { InventoryManager.Instance.AddItem(item, lack); }
            given.Add($"{item.DisplayName}×{count}");
        }

        foreach(string guid in AssetDatabase.FindAssets("t:GameFlag"))
        {
            if(!EditorPrefs.GetBool(Prefix + "Flag." + guid, false)) { continue; }

            GameFlag flag = AssetDatabase.LoadAssetAtPath<GameFlag>(AssetDatabase.GUIDToAssetPath(guid));
            if(flag == null) { continue; }

            flag.Value = true;
            given.Add(flag.Id);
        }

        if(given.Count > 0) { Debug.Log("[デバッグ] 再生開始時の状態を反映しました: " + string.Join(", ", given)); }
    }
}
