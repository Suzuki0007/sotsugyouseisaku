/*********************************************************************/
// * \file   EventSequenceEditor.cs
// * \brief  EventSequence の Inspector 表示（流れの一覧・部品の追加・その場で試す）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// EventSequence の Inspector
/// </summary>
///
/// ・「流れ」：実行する部品を、番号・日本語名・中身の一言要約で上から並べる（▶ で開くとその部品の設定をここで直せる）
///   ↑↓ で順番を入れ替え、× で外す。「＋ 部品を追加」で分類ごとのメニューから足す
/// ・再生中は、今実行している部品に印を付け、「今すぐ実行」「止める」でその場で試せる
/// ・登録忘れ（このオブジェクトに付いているのに Actions に入っていない部品）の警告
[CustomEditor(typeof(EventSequence))]
public class EventSequenceEditor : Editor
{
    private readonly Dictionary<Object, bool> opened = new Dictionary<Object, bool>();
    private readonly Dictionary<Object, Editor> inlineEditors = new Dictionary<Object, Editor>();
    private bool showRawList;

    private static GUIStyle rowStyle;
    private static GUIStyle summaryStyle;

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    private void OnDisable()
    {
        foreach(Editor e in inlineEditors.Values) { if(e != null) { DestroyImmediate(e); } }
        inlineEditors.Clear();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EventSequence sequence = (EventSequence)target;
        SerializedProperty actionsProp = serializedObject.FindProperty("actions");

        EditorGUILayout.PropertyField(serializedObject.FindProperty("playMode"), new GUIContent("実行のしかた"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lockPlayer"), new GUIContent("プレイヤーを止める", "実行中はプレイヤーを動けなくする（演出の待ち時間に歩き回らせたくないとき）"));

        EditorGUILayout.Space();
        DrawPlayButtons(sequence);
        EditorGUILayout.Space();

        bool explicitList = actionsProp.arraySize > 0;
        var list = new List<EventAction>();
        if(explicitList)
        {
            for(int i = 0; i < actionsProp.arraySize; i++) { list.Add(actionsProp.GetArrayElementAtIndex(i).objectReferenceValue as EventAction); }
        }
        else
        {
            list.AddRange(sequence.GetComponents<EventAction>());
        }

        DrawFlow(sequence, actionsProp, list, explicitList);
        DrawWarnings(sequence, actionsProp);

        EditorGUILayout.Space();
        showRawList = EditorGUILayout.Foldout(showRawList, "詳しい設定（Actions の欄を直接編集）", true);
        if(showRawList)
        {
            EditorGUILayout.PropertyField(actionsProp, new GUIContent("Actions（空なら、このオブジェクトの部品を上から順に）"), true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    //=====================================================================
    // その場で試す
    //=====================================================================

    private static void DrawPlayButtons(EventSequence sequence)
    {
        if(!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("再生中は、ここの「今すぐ実行」でこのイベントをその場で試せます（調べる・エリアに入るなどのきっかけなしで）。", MessageType.None);
            return;
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = sequence.isActiveAndEnabled && !sequence.IsPlaying;
            if(GUILayout.Button("▶ 今すぐ実行（操作中のキャラで）", GUILayout.Height(24)))
            {
                PlayerSwitchManager m = PlayerSwitchManager.Instance;
                sequence.Play(m != null ? m.CurrentPlayer : null);
            }
            GUI.enabled = sequence.IsPlaying;
            if(GUILayout.Button("■ 止める", GUILayout.Width(80), GUILayout.Height(24))) { sequence.Stop(); }
            GUI.enabled = true;
        }

        string state = sequence.IsPlaying ? "実行中" : "待機中";
        if(!sequence.isActiveAndEnabled) { state += "（非表示なので順番に実行できない）"; }
        EditorGUILayout.LabelField("状態", state);
    }

    //=====================================================================
    // 流れ
    //=====================================================================

    private void DrawFlow(EventSequence sequence, SerializedProperty actionsProp, List<EventAction> list, bool explicitList)
    {
        if(rowStyle == null)
        {
            rowStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(4, 4, 2, 2) };
            summaryStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        }

        EditorGUILayout.LabelField(explicitList ? "流れ（Actions の順に実行）" : "流れ（このオブジェクトの部品を上から順に実行）", EditorStyles.boldLabel);

        Action deferred = null;
        EventAction current = Application.isPlaying ? sequence.CurrentAction : null;

        for(int i = 0; i < list.Count; i++)
        {
            EventAction action = list[i];
            int index = i;

            Color old = GUI.backgroundColor;
            if(action != null && action == current) { GUI.backgroundColor = new Color(0.55f, 0.9f, 0.55f); }

            using(new EditorGUILayout.VerticalScope(rowStyle))
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    bool isOpen = action != null && opened.TryGetValue(action, out bool o) && o;
                    string title = action == null ? $"{i + 1}. （空の欄）"
                        : $"{i + 1}. {(action == current ? "▶実行中 " : "")}{EventActionCatalog.NameOf(action.GetType())}" +
                          (action.gameObject != sequence.gameObject ? $"（{action.gameObject.name}）" : "");

                    bool newOpen = EditorGUILayout.Foldout(isOpen, title, true);
                    if(action != null && newOpen != isOpen) { opened[action] = newOpen; }

                    GUI.enabled = index > 0;
                    if(GUILayout.Button("↑", GUILayout.Width(22))) { deferred = () => Move(sequence, actionsProp, list, explicitList, index, -1); }
                    GUI.enabled = index < list.Count - 1;
                    if(GUILayout.Button("↓", GUILayout.Width(22))) { deferred = () => Move(sequence, actionsProp, list, explicitList, index, 1); }
                    GUI.enabled = true;
                    if(GUILayout.Button("×", GUILayout.Width(22))) { deferred = () => Remove(sequence, actionsProp, list, explicitList, index); }
                }

                EditorGUILayout.LabelField(EventActionCatalog.Summary(action), summaryStyle);

                if(action != null && opened.TryGetValue(action, out bool show) && show)
                {
                    if(!inlineEditors.TryGetValue(action, out Editor ed) || ed == null)
                    {
                        ed = CreateEditor(action);
                        inlineEditors[action] = ed;
                    }
                    EditorGUI.indentLevel++;
                    ed.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
            }

            GUI.backgroundColor = old;
        }

        if(list.Count == 0)
        {
            EditorGUILayout.HelpBox("まだ部品がありません。下の「＋ 部品を追加」から足してください。", MessageType.Info);
        }

        if(GUILayout.Button("＋ 部品を追加"))
        {
            ShowAddMenu(sequence, explicitList);
        }

        deferred?.Invoke();
    }

    private void ShowAddMenu(EventSequence sequence, bool explicitList)
    {
        var menu = new GenericMenu();
        foreach(Type type in EventActionCatalog.AllTypes())
        {
            Type t = type;
            menu.AddItem(new GUIContent(EventActionCatalog.CategoryOf(t) + "/" + EventActionCatalog.NameOf(t)), false, () =>
            {
                var added = (EventAction)Undo.AddComponent(sequence.gameObject, t);
                if(explicitList)
                {
                    var so = new SerializedObject(sequence);
                    SerializedProperty prop = so.FindProperty("actions");
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = added;
                    so.ApplyModifiedProperties();
                }
                opened[added] = true;
            });
        }
        menu.ShowAsContext();
    }

    private void Move(EventSequence sequence, SerializedProperty actionsProp, List<EventAction> list, bool explicitList, int index, int delta)
    {
        if(explicitList)
        {
            actionsProp.MoveArrayElement(index, index + delta);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // 部品の並び順で実行しているので、部品そのものを上下に動かす（間に別の種類の部品があっても、次の部品を越えるまで動かす）
        EventAction action = list[index];
        EventAction other = list[index + delta];
        if(action == null || other == null) { return; }

        Component[] all = sequence.GetComponents<Component>();
        int from = Array.IndexOf(all, action);
        int to = Array.IndexOf(all, other);
        int steps = Mathf.Abs(to - from);
        Undo.RegisterCompleteObjectUndo(sequence.gameObject, "部品の順番");
        for(int s = 0; s < steps; s++)
        {
            if(delta < 0) { UnityEditorInternal.ComponentUtility.MoveComponentUp(action); }
            else          { UnityEditorInternal.ComponentUtility.MoveComponentDown(action); }
        }
    }

    private void Remove(EventSequence sequence, SerializedProperty actionsProp, List<EventAction> list, bool explicitList, int index)
    {
        EventAction action = list[index];

        if(explicitList)
        {
            // 欄から外す。同じオブジェクトの部品なら、部品も消すか聞く
            actionsProp.GetArrayElementAtIndex(index).objectReferenceValue = null;
            actionsProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            if(action != null && action.gameObject == sequence.gameObject &&
               EditorUtility.DisplayDialog("部品を外す", $"「{EventActionCatalog.NameOf(action.GetType())}」を流れから外しました。部品そのものも消しますか？", "消す", "残す"))
            {
                Undo.DestroyObjectImmediate(action);
            }
            return;
        }

        if(action == null) { return; }
        if(EditorUtility.DisplayDialog("部品を消す", $"「{EventActionCatalog.NameOf(action.GetType())}」を消しますか？（元に戻す：Ctrl+Z）", "消す", "やめる"))
        {
            Undo.DestroyObjectImmediate(action);
        }
    }

    //=====================================================================
    // 警告
    //=====================================================================

    private static void DrawWarnings(EventSequence sequence, SerializedProperty actionsProp)
    {
        var registered = new HashSet<Object>();
        bool hasNull = false;
        for(int i = 0; i < actionsProp.arraySize; i++)
        {
            Object obj = actionsProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if(obj == null) { hasNull = true; }
            else            { registered.Add(obj); }
        }

        if(actionsProp.arraySize == 0) { return; }

        var missing = new List<EventAction>();
        foreach(EventAction action in sequence.GetComponents<EventAction>())
        {
            if(!registered.Contains(action)) { missing.Add(action); }
        }

        if(missing.Count > 0)
        {
            var names = new List<string>();
            foreach(EventAction a in missing) { names.Add("・" + EventActionCatalog.NameOf(a.GetType())); }
            EditorGUILayout.HelpBox("このオブジェクトに付いているのに流れ（Actions）に入っていない部品があります（実行されません）：\n" + string.Join("\n", names), MessageType.Warning);

            if(GUILayout.Button("入っていない部品を流れの最後に追加"))
            {
                foreach(EventAction action in missing)
                {
                    int index = actionsProp.arraySize;
                    actionsProp.InsertArrayElementAtIndex(index);
                    actionsProp.GetArrayElementAtIndex(index).objectReferenceValue = action;
                }
                actionsProp.serializedObject.ApplyModifiedProperties();
            }
        }

        if(hasNull)
        {
            EditorGUILayout.HelpBox("流れ（Actions）に空の欄があります（飛ばされます）。× で外してください。", MessageType.Warning);
        }
    }
}
