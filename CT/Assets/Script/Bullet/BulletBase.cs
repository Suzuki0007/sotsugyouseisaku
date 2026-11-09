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

    [Header("エフェクト (任意。右向きの絵なら弾の進行方向に回転して出る)")]

    [SerializeField, Tooltip("狙った相手 (敵の弾ならプレイヤー、プレイヤーの弾なら敵) に当たったとき")]
    private EffectData hitEffect;

    [SerializeField, Tooltip("壁など、相手以外に当たったとき")]
    private EffectData obstacleHitEffect;

    [SerializeField, Tooltip("寿命が切れたとき・弾消しで消されたとき")]
    private EffectData vanishEffect;

    [SerializeField, Tooltip("エフェクトを弾の進行方向に回転させる")]
    private bool rotateEffectToDirection = true;

    private float initialLifeTime;

    protected Rigidbody2D rb;

    protected GameObject shooter;

    protected bool isReturned;

    //=====================================================================
    // プロパティ
    //=====================================================================

    // 攻撃のデータで上書きする速さ・ダメージ（負なら上書きしない。取り出すたびに戻す）
    private float speedOverride = -1.0f;
    private int damageOverride = -1;

    public float BulletSpeed => speedOverride > 0.0f ? speedOverride : bulletSpeed;

    public int BulletDamage => damageOverride >= 0 ? damageOverride : bulletDamage;

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
        speedOverride = -1.0f;
        damageOverride = -1;
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
            Vanish();
        }
    }

    /// <summary>
    /// 消えるエフェクトを出してプールに返却する (寿命切れ・弾消しなど)
    /// </summary>
    public void Vanish()
    {
        if(isReturned) return;

        EffectManager.Play(vanishEffect, transform.position, GetEffectAngle());
        ReturnToPool();
    }

    /// <summary>
    /// 当たったときのエフェクトを出す (返却する前に呼ぶ)
    /// </summary>
    /// <param name="other"> 当たった相手 </param>
    /// <param name="isTarget"> 狙った相手に当たったか </param>
    protected void PlayHitEffect(Collider2D other, bool isTarget)
    {
        EffectData effect = isTarget ? hitEffect : obstacleHitEffect;
        if(effect == null) return;

        // 当たった相手の表面に出す
        Vector3 point = other != null ? (Vector3)other.ClosestPoint(transform.position) : transform.position;
        EffectManager.Play(effect, point, GetEffectAngle());
    }

    /// <summary>
    /// 当たっても無視するトリガーか
    /// </summary>
    /// <param name="other"> 当たった相手 </param>
    /// <returns> イベントの範囲・調べる範囲などのトリガーなら true（弾・弾消しエリアは false） </returns>
    protected static bool IgnoresTrigger(Collider2D other)
    {
        if(other == null || !other.isTrigger) { return false; }

        // 弾どうし・弾消しエリアは今まで通り当たる
        if(other.GetComponent<BulletBase>() != null) { return false; }
        if(other.GetComponent<BulletDeleteArea>() != null) { return false; }

        return true;
    }

    /// <summary>
    /// エフェクトの向き (進行方向の角度)
    /// </summary>
    private float GetEffectAngle()
    {
        if(!rotateEffectToDirection || rb == null) return 0f;

        Vector2 v = rb.linearVelocity;
        if(v.sqrMagnitude <= 0.0001f) return 0f;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 物理演算の速度を設定する
    /// </summary>
    /// <param name="direction"></param>
    public virtual void SetDirection(Vector2 direction)
    {
        if(rb == null) return;

        rb.linearVelocity = direction.normalized * BulletSpeed;
    }

    /// <summary>
    /// 速さとダメージを、この1発だけ変える（SetDirection より前に呼ぶ。速さ 0 以下・ダメージ負なら変えない）
    /// </summary>
    public void Configure(float speed, int damage)
    {
        speedOverride = speed > 0.0f ? speed : -1.0f;
        damageOverride = damage;
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
