/*********************************************************************/
// * \file   WalkAreaMask.cs
// * \brief  マスク画像 ⇄ 歩ける範囲（WalkArea）の変換（輪郭をなぞる・間引く・書き出す）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// マスク画像と歩ける範囲の変換（エディタ）
/// </summary>
///
/// ・読み込み：マスク画像の白い所の輪郭をなぞり（マーチングスクエア）、点を間引いて（ダグラス・ポイカー）、
///   子「マスクから作った範囲」の PolygonCollider2D（輪郭ごとに1本の path。内側の輪郭は穴）にする
/// ・書き出し：今の形（表示されている子）を白黒の PNG にする（今ある範囲を塗り直したいとき）
/// ・マスク画像は「合わせるマップの絵」と同じ位置・大きさとして読む（縦横比が違うと警告）
public static class WalkAreaMask
{
    public const string GeneratedChildName = "マスクから作った範囲";

    //=====================================================================
    // マスク画像 → 形
    //=====================================================================

    /// <summary>
    /// マスク画像から形を作る（前に作った「マスクから作った範囲」は作り直す）
    /// </summary>
    /// <returns> 結果の説明（失敗したときは理由） </returns>
    public static string Build(WalkArea area)
    {
        Texture2D mask = area.MaskTexture;
        SpriteRenderer target = area.MaskTarget;
        if(mask == null)   { return "マスク画像が入っていません"; }
        if(target == null || target.sprite == null) { return "合わせるマップの絵がありません（「合わせるマップの絵」に入れるか、シーンに MapBounds を置く）"; }

        Bounds world = target.bounds;
        float worldPerPixel = world.size.x / mask.width;

        // 読む細かさ：線の細かさの半分くらいの升目（細かすぎると遅いので）
        int step = Mathf.Max(1, Mathf.FloorToInt(area.MaskSimplify * 0.5f / Mathf.Max(0.0001f, worldPerPixel)));
        bool[,] grid = ReadGrid(mask, step, area.MaskThreshold, out int w, out int h);
        if(grid == null) { return "マスク画像を読めませんでした"; }

        List<List<Vector2>> loops = Trace(grid, w, h);

        // 升目の座標 → ワールド座標
        var worldLoops = new List<List<Vector2>>();
        foreach(List<Vector2> loop in loops)
        {
            var pts = new List<Vector2>(loop.Count);
            foreach(Vector2 g in loop)
            {
                pts.Add(new Vector2(
                    world.min.x + (g.x + 0.5f) / w * world.size.x,
                    world.min.y + (g.y + 0.5f) / h * world.size.y));
            }

            List<Vector2> simple = SimplifyClosed(pts, area.MaskSimplify);
            if(simple.Count < 3) { continue; }
            if(Mathf.Abs(SignedArea(simple)) < area.MaskSimplify * area.MaskSimplify * 4.0f) { continue; }
            worldLoops.Add(simple);
        }

        if(worldLoops.Count == 0) { return "白い所が見つかりませんでした（白とみなす明るさを下げるか、マスク画像を確かめてください）"; }

        // 子を作り直す
        Transform old = area.transform.Find(GeneratedChildName);
        if(old != null) { Undo.DestroyObjectImmediate(old.gameObject); }

        var go = new GameObject(GeneratedChildName);
        Undo.RegisterCreatedObjectUndo(go, "マスクから歩ける範囲を作る");
        go.transform.SetParent(area.transform, false);
        go.transform.SetAsFirstSibling();

        var poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.pathCount = worldLoops.Count;
        int points = 0;
        for(int i = 0; i < worldLoops.Count; i++)
        {
            var local = new Vector2[worldLoops[i].Count];
            for(int k = 0; k < local.Length; k++) { local[k] = go.transform.InverseTransformPoint(worldLoops[i][k]); }
            poly.SetPath(i, local);
            points += local.Length;
        }

        EditorUtility.SetDirty(area);
        string aspect = Mathf.Abs((float)mask.width / mask.height - world.size.x / world.size.y) > 0.02f
            ? "\n⚠ マスク画像と絵の縦横比が違います（引き伸ばして重ねました）" : "";
        return $"作りました：輪郭 {worldLoops.Count} 本（内側の輪郭は穴）・点 {points} 個（読んだ升目 {w}×{h}）{aspect}";
    }

