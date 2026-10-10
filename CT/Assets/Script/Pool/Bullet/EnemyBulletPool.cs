/*********************************************************************/
// * \file   EnemyBulletPool.cs
// * \brief  敵の弾のプールクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 敵の弾のプールクラス
/// </summary>
public class EnemyBulletPool : MonoBehaviour
{
    //====================================================================================
    // 変数
    //====================================================================================

    [Header("弾の設定")]
    [SerializeField] 
    private EnemyBullet bulletPrefab;

    [SerializeField] 
    private int initialSize = 300; 
    
    [SerializeField] 
    private int maxSize = 1000; 
    
    private ObjectPool<EnemyBullet> pool;

    //====================================================================================
    // 関数
    //====================================================================================

    void Awake()
    {
        // プールの初期化
        InitializePool();

        // 初期数の弾を生成してプールに戻す
        PreloadBullets();
    }

    /// <summary>
    /// プールの初期化
    /// </summary>
    private void InitializePool()
    {
        // プールの初期化
        pool = new ObjectPool<EnemyBullet>(

            // 弾を生成する関数
            createFunc: () =>
            {
                EnemyBullet bullet = Instantiate(bulletPrefab);
                bullet.InitializePool(this);
                bullet.gameObject.SetActive(false);
                return bullet;
            },

            // 弾を取得する関数
            actionOnGet: bullet =>
            {
                bullet.gameObject.SetActive(true);
            },

            // 弾を返却する関数
            actionOnRelease: bullet =>
            {
                bullet.gameObject.SetActive(false);
            },

            // 弾を破棄する関数
            actionOnDestroy: bullet =>
            {
                Destroy(bullet.gameObject);
            },

            // プールの収集チェックを有効にするかどうか
            collectionCheck: true,

            // プールの初期容量と最大容量を設定
            defaultCapacity: initialSize,

            // プールの最大容量を設定
            maxSize: maxSize);
    }

    /// <summary>
    /// 初期数の弾を生成してプールに戻す関数
    /// </summary>
    private void PreloadBullets()
    {
        EnemyBullet[] bullets = new EnemyBullet[initialSize];

        // 初期数の弾を生成
        for(int i = 0; i < initialSize; i++)
        {
            bullets[i] = pool.Get();
        }

        // 生成した弾をプールに戻す
        for(int i = 0; i < initialSize; i++)
        {
            pool.Release(bullets[i]);
        }
    }

    /// <summary> 
    /// 弾を取得する 
    /// </summary> 
    public EnemyBullet GetBullet(Vector2 position, Quaternion rotation)
    {
        EnemyBullet bullet = pool.Get();

        // 位置と回転を設定して返す
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        return bullet; 
    } 

    /// <summary> 
    /// 弾をプールに返却する
    /// </summary>
    public void ReturnBullet(EnemyBullet bullet)
    { 
        pool.Release(bullet); 
    }
}
