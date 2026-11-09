/*********************************************************************/
// * \file   EnemyPatternEditor.cs
// * \brief  行動パターンの Inspector（段階ごとの行動リスト・「＋行動を追加」）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// 行動パターンの Inspector
/// </summary>
///
/// 行動の部品は SerializeReference なので、Unity の標準の表示では追加できない。
/// 「＋」で部品の種類（EnemyActionMenu の名前）を選べるようにする
[CustomEditor(typeof(EnemyPattern))]
public class EnemyPatternEditor : Editor
{
    private readonly Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();

    private static List<(Type type, EnemyActionMenuAttribute menu)> actionTypes;

    private static List<(Type type, EnemyActionMenuAttribute menu)> ActionTypes
    {
        get
        {
            if(actionTypes != null) { return actionTypes; }

            actionTypes = TypeCache.GetTypesDerivedFrom<EnemyAction>()
                .Where(t => !t.IsAbstract && t.GetCustomAttribute<EnemyActionMenuAttribute>() != null)
                .Select(t => (t, t.GetCustomAttribute<EnemyActionMenuAttribute>()))
                .OrderBy(p => p.Item2.order)
                .ToList();
            return actionTypes;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("段階は上から順に進みます（戻りません）。各段階では、行動を「選び方」に従ってくり返します。\n行動は「＋」で追加し、つまんで並べ替えられます。", MessageType.Info);

        SerializedProperty phases = serializedObject.FindProperty("phases");
        int removeAt = -1;
        int moveUp = -1;

        for(int i = 0; i < phases.arraySize; i++)
        {
            SerializedProperty phase = phases.GetArrayElementAtIndex(i);
            string name = phase.FindPropertyRelative("name").stringValue;

            EditorGUILayout.Space(4);
            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    phase.isExpanded = EditorGUILayout.Foldout(phase.isExpanded, $"段階 {i + 1}：{name}", true, EditorStyles.foldoutHeader);
                    GUI.enabled = i > 0;
                    if(GUILayout.Button("▲", GUILayout.Width(24))) { moveUp = i; }
                    GUI.enabled = phases.arraySize > 1;
                    if(GUILayout.Button("✕", GUILayout.Width(24))) { removeAt = i; }
                    GUI.enabled = true;
                }

                if(phase.isExpanded) { DrawPhase(phase, i == phases.arraySize - 1); }
            }
        }

        if(moveUp > 0) { phases.MoveArrayElement(moveUp, moveUp - 1); }
        if(removeAt >= 0 && EditorUtility.DisplayDialog("段階を消す", $"段階 {removeAt + 1} を消しますか？", "消す", "やめる")) { phases.DeleteArrayElementAtIndex(removeAt); }

        EditorGUILayout.Space(4);
        if(GUILayout.Button("＋ 段階を追加"))
        {
            phases.arraySize++;
            SerializedProperty added = phases.GetArrayElementAtIndex(phases.arraySize - 1);

            // 複製された中身（行動の参照など）を空にする
            added.FindPropertyRelative("name").stringValue = $"段階{phases.arraySize}";
            added.FindPropertyRelative("actions").arraySize = 0;
            added.FindPropertyRelative("exit").enumValueIndex = 0;
            added.isExpanded = true;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("memo"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPhase(SerializedProperty phase, bool isLast)
    {
        EditorGUILayout.PropertyField(phase.FindPropertyRelative("name"));
        EditorGUILayout.PropertyField(phase.FindPropertyRelative("order"));

        SerializedProperty actions = phase.FindPropertyRelative("actions");
        ReorderableList list = GetList(actions);
        list.serializedProperty = actions;
        list.DoLayoutList();

        SerializedProperty exit = phase.FindPropertyRelative("exit");
        EditorGUILayout.PropertyField(exit);

        var exitType = (PhaseExit)exit.enumValueIndex;
        if(exitType != PhaseExit.None && isLast)
        {
            EditorGUILayout.HelpBox("最後の段階なので、次の段階へは進みません（「＋段階を追加」で足してください）", MessageType.None);
        }

        using(new EditorGUI.IndentLevelScope())
        {
            switch(exitType)
            {
                case PhaseExit.HealthBelow: EditorGUILayout.PropertyField(phase.FindPropertyRelative("healthRatio")); break;
                case PhaseExit.Time:        EditorGUILayout.PropertyField(phase.FindPropertyRelative("seconds")); break;
                case PhaseExit.Condition:   EditorGUILayout.PropertyField(phase.FindPropertyRelative("condition"), true); break;
            }
            if(exitType != PhaseExit.None) { EditorGUILayout.PropertyField(phase.FindPropertyRelative("interrupt")); }
        }

        SerializedProperty enterEffect = phase.FindPropertyRelative("enterEffect");
        enterEffect.isExpanded = EditorGUILayout.Foldout(enterEffect.isExpanded, "この段階に入ったとき（演出）", true);
        if(enterEffect.isExpanded)
        {
            using(new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(enterEffect);
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("enterSe"));
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("enterAnimation"));
                EditorGUILayout.PropertyField(phase.FindPropertyRelative("enterPause"));
            }
        }
    }

    private ReorderableList GetList(SerializedProperty actions)
    {
        if(lists.TryGetValue(actions.propertyPath, out ReorderableList cached)) { return cached; }

        var list = new ReorderableList(serializedObject, actions, true, true, true, true);

        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "行動（＋で追加。つまんで並べ替え）");

        list.elementHeightCallback = index =>
        {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 4.0f;
        };

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            var action = element.managedReferenceValue as EnemyAction;
            string label = action != null ? $"{index + 1}. {action.Summary}" : $"{index + 1}. （空）";

            rect.x += 10.0f;
            rect.width -= 10.0f;
            rect.y += 2.0f;
            EditorGUI.PropertyField(rect, element, new GUIContent(label), true);
        };

        list.onAddDropdownCallback = (rect, l) =>
        {
            var menu = new GenericMenu();
            foreach(var (type, attr) in ActionTypes)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(attr.path), false, () =>
                {
                    SerializedProperty prop = l.serializedProperty;
                    prop.serializedObject.Update();
                    prop.arraySize++;
                    SerializedProperty added = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                    added.managedReferenceValue = Activator.CreateInstance(captured);
                    added.isExpanded = true;
                    prop.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        };

        lists[actions.propertyPath] = list;
        return list;
    }
}
