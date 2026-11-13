/*********************************************************************/
// * \file   WalkArea.cs
// * \brief  プレイヤーが歩ける範囲を、円・四角・多角形の組み合わせで決める
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーが歩ける範囲（円・四角・多角形の組み合わせ）
/// </summary>
///
/// ・このオブジェクトの子に Collider2D（Circle / Box / Polygon）を付けた物を置くと、その形を合わせた範囲だけ歩ける
///   （ティンパニの上と、それをつなぐ通路だけ歩ける、など）
/// ・形はシーンビューで Collider2D を編集して決める。数・位置・大きさは自由
/// ・子を非表示にすると、その形は範囲から外れる（イベントで表示すれば、通路が開く）
/// ・Collider2D は形を読むためだけに使い、再生中は無効にする（弾や敵にはぶつからない）
/// ・シーンに置かなければ、今までどおりマップ全体（MapBounds）を歩ける
public class WalkArea : MonoBehaviour
{
    //=====================================================================
    // 型
    //=====================================================================

    private class Shape
    {
        public GameObject owner;
        public Transform transform;

        // 0 = 円、1 = 四角、2 = 多角形
        public int kind;
        public Vector2 offset;
        public float radius;
        public Vector2 halfSize;
        public Vector2[] points;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("シーンビューに範囲を表示")]
    [Tooltip("選んでいなくても、歩ける範囲を緑の線で表示する")]
    private bool alwaysShowGizmos = true;

    private static WalkArea active;

    private readonly List<Shape> shapes = new List<Shape>();

