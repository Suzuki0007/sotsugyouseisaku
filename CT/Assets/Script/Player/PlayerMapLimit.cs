/*********************************************************************/
// * \file   PlayerMapLimit.cs
// * \brief  プレイヤーのマップ制限クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// プレイヤーのマップ制限クラス
/// </summary>
public class PlayerMapLimit : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private PlayerMovement movement;

    private Tilemap tilemap;

    private MapBounds mapBounds;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if(movement == null) { Debug.LogError($"{name}: PlayerMovementが見つかりません。", this); }

        // マップ範囲がnullな場合は、FindAnyObjectByTypeで探す
        // FindAnyObjectByTypeは、シーン内の指定された型のオブジェクトを1つだけ返す
        if(mapBounds == null) { mapBounds = FindAnyObjectByType<MapBounds>(); }
        if(mapBounds == null) { Debug.LogError($"{name}: MapBoundsが見つかりません。", this); }
    }

    private void LateUpdate()
    {
        //if(movement == null || tilemap == null) { return; }

        //Vector2 pos = movement.transform.position;

        //// Tilemapが使用しているセルの範囲
        //BoundsInt cellBounds = tilemap.cellBounds;

        //// セルの範囲をワールド座標へ変換
        //Vector3 minWorld = tilemap.CellToWorld(cellBounds.min);
        //Vector3 maxWorld = tilemap.CellToWorld(cellBounds.max);

        //// ノアの移動範囲を制限
        //pos.x = Mathf.Clamp(pos.x, minWorld.x, maxWorld.x);
        //pos.y = Mathf.Clamp(pos.y, minWorld.y, maxWorld.y);

        //// ノアの位置を更新
        //movement.transform.position = pos;



        if(movement == null || mapBounds == null) { return; }

        Bounds bounds = mapBounds.WorldBounds;

        Vector3 position = movement.transform.position;

        Collider2D playerCollider = GetComponent<Collider2D>();

        float halfWidth = playerCollider.bounds.extents.x;
        float halfHeight = playerCollider.bounds.extents.y;

        position.x = Mathf.Clamp(
            position.x,
            bounds.min.x + halfWidth,
            bounds.max.x - halfWidth
        );

        position.y = Mathf.Clamp(
            position.y,
            bounds.min.y + halfHeight,
            bounds.max.y - halfHeight
        );

        movement.transform.position = position;
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetTilemap(Tilemap newTilemap)
    {
        if(newTilemap == null) { Debug.LogError($"{name}: Tilemapの設定に失敗しました", this); }
        tilemap = newTilemap;
    }

    public void SetMapBounds(MapBounds newMapBounds)
    {
        if(newMapBounds == null) { Debug.LogError($"{name}: MapBoundsの設定に失敗しました", this); }
        mapBounds = newMapBounds;
    }
}
