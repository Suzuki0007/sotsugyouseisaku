/*********************************************************************/
// * \file   PlayerFollower.cs
// * \brief  プレイヤー追従クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// プレイヤー追従クラス
/// </summary>
public class FocusPlayer : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("追従設定")]

    [SerializeField]
    private GameObject targetPlayer;

    [SerializeField]
    private Vector2 focusOffset = new Vector2(0, 0);

    //=====================================================================
    // 関数
    //=====================================================================

    private void Start()
    {
        if(targetPlayer == null)
        {
            Debug.LogError($"{name}: 追従対象のプレイヤーが設定されていません");
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        if(targetPlayer == null) { return; }

        // 追従対象のプレイヤーの位置にオフセットを加えて追従
        Vector3 targetPosition = targetPlayer.transform.position + new Vector3(focusOffset.x, focusOffset.y, 0);
        transform.position = targetPosition;
    }
}
