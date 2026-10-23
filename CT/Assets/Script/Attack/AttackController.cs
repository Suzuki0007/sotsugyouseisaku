/*********************************************************************/
// * \file   AttackController.cs
// * \brief  攻撃コントローラー基底クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 攻撃コントローラー基底クラス
/// </summary>
public class AttackController : CharaBase
{
    [Header("攻撃開始の入力設定")]

    [SerializeField]
    private Key attackKey;

    void Awake()
    {
        if(attackKey == Key.None) { Debug.LogError("攻撃開始の入力設定が設定されていません"); }
    }

    private void Update()
    {
        // 指定されたキーが押されたら攻撃を開始する
        if(Keyboard.current[attackKey].wasPressedThisFrame)
        {
            StartAttack();
        }
    }

    /// <summary>
    /// 攻撃開始時の処理を行う仮想関数
    /// </summary>
    virtual protected void StartAttack() { }
}
