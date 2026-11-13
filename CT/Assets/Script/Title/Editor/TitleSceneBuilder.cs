/*********************************************************************/
// * \file   TitleSceneBuilder.cs
// * \brief  タイトル画面のシーンを作るエディタ拡張
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static MenuUIBuilder;

/// <summary>
/// Tools > タイトル画面 > タイトルシーンを作成
/// ・Assets/Scenes/Title.unity を作り（すでにあれば確認してから作り直す）、Build Settings の先頭に入れる
/// ・背景・メニュー・セーブデータ選択画面（共通の SaveSlotPanel）と TitleMenu を配線する
/// 作ったあとは、見た目（画像・色・文字・位置）を Inspector で自由に変えてよい
/// </summary>
public static class TitleSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Title.unity";
    private const string BackgroundPath = "Assets/Images/UI/UI_TitleImage.PNG";

    [MenuItem("Tools/タイトル画面/タイトルシーンを作成")]
    private static void BuildFromMenu()
    {
        if(System.IO.File.Exists(ScenePath) &&
           !EditorUtility.DisplayDialog("タイトルシーンを作成", "Title.unity はすでにあります。作り直しますか？（手で変えた見た目は消えます）", "作り直す", "やめる"))
        {
            return;
        }
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

        Build();
    }

    /// <summary>
    /// タイトルシーンを作って保存し、Build Settings の先頭に入れる
    /// </summary>
    public static void Build()
    {
        EnsurePlaceNameTable();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // カメラ（背景の白に合わせる）
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        // システム（60fps・BGM を止める・UI の入力）
        var system = new GameObject("System");
        system.AddComponent<Fps>();
        system.AddComponent<SceneBgm>();   // 曲を設定していないので、ステージの BGM が止まる（タイトル曲を流すなら Default Clip に入れる）
        var es = new GameObject("EventSystem");
        es.transform.SetParent(system.transform, false);
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // キャンバス
        Canvas canvas = CreateCanvas("TitleCanvas", 0);
        Transform root = canvas.transform;

        // 背景（タイトルロゴ入りの画像）
        var bg = CreateImage("Background", root, Color.white);
        Stretch(bg.rectTransform);
        bg.sprite = LoadSprite(BackgroundPath);
        bg.raycastTarget = false;
        // 縦横比を保ったまま画面いっぱいに（はみ出た上下・左右は切れる）
        if(bg.sprite != null)
        {
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = bg.sprite.rect.width / bg.sprite.rect.height;
        }

        // メインメニュー
        var main = CreateRect("MainMenu", root);
        SetRect(main, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(460, 270));
        AddVerticalLayout(main, 15);
        Button newGame = CreateButton("NewGameButton", main, "はじめから", 40);
        Button cont = CreateButton("ContinueButton", main, "つづきから", 40);
        Button quit = CreateButton("QuitButton", main, "ゲーム終了", 40);

        // セーブデータ選択画面（メニュー画面・セーブポイントと共通）
        SaveSlotPanel slotPanel = CreateSlotPanel(root);

        // TitleMenu を配線する
        var menu = canvas.gameObject.AddComponent<TitleMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("firstScene").stringValue = "Stage1_1";
        so.FindProperty("firstSpawnPointId").stringValue = "";
        so.FindProperty("mainMenu").objectReferenceValue = main.gameObject;
        so.FindProperty("newGameButton").objectReferenceValue = newGame;
        so.FindProperty("continueButton").objectReferenceValue = cont;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("slotPanel").objectReferenceValue = slotPanel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettingsFirst(ScenePath);
        Debug.Log("[タイトル画面] " + ScenePath + " を作成し、Build Settings の先頭に入れました");
    }

    private static Sprite LoadSprite(string path)
    {
        foreach(Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if(o is Sprite s) { return s; }
        }
        return null;
    }

    private static void AddToBuildSettingsFirst(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == path);
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
