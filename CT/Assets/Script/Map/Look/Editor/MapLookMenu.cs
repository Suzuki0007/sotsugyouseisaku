/*********************************************************************/
// * \file   MapLookMenu.cs
// * \brief  マップの見た目の道具：明かり（ゆらぐ）・場所の色味を作るメニューと、壁の影（Shadow Caster 2D）を Tilemap から作る処理
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// マップの見た目の道具（Hierarchy 右クリック > マップの見た目、Tilemap の ⋮ > 壁の影を作り直す）
/// </summary>
///
/// ・明かり（ゆらぐ）：Light 2D（点の明かり・影あり）＋明かりのゆらぎ。たいまつ・窓の光・水面のきらめきに
/// ・場所の色味：入ると全体の色・明るさが変わる四角
/// ・壁の影：壁の Tilemap の塗ってあるマスから、明かりをさえぎる形（Shadow Caster 2D）を作る（横に続くマスは 1 つにまとめる）
/// ・足元の歩けない形：選んだ物（歯車・柱・木など）の絵の大きさから、足元の四角（幅 7 割 × 下の端から中心まで）を歩ける範囲から抜く。
///   丸い物でも当たり判定は四角でざっくり（細かい形はキャラが引っかかったり、ぶるぶるしたりするもと）
public static class MapLookMenu
{
    public const string ShadowRootName = "壁の影（Shadow Caster 2D・自動）";

    //=====================================================================
    // メニュー
    //=====================================================================

    [MenuItem("GameObject/マップの見た目/明かり（ゆらぐ）", false, 40)]
    private static void MenuLight(MenuCommand cmd)
    {
        GameObject go = CreateLight("明かり（ゆらぐ）", ScenePivot(), new Color(1.0f, 0.62f, 0.3f), 1.2f, 4.5f, true);
        Place(go, cmd);
    }

    [MenuItem("GameObject/マップの見た目/場所の色味", false, 41)]
    private static void MenuTone(MenuCommand cmd)
    {
        var go = new GameObject("場所の色味");
        go.transform.position = ScenePivot();
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(10.0f, 8.0f);
        go.AddComponent<AreaTone>();
        Place(go, cmd);
    }

    [MenuItem("GameObject/マップの見た目/足元の歩けない形を付ける", false, 42)]
    private static void MenuFootprint(MenuCommand cmd)
    {
        // 複数選んだときは 1 つずつ呼ばれる（cmd.context）。右クリックでなくメニューから呼んだときは選んでいる物すべて
        var targets = new List<GameObject>();
        if(cmd.context is GameObject one) { targets.Add(one); }
        else { targets.AddRange(Selection.gameObjects); }

        foreach(GameObject go in targets)
        {
            GameObject box = AddFootprint(go, true);
            if(box != null) { Debug.Log($"{go.name}：足元の歩けない形を作りました（{box.transform.parent.name} の子）。大きさは BoxCollider2D で直せます", box); }
        }
    }

    [MenuItem("CONTEXT/Tilemap/壁の影（Shadow Caster 2D）を作り直す")]
    private static void ContextShadows(MenuCommand cmd)
    {
        var tm = (Tilemap)cmd.context;
        int n = BuildShadowCasters(tm, true);
        Debug.Log($"{tm.name}：壁の影を {n} 個作りました（明かりの「影」をオンにすると見えます）", tm);
    }

    private static Vector3 ScenePivot()
    {
        Vector3 p = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        p.z = 0.0f;
        return p;
    }

