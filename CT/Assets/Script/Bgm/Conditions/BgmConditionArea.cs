/*********************************************************************/
// * \file   BgmConditionArea.cs
// * \brief  プレイヤーがエリア内にいるときのBGM条件
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーがこのオブジェクトのトリガー(Collider2D)内にいるときに満たす条件
/// Collider2D の Is Trigger をオンにして使う
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BgmConditionArea : BgmCondition
{
    [SerializeField, Tooltip("対象のタグ")]
    private string targetTag = "Player";

    [SerializeField, Tooltip("操作中のキャラクターだけを対象にする")]
    private bool currentPlayerOnly = true;

    // エリア内にいる対象
    private readonly HashSet<GameObject> inside = new HashSet<GameObject>();

    protected override bool IsMet()
    {
        inside.RemoveWhere(obj => obj == null || !obj.activeInHierarchy);

        if(!currentPlayerOnly || PlayerSwitchManager.Instance == null)
        {
            return inside.Count > 0;
        }

        GameObject current = PlayerSwitchManager.Instance.CurrentPlayer;
        return current != null ? inside.Contains(current) : inside.Count > 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag(targetTag)) { return; }
        inside.Add(collision.attachedRigidbody != null ? collision.attachedRigidbody.gameObject : collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(!collision.CompareTag(targetTag)) { return; }
        inside.Remove(collision.attachedRigidbody != null ? collision.attachedRigidbody.gameObject : collision.gameObject);
    }

    private void OnDisable()
    {
        inside.Clear();
    }
}
