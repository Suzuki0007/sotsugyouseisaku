/*********************************************************************/
// * \file   BulletPoolBase.cs
// * \brief  弾のプール基底クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 弾のプール基底クラス
/// </summary>
/// 
/// この書き方は、ジェネリック型を使用して、異なる種類の弾のプールを共通の基底クラスで管理する方法です。
/// where TBullet : Componentは、TBulletがUnityのComponentクラスを継承していることを示していま
/// これにより、TBulletはMonoBehaviourを継承したクラスである必要があります。
public abstract class BulletPoolBase<TBullet> : MonoBehaviour where TBullet : Component
{
    //====================================================================================
    // 変数
    //====================================================================================

    [Header("弾の設定")]

    [SerializeField]
    private TBullet bulletPrefab;

    [SerializeField]
    private int initialSize = 300;

    [SerializeField]
    private int maxSize = 1000;

    private ObjectPool<TBullet> pool;

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

    private void InitializePool()
    {
        pool = new ObjectPool<TBullet>(
            createFunc      : CreateBullet,
            actionOnGet     : OnGetBullet,
            actionOnRelease : OnReleaseBullet,
            actionOnDestroy : OnDestroyBullet,
            collectionCheck : true,
            defaultCapacity : initialSize,
            maxSize         : maxSize
        );
    }

    /// <summary>
    /// 弾を生成したときの共通処理
    /// </summary>
    /// <returns></returns>
    private TBullet CreateBullet()
    {
        TBullet bullet = Instantiate(bulletPrefab, transform);
        bullet.gameObject.SetActive(false);
        OnCreateBullet(bullet);
        return bullet;
    }

    /// <summary>
    /// 弾を取得したときの共通処理
    /// </summary>
    private void OnGetBullet(TBullet bullet)
    {
        bullet.gameObject.SetActive(true);
        OnBulletTaken(bullet);
    }

    /// <summary>
    /// 弾を返却したときの共通処理
    /// </summary>
    private void OnReleaseBullet(TBullet bullet)
    {
        OnBulletReturned(bullet);
        bullet.gameObject.SetActive(false);
    }

    /// <summary>
    /// プール上限を超えた弾を破棄する
    /// </summary>
    private void OnDestroyBullet(TBullet bullet)
    {
        if(bullet != null)
        {
            Destroy(bullet);
        }
    }

    /// <summary>
    /// 弾を事前生成する
    /// </summary>
    private void PreloadBullets()
    {
        TBullet[] bullets = new TBullet[initialSize];

        for(int i = 0; i < initialSize; i++)
        {
            bullets[i] = pool.Get();
        }

        for(int i = 0; i < initialSize; i++)
        {
            pool.Release(bullets[i]);
        }
    }

    /// <summary>
    /// 弾を取得する
    /// </summary>
    public TBullet GetBullet(Vector2 position, Quaternion rotation)
    {
        TBullet bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(position, rotation);
        return bullet;
    }

    /// <summary>
    /// 弾を返却する
    /// </summary>
    public void ReturnBullet(TBullet bullet)
    {
        if(bullet == null) { return; }
        pool.Release(bullet);
    }

    /// <summary>
    /// 弾の生成時に実行する
    /// </summary>
    protected virtual void OnCreateBullet(TBullet bullet) { }

    /// <summary>
    /// 弾の取得時に実行する
    /// </summary>
    protected virtual void OnBulletTaken(TBullet bullet) { }

    /// <summary>
    /// 弾の返却時に実行する
    /// </summary>
    protected virtual void OnBulletReturned(TBullet bullet) { }
}
