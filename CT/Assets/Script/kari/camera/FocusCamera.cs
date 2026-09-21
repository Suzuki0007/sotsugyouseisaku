/*********************************************************************/
// * \file   FocusCamera.cs
// * \brief  追従カメラクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 追従カメラクラス
/// </summary>
public class Focus : MonoBehaviour
{
    public Transform player;
    public Tilemap tilemap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    // この関数は、Update関数の後に呼ばれる
    // LateUpdate is called once per frame, after Update
    void LateUpdate()
    {
        // カメラの位置はプレイヤーの位置
        float cameraX = player.position.x;
        float cameraY = player.position.y;

        // Tilemapが使用しているセルの範囲
        BoundsInt cellBounds = tilemap.cellBounds;

        // セルの範囲をワールド座標へ変換
        Vector3 minWorld = tilemap.CellToWorld(cellBounds.min);
        Vector3 maxWorld = tilemap.CellToWorld(cellBounds.max);

        // カメラのサイズ
        Camera camera = GetComponent<Camera>();
        float cameraHeight = camera.orthographicSize;
        float cameraWidth = cameraHeight * camera.aspect;

        // カメラの移動範囲
        float minX = minWorld.x + cameraWidth;
        float maxX = maxWorld.x - cameraWidth;
        float minY = minWorld.y + cameraHeight;
        float maxY = maxWorld.y - cameraHeight;

        // マップ外に出ないようにする
        cameraX = Mathf.Clamp(cameraX, minX, maxX);
        cameraY = Mathf.Clamp(cameraY, minY, maxY);

        // カメラ位置を決定
        transform.position = new Vector3(
            cameraX,
            cameraY,
            transform.position.z
        );
    }
}
