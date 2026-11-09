/*********************************************************************/
// * \file   LabelDrawer.cs
// * \brief  LabelAttribute の表示
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// [Label] を付けた項目を、日本語の名前で表示する（最小値・スライダーも扱う）
/// </summary>
[CustomPropertyDrawer(typeof(LabelAttribute))]
public class LabelDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (LabelAttribute)attribute;
        var content = new GUIContent(attr.label, label.tooltip);

        EditorGUI.BeginProperty(position, content, property);

        if(attr.isRange && property.propertyType == SerializedPropertyType.Float)
        {
            EditorGUI.Slider(position, property, attr.min, attr.max, content);
        }
        else if(attr.isRange && property.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.IntSlider(position, property, (int)attr.min, (int)attr.max, content);
        }
        else
        {
            EditorGUI.PropertyField(position, property, content, true);

            if(attr.hasMin)
            {
                if(property.propertyType == SerializedPropertyType.Float && property.floatValue < attr.min) { property.floatValue = attr.min; }
                if(property.propertyType == SerializedPropertyType.Integer && property.intValue < attr.min) { property.intValue = (int)attr.min; }
            }
        }

        EditorGUI.EndProperty();
    }
}
