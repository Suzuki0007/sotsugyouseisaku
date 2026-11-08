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

        DrawPreview(data);
    }

    //=====================================================================
    // 形のプレビュー（Inspector と Scene ビュー）
    //=====================================================================

    private const string ScenePreviewKey = "CT.AttackPreview.Scene";

    private static bool ScenePreview
    {
        get => EditorPrefs.GetBool(ScenePreviewKey, true);
        set => EditorPrefs.SetBool(ScenePreviewKey, value);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Inspector の下に、形の小さな図を出す（1 マスごとの目盛り・白い点＝起点・矢印＝前）
    /// </summary>
    private static void DrawPreview(AttackData data)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("形のプレビュー", EditorStyles.boldLabel);

        bool scene = EditorGUILayout.ToggleLeft("Scene ビューにも表示する（最後に選んだ敵の位置。いなければ Scene ビューの真ん中）", ScenePreview);
        if(scene != ScenePreview) { ScenePreview = scene; SceneView.RepaintAll(); }

        Rect rect = GUILayoutUtility.GetRect(10, 180, GUILayout.ExpandWidth(true));
        if(Event.current.type != EventType.Repaint) { return; }

        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));

        // 形が収まる大きさにする
        float extent = Mathf.Max(1.0f, data.shape.OuterRadius() + Mathf.Abs(data.forwardOffset)) * 1.15f;
        float scale = Mathf.Min(rect.width, rect.height) * 0.5f / extent;
        Vector2 center = rect.center;
        if(data.shape.type == AttackShapeType.Rectangle || data.shape.type == AttackShapeType.Sector)
        {
            // 前に伸びる形は、起点を左に寄せる
            center.x = rect.x + rect.width * 0.5f - Mathf.Min(rect.width * 0.35f, data.shape.OuterRadius() * 0.5f * scale);
        }

        Vector2 forward = PreviewForward(data);
        System.Func<Vector2, Vector3> map = p => new Vector3(center.x + p.x * scale, center.y - p.y * scale, 0.0f);

        GUI.BeginClip(rect);
        Vector2 offset = rect.position;
        System.Func<Vector2, Vector3> local = p => map(p) - (Vector3)offset;

        // 1 マスごとの目盛り
        Handles.color = new Color(1, 1, 1, 0.06f);
        int lines = Mathf.CeilToInt(Mathf.Max(rect.width, rect.height) / Mathf.Max(4.0f, scale)) + 2;
        for(int i = -lines; i <= lines; i++)
        {
            Handles.DrawLine(local(new Vector2(i, -lines)), local(new Vector2(i, lines)));
            Handles.DrawLine(local(new Vector2(-lines, i)), local(new Vector2(lines, i)));
        }

        Vector2 origin = forward * data.forwardOffset;
        DrawShape(data, origin, forward, local);

        // 起点と向き
        Handles.color = Color.white;
        Handles.DrawSolidDisc(local(Vector2.zero), Vector3.forward, 3.0f);
        Handles.DrawLine(local(Vector2.zero), local(forward * 0.8f));
        GUI.EndClip();

        GUI.Label(new Rect(rect.x + 4, rect.y + 2, rect.width - 8, 18), $"1 目盛り = 1 マス　{PreviewNote(data)}", EditorStyles.miniLabel);
    }

    private static Vector2 PreviewForward(AttackData data)
    {
        if(data.aim == AttackAim.Fixed)
        {
            float a = data.fixedAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
        return Vector2.right;
    }

    private static string PreviewNote(AttackData data)
    {
        string where = data.origin == AttackOrigin.Enemy ? "白い点＝敵" : "白い点＝プレイヤー";
        string dir = data.aim == AttackAim.Fixed ? "向きは固定" : (data.aim == AttackAim.Player ? "右＝プレイヤーの方向" : "右＝敵の向いている方向");
        return $"{where}　{dir}";
    }

    /// <summary>
    /// 形（塗り・枠）と、弾なら撃つ方向の線を描く
    /// </summary>
    private static void DrawShape(AttackData data, Vector2 origin, Vector2 forward, System.Func<Vector2, Vector3> map)
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        Vector2 left = new Vector2(-forward.y, forward.x);
        System.Func<Vector3, Vector3> toWorld = v => map(origin + forward * v.x + left * v.y);

        Color fill = data.telegraphColor;
        fill.a = Mathf.Clamp(fill.a, 0.25f, 0.6f);
        Color line = data.telegraphColor;
        line.a = 1.0f;

        data.shape.BuildFill(verts, tris);
        Handles.color = fill;
        DrawTriangles(verts, tris, toWorld);

        data.shape.BuildOutline(verts, tris, 0.06f);
        Handles.color = line;
        DrawTriangles(verts, tris, toWorld);

        if(data.activation != AttackActivation.Bullet) { return; }

        // 弾の方向
        Handles.color = new Color(1.0f, 0.9f, 0.4f, 0.9f);
        float reach = Mathf.Max(2.0f, data.shape.OuterRadius());
        foreach(float deg in BulletAngles(data))
        {
            float a = deg * Mathf.Deg2Rad;
            Vector2 dir = forward * Mathf.Cos(a) + left * Mathf.Sin(a);
            Handles.DrawLine(map(origin), map(origin + dir * reach));
        }
    }

    private static System.Collections.Generic.IEnumerable<float> BulletAngles(AttackData data)
    {
        int count = Mathf.Max(1, data.bulletCount);
        switch(data.bulletPattern)
        {
            case BulletPattern.Circle:
            case BulletPattern.Spiral:
                for(int i = 0; i < count; i++) { yield return 360.0f * i / count; }
                break;
            default:
                if(count == 1) { yield return 0.0f; break; }
                for(int i = 0; i < count; i++) { yield return Mathf.Lerp(-data.bulletSpread * 0.5f, data.bulletSpread * 0.5f, (float)i / (count - 1)); }
                break;
        }
    }

    private static void DrawTriangles(System.Collections.Generic.List<Vector3> verts, System.Collections.Generic.List<int> tris, System.Func<Vector3, Vector3> toWorld)
    {
        var tri = new Vector3[3];
        for(int i = 0; i + 2 < tris.Count; i += 3)
        {
            tri[0] = toWorld(verts[tris[i]]);
            tri[1] = toWorld(verts[tris[i + 1]]);
            tri[2] = toWorld(verts[tris[i + 2]]);
            Handles.DrawAAConvexPolygon(tri);
        }
    }

    /// <summary>
    /// Scene ビューに、最後に選んだ敵（なければ Scene ビューの真ん中）を起点に形を出す
    /// </summary>
    private void OnSceneGUI(SceneView view)
    {
        if(!ScenePreview || Event.current.type != EventType.Repaint) { return; }
        var data = target as AttackData;
        if(data == null) { return; }

        Vector2 anchor = AttackPreviewAnchor.Position(view);
        Vector2 forward = PreviewForward(data);
        Vector2 origin = anchor + forward * data.forwardOffset;

        DrawShape(data, origin, forward, p => new Vector3(p.x, p.y, 0.0f));

        Handles.color = Color.white;
        Handles.DrawWireDisc(anchor, Vector3.forward, 0.15f);
        Handles.Label(anchor + new Vector2(0.2f, -0.2f), $"{data.name}（{PreviewNote(data)}）", EditorStyles.whiteMiniLabel);
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
