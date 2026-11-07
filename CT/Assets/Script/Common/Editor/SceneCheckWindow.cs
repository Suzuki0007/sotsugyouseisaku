/*********************************************************************/
// * \file   SceneCheckWindow.cs
// * \brief  シーンチェックウィンドウ
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > シーンチェック
/// ・設定ミス（IDの重なり、空の参照、Build Settings の入れ忘れなど）を一覧にする
/// ・行をクリックすると、そのオブジェクトを選択する（閉じているシーンなら開く）
/// </summary>
public class SceneCheckWindow : EditorWindow
{
    private readonly List<SceneChecker.Issue> issues = new List<SceneChecker.Issue>();
    private bool showError = true;
    private bool showWarning = true;
    private bool showInfo = false;
    // 結果の一覧はスクリプトの再読み込みで消えるので、「チェック済み」も一緒に消えるようにする
    [System.NonSerialized] private bool hasChecked;
    private Vector2 scroll;

    // スクリプトの再読み込み後などは、開いているシーンを自動でチェックし直す
    private void OnEnable()
    {
        EditorApplication.delayCall += () =>
        {
            if(this != null && !hasChecked && !EditorApplication.isPlayingOrWillChangePlaymode) { CheckOpenScenes(); }
        };
    }

    [MenuItem("Tools/シーンチェック", false, 0)]
    private static void Open()
    {
        var window = GetWindow<SceneCheckWindow>("シーンチェック");
        window.CheckOpenScenes();
    }

    //=====================================================================
    // チェック
    //=====================================================================

    private void CheckOpenScenes()
    {
        issues.Clear();
        SceneChecker.CheckProject(issues);

        for(int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if(scene.isLoaded) { SceneChecker.CheckScene(scene, issues); }
        }

        Finish();
    }

    private void CheckBuildScenes()
    {
        if(Application.isPlaying)
        {
            EditorUtility.DisplayDialog("シーンチェック", "再生中は全シーンのチェックはできません。再生を止めてから実行してください。", "OK");
            return;
        }

        issues.Clear();
        SceneChecker.CheckProject(issues);

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        try
        {
            for(int i = 0; i < scenes.Length; i++)
            {
                if(!scenes[i].enabled || !File.Exists(scenes[i].path)) { continue; }

                EditorUtility.DisplayProgressBar("シーンチェック", scenes[i].path, (float)i / scenes.Length);

                Scene scene = SceneManager.GetSceneByPath(scenes[i].path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if(!wasOpen) { scene = EditorSceneManager.OpenScene(scenes[i].path, OpenSceneMode.Additive); }

                SceneChecker.CheckScene(scene, issues);

                if(!wasOpen) { EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Finish();
    }

    private void Finish()
    {
        issues.Sort((a, b) => a.level != b.level ? a.level.CompareTo(b.level) : string.CompareOrdinal(a.sceneName, b.sceneName));
        hasChecked = true;
        Repaint();
    }

    //=====================================================================
    // 表示
    //=====================================================================

    private void OnGUI()
    {
        int errors = Count(SceneChecker.Level.Error);
        int warnings = Count(SceneChecker.Level.Warning);
        int infos = Count(SceneChecker.Level.Info);

        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if(GUILayout.Button("開いているシーンをチェック", EditorStyles.toolbarButton)) { CheckOpenScenes(); }
            if(GUILayout.Button("Build Settings の全シーンをチェック", EditorStyles.toolbarButton)) { CheckBuildScenes(); }
            GUILayout.FlexibleSpace();
        }

        // 絞り込みは2段目に置く（ウィンドウが狭くても隠れないように）
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            showError = GUILayout.Toggle(showError, $"エラー {errors}", EditorStyles.toolbarButton);
            showWarning = GUILayout.Toggle(showWarning, $"注意 {warnings}", EditorStyles.toolbarButton);
            showInfo = GUILayout.Toggle(showInfo, $"参考 {infos}", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
        }

        if(!hasChecked)
        {
            EditorGUILayout.HelpBox("上のボタンでチェックを始めてください。", MessageType.Info);
            return;
        }

        if(errors + warnings == 0)
        {
            EditorGUILayout.HelpBox("エラー・注意はありません。", MessageType.Info);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach(SceneChecker.Issue issue in issues)
        {
            if(issue.level == SceneChecker.Level.Error && !showError) { continue; }
            if(issue.level == SceneChecker.Level.Warning && !showWarning) { continue; }
            if(issue.level == SceneChecker.Level.Info && !showInfo) { continue; }

            DrawIssue(issue);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawIssue(SceneChecker.Issue issue)
    {
        MessageType type = issue.level == SceneChecker.Level.Error ? MessageType.Error :
                           issue.level == SceneChecker.Level.Warning ? MessageType.Warning : MessageType.Info;

        string where = string.IsNullOrEmpty(issue.sceneName) ? "プロジェクト" : issue.sceneName;
        if(!string.IsNullOrEmpty(issue.objectPath)) { where += " / " + issue.objectPath; }
        else if(issue.target != null && string.IsNullOrEmpty(issue.sceneName)) { where += " / " + issue.target.name; }

        using(new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.HelpBox($"{issue.message}\n{where}", type);

            if(GUILayout.Button("選択", GUILayout.Width(44), GUILayout.Height(38))) { Select(issue); }
        }
    }

    private int Count(SceneChecker.Level level)
    {
        int count = 0;
        foreach(SceneChecker.Issue issue in issues) { if(issue.level == level) { count++; } }
        return count;
    }

    //=====================================================================
    // 選択
    //=====================================================================

    private static void Select(SceneChecker.Issue issue)
    {
        Object target = issue.target;

        // シーンを閉じたなどで参照が切れていたら、シーンを開いて探す
        if(target == null && !string.IsNullOrEmpty(issue.scenePath) && !string.IsNullOrEmpty(issue.objectPath))
        {
            Scene scene = SceneManager.GetSceneByPath(issue.scenePath);
            if(!scene.IsValid() || !scene.isLoaded)
            {
                if(Application.isPlaying) { return; }
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
                scene = EditorSceneManager.OpenScene(issue.scenePath, OpenSceneMode.Single);
            }
            target = FindByPath(scene, issue.objectPath);
            issue.target = target;
        }

        if(target == null) { return; }

        Selection.activeObject = target;
        EditorGUIUtility.PingObject(target);
        if(target is GameObject && SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.FrameSelected(); }
    }

    private static GameObject FindByPath(Scene scene, string path)
    {
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if(SceneChecker.HierarchyPath(t) == path) { return t.gameObject; }
            }
        }
        return null;
    }
}
