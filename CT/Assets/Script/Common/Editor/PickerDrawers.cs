/*********************************************************************/
// * \file   PickerDrawers.cs
// * \brief  SceneName / SpawnPointId 属性の Inspector 表示
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 選択式の欄を描く共通処理
/// ・候補にない値が入っているときは「⚠ 値（理由）」を選択肢に出して、値を消さずに残す
/// ・「手入力」を選ぶと、文字を直接打てる欄に切り替わる
/// </summary>
internal static class PickerGUI
{
    private const string ManualLabel = "（手入力に切り替え）";

    // 手入力モードにしている欄（オブジェクト + 欄のパス）
    private static readonly HashSet<string> manual = new HashSet<string>();

    public static void Draw(Rect position, SerializedProperty property, GUIContent label,
                            List<string> candidates, string emptyLabel, string unknownReason)
    {
        if(property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "string の欄にだけ使えます");
            return;
        }

        string key = property.serializedObject.targetObject.GetHashCode() + "/" + property.propertyPath;
        string value = property.stringValue ?? "";

        EditorGUI.BeginProperty(position, label, property);

        if(manual.Contains(key))
        {
            Rect field = new Rect(position.x, position.y, position.width - 50, position.height);
            Rect button = new Rect(position.xMax - 48, position.y, 48, position.height);

            property.stringValue = EditorGUI.TextField(field, label, value);
            if(GUI.Button(button, "選択")) { manual.Remove(key); }

            EditorGUI.EndProperty();
            return;
        }

        var options = new List<string> { emptyLabel };
        options.AddRange(candidates);

        int index = 0;
        if(value.Length > 0)
        {
            index = candidates.IndexOf(value) + 1;
            if(index == 0)
            {
                options.Add($"⚠ {value}（{unknownReason}）");
                index = options.Count - 1;
            }
        }
        options.Add(ManualLabel);

        Color old = GUI.color;
        if(value.Length > 0 && !candidates.Contains(value)) { GUI.color = new Color(1f, 0.6f, 0.4f); }

        int next = EditorGUI.Popup(position, label.text, index, options.ToArray());

        GUI.color = old;

        if(next != index)
        {
            if(next == options.Count - 1) { manual.Add(key); }
            else if(next == 0)            { property.stringValue = ""; }
            else if(next - 1 < candidates.Count) { property.stringValue = candidates[next - 1]; }
        }

        EditorGUI.EndProperty();
    }
}

/// <summary>
/// [SceneName] の欄：Build Settings のシーンから選ぶ
/// </summary>
[CustomPropertyDrawer(typeof(SceneNameAttribute))]
public class SceneNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        PickerGUI.Draw(position, property, label, SceneLookup.BuildSceneNames(),
                       "（なし）", "Build Settings に入っていません");
    }
}

/// <summary>
/// [SpawnPointId(シーン名の欄)] の欄：そのシーンにある PlayerSpawnPoint の ID から選ぶ
/// </summary>
[CustomPropertyDrawer(typeof(SpawnPointIdAttribute))]
public class SpawnPointIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (SpawnPointIdAttribute)attribute;

        // 同じ階層にあるシーン名の欄を探す
        string path = property.propertyPath;
        int dot = path.LastIndexOf('.');
        string scenePath = dot >= 0 ? path.Substring(0, dot + 1) + attr.SceneField : attr.SceneField;
        SerializedProperty sceneProp = property.serializedObject.FindProperty(scenePath);

        string sceneName = sceneProp != null ? sceneProp.stringValue : "";
        string sceneFile = SceneLookup.FindScenePath(sceneName);

        List<string> ids = SceneLookup.SpawnPointIds(sceneFile);
        string reason = string.IsNullOrEmpty(sceneFile) ? "遷移先のシーンが見つかりません" : $"{sceneName} にありません";

        PickerGUI.Draw(position, property, label, ids, "（デフォルトの位置）", reason);
    }
}
