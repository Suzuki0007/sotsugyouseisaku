/*********************************************************************/
// * \file   SigleMovement.cs
// * \brief  シグルの移動クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 移動クラス
/// </summary>
public class SigleMovement : CharaBase
{
    NoahHistory noahHistory;

    SigleAnimation sigleAnimation;

    //GameObject noah;
    public GameObject noah;

    // 何個前の位置を追いかけるか
    public int followDistance = 10;

    void Start()
    {
        sigleAnimation = GetComponent<SigleAnimation>();
        if(sigleAnimation == null)
        {
            Debug.LogError("SigleAnimationが付いていません");
            return;
        }

        noah = GameObject.Find("Player_Noah");
        if(noah == null)
        {
            Debug.LogError("Noahオブジェクトが見つかりません");
            return;
        }
        transform.position = noah.transform.position;

        noahHistory = noah.GetComponent<NoahHistory>();
        if(noahHistory == null)
        {
            Debug.LogError("NoahにNoahHistoryが付いていません");
            return;
        }
    }

    void LateUpdate()
    {
        if(noahHistory == null) { return; }

        // 追従する位置のインデックスを計算
        int targetIndex = (noahHistory.posHistory.Count - 1) - followDistance;
        if(targetIndex < 0) { return; }

        // 追従する位置
        Vector3 targetPosition = noahHistory.posHistory[targetIndex];
        Vector3 direction = targetPosition - transform.position;

        // 移動方向がある場合、アニメーションを更新
        if(direction != Vector3.zero && sigleAnimation != null)
        {
            sigleAnimation.SetDirection(direction);
        }

        transform.position = targetPosition;
    }
}
