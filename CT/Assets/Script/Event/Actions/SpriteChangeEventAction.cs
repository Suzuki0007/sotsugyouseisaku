/*********************************************************************/
// * \file   SpriteChangeEventAction.cs
// * \brief  イベントでスプライトを差し替えるアクション
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントでスプライトを差し替えるアクション
/// （宝箱が開く、像が光る、スイッチが切り替わる、など）
/// EventSequence の actions リストに追加して使う
/// </summary>
///
/// セーブから復元するときも同じ見た目にする（ItemInteraction の完了状態の保存）
public class SpriteChangeEventAction : EventAction
{
    [SerializeField, Tooltip("見た目を変える SpriteRenderer")]
    private SpriteRenderer target;

    [SerializeField, Tooltip("差し替える画像")]
    private Sprite sprite;

    [SerializeField, Tooltip("エリアから出たときに元の画像に戻す")]
    private bool revertOnExit = false;

    // 差し替える前の画像
    private Sprite original;

    private bool hasOriginal;

    public override void Execute(GameObject gameObject)
    {
        Change();
    }

    public override void OnExit(GameObject gameObject)
    {
        if(!revertOnExit || target == null || !hasOriginal) { return; }

        target.sprite = original;
    }

    public override void Restore(GameObject gameObject)
    {
        // 「出たら戻す」設定のものは一時的な変化なので、復元しない
        if(revertOnExit) { return; }

        Change();
    }

    private void Change()
    {
        if(target == null) { Debug.LogWarning($"{name}: SpriteRendererが設定されていません", this); return; }

        if(!hasOriginal)
        {
            original = target.sprite;
            hasOriginal = true;
        }

        target.sprite = sprite;
    }
}
