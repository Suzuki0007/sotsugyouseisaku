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

    [SerializeField]
    private Transform playerPos;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(playerPos == null) { Debug.LogError("プレイヤーの位置が設定されていません"); }
    }

    void Update()
    {
        // プレイヤーの位置に追従する
        transform.position = playerPos.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("EnemyBullet") == true)
        {
            EnemyBullet bullet = collision.GetComponent<EnemyBullet>();
            if(bullet == null) { Debug.Log("敵の弾クラスがないです"); }

            // 弾をプールに返却する
            bullet.ReturnToPool();
        }
    }
}