    private static void Place(GameObject go, MenuCommand cmd)
    {
        GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "作成 " + go.name);
        Selection.activeObject = go;
    }

    //=====================================================================
    // 明かり
    //=====================================================================

    /// <summary>
    /// 点の明かりを作る（影あり・ゆらぎ付きにできる）。レシピからも使う
    /// </summary>
    public static GameObject CreateLight(string name, Vector3 position, Color color, float intensity, float radius, bool flicker)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightInnerRadius = radius * 0.12f;
        l.pointLightOuterRadius = radius;
        l.falloffIntensity = 0.6f;
        l.shadowsEnabled = true;
        l.shadowIntensity = 0.75f;
        l.shadowSoftness = 0.3f;
        if(flicker) { go.AddComponent<LightFlicker>(); }
        return go;
    }

    //=====================================================================
    // 壁の影（Shadow Caster 2D）
    //=====================================================================

    /// <summary>
    /// Tilemap の塗ってあるマスから Shadow Caster 2D を作り直す（子の「壁の影」を消してから）。作った数を返す
    /// </summary>
    public static int BuildShadowCasters(Tilemap tm, bool undo)
    {
        Transform old = tm.transform.Find(ShadowRootName);
        if(old != null)
        {
            if(undo) { Undo.DestroyObjectImmediate(old.gameObject); }
            else     { Object.DestroyImmediate(old.gameObject); }
        }

        var root = new GameObject(ShadowRootName);
        root.transform.SetParent(tm.transform, false);
        if(undo) { Undo.RegisterCreatedObjectUndo(root, "壁の影を作り直す"); }

        tm.CompressBounds();
        BoundsInt b = tm.cellBounds;
        Vector3 cell = tm.layoutGrid != null ? tm.layoutGrid.cellSize : Vector3.one;
        int count = 0;
        for(int y = b.yMin; y < b.yMax; y++)
        {
            int run = int.MinValue;
            for(int x = b.xMin; x <= b.xMax; x++)
            {
                bool filled = x < b.xMax && tm.HasTile(new Vector3Int(x, y, 0));
                if(filled && run == int.MinValue) { run = x; }
                if(!filled && run != int.MinValue)
                {
                    // run 〜 x-1 を 1 つの四角に
                    Vector3 min = tm.CellToWorld(new Vector3Int(run, y, 0));
                    Vector3 max = tm.CellToWorld(new Vector3Int(x, y, 0)) + new Vector3(0.0f, cell.y, 0.0f);
                    AddBox(root.transform, $"影 y{y} x{run}〜{x - 1}", (min + max) * 0.5f, new Vector2(max.x - min.x, max.y - min.y));
                    count++;
                    run = int.MinValue;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 四角の Shadow Caster 2D を作る（絵も当たり判定もない物に付けると 1×1 の四角になるので、それを引き伸ばす）
    /// </summary>
    public static GameObject AddBox(Transform parent, string name, Vector3 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var sc = go.AddComponent<ShadowCaster2D>();   // ここで 1×1 の形ができる（大きさは 1 のまま付ける）
        sc.selfShadows = false;
        sc.castsShadows = true;
        go.transform.localScale = new Vector3(size.x, size.y, 1.0f);
        return go;
    }

    /// <summary>
    /// 円（多角形）の Shadow Caster 2D を物に付ける（歯車・柱など）。半径はワールドの大きさ
    /// </summary>
    public static ShadowCaster2D AddCircle(GameObject target, float radius, int segments)
    {
        var holder = new GameObject("影をさえぎる形（円）");
        holder.transform.SetParent(target.transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        var sc = holder.AddComponent<ShadowCaster2D>();
        sc.selfShadows = false;
        sc.castsShadows = true;

        // 形はローカルの点で決めるので、親の大きさで割っておく
        Vector3 s = holder.transform.lossyScale;
        var path = new List<Vector3>();
        for(int i = 0; i < Mathf.Max(3, segments); i++)
        {
            float a = Mathf.PI * 2.0f * i / Mathf.Max(3, segments);
            path.Add(new Vector3(Mathf.Cos(a) * radius / Mathf.Max(0.0001f, s.x), Mathf.Sin(a) * radius / Mathf.Max(0.0001f, s.y), 0.0f));
        }
        SetShapePath(sc, path.ToArray());
        return sc;
    }

    //=====================================================================
    // 足元の歩けない形（丸い物・大きな物の当たり判定をざっくりと）
    //=====================================================================

    /// <summary> 足元の四角の幅（見た目の幅に対する割合） </summary>
    public const float FootprintWidth = 0.7f;

    /// <summary>
    /// 物の絵の大きさから「足元の四角」を求める（見た目の幅の 7 割 × 絵の下の端から中心まで）
    /// </summary>
    /// ・上の端を中心に合わせるのは、キャラと物の前後が上下の位置（中心）で決まるから。四角より上なら後ろ、下なら前になり、ずれない
    public static Rect FootprintRect(GameObject target)
    {
        Renderer r = target.GetComponentInChildren<SpriteRenderer>();
        if(r == null) { r = target.GetComponentInChildren<Renderer>(); }
        if(r == null) { return new Rect(target.transform.position, Vector2.zero); }
        Bounds b = r.bounds;
        float w = b.size.x * FootprintWidth;
        return new Rect(b.center.x - w * 0.5f, b.min.y, w, b.center.y - b.min.y);
    }

    /// <summary>
    /// 物の足元に歩けない四角（BoxCollider2D＋WalkAreaExclude）を作り、物のある歩ける範囲の子にする。作った物を返す
    /// </summary>
    public static GameObject AddFootprint(GameObject target, bool undo)
    {
        Rect rect = FootprintRect(target);
        if(rect.width <= 0.0f || rect.height <= 0.0f)
        {
            Debug.LogWarning($"{target.name}：絵（SpriteRenderer）がないので、足元の形を作れません", target);
            return null;
        }
        WalkArea area = FindWalkArea(rect.center);
        if(area == null)
        {
            Debug.LogWarning($"{target.name}：シーンに歩ける範囲（WalkArea）がないので、足元の形を作れません", target);
            return null;
        }
        return AddFootprintRect(area, "歩けない 足元：" + target.name, rect, undo);
    }

    /// <summary>
    /// 歩ける範囲の子に、歩けない四角を作る（いくつかの物をまとめて 1 つの四角にするときにも使う）
    /// </summary>
    public static GameObject AddFootprintRect(WalkArea area, string name, Rect rect, bool undo)
    {
        Transform old = area.transform.Find(name);
        if(old != null)
        {
            if(undo) { Undo.DestroyObjectImmediate(old.gameObject); }
            else     { Object.DestroyImmediate(old.gameObject); }
        }

        var go = new GameObject(name);
        go.transform.SetParent(area.transform, false);
        go.transform.position = new Vector3(rect.center.x, rect.center.y, 0.0f);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        Vector3 s = go.transform.lossyScale;
        box.size = new Vector2(rect.width / Mathf.Max(0.0001f, s.x), rect.height / Mathf.Max(0.0001f, s.y));
        go.AddComponent<WalkAreaExclude>();
        if(undo) { Undo.RegisterCreatedObjectUndo(go, "足元の歩けない形"); }
        return go;
    }

    /// <summary> その点を含む歩ける範囲（なければいちばん近い物） </summary>
    private static WalkArea FindWalkArea(Vector2 point)
    {
        WalkArea best = null;
        float bestDist = float.MaxValue;
        foreach(WalkArea a in Object.FindObjectsByType<WalkArea>())
        {
            if(a.ContainsPoint(point)) { return a; }
            float d = Vector2.Distance(point, a.transform.position);
            if(d < bestDist) { bestDist = d; best = a; }
        }
        return best;
    }

    private static void SetShapePath(ShadowCaster2D sc, Vector3[] path)
    {
        var so = new SerializedObject(sc);
        SerializedProperty p = so.FindProperty("m_ShapePath");
        p.arraySize = path.Length;
        for(int i = 0; i < path.Length; i++) { p.GetArrayElementAtIndex(i).vector3Value = path[i]; }
        // 形が変わったことを知らせる（前と違う値なら作り直される）
        SerializedProperty hash = so.FindProperty("m_ShapePathHash");
        if(hash != null) { hash.intValue = hash.intValue + 7919 + path.Length; }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
