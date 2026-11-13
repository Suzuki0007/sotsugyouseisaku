/*********************************************************************/
// * \file   MapArea.cs
// * \brief  マップの「歩ける範囲」と「映す範囲」の窓口（プレイヤー・カメラ・敵・相棒の道が同じ範囲を使う）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// マップの範囲の窓口
/// </summary>
///
/// ・歩ける範囲：歩ける範囲（WalkArea）→ マップの絵（MapBounds）→ Tilemap（PlayerSceneContext）の順に、あるものを使う
///   （プレイヤーの PlayerMapLimit・敵の移動・敵と相棒の道 NavMesh2D・アイテムや落石の出る場所）
/// ・映す範囲：マップの絵（MapBounds）→ Tilemap の順（カメラの範囲 CameraArea がある所はそちらが先。FocusCamera）
/// ・どのシーンでも同じ決め方なので、カメラ・敵・相棒の道の範囲が食い違わない
public static class MapArea
{
    private static MapBounds cachedBounds;
    private static PlayerSceneContext cachedContext;
    private static int cachedFrame = -1;
    private static Scene cachedScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedBounds = null;
        cachedContext = null;
        cachedFrame = -1;
    }

    //=====================================================================
    // 映す範囲
    //=====================================================================

    /// <summary>
    /// カメラが映してよい範囲（マップの絵 → Tilemap）。どちらもなければ false
    /// </summary>
    public static bool TryGetViewRect(out Rect rect)
    {
        Refresh();
        if(cachedBounds != null && TryGetSpriteRect(cachedBounds, out rect)) { return true; }
        return TryGetTilemapRect(out rect);
    }

    //=====================================================================
    // 歩ける範囲
    //=====================================================================

    /// <summary>
    /// 歩ける範囲を囲む四角（歩ける範囲 → マップの絵 → Tilemap）。どれもなければ false
    /// </summary>
    public static bool TryGetWalkRect(out Rect rect)
    {
        if(WalkArea.TryGetBounds(out Bounds b))
        {
            rect = Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
            return true;
        }
        return TryGetViewRect(out rect);
    }

    /// <summary> 歩ける範囲の形が決められているか（WalkArea がある） </summary>
    public static bool HasWalkShape => WalkArea.Exists;

    /// <summary> 位置が歩ける範囲の中か（範囲が何もなければ true） </summary>
    public static bool Contains(Vector2 position)
    {
        if(WalkArea.Exists) { return WalkArea.Contains(position); }
        return !TryGetViewRect(out Rect rect) || rect.Contains(position);
    }

    /// <summary>
    /// 位置を歩ける範囲の中に収める（四角の中に収めてから、歩ける範囲の形の中へ）
    /// </summary>
    /// <param name="margin"> 四角のふちから内側に空ける量（体の大きさなど） </param>
    public static Vector2 Clamp(Vector2 position, float margin = 0.0f)
    {
        if(TryGetViewRect(out Rect rect))
        {
            position.x = ClampAxis(position.x, rect.xMin + margin, rect.xMax - margin);
            position.y = ClampAxis(position.y, rect.yMin + margin, rect.yMax - margin);
        }
        return WalkArea.Clamp(position);
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static float ClampAxis(float value, float min, float max)
    {
        if(min > max) { return (min + max) * 0.5f; }
        return Mathf.Clamp(value, min, max);
    }

    // シーンが変わったら探し直す（同じフレームの中では探し直さない）
    private static void Refresh()
    {
        Scene scene = SceneManager.GetActiveScene();
        bool sceneChanged = scene != cachedScene;
        if(!sceneChanged && cachedFrame == Time.frameCount) { return; }
        if(!sceneChanged && Application.isPlaying && cachedBounds != null) { return; }

        cachedScene = scene;
        cachedFrame = Time.frameCount;
        cachedBounds = Object.FindAnyObjectByType<MapBounds>();
        cachedContext = Object.FindAnyObjectByType<PlayerSceneContext>();
    }

    private static bool TryGetSpriteRect(MapBounds bounds, out Rect rect)
    {
        rect = default;
        SpriteRenderer sr = bounds.GetComponent<SpriteRenderer>();
        if(sr == null || sr.sprite == null) { return false; }
        Bounds b = sr.bounds;
        rect = Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
        return true;
    }

    private static bool TryGetTilemapRect(out Rect rect)
    {
        rect = default;
        Tilemap tilemap = cachedContext != null ? cachedContext.PlayerLimitTilemap : null;
        if(tilemap == null) { return false; }

        BoundsInt cells = tilemap.cellBounds;
        Vector3 min = tilemap.CellToWorld(cells.min);
        Vector3 max = tilemap.CellToWorld(cells.max);
        rect = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        return true;
    }
}
