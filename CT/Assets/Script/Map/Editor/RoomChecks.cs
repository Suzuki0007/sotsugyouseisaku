/*********************************************************************/
// * \file   RoomChecks.cs
// * \brief  部屋（Room）の点検（歩ける範囲・出現位置・カメラの範囲・調べる物・戦闘・オートセーブ）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 部屋の点検（Room の Inspector と、シーンチェックで使う）
/// </summary>
public static class RoomChecks
{
    public enum Level { Ok, Info, Warning, Error }

    public struct Result
    {
        public Level level;
        public string text;
        public Object target;
    }

    // 画面の縦横比（マップは 4:3）
    private const float ViewAspect = 4.0f / 3.0f;

    /// <summary>
    /// 部屋を点検する（OK の項目も返す）
    /// </summary>
    public static List<Result> Check(Room room)
    {
        var results = new List<Result>();
        void Add(Level level, string text, Object target = null) => results.Add(new Result { level = level, text = text, target = target != null ? target : room });

        if(string.IsNullOrEmpty(room.RoomId)) { Add(Level.Info, "部屋の ID が空です（出現位置・戦闘の記録の名前に使うので、入れておくと安全）"); }

        // ---- 歩ける範囲 ----
        var areas = new List<WalkArea>(room.GetComponentsInChildren<WalkArea>(true));
        var testers = new List<Func<Vector2, bool>>();
        var hiddenTesters = new List<Func<Vector2, bool>>();
        bool hasWalk = false;
        Bounds walkBounds = new Bounds();
        foreach(WalkArea area in areas)
        {
            if(!area.gameObject.activeInHierarchy || area.ShapeCount == 0) { continue; }
            testers.Add(area.BuildTester());
            hiddenTesters.Add(area.BuildTester(true));
            foreach(Collider2D c in area.GetComponentsInChildren<Collider2D>())
            {
                if(c.GetComponent<WalkAreaExclude>() != null) { continue; }
                Bounds b = ShapeBounds(c);
                if(!hasWalk) { walkBounds = b; hasWalk = true; } else { walkBounds.Encapsulate(b); }
            }
        }

        bool Inside(Vector2 p)
        {
            foreach(var t in testers) { if(t(p)) { return true; } }
            return false;
        }

        if(!hasWalk)
        {
            Add(Level.Error, "歩ける範囲（WalkArea）がないか、形がありません（部屋の外まで歩けてしまう）。子の「歩ける範囲」に形を足すか、マスク画像から作ってください");
        }
        else
        {
            Add(Level.Ok, "歩ける範囲がある");

            SpriteRenderer image = room.Image;
            if(image != null && image.sprite != null)
            {
                if(Contains(image.bounds, walkBounds, 0.2f)) { Add(Level.Ok, "歩ける範囲が部屋の絵の中に収まっている"); }
                else { Add(Level.Warning, "歩ける範囲が部屋の絵からはみ出しています（絵のない所を歩ける）", areas.Count > 0 ? areas[0] : null); }
            }
        }

        // ---- 出現位置 ----
        PlayerSpawnPoint[] spawns = room.GetComponentsInChildren<PlayerSpawnPoint>(true);
        if(spawns.Length == 0) { Add(Level.Info, "出現位置がありません（この部屋に別のシーンから入らないなら問題なし）"); }
        foreach(PlayerSpawnPoint spawn in spawns)
        {
            if(!hasWalk) { break; }
            if(Inside(spawn.transform.position)) { Add(Level.Ok, $"出現位置「{spawn.name}」が歩ける範囲の中", spawn); }
            else { Add(Level.Error, $"出現位置「{spawn.name}」が歩ける範囲の外です（出た瞬間に押し戻される）", spawn); }
        }

        // ---- カメラの範囲 ----
        CameraArea[] cams = room.GetComponentsInChildren<CameraArea>(true);
        if(cams.Length == 0)
        {
            Add(Level.Warning, "カメラの範囲（CameraArea）がありません（マップ全体の絵で止まる。部屋の外が映ることがある）");
        }
        else
        {
            Bounds camBounds = CamBounds(cams[0]);
            foreach(CameraArea c in cams) { camBounds.Encapsulate(CamBounds(c)); }

            Vector2 view = ViewSize();
            if(view.x > 0.0f && (camBounds.size.x + 0.01f < view.x || camBounds.size.y + 0.01f < view.y))
            {
                Add(Level.Info, $"カメラの範囲（{camBounds.size.x:0.#}×{camBounds.size.y:0.#}）が画面（{view.x:0.#}×{view.y:0.#}）より小さいので、その向きには動かず真ん中に止まります", cams[0]);
            }
            if(hasWalk)
            {
                if(Contains(camBounds, walkBounds, 0.1f)) { Add(Level.Ok, "カメラの範囲が歩ける範囲を覆っている"); }
                else { Add(Level.Warning, "歩ける範囲の一部がカメラの範囲の外です（そこではキャラが画面の外に出る）", cams[0]); }
            }
        }

        // ---- 調べる物・出口 ----
        if(hasWalk)
        {
            var targets = new List<Component>();
            targets.AddRange(room.GetComponentsInChildren<Intaractable>());
            targets.AddRange(room.GetComponentsInChildren<SceneChange>());
            int reachable = 0;
            foreach(Component t in targets)
            {
                Collider2D col = t.GetComponent<Collider2D>();
                if(col == null || !col.isTrigger) { continue; }   // 範囲のない物（イベントから使う会話など）は対象外
                if(Reaches(col.bounds, Inside)) { reachable++; continue; }
                if(Reaches(col.bounds, p => { foreach(var h in hiddenTesters) { if(h(p)) { return true; } } return false; }))
                {
                    Add(Level.Info, $"「{t.name}」は、今は非表示の通路（歩ける範囲の子）の先にあります（イベントで通路を表示するなら問題なし）", t);
                    continue;
                }
                Add(Level.Warning, $"「{t.name}」（{t.GetType().Name}）の範囲が歩ける範囲に届いていません（調べられない・通れない）", t);
            }
            if(reachable > 0) { Add(Level.Ok, $"調べる物・出口 {reachable} 個が歩ける範囲から届く"); }
        }

        // ---- 戦闘 ----
        foreach(BattleArea battle in room.GetComponentsInChildren<BattleArea>(true))
        {
            var so = new UnityEditor.SerializedObject(battle);
            var restart = so.FindProperty("restartPoint").objectReferenceValue as Transform;
            if(restart != null && hasWalk && !Inside(restart.position)) { Add(Level.Error, $"戦闘エリア「{battle.name}」のやり直す位置が歩ける範囲の外です", restart); }

            var onWin = so.FindProperty("onWin").objectReferenceValue as EventSequence;
            if(onWin == null || !EndsWithAutoSave(onWin))
            {
                Add(Level.Info, $"戦闘エリア「{battle.name}」の勝ったときのイベントの最後にオートセーブがありません（勝ったあとに通り道が変わるなら置く）", onWin != null ? (Object)onWin : battle);
            }
            else
            {
                Add(Level.Ok, $"戦闘エリア「{battle.name}」の勝ったときの最後にオートセーブがある");
            }
        }

        // ---- 着いたとき ----
        foreach(SceneStartEvent start in room.GetComponentsInChildren<SceneStartEvent>(true))
        {
            if(!start.gameObject.activeInHierarchy) { continue; }
            var seq = new UnityEditor.SerializedObject(start).FindProperty("sequence").objectReferenceValue as EventSequence;
            if(seq == null) { Add(Level.Warning, $"「{start.name}」の実行するイベントが空です", start); }
        }

        return results;
    }

