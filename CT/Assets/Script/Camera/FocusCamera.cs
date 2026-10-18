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
public class FocusCamera : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    public Transform player;

    [SerializeField]
    public Tilemap tilemap;

    private Camera cam;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(player == null) { Debug.LogError("追従対象のPlayerが設定されていません"); }

        if(tilemap == null) { Debug.LogError("Tilemapが設定されていません"); }

        cam = GetComponent<Camera>();
        if(cam == null) { Debug.LogError("Cameraコンポーネントが付いていません"); }
    }

    void LateUpdate()
    {
        if(player == null || tilemap == null || cam == null) { return; }

        // カメラの位置はプレイヤーの位置
        float cameraX = player.position.x; 
        float cameraY = player.position.y;

        // Tilemapが使用しているセルの範囲
        BoundsInt cellBounds = tilemap.cellBounds;

        // セルの範囲をワールド座標へ変換
        Vector3 minWorld = tilemap.CellToWorld(cellBounds.min); 
        Vector3 maxWorld = tilemap.CellToWorld(cellBounds.max);

        // カメラのサイズ
        float cameraHeight = cam.orthographicSize; 
        float cameraWidth = cameraHeight * cam.aspect;

        // カメラの移動範囲
        float minX = minWorld.x + cameraWidth; 
        float maxX = maxWorld.x - cameraWidth; 
        float minY = minWorld.y + cameraHeight; 
        float maxY = maxWorld.y - cameraHeight;

        // マップがカメラより小さい場合は、マップの中央に配置する
        if (minX > maxX)
        {
            // 半分の幅を計算して中央に配置
            cameraX = (minWorld.x + maxWorld.x) / 2f;
        }
        else
        {
            cameraX = Mathf.Clamp(cameraX, minX, maxX);
        }
        if(minY > maxY)
        {
            // 半分の高さを計算して中央に配置
            cameraY = (minWorld.y + maxWorld.y) / 2f;
        }
        else
        {
            cameraY = Mathf.Clamp(cameraY, minY, maxY);
        }

        transform.position = new Vector3(cameraX, cameraY, transform.position.z);
    }

    public void SetTarget(Transform newTarget)
    {
        if(newTarget == null) { Debug.LogError("追従対象がありません"); return; }
        player = newTarget;
    }
}
