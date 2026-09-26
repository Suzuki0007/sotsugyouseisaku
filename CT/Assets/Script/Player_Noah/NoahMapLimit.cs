/*********************************************************************/
// * \file   NoahMapLimit.cs
// * \brief  ノアのマップ制限クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ノアのマップ制限クラス
/// </summary>
public class NoahMapLimit : CharaBase
{
    /// <summary>
    /// ノアの移動クラス
    /// </summary>
    NoahMovement movement;

    /// <summary>
    /// マップのタイルマップ
    /// </summary>
    public Tilemap tilemap;

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        movement = GetComponent<NoahMovement>();
    }
    
    /// <summary>
    /// 更新後、遅れて呼ばれる更新関数
    /// </summary>
    private void LateUpdate()
    {
        Vector2 pos = movement.transform.position;

        // Tilemapが使用しているセルの範囲
        BoundsInt cellBounds = tilemap.cellBounds;

        // セルの範囲をワールド座標へ変換
        Vector3 minWorld = tilemap.CellToWorld(cellBounds.min);
        Vector3 maxWorld = tilemap.CellToWorld(cellBounds.max);

        // ノアの移動範囲を制限
        pos.x = Mathf.Clamp(pos.x, minWorld.x, maxWorld.x);
        pos.y = Mathf.Clamp(pos.y, minWorld.y, maxWorld.y);

        // ノアの位置を更新
        movement.transform.position = pos;
    }
}
