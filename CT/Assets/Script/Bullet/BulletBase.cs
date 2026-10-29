/*********************************************************************/
// * \file   BulletBase.cs
// * \brief  弾の基底クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 弾の基底クラス
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BulletBase : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("弾の基本設定")]

    [SerializeField]
    private float bulletSpeed;

    [SerializeField]
    private int bulletDamage = 0;

    [SerializeField]
    private float bulletLifeTime = 0f;

    private float initialLifeTime;

    protected Rigidbody2D rb;

    protected GameObject shooter;

    protected bool isReturned;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public float BulletSpeed => bulletSpeed;

    public int BulletDamage => bulletDamage;

    public GameObject Shooter => shooter;

    protected bool IsReturned => isReturned;

    //=====================================================================
    // 関数
    //=====================================================================

    protected virtual void Awake()
    {
        initialLifeTime = bulletLifeTime;

        if(bulletSpeed    <= 0)  { Debug.LogError("弾の速度が設定されていません"); }
        if(bulletDamage   <= 0)  { Debug.LogError("弾のダメージが設定されていません"); }
        if(bulletLifeTime <= 0f) { Debug.LogError("弾の寿命が設定されていません"); }

        rb = GetComponent<Rigidbody2D>();
        if(rb == null) { Debug.LogError("Rigidbody2Dがアタッチされていません"); }
    }

    protected virtual void OnEnable()
    {
        isReturned = false;
        bulletLifeTime = initialLifeTime;
        shooter = null;
        ResetVelocity();
    }

    protected virtual void OnDisable()
    {
        ResetVelocity();
    }

    protected virtual void Update()
    {
        if(isReturned) return;

        UpdateLifeTime();
    }

    /// <summary>
    /// 弾の寿命を更新する
    /// </summary>
    protected virtual void UpdateLifeTime()
    {
        bulletLifeTime -= Time.deltaTime;

        if(bulletLifeTime <= 0f)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 物理演算の速度を設定する
    /// </summary>
    /// <param name="direction"></param>
    public virtual void SetDirection(Vector2 direction)
    {
        if(rb == null) return;

        rb.linearVelocity = direction.normalized * bulletSpeed;
    }

    /// <summary>
    /// 弾を撃ったオブジェクトを設定する
    /// </summary>
    /// <param name="shooter"></param>
    public virtual void SetShooter(GameObject shooter)
    {
        this.shooter = shooter;
    }

    /// <summary>
    /// 物理演算の速度をリセットする
    /// </summary>
    protected void ResetVelocity()
    {
        if(rb == null) return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    /// <summary>
    /// 弾をプールに返却する純粋仮想関数
    /// </summary>
    public abstract void ReturnToPool();
}
