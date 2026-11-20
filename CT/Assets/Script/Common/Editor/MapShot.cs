/*********************************************************************/
// * \file   MapShot.cs
// * \brief  マップ全体を上から撮り、歩ける範囲・調べる範囲・出口・出現位置を重ねた画像を作る
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置の撮影（Tools > シーンチェック > 配置を撮影する）
/// </summary>
///
/// ・開いているシーンのマップ全体（MapBounds の絵。なければ絵の全部）を上から撮り、範囲を色で重ねた PNG を作る
/// ・歩けない所は暗く、1 マス（100px）ごとに細い線・5 マスごとに太い線
/// ・色：赤＝出口、橙＝入ると動く範囲、水色＝調べる範囲、紫＝戦闘エリア、灰＝壁、
///   桃＝出現位置（丸はノアの足元の当たり判定）、黄＝直接再生したときの位置
/// ・物の寄りすぎ・範囲の重なり・出口と出現位置の近さを、作った直後に目で確かめるため
public static class MapShot
{
    //=====================================================================
    // 定数
    //=====================================================================

    static readonly Color Exit = new Color(1.0f, 0.15f, 0.15f);
    static readonly Color Enter = new Color(1.0f, 0.6f, 0.1f);
    static readonly Color Examine = new Color(0.2f, 0.85f, 1.0f);
    static readonly Color Battle = new Color(0.7f, 0.3f, 1.0f);
    static readonly Color Wall = new Color(0.6f, 0.6f, 0.6f);
    static readonly Color Spawn = new Color(1.0f, 0.3f, 0.85f);
    static readonly Color Direct = new Color(1.0f, 0.95f, 0.2f);

    //=====================================================================
    // 入口
    //=====================================================================

