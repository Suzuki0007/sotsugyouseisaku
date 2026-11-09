/*********************************************************************/
// * \file   PuzzleMenu.cs
// * \brief  Hierarchy 右クリック > 仕掛け > 謎解き > …（番号錠・メモ・調べるたびに変わる・即死の罠・やり直す位置）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 謎解き（ホラー）の仕掛けを置くメニュー
/// </summary>
public static class PuzzleMenu
{
    public const string SettingsPath = "Assets/Resources/PuzzleUI.asset";
    public const string NotesFolder = "Assets/Resources/Notes";

    [MenuItem("GameObject/仕掛け/謎解き/番号錠", false, 60)]
    private static void PlaceCodeLock(MenuCommand command)
    {
        EnsureSettings();
        var go = new GameObject("番号錠");
        GimmickMenu.Look(go, GimmickMenu.Square, new Vector2(0.8f, 0.8f), new Color(0.5f, 0.55f, 0.6f, 1.0f), 4);
        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 1.0f;
        go.AddComponent<GimmickState>();
        go.AddComponent<CodeLock>();
        GimmickMenu.Place(go, command, "番号錠");
    }

    [MenuItem("GameObject/仕掛け/謎解き/メモ（調べると読む）", false, 61)]
    private static void PlaceNote(MenuCommand command)
    {
        EnsureSettings();
        System.IO.Directory.CreateDirectory(NotesFolder);
        var go = new GameObject("メモ");
        GimmickMenu.Look(go, GimmickMenu.Square, new Vector2(0.45f, 0.55f), new Color(0.93f, 0.9f, 0.82f, 1.0f), 4);
        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.8f;
        go.AddComponent<NotePickup>();
        GimmickMenu.Place(go, command, "メモ");
    }

    [MenuItem("GameObject/仕掛け/謎解き/調べるたびに変わる", false, 62)]
    private static void PlaceExamine(MenuCommand command)
    {
        var go = new GameObject("調べるたびに変わる");
        GimmickMenu.Look(go, GimmickMenu.Square, new Vector2(0.8f, 0.8f), new Color(0.7f, 0.6f, 0.8f, 1.0f), 4);
        var area = go.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 1.0f;

        var exam = go.AddComponent<ExamineSequence>();
        var so = new SerializedObject(exam);
        SerializedProperty list = so.FindProperty("sequences");
        for(int i = 0; i < 3; i++)
        {
            var child = new GameObject($"{i + 1}回目");
            child.transform.SetParent(go.transform, false);
            var seq = child.AddComponent<EventSequence>();
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = seq;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        GimmickMenu.Place(go, command, "調べるたびに変わる");
    }

    [MenuItem("GameObject/仕掛け/謎解き/即死の罠", false, 63)]
    private static void PlaceDeathTrap(MenuCommand command)
    {
        var go = new GameObject("即死の罠");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.0f, 2.0f);

        var child = new GameObject("即死の演出");
        child.transform.SetParent(go.transform, false);
        var seq = child.AddComponent<EventSequence>();

        var trap = go.AddComponent<DeathTrap>();
        var so = new SerializedObject(trap);
        so.FindProperty("beforeDeath").objectReferenceValue = seq;
        so.ApplyModifiedPropertiesWithoutUndo();
        GimmickMenu.Place(go, command, "即死の罠");
    }

    [MenuItem("GameObject/仕掛け/謎解き/やり直す位置（部屋の入口）", false, 64)]
    private static void PlaceRetryPoint(MenuCommand command)
    {
        var go = new GameObject("やり直す位置");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.0f, 2.0f);

        var respawn = new GameObject("出る位置");
        respawn.transform.SetParent(go.transform, false);

        var retry = go.AddComponent<RetryPoint>();
        var so = new SerializedObject(retry);
        so.FindProperty("respawn").objectReferenceValue = respawn.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        GimmickMenu.Place(go, command, "やり直す位置");
    }

    //=====================================================================
    // 設定・メニュー画面
    //=====================================================================

    [MenuItem("Tools/仕掛け/謎解きの画面の設定を作る", false, 320)]
    private static void CreateSettingsMenu()
    {
        PuzzleUISettings s = EnsureSettings();
        Selection.activeObject = s;
        EditorGUIUtility.PingObject(s);
    }

    /// <summary> 謎解きの画面の設定（Resources/PuzzleUI.asset）がなければ作る </summary>
    public static PuzzleUISettings EnsureSettings()
    {
        var s = AssetDatabase.LoadAssetAtPath<PuzzleUISettings>(SettingsPath);
        if(s != null) { return s; }

        s = ScriptableObject.CreateInstance<PuzzleUISettings>();
        s.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NotoSansJP-Regular SDF.asset");
        System.IO.Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.CreateAsset(s, SettingsPath);
        AssetDatabase.SaveAssets();
        return s;
    }

    /// <summary>
    /// 今あるメニュー画面のプレハブに「メモ」のボタンを足す（作り直さずに。手で変えた見た目を残すため）
    /// </summary>
    [MenuItem("Tools/メニュー画面/メニューに「メモ」を足す", false, 10)]
    public static void AddNoteButtonToMenu()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameMenuBuilder.PrefabPath);
        try
        {
            var menu = root.GetComponent<GameMenu>();
            var so = new SerializedObject(menu);
            if(so.FindProperty("noteButton").objectReferenceValue != null) { Debug.Log("[メニュー画面] もう「メモ」があります"); return; }

            var item = so.FindProperty("itemButton").objectReferenceValue as UnityEngine.UI.Button;
            if(item == null) { Debug.LogError("[メニュー画面] アイテムのボタンが見つかりません"); return; }

            // アイテムのボタンを写して、すぐ下に置く
            GameObject copy = Object.Instantiate(item.gameObject, item.transform.parent);
            copy.name = "NoteButton";
            copy.transform.SetSiblingIndex(item.transform.GetSiblingIndex() + 1);
            var label = copy.GetComponentInChildren<TMP_Text>();
            if(label != null) { label.text = "メモ"; }
            var button = copy.GetComponent<UnityEngine.UI.Button>();
            button.onClick.RemoveAllListeners();

            // ボタン 1 つ分、箱と並びを高くする
            var list = item.transform.parent as RectTransform;
            var box = list != null ? list.parent as RectTransform : null;
            float add = ((RectTransform)item.transform).rect.height + 16.0f;
            if(list != null) { list.sizeDelta += new Vector2(0, add); }
            if(box != null)  { box.sizeDelta += new Vector2(0, add); }

            so.FindProperty("noteButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, GameMenuBuilder.PrefabPath);
            Debug.Log("[メニュー画面] 「メモ」のボタンを足しました");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
