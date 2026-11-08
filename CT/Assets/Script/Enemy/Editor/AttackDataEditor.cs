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
            if(it.name == "activation")
            {
                string help = ActivationHelp(data.activation);
                if(!string.IsNullOrEmpty(help)) { EditorGUILayout.HelpBox(help, MessageType.None); }
            }
            if(it.name == "motion" && data.motion == AttackMotion.Charge && data.shape.type != AttackShapeType.Rectangle)
            {
                EditorGUILayout.HelpBox("突進は、形を「長方形」にしてください（長さ＝走る距離、幅＝当たる幅）", MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static string ActivationHelp(AttackActivation activation)
    {
        switch(activation)
        {
            case AttackActivation.Bullet:
                return "弾の見た目は、シーンの敵の弾のプール（EnemyBulletPool）の弾のプレハブです。速さ・ダメージ・すり抜けるキャラ・弾消しで消せるかは、ここで変えられます。予告の形は、撃つ方向を見せる目安です。";
            case AttackActivation.Drop:
                return "予告（影）の最後の「落ちる時間」で、上から物が落ちてきて、着地した瞬間に範囲に当たります。";
            case AttackActivation.Rise:
                return "発動の瞬間に、範囲の真ん中から物が突き出て当たります。突き出る物に当たり判定（Collider2D）があれば、残っている間は壁になります（敵の通り道も自動で変わります）。";
            case AttackActivation.Place:
                return "予告のあと、起点に物を置きます（置いたら敵は次の行動へ）。時間・触れる・攻撃されるで爆発し、範囲の形に当たります。「当たる相手：両方」にすると、敵も巻き込みます。";
        }
        return null;
    }

    private static bool IsVisible(string name, AttackData d)
    {
        bool hasTelegraphTime = d.telegraphTime > 0.0f;
        bool sustained = d.activation == AttackActivation.Sustained;
        bool place = d.activation == AttackActivation.Place;
        bool showsView = (hasTelegraphTime || sustained || place) && d.showTelegraph;
        bool bullet = d.activation == AttackActivation.Bullet;
        bool instant = d.activation == AttackActivation.Instant;
        bool rise = d.activation == AttackActivation.Rise;

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
            case "rotateSpeed":
                return sustained;

            case "hitInterval":
                return sustained || (rise && d.riseHitWhileLingering);

            case "bulletPattern":
            case "bulletCount":
            case "bulletVolleys":
            case "bulletVolleyInterval":
            case "bulletSpeed":
            case "bulletDeletable":
                return bullet;

            case "bulletSpread":
                return bullet && (d.bulletPattern == BulletPattern.Aimed || d.bulletPattern == BulletPattern.Random);

            case "bulletSpiralStep":
                return bullet && d.bulletPattern == BulletPattern.Spiral;

            case "dropObject":
            case "dropHeight":
            case "dropTime":
                return d.activation == AttackActivation.Drop;

            case "riseObject":
            case "riseLinger":
            case "riseHitWhileLingering":
                return rise;

            case "placeObject":
            case "fuseTime":
            case "detonateOnTouch":
            case "detonateOnAttacked":
            case "showFuseRange":
                return place;

            case "targets":
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
