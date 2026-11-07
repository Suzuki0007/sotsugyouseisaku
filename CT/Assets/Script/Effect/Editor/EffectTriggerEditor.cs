/*********************************************************************/
// * \file   EffectTriggerEditor.cs
// * \brief  EffectTrigger の Inspector 表示 (必要な項目だけ表示・設定ミスの警告・試し再生)
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// EffectTrigger の Inspector
/// </summary>
[CustomEditor(typeof(EffectTrigger))]
public class EffectTriggerEditor : Editor
{
    private SerializedProperty entries;

    private void OnEnable()
    {
        entries = serializedObject.FindProperty("entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("「＋」で行を追加し、いつ・どのエフェクトを出すかを選んでください。", MessageType.None);
        EditorGUILayout.PropertyField(entries, new GUIContent("出すエフェクト"), true);

        serializedObject.ApplyModifiedProperties();

        DrawWarnings((EffectTrigger)target);
        DrawTestButtons((EffectTrigger)target);
    }

    //=====================================================================
    // 警告
    //=====================================================================

    private static void DrawWarnings(EffectTrigger trigger)
    {
        bool usesHealth = false, usesTrigger = false, usesCollision = false, usesManual = false;
        foreach(EffectTriggerEntry e in trigger.Entries)
        {
            if(e.effect == null)
            {
                EditorGUILayout.HelpBox($"「{Label(e)}」の行にエフェクトが入っていません。", MessageType.Warning);
            }
            usesHealth    |= EffectTriggerEntry.IsHealthTiming(e.timing);
            usesTrigger   |= e.timing == EffectTiming.OnTriggerEnter || e.timing == EffectTiming.OnTriggerExit;
            usesCollision |= e.timing == EffectTiming.OnCollisionEnter;
            usesManual    |= e.timing == EffectTiming.Manual;
        }

        if(usesHealth && trigger.GetComponentInParent<Health>() == null)
        {
            EditorGUILayout.HelpBox("ダメージ・回復・死亡のタイミングを使うには、このオブジェクト (か親) に Health が必要です。", MessageType.Warning);
        }

        Collider2D[] colliders = trigger.GetComponents<Collider2D>();
        if((usesTrigger || usesCollision) && colliders.Length == 0)
        {
            EditorGUILayout.HelpBox("「触れたとき」のタイミングを使うには、このオブジェクトに Collider2D (BoxCollider2D など) が必要です。", MessageType.Warning);
        }
        else
        {
            bool anyTrigger = false, anySolid = false;
            foreach(Collider2D c in colliders) { if(c.isTrigger) { anyTrigger = true; } else { anySolid = true; } }

            if(usesTrigger && !anyTrigger)
            {
                EditorGUILayout.HelpBox("「入ってきたとき / 出ていったとき」を使うには、Collider2D の「Is Trigger」にチェックを入れてください。", MessageType.Warning);
            }
            if(usesCollision && !anySolid)
            {
                EditorGUILayout.HelpBox("「ぶつかったとき」を使うには、Is Trigger がオフの Collider2D が必要です。", MessageType.Warning);
            }
        }

        if(usesManual)
        {
            EditorGUILayout.HelpBox("「手動」のエフェクトは、UnityEvent やアニメーションイベントから\n" +
                                    "EffectTrigger.Play() (すべて) / Play(\"名前\") (名前を指定) を呼ぶと出ます。", MessageType.Info);
        }
    }

    //=====================================================================
    // 試し再生
    //=====================================================================

    private static void DrawTestButtons(EffectTrigger trigger)
    {
        if(!Application.isPlaying || trigger.Entries.Count == 0) { return; }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("試し再生 (Playモード中のみ)", EditorStyles.boldLabel);

        for(int i = 0; i < trigger.Entries.Count; i++)
        {
            EffectTriggerEntry e = trigger.Entries[i];
            using(new EditorGUI.DisabledScope(e.effect == null))
            {
                if(GUILayout.Button($"▶ {Label(e)}"))
                {
                    trigger.PlayIndex(i);
                }
            }
        }

        if(GUILayout.Button("■ ループを止める"))
        {
            trigger.StopLoops();
        }
    }

    internal static string Label(EffectTriggerEntry e)
    {
        if(!string.IsNullOrEmpty(e.name)) { return e.name; }
        return e.effect != null ? e.effect.name : "(未設定)";
    }
}

/// <summary>
/// EffectTriggerEntry (1行分) の表示
/// タイミングに関係する項目だけを表示する
/// </summary>
[CustomPropertyDrawer(typeof(EffectTriggerEntry))]
public class EffectTriggerEntryDrawer : PropertyDrawer
{
    private const float Spacing = 2.0f;

    private enum Kind { Normal, Tag }

