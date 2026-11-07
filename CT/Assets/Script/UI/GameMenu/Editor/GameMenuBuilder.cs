/*********************************************************************/
// * \file   GameMenuBuilder.cs
// * \brief  メニュー画面のプレハブを作るエディタ拡張
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static MenuUIBuilder;

/// <summary>
/// Tools > メニュー画面 > メニューのプレハブを作成
/// Assets/Resources/UI/GameMenu.prefab を作る（ゲーム中に自動で生成される）
/// 見た目はプレハブを開いて自由に変えてよい（作り直すと元に戻る）
/// </summary>
public static class GameMenuBuilder
{
    public const string PrefabPath = "Assets/Resources/UI/GameMenu.prefab";

    [MenuItem("Tools/メニュー画面/メニューのプレハブを作成")]
    private static void BuildFromMenu()
    {
        if(System.IO.File.Exists(PrefabPath) &&
           !EditorUtility.DisplayDialog("メニューのプレハブを作成", "GameMenu.prefab はすでにあります。作り直しますか？（手で変えた見た目は消えます）", "作り直す", "やめる"))
        {
            return;
        }

        Build();
    }

    /// <summary>
    /// プレハブを作る（一時的なシーンで組み立てて保存する）
    /// </summary>
    public static void Build()
    {
        EnsurePlaceNameTable();

        Scene active = SceneManager.GetActiveScene();
        Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temp);

        // ほかの UI（会話 100、アイテム画面など）より手前に出す
        Canvas canvas = CreateCanvas("GameMenu", 500);
        Transform root = canvas.transform;

        // メニュー（画面を暗くして中央に箱）
        var menuPanel = CreateImage("MenuPanel", root, new Color(0, 0, 0, 0.55f));
        Stretch(menuPanel.rectTransform);
        var box = CreateImage("Box", menuPanel.transform, new Color(0.08f, 0.08f, 0.12f, 0.96f));
        SetRect(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 560));
        var title = CreateText("Title", box.transform, "メニュー", 44, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(500, 70));
        var list = CreateRect("Buttons", box.transform);
        SetRect(list, new Vector2(0.5f, 0.5f), new Vector2(0, -35), new Vector2(440, 380));
        AddVerticalLayout(list, 16);
        Button item = CreateButton("ItemButton", list, "アイテム", 38);
        Button load = CreateButton("LoadButton", list, "ロード", 38);
        Button titleBtn = CreateButton("TitleButton", list, "タイトルに戻る", 38);
        Button close = CreateButton("CloseButton", list, "とじる（Tab / Esc）", 32);
        menuPanel.gameObject.SetActive(false);

        // 共通の画面
        SaveSlotPanel slotPanel = CreateSlotPanel(root);
        ConfirmDialog confirm = CreateConfirmDialog(root);

        // メッセージ（「セーブしました」）
        var message = CreateImage("Message", root, new Color(0.08f, 0.08f, 0.12f, 0.92f));
        SetRect(message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 140), new Vector2(760, 90));
        message.raycastTarget = false;
        var messageText = CreateText("Text", message.transform, "", 36, TextAlignmentOptions.Center);
        Stretch(messageText.rectTransform);
        message.gameObject.SetActive(false);

        var menu = canvas.gameObject.AddComponent<GameMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("menuPanel").objectReferenceValue = menuPanel.gameObject;
        so.FindProperty("itemButton").objectReferenceValue = item;
        so.FindProperty("loadButton").objectReferenceValue = load;
        so.FindProperty("titleButton").objectReferenceValue = titleBtn;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("slotPanel").objectReferenceValue = slotPanel;
        so.FindProperty("confirm").objectReferenceValue = confirm;
        so.FindProperty("messagePanel").objectReferenceValue = message.gameObject;
        so.FindProperty("messageText").objectReferenceValue = messageText;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PrefabPath);

        if(active.IsValid()) { SceneManager.SetActiveScene(active); }
        EditorSceneManager.CloseScene(temp, true);

        Debug.Log("[メニュー画面] " + PrefabPath + " を作成しました");
    }
}
