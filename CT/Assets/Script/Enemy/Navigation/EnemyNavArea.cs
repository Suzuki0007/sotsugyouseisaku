/*********************************************************************/
// * \file   EnemyNavArea.cs
// * \brief  敵の通行設定エリア（敵だけが入れない・通りにくい場所）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の通行設定エリア
/// </summary>
///
/// ・Collider2D（Is Trigger をオン）と一緒に付ける。プレイヤーは普通に通れる
/// ・入れない：敵はこの範囲に入る経路を選ばない（入口の外へは追ってこない、など）。形はコライダーのとおり
/// ・通りにくい：敵はできるだけ避けて回り込む（毒の床など）。形はコライダーの外枠の四角で効く
/// 表示を切り替える（SetActive）と、0.5 秒以内に敵の経路に反映される
[RequireComponent(typeof(Collider2D))]
public class EnemyNavArea : MonoBehaviour
{
    public enum AreaMode
    {
        [InspectorName("入れない")]
        Block,

        [InspectorName("通りにくい（できれば避ける）")]
        Avoid,
    }

    [SerializeField, Label("敵にとってこの場所は")]
    [Tooltip("入れない：敵はこの範囲を通らない ／ 通りにくい：遠回りになっても避ける（どうしても必要なら通る）")]
    private AreaMode mode = AreaMode.Block;

    public AreaMode Mode => mode;

    private void Reset()
    {
        // プレイヤーや弾が引っかからないように、トリガーにしておく
        Collider2D col = GetComponent<Collider2D>();
        if(col != null) { col.isTrigger = true; }
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if(col == null) { return; }

        Gizmos.color = mode == AreaMode.Block ? new Color(1.0f, 0.2f, 0.2f, 0.25f) : new Color(1.0f, 0.6f, 0.1f, 0.2f);
        Bounds b = col.bounds;
        Gizmos.DrawCube(b.center, b.size);
    }
}
