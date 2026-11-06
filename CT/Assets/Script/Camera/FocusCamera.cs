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

    [Header("追従対象")]

    [SerializeField]
    public Transform player;

    [Header("マップ範囲(画像)")]

    [SerializeField]
    private Transform upperLeftPosition;

    [SerializeField]
    private Transform upperRightPosition;

    [SerializeField]
    private Transform lowerLeftPosition;

    [SerializeField]
    private Transform lowerRightPosition;

    public Tilemap tilemap;

    private Camera cam;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if(cam == null) { Debug.LogError("Cameraコンポーネントが付いていません"); }
    }

    //=====================================================================
    // 更新関数
    //=====================================================================

    private void LateUpdate()
    {
        if(!IsValid()) { return; }

        float cameraX = player.position.x;
        float cameraY = player.position.y;

        float minX = Mathf.Min(
            upperLeftPosition.position.x,
            upperRightPosition.position.x,
            lowerLeftPosition.position.x,
            lowerRightPosition.position.x
        );

        float maxX = Mathf.Max(
            upperLeftPosition.position.x,
            upperRightPosition.position.x,
            lowerLeftPosition.position.x,
            lowerRightPosition.position.x
        );

        float minY = Mathf.Min(
            upperLeftPosition.position.y,
            upperRightPosition.position.y,
            lowerLeftPosition.position.y,
            lowerRightPosition.position.y
        );

        float maxY = Mathf.Max(
            upperLeftPosition.position.y,
            upperRightPosition.position.y,
            lowerLeftPosition.position.y,
            lowerRightPosition.position.y
        );

        float cameraHeight = cam.orthographicSize;
        float cameraWidth = cameraHeight * cam.aspect;

        float minCameraX = minX + cameraWidth;
        float maxCameraX = maxX - cameraWidth;
        float minCameraY = minY + cameraHeight;
        float maxCameraY = maxY - cameraHeight;

        if(minCameraX > maxCameraX)
        {
            cameraX = (minX + maxX) / 2f;
        }
        else
        {
            cameraX = Mathf.Clamp(cameraX, minCameraX, maxCameraX);
        }

        if(minCameraY > maxCameraY)
        {
            cameraY = (minY + maxY) / 2f;
        }
        else
        {
            cameraY = Mathf.Clamp(cameraY, minCameraY, maxCameraY);
        }

        transform.position = new Vector3(
            cameraX,
            cameraY,
            transform.position.z
        );
    }

    //=====================================================================
    // 判定関数
    //=====================================================================

    /// <summary>
    /// カメラの追従が有効かどうかを判定する関数
    /// </summary>
    /// <returns></returns>
    private bool IsValid()
    {
        if(cam == null) { return false; }
        if(player == null) { return false; }

        if(upperLeftPosition == null ||
           upperRightPosition == null ||
           lowerLeftPosition == null ||
           lowerRightPosition == null)
        {
            return false;
        }

        return true;
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetTarget(Transform newTarget)
    {
        if(newTarget == null)
        {
            Debug.LogError("追従対象の設定に失敗しました。", this);
            return;
        }

        player = newTarget;
    }

    public void SetTilemap(Tilemap newTilemap)
    {
        if(newTilemap == null)
        {
            Debug.LogError("タイルマップの設定に失敗しました。", this);
            return;
        }

        tilemap = newTilemap;
    }
}
