/*********************************************************************/
// * \file   TelegraphView.cs
// * \brief  攻撃の予告（範囲の表示）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃の予告の表示
/// </summary>
///
/// ・攻撃を出すときに自動で作られ、終わると片づけられる（使い回す）。シーンに置かなくてよい
/// ・形（AttackShape）からメッシュを作って描く。塗り・枠・内側から満ちていく表示・点滅
/// ・光の影響を受けないマテリアル（Unlit）を既定にする（暗いステージでも見えるように）
public class TelegraphView : MonoBehaviour
{
    //=====================================================================
    // 使い回し
    //=====================================================================

    private static readonly Stack<TelegraphView> pool = new Stack<TelegraphView>();
    private static Material unlitMaterial;
    private static Material litMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        pool.Clear();
        unlitMaterial = null;
        litMaterial = null;
    }

    /// <summary>
    /// 予告を出す
    /// </summary>
    public static TelegraphView Show(AttackData data, Vector2 origin, Vector2 direction, float lengthOverride)
    {
        TelegraphView view = null;
        while(pool.Count > 0 && view == null) { view = pool.Pop(); }
        if(view == null) { view = Create(); }

        view.gameObject.SetActive(true);
        view.data = data;
        view.Setup(data.shape, data.telegraphColor, data.telegraphStyle, data.telegraphFill && data.telegraphTime > 0.0f, data.telegraphUnlit, data.telegraphSortingOrder, lengthOverride);
        view.SetPose(origin, direction);
        view.SetProgress(0.0f);
        return view;
    }

    /// <summary>
    /// 形と色を直接指定して出す（仲間を呼ぶ場所・置いた爆弾など、AttackData がないもの）
    /// </summary>
    public static TelegraphView ShowShape(AttackShape shape, Vector2 origin, Vector2 direction, Color color, bool fill, int sortingOrder = 3, bool blink = false)
    {
        TelegraphView view = null;
        while(pool.Count > 0 && view == null) { view = pool.Pop(); }
        if(view == null) { view = Create(); }

        view.gameObject.SetActive(true);
        view.data = null;
        view.blinkOverride = blink;
        view.Setup(shape, color, TelegraphStyle.FillAndOutline, fill, true, sortingOrder, -1.0f);
        view.SetPose(origin, direction);
        view.SetProgress(0.0f);
        return view;
    }

    /// <summary>
    /// 円の予告を出す（ShowShape の簡単版）
    /// </summary>
    public static TelegraphView ShowCircle(Vector2 center, float radius, Color color, bool fill = true, int sortingOrder = 3)
    {
        var shape = new AttackShape { type = AttackShapeType.Circle, radius = radius };
        return ShowShape(shape, center, Vector2.right, color, fill, sortingOrder);
    }

    /// <summary>
    /// 片づける（使い回す）
    /// </summary>
    public void Hide()
    {
        if(this == null) { return; }
        gameObject.SetActive(false);
        pool.Push(this);
    }

    private static TelegraphView Create()
    {
        var go = new GameObject("攻撃の予告");
        Object.DontDestroyOnLoad(go);
        var view = go.AddComponent<TelegraphView>();
        view.fill = CreatePart(go.transform, "塗り");
        view.progress = CreatePart(go.transform, "満ちていく塗り");
        view.outline = CreatePart(go.transform, "枠");
        return view;
    }

    private static Part CreatePart(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var part = new Part
        {
            transform = go.transform,
            mesh = new Mesh { name = name },
            renderer = go.AddComponent<MeshRenderer>(),
        };
        part.mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = part.mesh;
        return part;
    }

    private static Material GetMaterial(bool unlit)
    {
        if(unlit)
        {
            if(unlitMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if(shader == null) { shader = Shader.Find("Sprites/Default"); }
                unlitMaterial = new Material(shader) { name = "Telegraph (Unlit)" };
            }
            return unlitMaterial;
        }

        if(litMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if(shader == null) { shader = Shader.Find("Sprites/Default"); }
            litMaterial = new Material(shader) { name = "Telegraph (Lit)" };
        }
        return litMaterial;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    private class Part
    {
        public Transform transform;
        public Mesh mesh;
        public MeshRenderer renderer;
    }

    private Part fill;
    private Part progress;
    private Part outline;

    private AttackData data;
    private Color baseColor;
    private bool showProgress;
    private bool rectangle;

    private static readonly List<Vector3> vertices = new List<Vector3>();
    private static readonly List<int> triangles = new List<int>();
    private static readonly List<Color> colors = new List<Color>();

    private const float OutlineThickness = 0.08f;

    //=====================================================================
    // 関数
    //=====================================================================

    private bool blinkOverride;

    private void Setup(AttackShape shape, Color color, TelegraphStyle style, bool fillProgress, bool unlit, int sortingOrder, float lengthOverride)
    {
        if(data != null) { blinkOverride = false; }
        baseColor = color;
        rectangle = shape.type == AttackShapeType.Rectangle;

        bool drawFill = style != TelegraphStyle.Outline;
        bool drawOutline = style != TelegraphStyle.Fill;
        showProgress = fillProgress;

        Material material = GetMaterial(unlit);

        shape.BuildFill(vertices, triangles, lengthOverride);
        Apply(fill, drawFill, material, sortingOrder, baseColor);
        Apply(progress, showProgress, material, sortingOrder, new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(baseColor.a * 1.6f)));

        shape.BuildOutline(vertices, triangles, OutlineThickness, lengthOverride);
        Apply(outline, drawOutline, material, sortingOrder + 1, new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(baseColor.a * 2.5f)));
    }

    private static void Apply(Part part, bool visible, Material material, int order, Color color)
    {
        part.transform.gameObject.SetActive(visible);
        if(!visible) { return; }

        colors.Clear();
        for(int i = 0; i < vertices.Count; i++) { colors.Add(color); }

        part.mesh.Clear();
        part.mesh.SetVertices(vertices);
        part.mesh.SetColors(colors);
        part.mesh.SetTriangles(triangles, 0);
        part.mesh.RecalculateBounds();

        part.renderer.sharedMaterial = material;
        part.renderer.sortingOrder = order;
    }

    /// <summary>
    /// 位置と向きを変える
    /// </summary>
    public void SetPose(Vector2 origin, Vector2 direction)
    {
        float angle = direction.sqrMagnitude > 0.0001f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0.0f;
        transform.SetPositionAndRotation(new Vector3(origin.x, origin.y, 0.0f), Quaternion.Euler(0.0f, 0.0f, angle));
    }

    /// <summary>
    /// 発動までの進み具合（0 → 1）。満ちていく表示と点滅に使う
    /// </summary>
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);

        if(showProgress)
        {
            // 長方形は起点から前へ伸びる。ほかは中心から広がる
            progress.transform.localScale = rectangle ? new Vector3(Mathf.Max(0.001f, t), 1.0f, 1.0f) : new Vector3(Mathf.Max(0.001f, t), Mathf.Max(0.001f, t), 1.0f);
        }

        bool visible = true;
        bool blink = data != null ? data.telegraphBlink : blinkOverride;
        if(blink && t > 0.7f)
        {
            visible = Mathf.Repeat(Time.time * 12.0f, 1.0f) < 0.5f;
        }
        SetRenderersEnabled(visible);
    }

    private void SetRenderersEnabled(bool enabled)
    {
        fill.renderer.enabled = enabled;
        progress.renderer.enabled = enabled;
        outline.renderer.enabled = enabled;
    }

    /// <summary>
    /// 発動中の表示（予告の色を濃くして出しておく。ビームなど出し続ける攻撃用）
    /// </summary>
    public void SetActiveLook()
    {
        SetRenderersEnabled(true);
        if(showProgress) { progress.transform.localScale = Vector3.one; }
    }
}
