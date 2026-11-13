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

    //=====================================================================
    // 追加：カメラの範囲・なめらかな移動・注目（謎解き・2人の仕掛け 段階4）
    //=====================================================================

    // 注目する場所（イベントの「カメラを向ける」）。null なら追従対象
    private Vector2? lookPoint;

    // なめらかに動かしている残り時間
    private float blendRemaining;
    private float blendTime = 0.4f;
    private Vector3 blendVelocity;
    private CameraArea lastArea;
    private bool hasArea;

    /// <summary>
    /// 今の位置から、次の位置へなめらかに動かす（切り替え・部屋の移動・注目）
    /// </summary>
    public void BeginBlend(float seconds = 0.45f)
    {
        blendTime = Mathf.Max(0.05f, seconds);
        blendRemaining = blendTime * 2.0f;
        blendVelocity = Vector3.zero;
    }

    /// <summary>
    /// 決めた場所にカメラを向ける（null で追従対象に戻す）
    /// </summary>
    public void LookAt(Vector2? point, float blendSeconds = 0.5f)
    {
        lookPoint = point;
        if(blendSeconds > 0.0f) { BeginBlend(blendSeconds); }
    }

    /// <summary> 今カメラを向けているか </summary>
    public bool IsLooking => lookPoint.HasValue;

    /// <summary> カメラが目標の位置に着いたか（なめらかな移動が終わったか） </summary>
    public bool Settled => blendRemaining <= 0.0f;

    void LateUpdate()
    {
        if(cam == null) { return; }

        Vector2 focus;
        if(lookPoint.HasValue)  { focus = lookPoint.Value; }
        else if(player != null) { focus = player.position; }
        else                    { return; }

        // カメラの範囲が変わったら（部屋を移った・別のエリアのキャラに切り替えた）、なめらかに動かす
        CameraArea area = CameraArea.Find(focus);
        if(hasArea && area != lastArea && blendRemaining <= 0.0f) { BeginBlend(0.35f); }
        lastArea = area;
        hasArea = true;

        Vector3 desired = Clamped(focus, area);
        desired.z = transform.position.z;

        if(blendRemaining > 0.0f)
        {
            blendRemaining -= Time.unscaledDeltaTime;
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref blendVelocity, blendTime * 0.35f, Mathf.Infinity, Time.unscaledDeltaTime);
            if((next - desired).sqrMagnitude < 0.0004f) { next = desired; blendRemaining = 0.0f; }
            transform.position = next;
        }
        else
        {
            transform.position = desired;
        }
    }

    /// <summary>
    /// 範囲（カメラの範囲 → なければマップ）の中に収めたカメラの位置
    /// </summary>
    private Vector3 Clamped(Vector2 focus, CameraArea area)
    {
        float cameraX = focus.x;
        float cameraY = focus.y;

        Vector3 minWorld;
        Vector3 maxWorld;
        if(area != null)
        {
            minWorld = area.Bounds.min;
            maxWorld = area.Bounds.max;
        }
        else if(MapArea.TryGetViewRect(out Rect view))
        {
            // マップの絵（MapBounds）の範囲。なければ Tilemap の範囲（MapArea が決める。プレイヤー・敵と同じ範囲）
            minWorld = view.min;
            maxWorld = view.max;
        }
        else if(tilemap != null)
        {
            // Tilemapが使用しているセルの範囲をワールド座標へ変換
            BoundsInt cellBounds = tilemap.cellBounds;
            minWorld = tilemap.CellToWorld(cellBounds.min);
            maxWorld = tilemap.CellToWorld(cellBounds.max);
        }
        else
        {
            return new Vector3(cameraX, cameraY, transform.position.z);
        }

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

        return new Vector3(cameraX, cameraY, transform.position.z);
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetTarget(Transform newTarget)
    {
        SetTarget(newTarget, false);
    }

    /// <summary>
    /// 追従対象を変える
    /// </summary>
    /// <param name="smooth"> 今の位置からなめらかに動かす（離れたキャラへの切り替えなど） </param>
    public void SetTarget(Transform newTarget, bool smooth)
    {
        player = newTarget;
        if(player == null) { Debug.LogError("追従対象の設定に失敗しました"); return; }
        if(smooth) { BeginBlend(0.45f); }
    }

    public void SetTilemap(Tilemap newTilemap)
    {
        tilemap = newTilemap;
        if(tilemap == null) { Debug.LogError("Tilemapの設定に失敗しました", this); }
    }
}