    //=====================================================================
    // 補助
    //=====================================================================

    // カメラの範囲の四角（エディタでは CameraArea.Bounds がまだ使えないので、BoxCollider2D から）
    private static Bounds CamBounds(CameraArea area)
    {
        BoxCollider2D box = area.GetComponent<BoxCollider2D>();
        return box != null ? ShapeBounds(box) : new Bounds(area.transform.position, Vector3.zero);
    }

    /// <summary> 最後の部品がオートセーブか </summary>
    public static bool EndsWithAutoSave(EventSequence sequence)
    {
        IList<EventAction> actions = sequence.GetActions();
        for(int i = actions.Count - 1; i >= 0; i--)
        {
            if(actions[i] == null) { continue; }
            return actions[i] is AutoSaveEventAction;
        }
        return false;
    }

    /// <summary> 画面（マップの 4:3）に映る大きさ（ワールド）。カメラが見つからなければ 0 </summary>
    public static Vector2 ViewSize()
    {
        FocusCamera focus = Object.FindAnyObjectByType<FocusCamera>();
        Camera cam = focus != null ? focus.GetComponent<Camera>() : Camera.main;
        if(cam == null || !cam.orthographic) { return Vector2.zero; }
        float h = cam.orthographicSize * 2.0f;
        return new Vector2(h * ViewAspect, h);
    }

    // a が b を（tolerance の余裕つきで）覆っているか
    private static bool Contains(Bounds a, Bounds b, float tolerance)
    {
        return b.min.x >= a.min.x - tolerance && b.min.y >= a.min.y - tolerance &&
               b.max.x <= a.max.x + tolerance && b.max.y <= a.max.y + tolerance;
    }

    // 範囲の四角の 5×5 の点のどれかが歩ける範囲の中なら届く
    private static bool Reaches(Bounds b, Func<Vector2, bool> inside)
    {
        for(int i = 0; i <= 4; i++)
        {
            for(int j = 0; j <= 4; j++)
            {
                var p = new Vector2(Mathf.Lerp(b.min.x, b.max.x, i / 4.0f), Mathf.Lerp(b.min.y, b.max.y, j / 4.0f));
                if(inside(p)) { return true; }
            }
        }
        return false;
    }

    // 無効にされたコライダーでも形の四角を出す（再生中・エディタどちらでも）
    private static Bounds ShapeBounds(Collider2D c)
    {
        Transform t = c.transform;
        Vector2 o = c.offset;
        Vector2 half;
        if(c is CircleCollider2D circle) { half = Vector2.one * circle.radius; }
        else if(c is BoxCollider2D box) { half = box.size * 0.5f; }
        else if(c is PolygonCollider2D poly)
        {
            bool any = false;
            Bounds pb = new Bounds();
            for(int i = 0; i < poly.pathCount; i++)
            {
                foreach(Vector2 p in poly.GetPath(i))
                {
                    Vector3 w = t.TransformPoint(p + o);
                    if(!any) { pb = new Bounds(w, Vector3.zero); any = true; } else { pb.Encapsulate(w); }
                }
            }
            return pb;
        }
        else { return c.bounds; }

        Bounds result = new Bounds(t.TransformPoint(o), Vector3.zero);
        result.Encapsulate(t.TransformPoint(o + new Vector2(-half.x, -half.y)));
        result.Encapsulate(t.TransformPoint(o + new Vector2(half.x, half.y)));
        result.Encapsulate(t.TransformPoint(o + new Vector2(-half.x, half.y)));
        result.Encapsulate(t.TransformPoint(o + new Vector2(half.x, -half.y)));
        return result;
    }
}
