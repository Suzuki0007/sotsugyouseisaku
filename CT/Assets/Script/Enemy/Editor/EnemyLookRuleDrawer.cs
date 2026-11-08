/*********************************************************************/
// * \file   EnemyLookRuleDrawer.cs
// * \brief  見た目の変化（EnemyLookRule）の表示（きっかけで使う項目だけ出す）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 見た目の変化の1行の表示
/// </summary>
[CustomPropertyDrawer(typeof(EnemyLookRule))]
public class EnemyLookRuleDrawer : PropertyDrawer
{
    private static readonly string[] Common = { "animation", "useTint", "tint", "blink", "effect", "se" };

    private static List<string> FieldsFor(LookTrigger trigger)
    {
        var fields = new List<string> { "trigger" };
        switch(trigger)
        {
            case LookTrigger.HealthBelow: fields.Add("healthRatio"); break;
            case LookTrigger.Hit:         fields.AddRange(new[] { "hitKind", "hitCharacter", "hitJudge", "duration" }); break;
            case LookTrigger.PartBroken:  fields.Add("partName"); break;
            case LookTrigger.Phase:       fields.Add("phase"); break;
            case LookTrigger.Condition:   fields.Add("condition"); break;
        }
        fields.AddRange(Common);
        return fields;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if(!property.isExpanded) { return height; }

        var trigger = (LookTrigger)property.FindPropertyRelative("trigger").enumValueIndex;
        foreach(string name in FieldsFor(trigger))
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + EditorGUIUtility.standardVerticalSpacing;
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var trigger = (LookTrigger)property.FindPropertyRelative("trigger").enumValueIndex;

        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        string title = $"{label.text}：{TriggerName(trigger)}";
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, title, true);
        if(!property.isExpanded) { return; }

        EditorGUI.indentLevel++;
        float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
        foreach(string name in FieldsFor(trigger))
        {
            SerializedProperty p = property.FindPropertyRelative(name);
            float h = EditorGUI.GetPropertyHeight(p, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), p, true);
            y += h + EditorGUIUtility.standardVerticalSpacing;
        }
        EditorGUI.indentLevel--;
    }

    private static string TriggerName(LookTrigger trigger)
    {
        switch(trigger)
        {
            case LookTrigger.HealthBelow: return "体力が減ったら";
            case LookTrigger.Hit:         return "攻撃を受けたとき";
            case LookTrigger.PartBroken:  return "部位が壊れたら";
            case LookTrigger.Phase:       return "段階";
            case LookTrigger.Staggered:   return "ひるんでいる間";
            case LookTrigger.Stopped:     return "止められている間";
            default:                      return "条件";
        }
    }
}
