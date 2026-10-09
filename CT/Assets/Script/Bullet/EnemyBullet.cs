/*********************************************************************/
// * \file   EnemyBullet.cs
// * \brief  敵の弾丸クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の弾丸クラス
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private float bulletSpeed;

    [SerializeField]
    private int bulletDamage = 0;

    [SerializeField]
    private float bulletLifeTime = 0f;

    private GameObject shooter;

    Rigidbody2D rb;

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        if(bulletSpeed <= 0) { Debug.LogError("弾の速度が設定されていません"); }

        if(bulletDamage <= 0) { Debug.LogError("弾のダメージが設定されていません"); }

        if(bulletLifeTime <= 0f) { Debug.LogError("弾の寿命が設定されていません"); }

        rb = GetComponent<Rigidbody2D>();
        if(rb == null) { Debug.LogError("Rigidbody2Dがアタッチされていません"); }
    }

    private void Update()
    {
        // 弾の寿命を更新
        UpdateLifeTime();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject == shooter) { return; }

        if(collision.CompareTag("Player"))
        {
            Health health = collision.GetComponent<Health>();
            if(health == null) { return;}
            health.Damage(bulletDamage);

            Debug.Log($" {gameObject.name} の残り体力: " + health.CurrentHealth);

            Destroy(gameObject);
        }
        else
        {
            Debug.Log($" {collision.name} に当たった");
            Destroy(gameObject);
        }
    }

    private void UpdateLifeTime()
    {
        bulletLifeTime -= Time.deltaTime;
        if(bulletLifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetDirection(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * bulletSpeed;
    }

    public void SetShooter(GameObject shooter)
    {
        this.shooter = shooter;
    }
}
