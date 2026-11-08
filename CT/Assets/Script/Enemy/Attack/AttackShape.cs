/*********************************************************************/
// * \file   AttackShape.cs
// * \brief  攻撃の範囲の形（当たり判定と予告の表示で同じものを使う）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃の範囲の形の種類
/// </summary>
public enum AttackShapeType
{
    [InspectorName("円")]
    Circle = 0,

    [InspectorName("長方形（ビーム・突進の通り道）")]
    Rectangle = 1,

    [InspectorName("扇形")]
    Sector = 2,

    [InspectorName("リング（ドーナツ）")]
    Ring = 3,
}

/// <summary>
/// 攻撃の範囲の形
/// </summary>
///
/// ・形は「起点」と「向き」で置く。向きは右（+X）が前
/// ・円・リング：起点が中心 ／ 長方形：起点から前へ「長さ」、左右に「幅」の半分ずつ ／ 扇形：起点が要（かなめ）で、前を中心に「角度」
/// ・予告の表示（TelegraphView）と当たり判定（Contains）は、同じこのデータから作る（見た目と判定がずれないように）
[Serializable]
public class AttackShape
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Label("形")]
    public AttackShapeType type = AttackShapeType.Circle;

    [Label("半径", 0.1f)]
    [Tooltip("円・扇形・リングの外側の半径（マス）")]
    public float radius = 1.5f;

    [Label("内側の半径", 0.0f)]
    [Tooltip("リングの穴の半径。この内側は安全")]
    public float innerRadius = 1.0f;

    [Label("長さ", 0.1f)]
    [Tooltip("長方形の、起点から前への長さ（マス）")]
    public float length = 6.0f;

    [Label("幅", 0.1f)]
    [Tooltip("長方形の幅（マス）")]
    public float width = 1.0f;

    [Label("角度", 1.0f, 360.0f)]
    [Tooltip("扇形の開き具合（度）。90 なら前の 90 度")]
    public float angle = 90.0f;

    //=====================================================================
    // 判定
    //=====================================================================

    /// <summary>
    /// 外側の大きさ（この半径の円に収まる。当たる相手を探す範囲に使う）
    /// </summary>
    public float OuterRadius(float lengthOverride = -1.0f)
    {
        switch(type)
        {
            case AttackShapeType.Rectangle:
                float len = lengthOverride >= 0.0f ? lengthOverride : length;
                return Mathf.Sqrt(len * len + width * width * 0.25f);
            default:
                return radius;
        }
    }

    /// <summary>
    /// 点が形の中にあるか
    /// </summary>
    /// <param name="point"> 調べる点（ワールド座標） </param>
    /// <param name="origin"> 起点 </param>
    /// <param name="direction"> 向き（前） </param>
    /// <param name="lengthOverride"> 長方形の長さを変える（壁で止めたときなど。負なら設定のまま） </param>
    public bool Contains(Vector2 point, Vector2 origin, Vector2 direction, float lengthOverride = -1.0f)
    {
        Vector2 local = ToLocal(point - origin, direction);

        switch(type)
        {
            case AttackShapeType.Circle:
                return local.sqrMagnitude <= radius * radius;

            case AttackShapeType.Ring:
                float d = local.magnitude;
                return d <= radius && d >= Mathf.Min(innerRadius, radius);

            case AttackShapeType.Rectangle:
                float len = lengthOverride >= 0.0f ? lengthOverride : length;
                return local.x >= 0.0f && local.x <= len && Mathf.Abs(local.y) <= width * 0.5f;

            case AttackShapeType.Sector:
                if(local.sqrMagnitude > radius * radius) { return false; }
                if(local.sqrMagnitude < 0.0001f) { return true; }
                return Mathf.Abs(Vector2.SignedAngle(Vector2.right, local)) <= angle * 0.5f;
        }
        return false;
    }

    /// <summary>
    /// コライダーが形にかかっているか（形のいちばん近い所で判定する、だいたいの判定）
    /// </summary>
    public bool Overlaps(Collider2D collider, Vector2 origin, Vector2 direction, float lengthOverride = -1.0f)
    {
        if(collider == null) { return false; }

        // 形の中心に近いコライダー上の点と、コライダーの中心の、どちらかが入っていれば当たり
        Vector2 center = CenterOf(origin, direction, lengthOverride);
        if(Contains(collider.ClosestPoint(center), origin, direction, lengthOverride)) { return true; }
        if(Contains(collider.ClosestPoint(origin), origin, direction, lengthOverride)) { return true; }
        return Contains(collider.bounds.center, origin, direction, lengthOverride);
    }

    /// <summary>
    /// 形の真ん中あたり（エフェクトを出す位置など）
    /// </summary>
    public Vector2 CenterOf(Vector2 origin, Vector2 direction, float lengthOverride = -1.0f)
    {
        Vector2 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        switch(type)
        {
            case AttackShapeType.Rectangle:
                float len = lengthOverride >= 0.0f ? lengthOverride : length;
                return origin + forward * (len * 0.5f);
            case AttackShapeType.Sector:
                return origin + forward * (radius * 0.5f);
            default:
                return origin;
        }
    }

    private static Vector2 ToLocal(Vector2 offset, Vector2 direction)
    {
        Vector2 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 left = new Vector2(-forward.y, forward.x);
        return new Vector2(Vector2.Dot(offset, forward), Vector2.Dot(offset, left));
    }

    //=====================================================================
    // 表示用のメッシュ
    //=====================================================================

    private const int CircleSegments = 48;

    /// <summary>
    /// 塗りつぶしのメッシュ（ローカル座標。右が前）
    /// </summary>
    public void BuildFill(List<Vector3> vertices, List<int> triangles, float lengthOverride = -1.0f)
    {
        vertices.Clear();
        triangles.Clear();

        switch(type)
        {
            case AttackShapeType.Circle:
                Fan(vertices, triangles, radius, -180.0f, 180.0f, CircleSegments);
                break;

            case AttackShapeType.Sector:
                Fan(vertices, triangles, radius, -angle * 0.5f, angle * 0.5f, Mathf.Max(4, Mathf.CeilToInt(CircleSegments * angle / 360.0f)));
                break;

            case AttackShapeType.Ring:
                RingStrip(vertices, triangles, Mathf.Min(innerRadius, radius), radius);
                break;

            case AttackShapeType.Rectangle:
                float len = lengthOverride >= 0.0f ? lengthOverride : length;
                float half = width * 0.5f;
                vertices.Add(new Vector3(0.0f, -half));
                vertices.Add(new Vector3(len, -half));
                vertices.Add(new Vector3(len, half));
                vertices.Add(new Vector3(0.0f, half));
                triangles.AddRange(new[] { 0, 2, 1, 0, 3, 2 });
                break;
        }
    }

    /// <summary>
    /// 外枠の線（太さのある帯）のメッシュ
    /// </summary>
    public void BuildOutline(List<Vector3> vertices, List<int> triangles, float thickness, float lengthOverride = -1.0f)
    {
        vertices.Clear();
        triangles.Clear();

        var loop = new List<Vector2>();
        switch(type)
        {
            case AttackShapeType.Circle:
                Arc(loop, radius, -180.0f, 180.0f, CircleSegments, false);
                Band(vertices, triangles, loop, thickness, true);
                break;

            case AttackShapeType.Ring:
                Arc(loop, radius, -180.0f, 180.0f, CircleSegments, false);
                Band(vertices, triangles, loop, thickness, true);
                loop.Clear();
                Arc(loop, Mathf.Min(innerRadius, radius), -180.0f, 180.0f, CircleSegments, false);
                Band(vertices, triangles, loop, thickness, true);
                break;

            case AttackShapeType.Sector:
                loop.Add(Vector2.zero);
                Arc(loop, radius, -angle * 0.5f, angle * 0.5f, Mathf.Max(4, Mathf.CeilToInt(CircleSegments * angle / 360.0f)), true);
                Band(vertices, triangles, loop, thickness, angle < 359.0f);
                break;

            case AttackShapeType.Rectangle:
                float len = lengthOverride >= 0.0f ? lengthOverride : length;
                float half = width * 0.5f;
                loop.Add(new Vector2(0.0f, -half));
                loop.Add(new Vector2(len, -half));
                loop.Add(new Vector2(len, half));
                loop.Add(new Vector2(0.0f, half));
                Band(vertices, triangles, loop, thickness, true);
                break;
        }
    }

    private static void Fan(List<Vector3> vertices, List<int> triangles, float r, float fromDeg, float toDeg, int segments)
    {
        int center = vertices.Count;
        vertices.Add(Vector3.zero);
        for(int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, (float)i / segments) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        for(int i = 0; i < segments; i++)
        {
            triangles.Add(center);
            triangles.Add(center + i + 2);
            triangles.Add(center + i + 1);
        }
    }

    private static void RingStrip(List<Vector3> vertices, List<int> triangles, float inner, float outer)
    {
        int start = vertices.Count;
        for(int i = 0; i <= CircleSegments; i++)
        {
            float a = Mathf.Lerp(-180.0f, 180.0f, (float)i / CircleSegments) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a));
            vertices.Add(dir * inner);
            vertices.Add(dir * outer);
        }
        for(int i = 0; i < CircleSegments; i++)
        {
            int a = start + i * 2;
            triangles.AddRange(new[] { a, a + 3, a + 1, a, a + 2, a + 3 });
        }
    }

    private static void Arc(List<Vector2> points, float r, float fromDeg, float toDeg, int segments, bool includeEnds)
    {
        int count = includeEnds ? segments + 1 : segments;
        for(int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, (float)i / segments) * Mathf.Deg2Rad;
            points.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    /// <summary>
    /// 点の並びに沿った、太さのある帯
    /// </summary>
    private static void Band(List<Vector3> vertices, List<int> triangles, List<Vector2> points, float thickness, bool closed)
    {
        int n = points.Count;
        if(n < 2) { return; }

        int segments = closed ? n : n - 1;
        float half = thickness * 0.5f;
        for(int i = 0; i < segments; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % n];
            Vector2 dir = (b - a).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * half;

            // 角の継ぎ目が欠けないよう、少しだけ延ばす
            Vector2 extend = dir * half;
            int v = vertices.Count;
            vertices.Add(a - extend - normal);
            vertices.Add(a - extend + normal);
            vertices.Add(b + extend + normal);
            vertices.Add(b + extend - normal);
            triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        }
    }
}
