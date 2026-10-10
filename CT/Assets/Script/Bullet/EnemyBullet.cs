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

    private float initialLifeTime;

    private bool isReturned;

    private GameObject shooter;

    private EnemyBulletPool bulletPool;

    Rigidbody2D rb;

    //=====================================================================
    // 関数
    //=====================================================================

    void Awake()
    {
        initialLifeTime = bulletLifeTime;

        if(bulletSpeed <= 0) { Debug.LogError("弾の速度が設定されていません"); }

        if(bulletDamage <= 0) { Debug.LogError("弾のダメージが設定されていません"); }

        if(bulletLifeTime <= 0f) { Debug.LogError("弾の寿命が設定されていません"); }

        rb = GetComponent<Rigidbody2D>();
        if(rb == null) { Debug.LogError("Rigidbody2Dがアタッチされていません"); }
    }

    private void Update()
    {
        if(isReturned) { return; }

        // 弾の寿命を更新
        UpdateLifeTime();
    }

    private void OnEnable() 
    { 
        isReturned = false; 
        
        bulletLifeTime = initialLifeTime;
        
        shooter = null;
        
        if(rb != null) 
        { 
            rb.linearVelocity = Vector2.zero; 
            rb.angularVelocity = 0f;
        }
    }

    private void OnDisable() 
    { 
        if(rb != null) 
        { 
            rb.linearVelocity = Vector2.zero; 
            rb.angularVelocity = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(isReturned) { return; }
        if(collision.gameObject == shooter) { return; }

        Debug.Log($"弾 {name} が {collision.name} に衝突。");

        if(collision.CompareTag("Player"))
        {
            Health health = collision.GetComponent<Health>();
            if(health == null) { return;}
            health.Damage(bulletDamage);

            Debug.Log($" {gameObject.name} の残り体力: " + health.CurrentHealth);
        }
        else
        {
            Debug.Log($" {collision.name} に当たった");
        }

        // 弾をプールに返却
        ReturnToPool();
    }

    private void UpdateLifeTime()
    {
        bulletLifeTime -= Time.deltaTime;
        if(bulletLifeTime <= 0f)
        {
            ReturnToPool();
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

    /// <summary>
    /// 使用するプールを登録する
    /// </summary>
    public void InitializePool(EnemyBulletPool pool)
    {
        bulletPool = pool;
    }

    /// <summary>
    /// 弾をプールに返却する
    /// </summary>
    public void ReturnToPool()
    {
        if(isReturned) { return; }

        isReturned = true;

        if(bulletPool != null)
        {
            bulletPool.ReturnBullet(this);
        }
        else
        {
            Debug.Log("弾のプールが設定されていません");

            gameObject.SetActive(false);
        }
    }
}
