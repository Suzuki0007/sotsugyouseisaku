/*********************************************************************/
// * \file   BgmProgressEventAction.cs
// * \brief  イベントでBGMの進行度を変えるアクションのクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント (EventZone → EventSequence) で BgmProgress を変えるアクション
/// </summary>
public class BgmProgressEventAction : EventAction
{
    public enum Mode
    {
        SetAtLeast,// 今より大きいときだけ設定 (後退しない)
        Set,       // そのまま設定
        Add        // 加算
    }

    [SerializeField]
    private Mode mode = Mode.SetAtLeast;

    [SerializeField, Tooltip("Set / SetAtLeast では進行度、Add では加算量")]
    [Range(-1.0f, 1.0f)]
    private float value = 0.25f;

    public override void Execute(GameObject gameObject)
    {
        switch(mode)
        {
            case Mode.SetAtLeast: BgmProgress.SetAtLeast(value); break;
            case Mode.Set:        BgmProgress.Set(value);        break;
            case Mode.Add:        BgmProgress.Add(value);        break;
        }
    }
}
