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
///
/// ・動く前に止める：PlayerMovement が物理で動かす前に ClampToMap で行き先を範囲内に収める（端の外へ出ない）
/// ・念のため LateUpdate でも戻す（ついていく処理・ワープなどで外に出たとき）
/// ・LateUpdate の順番：PlayerFollower（0）→ これ（100）→ PlayerHistory（200）
///   （順番が決まっていないと、履歴が「外の位置」と「戻した位置」を交互に記録し、ついてくる相棒が震える）
[DefaultExecutionOrder(100)]
public class PlayerMapLimit : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private CharacterMovement movement;

    private Tilemap tilemap;

    private MapBounds mapBounds;

    private Collider2D body;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if(movement == null) { Debug.LogError($"{name}: PlayerMovementが見つかりません。", this); }

        // マップ範囲がnullな場合は、FindAnyObjectByTypeで探す
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

        Vector3 position = movement.transform.position;
        Vector2 clamped = ClampToMap(position);
        if(clamped.x == position.x && clamped.y == position.y) { return; }

        position.x = clamped.x;
        position.y = clamped.y;
        movement.transform.position = position;

        // 物理の位置も合わせる（合わせないと、次の物理の計算で外の位置に戻される）
        if(movement.Rb != null) { movement.Rb.position = clamped; }
    }

    //=====================================================================
    // 範囲
    //=====================================================================

    /// <summary>
    /// 位置をマップの範囲内に収める（体の大きさの分だけ内側）
    /// MapBounds がないときはそのまま返す
    /// </summary>
    public Vector2 ClampToMap(Vector2 position)
    {
        if(mapBounds == null) { return position; }

        Bounds bounds = mapBounds.WorldBounds;
        Vector2 half = BodyHalfSize();

        position.x = Mathf.Clamp(position.x, bounds.min.x + half.x, bounds.max.x - half.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y + half.y, bounds.max.y - half.y);
        return position;
    }

    /// <summary>
    /// 体の大きさの半分
    /// 当たり判定が無効のとき（ついてきているシグル）は bounds が 0 になるので、形の大きさから求める
    /// （有効・無効で端の位置が変わると、切り替えたときにずれる）
    /// </summary>
    private Vector2 BodyHalfSize()
    {
        if(body == null) { body = GetComponent<Collider2D>(); }
        if(body == null) { return Vector2.zero; }

        Vector3 scale = body.transform.lossyScale;
        float sx = Mathf.Abs(scale.x);
        float sy = Mathf.Abs(scale.y);

        if(body is CircleCollider2D circle)  { float r = circle.radius * Mathf.Max(sx, sy); return new Vector2(r, r); }
        if(body is BoxCollider2D box)        { return new Vector2(box.size.x * sx, box.size.y * sy) * 0.5f; }
        if(body is CapsuleCollider2D capsule) { return new Vector2(capsule.size.x * sx, capsule.size.y * sy) * 0.5f; }
        return body.bounds.extents;
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public bool SetTilemap(Tilemap newTilemap)
    {
        if(newTilemap == null)
        {
            Debug.LogError($"{name}: Tilemapの設定に失敗しました", this);
            return false;
        }

        tilemap = newTilemap;
        return true;
    }

    public bool SetMapBounds(MapBounds newMapBounds)
    {
        if(newMapBounds == null)
        {
            Debug.LogError($"{name}: MapBoundsの設定に失敗しました", this);
            return false;
        }

        mapBounds = newMapBounds;
        return true;
    }
}
