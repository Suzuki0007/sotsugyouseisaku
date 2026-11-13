/*********************************************************************/
// * \file   AutoSaveEventAction.cs
// * \brief  イベントの部品「オートセーブ」（オートセーブ枠に、今の進み具合と再開する位置を書く）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントの部品「オートセーブ」
/// </summary>
///
/// ・オートセーブ枠に、今の進み具合と「再開する位置」を書き込む（手動の枠は変えない）
/// ・**入口に戻れなくなるなど、マップの通り道が変わるイベントの直後には必ず置く**
///   （置かないと、ロードしたときにシーンに着いた位置＝通れなくなった所から始まってしまう）
/// ・話の区切り（仲間になった・ボスを倒した など）にも置くと、戻される距離が短くなる
/// ・会話などの途中ではなく、イベントの最後（プレイヤーが動けるようになる直前）に置く
public class AutoSaveEventAction : EventAction
{
    public enum Where
    {
        [InspectorName("操作しているキャラの今の位置")]
        CurrentPlayer = 0,

        [InspectorName("指定した場所（下の欄）")]
        Custom = 1,
    }

    [SerializeField, Label("再開する位置")]
    [Tooltip("ロードしたときに出てくる位置")]
    private Where where = Where.CurrentPlayer;

    [SerializeField, Label("指定した場所")]
    [Tooltip("「指定した場所」のとき。空のオブジェクトを置いて入れる")]
    private Transform customPoint;

    public override void Execute(GameObject gameObject)
    {
        Vector2 position = transform.position;

        if(where == Where.Custom && customPoint != null)
        {
            position = customPoint.position;
        }
        else
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            if(manager != null && manager.CurrentPlayer != null) { position = manager.CurrentPlayer.transform.position; }
        }

        GameSession.AutoSaveAt(this.gameObject.scene.name, position);
    }
}
