/*********************************************************************/
// * \file   AreaAttack.cs
// * \brief  範囲攻撃クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 範囲攻撃クラス
/// </summary>
public class AreaAttack : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("攻撃範囲")]
    [SerializeField]
    private float attackRadius = 1.5f;
    
    [SerializeField] 
    private int attackDamage = 1;

    [Tooltip("攻撃判定を有効にするフレーム数")]
    [SerializeField] 
    private int activeFrames = 3;

    // LayerMaskで指定されたレイヤーのオブジェクト
    [SerializeField]
    private LayerMask targetLayer;

    [Header("エフェクト")]

    [Tooltip("攻撃したとき (攻撃する人の位置に出る)")]
    [SerializeField]
    private EffectData attackEffect;

    [Tooltip("攻撃が当たったとき (当たった相手の位置に出る)")]
    [SerializeField]
    private EffectData hitEffect;

    private Transform attackerPos;

    // 現在の攻撃ですでにダメージを与えた対象（体力を減らす相手ごと。本体と部位に同時に当たっても1回だけ）
    private HashSet<Object> hitTargets = new();

    // 今の攻撃の種類・ゲージの判定（敵ごとの効き方・ギミックの反応に使う）
    private DamageKind currentKind = DamageKind.AreaAttack;
    private GaugeJudge currentJudge = GaugeJudge.None;

    private float defaultDamage = 1f;

    private bool isAttacking = false;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        if(targetLayer == 0) { Debug.LogError("ターゲットレイヤーが設定されていません"); }

        defaultDamage = attackDamage;
    }

    //=====================================================================
    // 攻撃関数
    //=====================================================================

    /// <summary>
    /// 攻撃開始関数
    /// </summary>
    public void StartAttack(float damageMultiplier)
    {
        StartAttack(damageMultiplier, DamageKind.AreaAttack, GaugeJudge.None);
    }

    /// <summary>
    /// 攻撃の種類を指定して攻撃を始める（ゲージ攻撃は GaugeAttack と判定の結果を渡す）
    /// </summary>
    public void StartAttack(float damageMultiplier, DamageKind kind, GaugeJudge judge)
    {
        if(attackerPos == null) { return; }

        if(isAttacking) { return; }

        currentKind = kind;
        currentJudge = judge;

        // 体力は整数なので四捨五入する（切り捨てだと 1 × 1.5 も 1 になり、倍率が効かない）
        // 最低でも 1 ダメージは与える
        attackDamage = Mathf.Max(1, Mathf.FloorToInt(defaultDamage * damageMultiplier + 0.5f));

        EffectManager.Play(attackEffect, attackerPos.position);

        // 攻撃コルーチンを開始
        StartCoroutine(AttackCoroutine());
    }

    /// <summary>
    /// 攻撃コルーチン
    /// </summary>
    /// <returns></returns>
    /// 
    /// IEumeratorを返すことで、コルーチンとして実行可能
    /// コルーチンとは、Unityの機能で、
    /// 時間の経過やフレームの進行に応じて処理を分割して実行することができる仕組みです。
    /// 
    /// 具体的には、IEnumeratorを返すメソッドを定義し、その中でyield returnを使って処理を一時停止させることができます。
    public IEnumerator AttackCoroutine()
    {
        isAttacking = true; 
        
        // 新しい攻撃なので、ヒット履歴をリセット
        hitTargets.Clear();
        
        // 指定フレーム数だけ攻撃判定を調べる
        for (int i = 0; i < activeFrames; i++)
        {
            // 攻撃判定をチェック
            CheckAttackTargets();

            // yield returnは、コルーチンの実行を一時停止し、指定された条件が満たされるまで待機するためのキーワードです。
            // 条件は、例えば、次のフレームまで待機する場合はyield return nullを使用します。
            yield return null;
        }
        
        isAttacking = false;
    }

    /// <summary>
    /// 攻撃判定をチェックする関数
    /// </summary>
    private void CheckAttackTargets()
    {
        // 指定した半径内のすべてのコライダーを取得
        // ターゲットレイヤー（敵）に加えて、攻撃を受ける部品（攻撃で作動するスイッチなど）はレイヤーに関係なく当てる
        Collider2D[] targets = Physics2D.OverlapCircleAll(attackerPos.position, attackRadius);

        //Debug.Log($"攻撃範囲内のCollider数: {targets.Length}");

        // foreachは、C++でいう範囲for文　それぞれの要素に対して処理を行う
        // varは、C++でいうauto
        // inは、C++でいうconst参照
        foreach(var target in targets)
        {
            bool inTargetLayer = (targetLayer.value & (1 << target.gameObject.layer)) != 0;
            if(!inTargetLayer && target.GetComponent<DamageReceiver>() == null) { continue; }

            // 攻撃を受ける相手（DamageReceiver の渡す先か、同じオブジェクトの Health）
            Object key = DamageUtility.HitKey(target);
            if(key == null)
            {
                Debug.Log("TargetHealthがnullです");
                continue;
            }

            // Addメソッドは、要素がすでに存在する場合はfalseを返す、それを利用してスキップする
            if(!hitTargets.Add(key)) { continue; }

            //Debug.Log($"{target.name} に攻撃がヒットしました");

            Vector2 hitPoint = target.ClosestPoint(attackerPos.position);
            DamageInfo info = DamageInfo.Create(attackDamage, currentKind, attackerPos.gameObject, hitPoint);
            info.judge = currentJudge;
            DamageUtility.Apply(target, info);

            EffectManager.Play(hitEffect, hitPoint);

            Health targetHealth = DamageUtility.FindHealth(target);
            if(targetHealth != null) { Debug.Log(" 残り体力: " + targetHealth.CurrentHealth); }
        }
    }

    //=====================================================================
    // Gizmos描画関数
    //=====================================================================

    /// <summary>
    /// この関数は、Unityエディタ上でオブジェクトが選択されているときに、Gizmosを描画するために呼び出されます。
    /// </summary>
    /// 
    /// Gizmosは、Unityエディタ上でオブジェクトの位置や範囲などを視覚的に表示するための機能です。
    private void OnDrawGizmosSelected()
    {
        if(attackerPos == null) { return; }

        Gizmos.color = Color.red;

        // DrawWireSphereは、指定した位置と半径でワイヤーフレームの球を描画する関数
        Gizmos.DrawWireSphere(attackerPos.position, attackRadius);
    }

    //=====================================================================
    // 設定関数
    //=====================================================================

    public void SetAttackerPos(Transform newAttackerPos)
    {
        attackerPos = newAttackerPos;
        if(attackerPos == null) { Debug.LogError("攻撃判定追従対象の設定に失敗しました"); }
    }
}