    /// <summary> 歩ける範囲が決められているシーンか </summary>
    public static bool Exists => active != null && active.isActiveAndEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { active = null; }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        active = this;
        Collect(true);
    }

    private void OnDestroy()
    {
        if(active == this) { active = null; }
    }

    /// <summary>
    /// 子の Collider2D から形を読む
    /// </summary>
    /// <param name="disableColliders"> 読んだあと Collider2D を無効にする（再生中） </param>
    private void Collect(bool disableColliders)
    {
        shapes.Clear();

        foreach(Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            var shape = new Shape { owner = col.gameObject, transform = col.transform, offset = col.offset };

            if(col is CircleCollider2D circle)
            {
                shape.kind = 0;
                shape.radius = circle.radius;
            }
            else if(col is BoxCollider2D box)
            {
                shape.kind = 1;
                shape.halfSize = box.size * 0.5f;
            }
            else if(col is PolygonCollider2D polygon)
            {
                shape.kind = 2;
                shape.points = polygon.points;
            }
            else
            {
                Debug.LogWarning($"{name}: 歩ける範囲に使えるのは Circle / Box / Polygon の Collider2D だけです（{col.name}）", col);
                continue;
            }

            shapes.Add(shape);
            if(disableColliders) { col.enabled = false; }
        }
    }

    //=====================================================================
    // 判定
    //=====================================================================

    /// <summary>
    /// 位置を歩ける範囲の中に収める（中ならそのまま。外なら一番近い範囲のふちへ）
    /// 歩ける範囲がないシーンではそのまま返す
    /// </summary>
    public static Vector2 Clamp(Vector2 position)
    {
        if(!Exists) { return position; }
        return active.ClampInternal(position);
    }

    /// <summary> 位置が歩ける範囲の中か（歩ける範囲がないシーンでは true） </summary>
    public static bool Contains(Vector2 position)
    {
        if(!Exists) { return true; }

        foreach(Shape shape in active.shapes)
        {
            if(shape.owner != null && shape.owner.activeInHierarchy && Closest(shape, position, out _)) { return true; }
        }
        return false;
    }

    /// <summary>
    /// 歩ける範囲の中のでたらめな位置（落石をばらまく場所など）。歩ける範囲がないシーンでは false
    /// </summary>
    public static bool TryRandomPoint(out Vector2 point)
    {
        point = Vector2.zero;
        if(!Exists) { return false; }

        // 表示されている形を囲む四角を求め、その中から範囲の中に入る点を選ぶ
        bool any = false;
        Bounds bounds = new Bounds();
        foreach(Shape shape in active.shapes)
        {
            if(shape.owner == null || !shape.owner.activeInHierarchy) { continue; }
            foreach(Vector2 corner in LocalCorners(shape))
            {
                Vector3 world = shape.transform.TransformPoint(corner + shape.offset);
                if(!any) { bounds = new Bounds(world, Vector3.zero); any = true; } else { bounds.Encapsulate(world); }
            }
        }
        if(!any) { return false; }

        for(int i = 0; i < 30; i++)
        {
            var p = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
            if(Contains(p)) { point = p; return true; }
        }
        return false;
    }

    // 形を囲む四角の角（形のローカル座標）
    private static IEnumerable<Vector2> LocalCorners(Shape shape)
    {
        switch(shape.kind)
        {
            case 0:
                yield return new Vector2(-shape.radius, -shape.radius);
                yield return new Vector2(shape.radius, shape.radius);
                break;
            case 1:
                yield return -shape.halfSize;
                yield return shape.halfSize;
                yield return new Vector2(-shape.halfSize.x, shape.halfSize.y);
                yield return new Vector2(shape.halfSize.x, -shape.halfSize.y);
                break;
            default:
                if(shape.points != null) { foreach(Vector2 p in shape.points) { yield return p; } }
                break;
        }
    }

    /// <summary>
    /// 位置がこの範囲の中か（エディタでも使える。シーンチェック用）
    /// </summary>
    public bool ContainsPoint(Vector2 position)
    {
        if(!Application.isPlaying) { Collect(false); }

        foreach(Shape shape in shapes)
        {
            if(shape.owner != null && shape.owner.activeInHierarchy && Closest(shape, position, out _)) { return true; }
        }
        return false;
    }

    /// <summary> 形の数（表示されているもの） </summary>
    public int ShapeCount
    {
        get
        {
            if(!Application.isPlaying) { Collect(false); }
            int count = 0;
            foreach(Shape shape in shapes) { if(shape.owner != null && shape.owner.activeInHierarchy) { count++; } }
            return count;
        }
    }

    private Vector2 ClampInternal(Vector2 position)
    {
        bool found = false;
        float best = float.MaxValue;
        Vector2 bestPoint = position;

        foreach(Shape shape in shapes)
        {
            if(shape.owner == null || !shape.owner.activeInHierarchy) { continue; }

            if(Closest(shape, position, out Vector2 point)) { return position; }

            float distance = (point - position).sqrMagnitude;
            if(distance < best) { best = distance; bestPoint = point; found = true; }
        }

        return found ? bestPoint : position;
    }

    /// <summary>
    /// 形の中なら true。外なら、形のふちで一番近い点を返す
    /// </summary>
    private static bool Closest(Shape shape, Vector2 world, out Vector2 closest)
    {
        Vector2 local = (Vector2)shape.transform.InverseTransformPoint(world) - shape.offset;

        switch(shape.kind)
        {
            case 0:
            {
                if(local.sqrMagnitude <= shape.radius * shape.radius) { closest = world; return true; }
                Vector2 edge = local.normalized * shape.radius;
                closest = shape.transform.TransformPoint(edge + shape.offset);
                return false;
            }
            case 1:
            {
                Vector2 clamped = new Vector2(
                    Mathf.Clamp(local.x, -shape.halfSize.x, shape.halfSize.x),
                    Mathf.Clamp(local.y, -shape.halfSize.y, shape.halfSize.y));
                if(clamped == local) { closest = world; return true; }
                closest = shape.transform.TransformPoint(clamped + shape.offset);
                return false;
            }
            default:
            {
                Vector2[] points = shape.points;
                if(points == null || points.Length < 3) { closest = world; return false; }

                // 多角形の中か（偶奇判定）
                bool inside = false;
                for(int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2 a = points[i];
                    Vector2 b = points[j];
                    if((a.y > local.y) != (b.y > local.y) &&
                       local.x < (b.x - a.x) * (local.y - a.y) / (b.y - a.y) + a.x)
                    {
                        inside = !inside;
                    }
                }
                if(inside) { closest = world; return true; }

                // 辺の上で一番近い点
                float best = float.MaxValue;
                Vector2 bestPoint = points[0];
                for(int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2 a = points[j];
                    Vector2 b = points[i];
                    Vector2 ab = b - a;
                    float t = ab.sqrMagnitude > 0.0f ? Mathf.Clamp01(Vector2.Dot(local - a, ab) / ab.sqrMagnitude) : 0.0f;
                    Vector2 p = a + ab * t;
                    float d = (p - local).sqrMagnitude;
                    if(d < best) { best = d; bestPoint = p; }
                }
                closest = shape.transform.TransformPoint(bestPoint + shape.offset);
                return false;
            }
        }
    }

    //=====================================================================
    // エディタ
    //=====================================================================

    [ContextMenu("円を追加")]
    private void AddCircle()
    {
        var go = new GameObject("円");
        go.transform.SetParent(transform, false);
        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 3.0f;
    }

    [ContextMenu("四角を追加（通路）")]
    private void AddBox()
    {
        var go = new GameObject("通路");
        go.transform.SetParent(transform, false);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4.0f, 1.5f);
    }

    private void OnDrawGizmos()
    {
        if(!alwaysShowGizmos) { return; }
        DrawShapes();
    }

    private void OnDrawGizmosSelected()
    {
        if(alwaysShowGizmos) { return; }
        DrawShapes();
    }

    private void DrawShapes()
    {
        if(!Application.isPlaying) { Collect(false); }

        Gizmos.color = new Color(0.2f, 1.0f, 0.4f, 0.9f);
        foreach(Shape shape in shapes)
        {
            if(shape.owner == null || !shape.owner.activeInHierarchy) { continue; }

            const int Segments = 40;
            if(shape.kind == 0)
            {
                Vector3 prev = shape.transform.TransformPoint(shape.offset + new Vector2(shape.radius, 0.0f));
                for(int i = 1; i <= Segments; i++)
                {
                    float angle = i * Mathf.PI * 2.0f / Segments;
                    Vector3 next = shape.transform.TransformPoint(shape.offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * shape.radius);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }
            else
            {
                Vector2[] points = shape.kind == 1
                    ? new[]
                    {
                        new Vector2(-shape.halfSize.x, -shape.halfSize.y), new Vector2(shape.halfSize.x, -shape.halfSize.y),
                        new Vector2(shape.halfSize.x, shape.halfSize.y), new Vector2(-shape.halfSize.x, shape.halfSize.y),
                    }
                    : shape.points;
                if(points == null) { continue; }

                for(int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Gizmos.DrawLine(shape.transform.TransformPoint(points[j] + shape.offset), shape.transform.TransformPoint(points[i] + shape.offset));
                }
            }
        }
    }
}