    private struct Row
    {
        public SerializedProperty prop;
        public GUIContent label;
        public Kind kind;
        public Row(SerializedProperty p, string l, Kind k = Kind.Normal) { prop = p; label = new GUIContent(l, p.tooltip); kind = k; }
    }

    private static List<Row> GetRows(SerializedProperty property)
    {
        var rows = new List<Row>();

        SerializedProperty timing   = property.FindPropertyRelative("timing");
        SerializedProperty effect   = property.FindPropertyRelative("effect");
        SerializedProperty spawnAt  = property.FindPropertyRelative("spawnAt");
        SerializedProperty follow   = property.FindPropertyRelative("follow");

        var t = (EffectTiming)timing.enumValueIndex;
        bool contact = EffectTriggerEntry.IsContactTiming(t);
        var at = contact ? (EffectSpawnAt)spawnAt.enumValueIndex : EffectSpawnAt.Self;
        var data = effect.objectReferenceValue as EffectData;

        rows.Add(new Row(timing, "いつ"));
        rows.Add(new Row(effect, "エフェクト"));
        rows.Add(new Row(property.FindPropertyRelative("name"), "名前・メモ"));

        if(contact)
        {
            rows.Add(new Row(spawnAt, "どこに"));
            rows.Add(new Row(property.FindPropertyRelative("targetTag"), "相手のタグ", Kind.Tag));
        }

        rows.Add(new Row(property.FindPropertyRelative("offset"), "位置のずらし"));

        // 消えたときは自分に追従できない・接触点には追従する相手がいない
        bool canFollow = t != EffectTiming.OnDisable && at != EffectSpawnAt.ContactPoint;
        if(canFollow)
        {
            rows.Add(new Row(follow, "くっついて動く"));
            if(follow.boolValue)
            {
                rows.Add(new Row(property.FindPropertyRelative("rotateWithTarget"), "　回転も合わせる"));
            }
        }

        if(t == EffectTiming.OnTriggerEnter && data != null && data.IsLoop)
        {
            rows.Add(new Row(property.FindPropertyRelative("stopOnExit"), "出ていったら止める"));
        }

        rows.Add(new Row(property.FindPropertyRelative("onlyOnce"), "1回だけ出す"));
        return rows;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        if(!property.isExpanded) { return line; }

        float h = line + Spacing;
        foreach(Row row in GetRows(property))
        {
            h += EditorGUI.GetPropertyHeight(row.prop, row.label, true) + Spacing;
        }
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 見出し : 「いつ → エフェクト名」
        var timing = (EffectTiming)property.FindPropertyRelative("timing").enumValueIndex;
        var effect = property.FindPropertyRelative("effect").objectReferenceValue;
        string nameText = property.FindPropertyRelative("name").stringValue;
        string title = $"{ShortTiming(timing)}  →  {(effect != null ? effect.name : "(エフェクト未設定)")}";
        if(!string.IsNullOrEmpty(nameText)) { title = $"[{nameText}]  " + title; }

        Rect r = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, title, true);

        if(property.isExpanded)
        {
            EditorGUI.indentLevel++;
            r.y += r.height + Spacing;

            foreach(Row row in GetRows(property))
            {
                r.height = EditorGUI.GetPropertyHeight(row.prop, row.label, true);
                if(row.kind == Kind.Tag) { DrawTag(r, row); }
                else                     { EditorGUI.PropertyField(r, row.prop, row.label, true); }
                r.y += r.height + Spacing;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// タグをプルダウンで選ぶ (先頭は「すべての相手」)
    /// </summary>
    private static void DrawTag(Rect rect, Row row)
    {
        string[] tags = InternalEditorUtility.tags;
        var options = new string[tags.Length + 1];
        options[0] = "(すべての相手)";
        int current = 0;
        for(int i = 0; i < tags.Length; i++)
        {
            options[i + 1] = tags[i];
            if(tags[i] == row.prop.stringValue) { current = i + 1; }
        }

        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(rect, row.label.text, current, options);
        if(EditorGUI.EndChangeCheck())
        {
            row.prop.stringValue = selected == 0 ? "" : tags[selected - 1];
        }
    }

    private static string ShortTiming(EffectTiming t)
    {
        switch(t)
        {
            case EffectTiming.OnStart:          return "出現時";
            case EffectTiming.OnEnable:         return "表示時";
            case EffectTiming.OnDisable:        return "消えた時";
            case EffectTiming.OnDamaged:        return "ダメージ";
            case EffectTiming.OnHealed:         return "回復";
            case EffectTiming.OnDied:           return "死亡";
            case EffectTiming.OnTriggerEnter:   return "入ってきた";
            case EffectTiming.OnTriggerExit:    return "出ていった";
            case EffectTiming.OnCollisionEnter: return "ぶつかった";
            case EffectTiming.Manual:           return "手動";
            default:                            return t.ToString();
        }
    }
}
