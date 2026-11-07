/*********************************************************************/
// * \file   EnemyDead.cs
// * \brief  敵の死亡クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵の死亡クラス
/// </summary>
public class EnemyDead : CharaBase
{
    //======================================================
    // 変数
    //======================================================

    [Header("エフェクト")]

    [SerializeField, Tooltip("死亡したとき (爆発など)。死亡アニメがある場合は、アニメが終わって消える瞬間に出る")]
    private EffectData deathEffect;

    [Header("アニメーション")]

    [SerializeField, Tooltip("死亡アニメの名前。SpriteAnimator にこの名前の動きがあれば、流し終わってから消える (無ければすぐ消える)")]
    private string deathAnimation = "Dead";

    private Health health;

    private bool dying;

    //======================================================
    // 関数
    //======================================================

    private void Awake()
    {
        health = GetComponent<Health>();
        if(health == null) { Debug.Log("Healthコンポーネントが見つかりません"); }
    }

    void OnEnable()
    {
        health.OnDied += OnEnemyDied;
    }

    void OnDisable()
    {
        health.OnDied -= OnEnemyDied;
    }

    private void OnEnemyDied()
    {
        if(dying) { return; }
        dying = true;

        Debug.Log("敵が死亡しました！");

        // 死亡アニメがあれば、流し終わってから消える
        SpriteAnimator anim = GetComponent<SpriteAnimator>();
        if(anim != null && anim.Play(deathAnimation, true))
        {
            StopActions();
            StartCoroutine(DieAfterAnimation(anim));
            return;
        }

        Vanish();
    }

    /// <summary>
    /// 死亡アニメの間、動き・攻撃・当たり判定を止める
    /// </summary>
    private void StopActions()
    {
        CharacterMovement movement = GetComponent<CharacterMovement>();
        if(movement != null) { movement.Stop(); movement.enabled = false; }

        EnemyShooter shooter = GetComponent<EnemyShooter>();
        if(shooter != null) { shooter.enabled = false; }

        foreach(Collider2D col in GetComponentsInChildren<Collider2D>()) { col.enabled = false; }
    }

    private System.Collections.IEnumerator DieAfterAnimation(SpriteAnimator anim)
    {
        yield return anim.WaitForCycle();
        Vanish();
    }

    private void Vanish()
    {
        // 敵は消えるので、追従させずにその場に出す
        EffectManager.Play(deathEffect, transform.position);
        Destroy(gameObject);
    }
}
