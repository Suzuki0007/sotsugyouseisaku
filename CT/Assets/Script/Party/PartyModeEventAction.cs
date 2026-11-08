/*********************************************************************/
// * \file   PartyModeEventAction.cs
// * \brief  イベントで2人の状態（一緒・待機・別行動）を変えるアクション
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベント（EventSequence）で2人の状態を変える
/// </summary>
///
/// 例：会話のあとで「別行動」にする（離れ離れになる）／合流の範囲に入ったら「一緒」に戻す
/// ・一緒：相棒がついてくる。切り替えると位置が入れ替わる。体力は共有（シグルの体力から減る）
/// ・待機・別行動：相棒はその場にいる。切り替えても位置はそのまま（カメラが移る）。体力は別々
/// ・今はセーブされない（シーンを読み込むと「一緒」に戻る）。離れ離れのステージの段階で、セーブするようにする
public class PartyModeEventAction : EventAction
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("2人の状態を変える")]
    [Tooltip("オフにすると、状態は変えずに、下の「操作するキャラ」「待て／ついてこいのキー」だけを変える")]
    private bool changeMode = true;

    [SerializeField, Label("2人の状態")]
    private PartyMode mode = PartyMode.Separated;

    [SerializeField, Label("操作するキャラも変える")]
    [Tooltip("オン：下で選んだキャラに操作を切り替える")]
    private bool changeCharacter = false;

    [SerializeField, Label("操作するキャラ")]
    private PlayerCharacter character = PlayerCharacter.Noah;

    [SerializeField, Label("待て／ついてこいのキー")]
    [Tooltip("変えない／使えなくする（追跡中・離れ離れの場面など）／使えるようにする")]
    private WaitKeyChange waitKey = WaitKeyChange.Keep;

    public enum WaitKeyChange
    {
        [InspectorName("変えない")]
        Keep = 0,

        [InspectorName("使えなくする")]
        Lock = 1,

        [InspectorName("使えるようにする")]
        Unlock = 2,
    }

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return; }

        if(changeMode) { manager.SetMode(mode); }
        if(waitKey == WaitKeyChange.Lock)   { PlayerSwitchManager.WaitCommandLocked = true; }
        if(waitKey == WaitKeyChange.Unlock) { PlayerSwitchManager.WaitCommandLocked = false; }

        if(changeCharacter)
        {
            GameObject target = PlayerCharacters.Get(character);
            if(target != null && target != manager.CurrentPlayer) { manager.SwitchPlayer(); }
        }
    }
}
