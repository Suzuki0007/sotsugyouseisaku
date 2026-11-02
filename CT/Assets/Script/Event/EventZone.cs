/*********************************************************************/
// * \file   EventZone.cs
// * \brief  イベントゾーンのクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントゾーンのクラス
/// </summary>
public class EventZone : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private EventSequence eventSequence;

    [SerializeField]
    private bool onceOnly = true;

    private bool hasTriggered;

    //=====================================================================
    // トリガー関数
    //=====================================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player")){ return; }

        // 1度だけ発動する場合、すでに発動済みなら何もしない
        if(onceOnly && hasTriggered){ return; }

        hasTriggered = true;
        eventSequence.Play(collision.gameObject);
    }
}
