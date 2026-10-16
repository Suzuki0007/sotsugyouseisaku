/*********************************************************************/
// * \file   AttackController.cs
// * \brief  攻撃コントローラークラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 攻撃コントローラークラス
/// </summary>
public class AttackController : CharaBase
{
    [SerializeField]
    private GameObject Attacker;

    [SerializeField]
    private AreaAttack areaAttack;

    void Awake()
    {
        if(Attacker == null) { Debug.LogError("Attackerが設定されていません"); }
        if(areaAttack == null) { Debug.LogError("AreaAttackが設定されていません"); }
    }

    private void Update()
    {
        // スペースキーが押されたら攻撃を開始する
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //Debug.Log("攻撃が開始されました");
            areaAttack.StartAttack();
        }
    }
}
