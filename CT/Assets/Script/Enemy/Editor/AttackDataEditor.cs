/*********************************************************************/
// * \file   AttackDataEditor.cs
// * \brief  攻撃データの Inspector（今の設定で使う項目だけを出す）
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 攻撃データの Inspector
/// </summary>
///
/// 項目が多いので、選んだ「形」「発動のしかた」「敵自身の動き」などで使わない項目は隠す
[CustomEditor(typeof(AttackData))]
public class AttackDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var data = (AttackData)target;

        EditorGUILayout.HelpBox("攻撃は「予告（範囲を見せる）→ 発動 → 後隙」の順に進みます。\n今の設定で使わない項目は隠しています。", MessageType.Info);

        SerializedProperty it = serializedObject.GetIterator();
        bool enter = true;
        while(it.NextVisible(enter))
        {
            enter = false;
            if(it.name == "m_Script") { continue; }
            if(!IsVisible(it.name, data)) { continue; }

            if(it.name == "shape")
            {
                DrawShape(it);
                continue;
            }

            EditorGUILayout.PropertyField(it, true);

            // 補足
            if(it.name == "activation" && data.activation == AttackActivation.Bullet)
            {
                EditorGUILayout.HelpBox("弾のダメージ・速さ・見た目は、シーンの敵の弾のプール（EnemyBulletPool）の弾のプレハブで決まります。\n「当たる相手」「効かないキャラ」は弾には使えません。予告の形は、撃つ方向を見せる目安に使います。", MessageType.None);
            }
            if(it.name == "motion" && data.motion == AttackMotion.Charge && data.shape.type != AttackShapeType.Rectangle)
            {
                EditorGUILayout.HelpBox("突進は、形を「長方形」にしてください（長さ＝走る距離、幅＝当たる幅）", MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static bool IsVisible(string name, AttackData d)
    {
        bool hasTelegraphTime = d.telegraphTime > 0.0f;
        bool sustained = d.activation == AttackActivation.Sustained;
        bool showsView = (hasTelegraphTime || sustained) && d.showTelegraph;
        bool bullet = d.activation == AttackActivation.Bullet;
        bool instant = d.activation == AttackActivation.Instant;

        switch(name)
        {
            case "showTelegraph":
                return hasTelegraphTime || sustained;

            case "telegraphStyle":
            case "telegraphColor":
            case "telegraphSortingOrder":
            case "telegraphUnlit":
                return showsView;

            case "telegraphFill":
            case "telegraphBlink":
                return showsView && hasTelegraphTime;

            case "telegraphSe":
            case "telegraphAnimation":
                return hasTelegraphTime;

            case "fixedAngle":
                return d.aim == AttackAim.Fixed;

            case "trackDuringTelegraph":
                return d.aim == AttackAim.Player && hasTelegraphTime;

            case "stopAtWall":
                return d.shape.type == AttackShapeType.Rectangle;

            case "sustainTime":
            case "hitInterval":
            case "rotateSpeed":
                return sustained;

            case "bulletCount":
            case "bulletSpread":
            case "bulletVolleys":
            case "bulletVolleyInterval":
                return bullet;

            case "damage":
            case "targets":
            case "ignoreNoah":
            case "ignoreSigle":
            case "damageKind":
                return !bullet;

            case "motion":
                return instant;

            case "chargeSpeed":
            case "chargeStunTime":
                return instant && d.motion == AttackMotion.Charge;
        }
        return true;
    }

    /// <summary>
    /// 形：選んだ形で使う項目だけ
    /// </summary>
    private static void DrawShape(SerializedProperty shape)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("範囲の形（当たり判定と予告は同じ形）", EditorStyles.boldLabel);

        using(new EditorGUI.IndentLevelScope())
        {
            SerializedProperty type = shape.FindPropertyRelative("type");
            EditorGUILayout.PropertyField(type);

            switch((AttackShapeType)type.enumValueIndex)
            {
                case AttackShapeType.Circle:
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("radius"));
                    break;

                case AttackShapeType.Ring:
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("radius"));
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("innerRadius"));
                    break;

                case AttackShapeType.Rectangle:
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("length"));
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("width"));
                    break;

                case AttackShapeType.Sector:
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("radius"));
                    EditorGUILayout.PropertyField(shape.FindPropertyRelative("angle"));
                    break;
            }
        }
    }
}
