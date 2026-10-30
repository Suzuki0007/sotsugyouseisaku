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

    private Transform attackerPos;

    // 現在の攻撃ですでにダメージを与えた対象
    private HashSet<GameObject> hitTargets = new();

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
        if(attackerPos == null) { return; }

        if(isAttacking) { return; }

        attackDamage = (int)(defaultDamage * damageMultiplier);

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
        Collider2D[] targets = Physics2D.OverlapCircleAll(
            attackerPos.position, 
            attackRadius,
            targetLayer
        );

        //Debug.Log($"攻撃範囲内のCollider数: {targets.Length}");

        // foreachは、C++でいう範囲for文　それぞれの要素に対して処理を行う
        // varは、C++でいうauto
        // inは、C++でいうconst参照
        foreach(var target in targets)
        {
            Health targetHealth = target.GetComponent<Health>();
            if(targetHealth == null)
            { 
                Debug.Log("TargetHealthがnullです"); 
                continue;
            }

            GameObject targetObject = target.gameObject;
            if(targetObject == null)
            { 
                Debug.Log("TargetObjectがnullです"); 
                continue;
            }

            // Addメソッドは、要素がすでに存在する場合はfalseを返す、それを利用してスキップする
            if(!hitTargets.Add(targetObject)) { continue; }

            //Debug.Log($"{targetObject.name} に攻撃がヒットしました");

            targetHealth.Damage(attackDamage);

            Debug.Log(" 残り体力: " + targetHealth.CurrentHealth);
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
