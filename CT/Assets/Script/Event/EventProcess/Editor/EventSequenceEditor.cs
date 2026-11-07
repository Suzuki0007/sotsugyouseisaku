/*********************************************************************/
// * \file   EventSequenceEditor.cs
// * \brief  EventSequence の Inspector 表示
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EventSequence の Inspector に、登録忘れの警告と「まとめて追加」ボタンを出す
/// </summary>
[CustomEditor(typeof(EventSequence))]
public class EventSequenceEditor : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty actionsProp = serializedObject.FindProperty("actions");
        EventSequence sequence = (EventSequence)target;

        DrawDefaultInspector();

        // 登録されている部品
        var registered = new HashSet<Object>();
        bool hasNull = false;
        for(int i = 0; i < actionsProp.arraySize; i++)
        {
            Object obj = actionsProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if(obj == null) { hasNull = true; }
            else            { registered.Add(obj); }
        }

        // 同じオブジェクトにあるのに登録されていない部品
        var missing = new List<EventAction>();
        foreach(EventAction action in sequence.GetComponents<EventAction>())
        {
            if(!registered.Contains(action)) { missing.Add(action); }
        }

        EditorGUILayout.Space();

        if(actionsProp.arraySize == 0)
        {
            if(missing.Count > 0)
            {
                EditorGUILayout.HelpBox("Actions が空なので、このオブジェクトの部品を上から順に実行します：\n" + Names(missing), MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("実行する部品がありません。Add Component で部品（〇〇EventAction）を追加してください。", MessageType.Warning);
            }
        }
        else if(missing.Count > 0)
        {
            EditorGUILayout.HelpBox("このオブジェクトに付いているのに Actions に入っていない部品があります（実行されません）：\n" + Names(missing), MessageType.Warning);

            if(GUILayout.Button("入っていない部品を Actions の最後に追加"))
            {
                foreach(EventAction action in missing)
                {
                    int index = actionsProp.arraySize;
                    actionsProp.InsertArrayElementAtIndex(index);
                    actionsProp.GetArrayElementAtIndex(index).objectReferenceValue = action;
                }
                serializedObject.ApplyModifiedProperties();
            }
        }

        if(hasNull)
        {
            EditorGUILayout.HelpBox("Actions に空の欄があります（飛ばされます）。", MessageType.Warning);
        }

        if(Application.isPlaying)
        {
            EditorGUILayout.LabelField("状態（再生中）", sequence.IsPlaying ? "実行中" : "待機中");
        }
    }

    private static string Names(List<EventAction> actions)
    {
        var names = new List<string>();
        foreach(EventAction action in actions) { names.Add("・" + action.GetType().Name); }
        return string.Join("\n", names);
    }
}
