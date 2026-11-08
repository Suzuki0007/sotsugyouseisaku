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

    [SerializeField, Label("2人の状態")]
    private PartyMode mode = PartyMode.Separated;

    [SerializeField, Label("操作するキャラも変える")]
    [Tooltip("オン：下で選んだキャラに操作を切り替える")]
    private bool changeCharacter = false;

    [SerializeField, Label("操作するキャラ")]
    private PlayerCharacter character = PlayerCharacter.Noah;

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return; }

        manager.SetMode(mode);

        if(changeCharacter)
        {
            GameObject target = PlayerCharacters.Get(character);
            if(target != null && target != manager.CurrentPlayer) { manager.SwitchPlayer(); }
        }
    }
}