    /// <summary>
    /// マスク画像を白黒の升目にする（step ピクセルごとに1つ。白さ×不透明さ が threshold より上なら歩ける）
    /// </summary>
    private static bool[,] ReadGrid(Texture2D mask, int step, float threshold, out int w, out int h)
    {
        w = Mathf.Max(1, mask.width / step);
        h = Mathf.Max(1, mask.height / step);

        // 取り込みの設定（読み書き不可・圧縮）に関係なく読めるように、一度描いて読み取る
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture prev = RenderTexture.active;
        Graphics.Blit(mask, rt);
        RenderTexture.active = rt;
        var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        read.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = read.GetPixels32();
        Object.DestroyImmediate(read);

        var grid = new bool[w, h];
        for(int y = 0; y < h; y++)
        {
            for(int x = 0; x < w; x++)
            {
                Color32 c = pixels[y * w + x];
                float v = (c.r + c.g + c.b) / (3.0f * 255.0f) * (c.a / 255.0f);
                grid[x, y] = v > threshold;
            }
        }
        return grid;
    }

    //=====================================================================
    // 輪郭をなぞる（マーチングスクエア）
    //=====================================================================

    // 升目の辺の中点を、2倍した整数座標で表す（同じ点を同じ値にするため）
    private static long Key(int x2, int y2) => ((long)(x2 + 4) << 32) | (uint)(y2 + 4);

