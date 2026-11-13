/*********************************************************************/
// * \file   WalkArea.cs
// * \brief  プレイヤー・敵・相棒が歩ける範囲を、円・四角・多角形の組み合わせで決める
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 歩ける範囲（円・四角・多角形の組み合わせ）
/// </summary>
///
/// ・このオブジェクトの子に Collider2D（Circle / Box / Polygon）を付けた物を置くと、その形を合わせた範囲だけ歩ける
///   （ティンパニの上と、それをつなぐ通路だけ歩ける、など）
/// ・子に WalkAreaExclude（歩けない形）を付けると、その形は範囲から抜ける（柱・穴）
/// ・多角形に輪郭が2つ以上あるときは、内側の輪郭は穴になる（マスク画像から作ったとき）
/// ・形はシーンビューで Collider2D を編集して決める。数・位置・大きさは自由
/// ・子を非表示にすると、その形は範囲から外れる（イベントで表示すれば、通路が開く）
/// ・Collider2D は形を読むためだけに使い、再生中は無効にする（弾や敵にはぶつからない）
/// ・1つのシーンに何個置いてもよい（部屋ごとなど）。全部を合わせた範囲になる
/// ・プレイヤー・敵の移動・敵と相棒の道（NavMesh2D）・アイテムや落石の出る場所が、この範囲を使う（MapArea）
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
        public bool exclude;

        // 0 = 円、1 = 四角、2 = 多角形
        public int kind;
        public Vector2 offset;
        public float radius;
        public Vector2 halfSize;
        public Vector2[][] paths;

        public bool Active => owner != null && owner.activeInHierarchy;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("シーンビューに範囲を表示")]
    [Tooltip("選んでいなくても、歩ける範囲を緑の線（歩けない形は赤い線）で表示する")]
    private bool alwaysShowGizmos = true;

    [Header("マスク画像から作る（エディタ）")]

    [SerializeField, Label("マスク画像")]
    [Tooltip("マップの絵と同じ大きさの画像。白く塗った所が歩ける所（透明・黒は歩けない）。下の「マスク画像から作る」で形を作る")]
    private Texture2D maskTexture;

    [SerializeField, Label("合わせるマップの絵")]
    [Tooltip("マスク画像を重ねる絵（この絵と同じ位置・大きさとして読む）。空ならシーンの MapBounds の絵")]
    private SpriteRenderer maskTarget;

    [SerializeField, Label("線の細かさ（マス）", 0.02f, 1.0f)]
    [Tooltip("輪郭の点を間引く細かさ。小さいほど絵に忠実だが点が増える（0.1 くらいがおすすめ）")]
    private float maskSimplify = 0.1f;

    [SerializeField, Label("白とみなす明るさ", 0.05f, 0.95f)]
    [Tooltip("この明るさ（白さ×不透明さ）より明るい所を歩ける所にする")]
    private float maskThreshold = 0.5f;

    /// <summary> エディタ用：マスク画像 </summary>
    public Texture2D MaskTexture => maskTexture;

    /// <summary> エディタ用：マスクを重ねる絵（空ならシーンの MapBounds の絵） </summary>
    public SpriteRenderer MaskTarget
    {
        get
        {
            if(maskTarget != null) { return maskTarget; }
            MapBounds bounds = FindAnyObjectByType<MapBounds>();
            return bounds != null ? bounds.GetComponent<SpriteRenderer>() : null;
        }
    }

    /// <summary> エディタ用：線の細かさ </summary>
    public float MaskSimplify => maskSimplify;

    /// <summary> エディタ用：白とみなす明るさ </summary>
    public float MaskThreshold => maskThreshold;

    /// <summary> エディタ用：マスク画像を入れる </summary>
    public void SetMaskTexture(Texture2D texture) { maskTexture = texture; }

    /// <summary>
    /// エディタ用：今の形（表示されている子だけ）で「中か」を調べる関数（形を1回だけ読むので速い）
    /// </summary>
    /// <param name="includeHidden"> 非表示の子（イベントで開く通路など）も形に入れる </param>
    public System.Func<Vector2, bool> BuildTester(bool includeHidden = false)
    {
        Collect(false);
        var list = new List<Shape>();
        foreach(Shape s in shapes) { if(s.Active || (includeHidden && s.owner != null)) { list.Add(s); } }
        return p => ContainsIn(list, p);
    }

    private static readonly List<WalkArea> all = new List<WalkArea>();

    private readonly List<Shape> shapes = new List<Shape>();

    /// <summary> 歩ける範囲が決められているシーンか </summary>
    public static bool Exists
    {
        get
        {
            foreach(WalkArea area in all) { if(area != null && area.isActiveAndEnabled) { return true; } }
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { all.Clear(); }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(!all.Contains(this)) { all.Add(this); }
        Collect(true);
    }

    private void OnDestroy()
    {
        all.Remove(this);
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
            var shape = new Shape
            {
                owner = col.gameObject,
                transform = col.transform,
                offset = col.offset,
                exclude = col.GetComponent<WalkAreaExclude>() != null,
            };

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
                shape.paths = new Vector2[polygon.pathCount][];
                for(int i = 0; i < polygon.pathCount; i++) { shape.paths[i] = polygon.GetPath(i); }
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
    // 判定（シーン全体）
    //=====================================================================

    /// <summary> 位置が歩ける範囲の中か（歩ける範囲がないシーンでは true） </summary>
    public static bool Contains(Vector2 position)
    {
        if(!Exists) { return true; }
        return ContainsIn(ActiveShapes(), position);
    }

    /// <summary>
    /// 位置を歩ける範囲の中に収める（中ならそのまま。外・歩けない形の中なら、一番近い歩ける所へ）
    /// 歩ける範囲がないシーンではそのまま返す
    /// </summary>
    public static Vector2 Clamp(Vector2 position)
    {
        if(!Exists) { return position; }
        return ClampIn(ActiveShapes(), position);
    }

    /// <summary> 歩ける範囲を囲む四角（歩ける範囲がないシーンでは false） </summary>
    public static bool TryGetBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        if(!Exists) { return false; }
        return BoundsOf(ActiveShapes(), out bounds);
    }

    /// <summary>
    /// 歩ける範囲の中のでたらめな位置（落石をばらまく場所など）。歩ける範囲がないシーンでは false
    /// </summary>
    public static bool TryRandomPoint(out Vector2 point)
    {
        point = Vector2.zero;
        if(!TryGetBounds(out Bounds bounds)) { return false; }

        List<Shape> list = ActiveShapes();
        for(int i = 0; i < 30; i++)
        {
            var p = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
            if(ContainsIn(list, p)) { point = p; return true; }
        }
        return false;
    }

    /// <summary>
    /// 範囲の形が変わったかの目印（敵の道を作り直すかどうかに使う）
    /// </summary>
    public static int Signature
    {
        get
        {
            unchecked
            {
                int hash = 17;
                foreach(WalkArea area in all)
                {
                    if(area == null || !area.isActiveAndEnabled) { continue; }
                    foreach(Shape s in area.shapes)
                    {
                        if(!s.Active) { continue; }
                        Vector3 p = s.transform.position;
                        hash = hash * 31 + Mathf.RoundToInt(p.x * 20.0f);
                        hash = hash * 31 + Mathf.RoundToInt(p.y * 20.0f);
                        hash = hash * 31 + Mathf.RoundToInt(s.transform.lossyScale.x * 100.0f);
                        hash = hash * 31 + (s.exclude ? 7 : 3);
                    }
                }
                return hash;
            }
        }
    }

    /// <summary>
    /// 表示されている形の輪郭（ワールド座標の閉じた線）。exclude は「歩けない形」（WalkAreaExclude）の線か
    /// （ゲーム中に範囲を見せる表示 MapDebugView に使う）
    /// </summary>
    public static void CollectOutlines(List<Vector3[]> lines, List<bool> exclude)
    {
        lines.Clear();
        exclude.Clear();
        foreach(Shape s in ActiveShapes())
        {
            const int Segments = 48;
            if(s.kind == 0)
            {
                var pts = new Vector3[Segments];
                for(int i = 0; i < Segments; i++)
                {
                    float a = i * Mathf.PI * 2.0f / Segments;
                    pts[i] = s.transform.TransformPoint(s.offset + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s.radius);
                }
                lines.Add(pts);
                exclude.Add(s.exclude);
            }
            else if(s.kind == 1)
            {
                Vector2 h = s.halfSize;
                lines.Add(new Vector3[]
                {
                    s.transform.TransformPoint(s.offset + new Vector2(-h.x, -h.y)), s.transform.TransformPoint(s.offset + new Vector2(h.x, -h.y)),
                    s.transform.TransformPoint(s.offset + new Vector2(h.x, h.y)),   s.transform.TransformPoint(s.offset + new Vector2(-h.x, h.y)),
                });
                exclude.Add(s.exclude);
            }
            else if(s.paths != null)
            {
                foreach(Vector2[] path in s.paths)
                {
                    if(path == null || path.Length < 2) { continue; }
                    var pts = new Vector3[path.Length];
                    for(int i = 0; i < path.Length; i++) { pts[i] = s.transform.TransformPoint(path[i] + s.offset); }
                    lines.Add(pts);
                    exclude.Add(s.exclude);
                }
            }
        }
    }

    //=====================================================================
    // 判定（この範囲だけ。エディタでも使える。シーンチェック用）
    //=====================================================================

    /// <summary> 位置がこの範囲の中か </summary>
    public bool ContainsPoint(Vector2 position)
    {
        if(!Application.isPlaying) { Collect(false); }
        var list = new List<Shape>();
        foreach(Shape s in shapes) { if(s.Active) { list.Add(s); } }
        return ContainsIn(list, position);
    }

    /// <summary> 歩ける形の数（表示されているもの。歩けない形は数えない） </summary>
    public int ShapeCount
    {
        get
        {
            if(!Application.isPlaying) { Collect(false); }
            int count = 0;
            foreach(Shape shape in shapes) { if(shape.Active && !shape.exclude) { count++; } }
            return count;
        }
    }

    //=====================================================================
    // 計算
    //=====================================================================

    private static List<Shape> ActiveShapes()
    {
        var list = new List<Shape>();
        foreach(WalkArea area in all)
        {
            if(area == null || !area.isActiveAndEnabled) { continue; }
            foreach(Shape s in area.shapes) { if(s.Active) { list.Add(s); } }
        }
        return list;
    }

    private static bool ContainsIn(List<Shape> list, Vector2 position)
    {
        bool inside = false;
        foreach(Shape s in list)
        {
            if(s.exclude) { continue; }
            if(Inside(s, position)) { inside = true; break; }
        }
        if(!inside) { return false; }

        foreach(Shape s in list)
        {
            if(s.exclude && Inside(s, position)) { return false; }
        }
        return true;
    }

    private static Vector2 ClampIn(List<Shape> list, Vector2 position)
    {
        if(ContainsIn(list, position)) { return position; }

        // 形のふちで一番近い点を、範囲の内側へ少しだけ押し込んだものから、一番近い歩ける所を選ぶ
        const float Nudge = 0.01f;
        Vector2 best = position;
        float bestDistance = float.MaxValue;
        Vector2 fallback = position;
        float fallbackDistance = float.MaxValue;

        foreach(Shape s in list)
        {
            Vector2 edge = ClosestOnEdge(s, position);
            Vector2 dir = edge - position;
            Vector2 candidate = dir.sqrMagnitude > 0.000001f ? edge + dir.normalized * Nudge : edge;
            float d = (candidate - position).sqrMagnitude;

            if(!s.exclude && d < fallbackDistance) { fallbackDistance = d; fallback = edge; }
            if(d < bestDistance && ContainsIn(list, candidate)) { bestDistance = d; best = candidate; }
        }

        return bestDistance < float.MaxValue ? best : fallback;
    }

    private static bool BoundsOf(List<Shape> list, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach(Shape s in list)
        {
            if(s.exclude) { continue; }
            foreach(Vector2 corner in LocalCorners(s))
            {
                Vector3 world = s.transform.TransformPoint(corner + s.offset);
                if(!any) { bounds = new Bounds(world, Vector3.zero); any = true; } else { bounds.Encapsulate(world); }
            }
        }
        return any;
    }

    // 形を囲む四角の角（形のローカル座標）
    private static IEnumerable<Vector2> LocalCorners(Shape shape)
    {
        switch(shape.kind)
        {
            case 0:
                yield return new Vector2(-shape.radius, -shape.radius);
                yield return new Vector2(shape.radius, shape.radius);
                yield return new Vector2(-shape.radius, shape.radius);
                yield return new Vector2(shape.radius, -shape.radius);
                break;
            case 1:
                yield return -shape.halfSize;
                yield return shape.halfSize;
                yield return new Vector2(-shape.halfSize.x, shape.halfSize.y);
                yield return new Vector2(shape.halfSize.x, -shape.halfSize.y);
                break;
            default:
                if(shape.paths == null) { break; }
                foreach(Vector2[] path in shape.paths) { foreach(Vector2 p in path) { yield return p; } }
                break;
        }
    }

    /// <summary> 形の中か（多角形は全部の輪郭で偶奇判定＝内側の輪郭は穴） </summary>
    private static bool Inside(Shape shape, Vector2 world)
    {
        Vector2 local = (Vector2)shape.transform.InverseTransformPoint(world) - shape.offset;

        switch(shape.kind)
        {
            case 0:
                return local.sqrMagnitude <= shape.radius * shape.radius;
            case 1:
                return Mathf.Abs(local.x) <= shape.halfSize.x && Mathf.Abs(local.y) <= shape.halfSize.y;
            default:
                if(shape.paths == null) { return false; }
                bool inside = false;
                foreach(Vector2[] points in shape.paths)
                {
                    if(points == null || points.Length < 3) { continue; }
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
                }
                return inside;
        }
    }

    /// <summary> 形のふちで一番近い点（ワールド座標） </summary>
    private static Vector2 ClosestOnEdge(Shape shape, Vector2 world)
    {
        Vector2 local = (Vector2)shape.transform.InverseTransformPoint(world) - shape.offset;

        switch(shape.kind)
        {
            case 0:
            {
                Vector2 dir = local.sqrMagnitude > 0.000001f ? local.normalized : Vector2.right;
                return shape.transform.TransformPoint(dir * shape.radius + shape.offset);
            }
            case 1:
            {
                Vector2 h = shape.halfSize;
                Vector2 c = new Vector2(Mathf.Clamp(local.x, -h.x, h.x), Mathf.Clamp(local.y, -h.y, h.y));
                if(c == local)
                {
                    // 中にいるときは、一番近い辺へ
                    float dx = h.x - Mathf.Abs(local.x);
                    float dy = h.y - Mathf.Abs(local.y);
                    if(dx < dy) { c.x = Mathf.Sign(local.x == 0.0f ? 1.0f : local.x) * h.x; }
                    else        { c.y = Mathf.Sign(local.y == 0.0f ? 1.0f : local.y) * h.y; }
                }
                return shape.transform.TransformPoint(c + shape.offset);
            }
            default:
            {
                float best = float.MaxValue;
                Vector2 bestPoint = local;
                if(shape.paths != null)
                {
                    foreach(Vector2[] points in shape.paths)
                    {
                        if(points == null || points.Length < 2) { continue; }
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
                    }
                }
                return shape.transform.TransformPoint(bestPoint + shape.offset);
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

    [ContextMenu("歩けない形を追加（円）")]
    private void AddExcludeCircle()
    {
        var go = new GameObject("歩けない形（円）");
        go.transform.SetParent(transform, false);
        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 1.0f;
        go.AddComponent<WalkAreaExclude>();
    }

    [ContextMenu("歩けない形を追加（四角）")]
    private void AddExcludeBox()
    {
        var go = new GameObject("歩けない形（四角）");
        go.transform.SetParent(transform, false);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.0f, 2.0f);
        go.AddComponent<WalkAreaExclude>();
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

        foreach(Shape shape in shapes)
        {
            if(!shape.Active) { continue; }
            Gizmos.color = shape.exclude ? new Color(1.0f, 0.3f, 0.3f, 0.9f) : new Color(0.2f, 1.0f, 0.4f, 0.9f);

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
                Vector2[][] paths = shape.kind == 1
                    ? new[] { new[]
                    {
                        new Vector2(-shape.halfSize.x, -shape.halfSize.y), new Vector2(shape.halfSize.x, -shape.halfSize.y),
                        new Vector2(shape.halfSize.x, shape.halfSize.y), new Vector2(-shape.halfSize.x, shape.halfSize.y),
                    } }
                    : shape.paths;
                if(paths == null) { continue; }

                foreach(Vector2[] points in paths)
                {
                    if(points == null) { continue; }
                    for(int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    {
                        Gizmos.DrawLine(shape.transform.TransformPoint(points[j] + shape.offset), shape.transform.TransformPoint(points[i] + shape.offset));
                    }
                }
            }
        }
    }
}