    [MenuItem("Tools/シーンチェック/配置を撮影する")]
    static void Menu()
    {
        string path = Capture("Temp/MapShot/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ".png", 50);
        EditorUtility.RevealInFinder(path.Split('\n')[0]);
    }

    /// <summary>開いているシーンを撮って outPath に書く。1 行目にパス、続けて凡例と出現位置の座標を返す</summary>
    public static string Capture(string outPath, int pxPerUnit = 50)
    {
        Bounds b = MapRect();
        int w = Mathf.Max(16, Mathf.RoundToInt(b.size.x * pxPerUnit));
        int h = Mathf.Max(16, Mathf.RoundToInt(b.size.y * pxPerUnit));

        // 撮る
        var camGo = new GameObject("__MapShotCamera") { hideFlags = HideFlags.HideAndDontSave };
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = b.extents.y;
            cam.aspect = (float)w / h;
            cam.transform.position = new Vector3(b.center.x, b.center.y, -10.0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            RenderTexture.active = prev;
            cam.targetTexture = null;
        }
        finally
        {
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        Color[] px = tex.GetPixels();
        System.Func<Vector2, Vector2Int> toPx = p => new Vector2Int(Mathf.RoundToInt((p.x - b.min.x) * pxPerUnit), Mathf.RoundToInt((p.y - b.min.y) * pxPerUnit));

        // 歩けない所を暗く
        WalkArea area = Object.FindAnyObjectByType<WalkArea>();
        if(area != null)
        {
            var inside = area.BuildTester();
            for(int y = 0; y < h; y++)
            {
                for(int x = 0; x < w; x++)
                {
                    Vector2 world = new Vector2(b.min.x + (x + 0.5f) / pxPerUnit, b.min.y + (y + 0.5f) / pxPerUnit);
                    if(!inside(world)) { int i = y * w + x; px[i] = Color.Lerp(px[i], Color.black, 0.6f); }
                }
            }
        }

        // 1 マスごとの線
        for(int y = 0; y < h; y++)
        {
            for(int x = 0; x < w; x++)
            {
                float wx = b.min.x + (float)x / pxPerUnit, wy = b.min.y + (float)y / pxPerUnit;
                bool major = OnLine(wx, 5.0f, pxPerUnit) || OnLine(wy, 5.0f, pxPerUnit);
                bool minor = OnLine(wx, 1.0f, pxPerUnit) || OnLine(wy, 1.0f, pxPerUnit);
                if(major || minor) { int i = y * w + x; px[i] = Color.Lerp(px[i], Color.white, major ? 0.35f : 0.12f); }
            }
        }

        // 範囲
        var legend = new StringBuilder();
        foreach(Collider2D col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
        {
            if(col.GetComponentInParent<WalkArea>() != null || col.GetComponentInParent<WalkAreaExclude>() != null) { continue; }
            if(!Kind(col, out Color c)) { continue; }
            DrawCollider(px, w, h, col, c, toPx, pxPerUnit);
        }

        // 出現位置とノアの足元
        Vector2 footOffset = new Vector2(0.0f, -0.61f);
        float footRadius = 0.40f;
        var spawns = new List<KeyValuePair<Vector2, string>>();
        Transform direct = null;
        foreach(PlayerPartySpawner spawner in Object.FindObjectsByType<PlayerPartySpawner>(FindObjectsInactive.Include))
        {
            var so = new SerializedObject(spawner);
            var noah = so.FindProperty("noahPrefab").objectReferenceValue as GameObject;
            if(noah != null && noah.TryGetComponent(out CircleCollider2D circle))
            {
                Vector3 s = noah.transform.lossyScale;
                footOffset = new Vector2(circle.offset.x * s.x, circle.offset.y * s.y);
                footRadius = circle.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
            }
            direct = so.FindProperty("noahSpawnPoint").objectReferenceValue as Transform;
        }
        foreach(PlayerSpawnPoint sp in Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude))
        {
            if(sp.NoahPoint == null) { continue; }
            Vector2 p = sp.NoahPoint.position;
            DrawMarker(px, w, h, toPx(p), toPx(p + footOffset), Mathf.RoundToInt(footRadius * pxPerUnit), Spawn);
            spawns.Add(new KeyValuePair<Vector2, string>(p, sp.SpawnPointId));
        }
        if(direct != null)
        {
            Vector2 p = direct.position;
            DrawMarker(px, w, h, toPx(p), toPx(p + footOffset), Mathf.RoundToInt(footRadius * pxPerUnit), Direct);
            spawns.Add(new KeyValuePair<Vector2, string>(p, "（直接再生）"));
        }

        tex.SetPixels(px);
        tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        legend.Append(Path.GetFullPath(outPath)).Append('\n');
        legend.Append($"map {b.size.x * 100:0}x{b.size.y * 100:0}px, image {w}x{h} (1 grid = 1 unit = 100px, thick = 5 units)\n");
        legend.Append("red=exit, orange=enter area, cyan=examine, purple=battle, gray=wall, pink=spawn point (circle = Noah's feet), yellow=direct play\n");
        foreach(var s in spawns)
        {
            // 指示書と同じ「マップの絵の中心が 0」の px（右が +x、下が +y）
            Vector2 c = new Vector2((s.Key.x - b.center.x) * 100.0f, (b.center.y - s.Key.y) * 100.0f);
            legend.Append($"spawn {s.Value}: world ({s.Key.x:0.00},{s.Key.y:0.00}) / map px from top-left ({(s.Key.x - b.min.x) * 100:0},{(b.max.y - s.Key.y) * 100:0}) / from center ({c.x:0},{c.y:0})\n");
        }
        return legend.ToString();
    }

    //=====================================================================
    // 補助
    //=====================================================================

    static Bounds MapRect()
    {
        MapBounds mb = Object.FindAnyObjectByType<MapBounds>();
        if(mb != null && mb.TryGetComponent(out SpriteRenderer msr)) { return msr.bounds; }   // WorldBounds は Awake の後でないと使えない
        bool any = false;
        Bounds b = new Bounds();
        foreach(SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
        {
            if(!any) { b = sr.bounds; any = true; } else { b.Encapsulate(sr.bounds); }
        }
        return any ? b : new Bounds(Vector3.zero, new Vector3(14.4f, 10.8f, 0.0f));
    }

    static bool OnLine(float v, float step, int ppu)
    {
        float m = Mathf.Repeat(v + 0.0001f, step);
        return m < 1.0f / ppu;
    }

    static bool Kind(Collider2D col, out Color c)
    {
        c = Wall;
        if(col.GetComponentInParent<SceneChange>() != null && col.isTrigger) { c = Exit; return true; }
        if(col.GetComponentInParent<BattleArea>() != null && col.isTrigger) { c = Battle; return true; }
        if(col.GetComponentInParent<EventZone>() != null && col.isTrigger) { c = Enter; return true; }
        var ii = col.GetComponentInParent<ItemInteraction>();
        if(ii != null && col.isTrigger)
        {
            var so = new SerializedObject(ii);
            var trig = so.FindProperty("trigger");
            c = trig != null && trig.enumValueIndex == 1 ? Enter : Examine;
            return true;
        }
        if(col.GetComponentInParent<Intaractable>() != null && col.isTrigger) { c = Examine; return true; }
        if(col.isTrigger) { return false; }
        if(col.attachedRigidbody != null && col.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) { return false; }
        return true;
    }

    static void DrawCollider(Color[] px, int w, int h, Collider2D col, Color c, System.Func<Vector2, Vector2Int> toPx, int ppu)
    {
        var polys = new List<Vector2[]>();
        Transform t = col.transform;
        if(col is BoxCollider2D box)
        {
            Vector2 o = box.offset, e = box.size * 0.5f;
            polys.Add(new[] {
                (Vector2)t.TransformPoint(o + new Vector2(-e.x, -e.y)), (Vector2)t.TransformPoint(o + new Vector2(e.x, -e.y)),
                (Vector2)t.TransformPoint(o + new Vector2(e.x, e.y)), (Vector2)t.TransformPoint(o + new Vector2(-e.x, e.y)) });
        }
        else if(col is CircleCollider2D circle)
        {
            Vector2 center = t.TransformPoint(circle.offset);
            float r = circle.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
            var pts = new Vector2[32];
            for(int i = 0; i < pts.Length; i++) { float a = i * Mathf.PI * 2.0f / pts.Length; pts[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            polys.Add(pts);
        }
        else if(col is PolygonCollider2D poly)
        {
            for(int p = 0; p < poly.pathCount; p++)
            {
                var path = poly.GetPath(p);
                var pts = new Vector2[path.Length];
                for(int i = 0; i < path.Length; i++) { pts[i] = t.TransformPoint(path[i] + poly.offset); }
                polys.Add(pts);
            }
        }
        else
        {
            Bounds bb = col.bounds;
            polys.Add(new[] { (Vector2)bb.min, new Vector2(bb.max.x, bb.min.y), (Vector2)bb.max, new Vector2(bb.min.x, bb.max.y) });
        }

        foreach(var poly in polys)
        {
            var pp = new Vector2Int[poly.Length];
            for(int i = 0; i < poly.Length; i++) { pp[i] = toPx(poly[i]); }
            FillPolygon(px, w, h, pp, c, 0.22f);
            for(int i = 0; i < pp.Length; i++) { DrawLine(px, w, h, pp[i], pp[(i + 1) % pp.Length], c, 2); }
        }
    }

    static void FillPolygon(Color[] px, int w, int h, Vector2Int[] p, Color c, float alpha)
    {
        int minX = w, minY = h, maxX = 0, maxY = 0;
        foreach(var v in p) { minX = Mathf.Min(minX, v.x); minY = Mathf.Min(minY, v.y); maxX = Mathf.Max(maxX, v.x); maxY = Mathf.Max(maxY, v.y); }
        minX = Mathf.Max(0, minX); minY = Mathf.Max(0, minY); maxX = Mathf.Min(w - 1, maxX); maxY = Mathf.Min(h - 1, maxY);
        for(int y = minY; y <= maxY; y++)
        {
            for(int x = minX; x <= maxX; x++)
            {
                bool inside = false;
                for(int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                {
                    if((p[i].y > y) != (p[j].y > y) && x < (float)(p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x) { inside = !inside; }
                }
                if(inside) { int k = y * w + x; px[k] = Color.Lerp(px[k], c, alpha); }
            }
        }
    }

    static void DrawLine(Color[] px, int w, int h, Vector2Int a, Vector2Int b, Color c, int thick)
    {
        int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), 1);
        for(int s = 0; s <= steps; s++)
        {
            int x = a.x + (b.x - a.x) * s / steps, y = a.y + (b.y - a.y) * s / steps;
            for(int dy = -thick / 2; dy <= thick / 2; dy++)
            {
                for(int dx = -thick / 2; dx <= thick / 2; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if(xx >= 0 && yy >= 0 && xx < w && yy < h) { px[yy * w + xx] = c; }
                }
            }
        }
    }

    static void DrawMarker(Color[] px, int w, int h, Vector2Int at, Vector2Int foot, int r, Color c)
    {
        DrawLine(px, w, h, at + new Vector2Int(-8, 0), at + new Vector2Int(8, 0), c, 3);
        DrawLine(px, w, h, at + new Vector2Int(0, -8), at + new Vector2Int(0, 8), c, 3);
        Vector2Int prev = foot + new Vector2Int(r, 0);
        for(int i = 1; i <= 24; i++)
        {
            float a = i * Mathf.PI * 2.0f / 24;
            var p = foot + new Vector2Int(Mathf.RoundToInt(Mathf.Cos(a) * r), Mathf.RoundToInt(Mathf.Sin(a) * r));
            DrawLine(px, w, h, prev, p, c, 2);
            prev = p;
        }
    }
}