    /// <summary>
    /// 白い所の輪郭（閉じた線。座標は升目の位置、0.5 刻み）
    /// </summary>
    private static List<List<Vector2>> Trace(bool[,] grid, int w, int h)
    {
        var neighbors = new Dictionary<long, List<long>>();
        var coords = new Dictionary<long, Vector2>();

        void Link(int ax, int ay, int bx, int by)
        {
            long a = Key(ax, ay), b = Key(bx, by);
            if(!neighbors.TryGetValue(a, out var la)) { la = new List<long>(2); neighbors[a] = la; coords[a] = new Vector2(ax * 0.5f, ay * 0.5f); }
            if(!neighbors.TryGetValue(b, out var lb)) { lb = new List<long>(2); neighbors[b] = lb; coords[b] = new Vector2(bx * 0.5f, by * 0.5f); }
            la.Add(b);
            lb.Add(a);
        }

        bool At(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && grid[x, y];

        // 升目の外側（-1 と w）も見るので、輪郭は必ず閉じる
        for(int cy = -1; cy < h; cy++)
        {
            for(int cx = -1; cx < w; cx++)
            {
                int index = (At(cx, cy) ? 1 : 0) | (At(cx + 1, cy) ? 2 : 0) | (At(cx + 1, cy + 1) ? 4 : 0) | (At(cx, cy + 1) ? 8 : 0);
                if(index == 0 || index == 15) { continue; }

                // 辺の中点（2倍した座標）
                int bX = 2 * cx + 1, bY = 2 * cy;          // 下
                int rX = 2 * cx + 2, rY = 2 * cy + 1;      // 右
                int tX = 2 * cx + 1, tY = 2 * cy + 2;      // 上
                int lX = 2 * cx,     lY = 2 * cy + 1;      // 左

                switch(index)
                {
                    case 1:  case 14: Link(lX, lY, bX, bY); break;
                    case 2:  case 13: Link(bX, bY, rX, rY); break;
                    case 3:  case 12: Link(lX, lY, rX, rY); break;
                    case 4:  case 11: Link(rX, rY, tX, tY); break;
                    case 6:  case 9:  Link(bX, bY, tX, tY); break;
                    case 7:  case 8:  Link(lX, lY, tX, tY); break;
                    case 5:  Link(lX, lY, bX, bY); Link(rX, rY, tX, tY); break;   // 斜めに2つ：別々の角として扱う
                    case 10: Link(bX, bY, rX, rY); Link(tX, tY, lX, lY); break;
                }
            }
        }

        // つながりをたどって閉じた線にする
        var loops = new List<List<Vector2>>();
        var visited = new HashSet<long>();
        foreach(long start in neighbors.Keys)
        {
            if(visited.Contains(start)) { continue; }

            var loop = new List<Vector2>();
            long prev = long.MinValue;
            long cur = start;
            while(cur != long.MinValue && !visited.Contains(cur))
            {
                visited.Add(cur);
                loop.Add(coords[cur]);

                long next = long.MinValue;
                foreach(long n in neighbors[cur])
                {
                    if(n != prev && !visited.Contains(n)) { next = n; break; }
                }
                prev = cur;
                cur = next;
            }
            if(loop.Count >= 3) { loops.Add(loop); }
        }
        return loops;
    }

    //=====================================================================
    // 間引く（ダグラス・ポイカー）
    //=====================================================================

    private static List<Vector2> SimplifyClosed(List<Vector2> pts, float epsilon)
    {
        if(pts.Count < 4) { return new List<Vector2>(pts); }

        // いちばん離れた2点で2つに分けて、それぞれ間引く
        int a = 0, b = 0;
        float best = -1.0f;
        for(int i = 0; i < pts.Count; i++)
        {
            float d = (pts[i] - pts[0]).sqrMagnitude;
            if(d > best) { best = d; b = i; }
        }

        var first = pts.GetRange(a, b - a + 1);
        var second = pts.GetRange(b, pts.Count - b);
        second.Add(pts[0]);

        var result = new List<Vector2>();
        List<Vector2> s1 = Simplify(first, epsilon);
        List<Vector2> s2 = Simplify(second, epsilon);
        result.AddRange(s1.GetRange(0, s1.Count - 1));
        result.AddRange(s2.GetRange(0, s2.Count - 1));
        return result;
    }

    private static List<Vector2> Simplify(List<Vector2> pts, float epsilon)
    {
        if(pts.Count < 3) { return new List<Vector2>(pts); }

        var keep = new bool[pts.Count];
        keep[0] = true;
        keep[pts.Count - 1] = true;
        var stack = new Stack<(int, int)>();
        stack.Push((0, pts.Count - 1));
        while(stack.Count > 0)
        {
            (int s, int e) = stack.Pop();
            float maxD = -1.0f;
            int index = -1;
            for(int i = s + 1; i < e; i++)
            {
                float d = DistanceToSegment(pts[i], pts[s], pts[e]);
                if(d > maxD) { maxD = d; index = i; }
            }
            if(index >= 0 && maxD > epsilon)
            {
                keep[index] = true;
                stack.Push((s, index));
                stack.Push((index, e));
            }
        }

        var result = new List<Vector2>();
        for(int i = 0; i < pts.Count; i++) { if(keep[i]) { result.Add(pts[i]); } }
        return result;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 0.0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0.0f;
        return (p - (a + ab * t)).magnitude;
    }

    private static float SignedArea(List<Vector2> pts)
    {
        float s = 0.0f;
        for(int i = 0, j = pts.Count - 1; i < pts.Count; j = i++) { s += (pts[j].x * pts[i].y) - (pts[i].x * pts[j].y); }
        return s * 0.5f;
    }

    //=====================================================================
    // 形 → マスク画像
    //=====================================================================

    /// <summary>
    /// 今の形（表示されている子）を白黒の PNG に書き出す。横は最大 maxWidth ピクセル
    /// </summary>
    /// <returns> 書き出したアセットのパス（失敗したら null） </returns>
    public static string Export(WalkArea area, string assetPath, int maxWidth, out string message)
    {
        SpriteRenderer target = area.MaskTarget;
        if(target == null || target.sprite == null) { message = "合わせるマップの絵がありません"; return null; }

        Bounds world = target.bounds;
        Texture2D source = target.sprite.texture;
        int w = Mathf.Min(maxWidth, source != null ? source.width : maxWidth);
        int h = Mathf.Max(1, Mathf.RoundToInt(w * world.size.y / world.size.x));

        System.Func<Vector2, bool> inside = area.BuildTester();
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        var white = new Color32(255, 255, 255, 255);
        var clear = new Color32(0, 0, 0, 0);
        int count = 0;
        for(int y = 0; y < h; y++)
        {
            float wy = world.min.y + (y + 0.5f) / h * world.size.y;
            for(int x = 0; x < w; x++)
            {
                float wx = world.min.x + (x + 0.5f) / w * world.size.x;
                bool v = inside(new Vector2(wx, wy));
                pixels[y * w + x] = v ? white : clear;
                if(v) { count++; }
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if(importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        message = $"書き出しました：{assetPath}（{w}×{h}、歩ける所 {count * 100 / (w * h)}%）";
        return assetPath;
    }
}
