/*********************************************************************/
// * \file   SetActiveEventAction.cs
// * \brief  イベントでオブジェクトを表示・非表示にするアクション
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベントでオブジェクトを表示・非表示にするアクション
/// （扉が消える、隠し通路が出る、など）
/// EventSequence の actions リストに追加して使う
/// </summary>
///
/// セーブから復元するときも同じ状態にする（ItemInteraction の完了状態の保存）
public class SetActiveEventAction : EventAction
{
    [SerializeField, Tooltip("表示するオブジェクト")]
    private List<GameObject> show = new List<GameObject>();

    [SerializeField, Tooltip("非表示にするオブジェクト")]
    private List<GameObject> hide = new List<GameObject>();

    [SerializeField, Tooltip("エリアから出たときに元に戻す")]
    private bool revertOnExit = false;

    public override void Execute(GameObject gameObject)
    {
        Apply(true);
    }

    public override void OnExit(GameObject gameObject)
    {
        if(!revertOnExit) { return; }

        Apply(false);
    }

    public override void Restore(GameObject gameObject)
    {
        // 「出たら戻す」設定のものは一時的な変化なので、復元しない
        if(revertOnExit) { return; }

        Apply(true);
    }

    /// <summary>
    /// 表示・非表示を反映する
    /// </summary>
    /// <param name="forward"> true なら設定どおり、false なら逆にする </param>
    private void Apply(bool forward)
    {
        SetAll(show, forward);
        SetAll(hide, !forward);
    }

    private void SetAll(List<GameObject> targets, bool active)
    {
        foreach(GameObject target in targets)
        {
            if(target == null) { continue; }

            if(target == gameObject && !active)
            {
                Debug.LogWarning($"{name}: 自分自身を非表示にすると、以後のアクションが動かなくなります", this);
            }

            target.SetActive(active);
        }
    }
}
