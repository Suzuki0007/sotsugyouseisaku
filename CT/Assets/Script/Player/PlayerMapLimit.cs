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

    [SerializeField]
    private Tilemap tilemap;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if(movement == null) { Debug.LogError($"{name}: PlayerMovementがアタッチされていません。", this); }

        if(tilemap == null) { Debug.LogError($"{name}: Tilemapがアタッチされていません。", this); }
    }

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

    /// <summary>
    /// タイルマップを外部から設定する関数
    /// </summary>
    /// <param name="newTilemap"></param>
    public void SetTilemap(Tilemap newTilemap)
    {
        tilemap = newTilemap;
    }
}
