/*********************************************************************/
// * \file   CinemaLookWindow.cs
// * \brief  Tools > 会話 > シネマ演出の見た目：黒帯・黒いぼかし・会話の枠の大きさと位置を調整する窓
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// シネマ演出の見た目を調整する窓
/// </summary>
///
/// ・`Resources/CinemaLookSettings.asset`（全部のシーンで共通）の値をスライダーで変える
/// ・再生中に動かすと、出ている会話・帯にすぐ反映される（値は再生を止めても残る）
/// ・再生中は「試しの会話を出す」で、会話を出して見ながら調整できる
public class CinemaLookWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/CinemaLookSettings.asset";

    private CinemaLookSettings settings;
    private SerializedObject so;

    [MenuItem("Tools/会話/シネマ演出の見た目（黒帯・ぼかし・会話の枠）")]
    private static void Open()
    {
        var w = GetWindow<CinemaLookWindow>("シネマ演出の見た目");
        w.minSize = new Vector2(360.0f, 260.0f);
        w.Show();
    }

    private void OnEnable()
    {
        Load();
    }

    private void Load()
    {
        settings = AssetDatabase.LoadAssetAtPath<CinemaLookSettings>(AssetPath);
        so = settings != null ? new SerializedObject(settings) : null;
    }

    private void OnGUI()
    {
        if(settings == null || so == null)
        {
            EditorGUILayout.HelpBox("設定（" + AssetPath + "）がありません。", MessageType.Warning);
            if(GUILayout.Button("設定を作る")) { Create(); }
            return;
        }

        EditorGUILayout.HelpBox("全部のシーンの会話で共通の見た目です。再生中に動かすと、出ている会話にすぐ反映されます（値は再生を止めても残ります）。", MessageType.Info);

        so.Update();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("黒帯", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("barHeight"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("下の黒いぼかし（フェード）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("fadeHeight"));
        EditorGUILayout.PropertyField(so.FindProperty("fadeStrength"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("会話の枠", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("boxScale"));
        EditorGUILayout.PropertyField(so.FindProperty("boxOffset"));

        if(EditorGUI.EndChangeCheck())
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            Refresh();
        }

        EditorGUILayout.Space();
        using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if(GUILayout.Button("試しの会話を出す（再生中）")) { ShowSample(); }
        }
        if(!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("再生中にこの窓を開いたまま「試しの会話を出す」を押すと、会話を見ながら調整できます。", MessageType.None);
        }

        EditorGUILayout.Space();
        if(GUILayout.Button("初めの値に戻す"))
        {
            Undo.RecordObject(settings, "シネマ演出の見た目を初めの値に戻す");
            var fresh = CreateInstance<CinemaLookSettings>();
            settings.barHeight = fresh.barHeight;
            settings.fadeHeight = fresh.fadeHeight;
            settings.fadeStrength = fresh.fadeStrength;
            settings.boxScale = fresh.boxScale;
            settings.boxOffset = fresh.boxOffset;
            DestroyImmediate(fresh);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            so.Update();
            Refresh();
        }
    }

    // 再生中なら、出ている会話・帯にすぐ反映する
    private static void Refresh()
    {
        if(!EditorApplication.isPlaying) { return; }
        if(DialogueManager.Instance != null) { DialogueManager.Instance.RefreshCinemaLook(); }
    }

    // 試しの会話（2文。長い文で折り返しも見られるように）
    private static void ShowSample()
    {
        DialogueManager dm = DialogueManager.Instance;
        if(dm == null) { Debug.LogWarning("[シネマ演出の見た目] このシーンには会話の仕組み（DialogueManager）がありません"); return; }
        if(dm.IsTalking) { return; }

        CharacterProfile noah = AssetDatabase.LoadAssetAtPath<CharacterProfile>("Assets/Data/Characters/Noah.asset");
        var a = new DialogueData.Line("", "（試しの会話）会話の枠の大きさと位置を見ています。") { character = noah };
        var b = new DialogueData.Line("", "長い文だと、こんなふうに折り返して二行になります。読みやすい大きさになっているでしょうか。") { character = noah };
        dm.StartDialogue(new[] { a, b });
    }

    private void Create()
    {
        var created = CreateInstance<CinemaLookSettings>();
        AssetDatabase.CreateAsset(created, AssetPath);
        AssetDatabase.SaveAssets();
        Load();
    }
}
