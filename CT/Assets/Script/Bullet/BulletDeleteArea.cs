/*********************************************************************/
// * \file   BulletDeleteArea.cs
// * \brief  弾丸を削除するエリア
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 弾を削除するエリア
/// </summary>
public class BulletDeleteArea : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    private Transform defenderPos;

    [Tooltip("削除範囲")]
    [SerializeField]
    private float deleteRadius = 5f;

    //=====================================================================
    // 初期化
    //=====================================================================

    void Update()
    {
        if(defenderPos == null) { return; }

        // 防御者の位置に追従する
        transform.position = defenderPos.position;
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    /// <summary>
    /// 防御者のTransformを設定する
    /// </summary>
    /// <param name="defender"></param>
    public void SetDefenderPos(Transform defender)
    {
        defenderPos = defender;
        if(defenderPos == null) { Debug.LogError("防御者の設定に失敗しました"); }
    }

    //=====================================================================
    // トリガーイベント
    //=====================================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("EnemyBullet")) { return; }

        EnemyBullet bullet = collision.GetComponent<EnemyBullet>();
        if(bullet == null) { Debug.Log("敵の弾クラスがないです"); }

        // 弾をプールに返却する
        bullet.ReturnToPool();
    }

    //=====================================================================
    // デバッグ用
    //=====================================================================

    private void OnDrawGizmosSelected()
    {
        if(defenderPos == null) { return; }

        Gizmos.color = Color.green;

        // DrawWireSphereは、指定した位置と半径でワイヤーフレームの球を描画する関数
        Gizmos.DrawWireSphere(defenderPos.position, deleteRadius);
    }
}
