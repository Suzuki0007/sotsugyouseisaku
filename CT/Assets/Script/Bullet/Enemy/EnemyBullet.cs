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
public class EnemyBullet : BulletBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    private EnemyBulletPool bulletPool;

    // 攻撃のデータで決める、この1発の設定（取り出すたびに戻す）
    private bool ignoreNoah;
    private bool ignoreSigle;
    private bool deletable = true;

    /// <summary> ノアの弾消しで消せるか </summary>
    public bool Deletable => deletable;

    //=====================================================================
    // 関数
    //=====================================================================

    protected override void OnEnable()
    {
        base.OnEnable();
        ignoreNoah = false;
        ignoreSigle = false;
        deletable = true;
    }

    /// <summary>
    /// この1発の設定（すり抜けるキャラ・弾消しで消せるか）
    /// </summary>
    public void SetRules(bool passNoah, bool passSigle, bool canDelete)
    {
        ignoreNoah = passNoah;
        ignoreSigle = passSigle;
        deletable = canDelete;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsReturned) return;

        if(collision.gameObject == shooter) return;

        // 弾消しエリアに入った場合は「消えたとき」のエフェクトにする（消せない弾は通り抜ける）
        if(collision.GetComponent<BulletDeleteArea>() != null)
        {
            if(deletable) { Vanish(); }
            return;
        }

        // 味方（ほかの敵）には当たらない
        if(collision.CompareTag("Enemies")) return;

        bool isPlayer = collision.CompareTag("Player");

        // この弾がすり抜けるキャラ
        if(isPlayer)
        {
            PlayerCharacter character = PlayerCharacters.Of(collision.gameObject);
            if((character == PlayerCharacter.Noah && ignoreNoah) || (character == PlayerCharacter.Sigle && ignoreSigle)) return;
        }

        // 攻撃で作動するスイッチなど（攻撃を受ける部品）にも当たる
        bool isTarget = isPlayer || collision.GetComponent<DamageReceiver>() != null;

        // イベントの範囲・調べる範囲・シーン遷移などのトリガーは通り抜ける（弾どうしは今まで通りぶつかる）
        if(!isTarget && IgnoresTrigger(collision)) return;
        if(isTarget)
        {
            DamageUtility.Apply(collision, DamageInfo.Create(BulletDamage, DamageKind.EnemyBullet, shooter, transform.position));
        }

        PlayHitEffect(collision, isTarget);
        ReturnToPool();
    }

    /// <summary>
    /// プールの初期化
    /// </summary>
    /// <param name="pool"></param>
    public void InitializePool(EnemyBulletPool pool)
    {
        bulletPool = pool;
    }

    public override void ReturnToPool()
    {
        if(isReturned) return;
        isReturned = true;

        if(bulletPool != null)
        {
            bulletPool.ReturnBullet(this);
        }
        else
        {
            Debug.LogWarning($"{name}: 弾のプールが設定されていません", this);

            gameObject.SetActive(false);
        }
    }
}
