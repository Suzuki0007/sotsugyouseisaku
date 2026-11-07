/*********************************************************************/
// * \file   InputBlock.cs
// * \brief  プレイヤーの操作を止めるかどうかの判定をまとめたクラス
// *
// * \author
/*********************************************************************/

/// <summary>
/// プレイヤーの操作（移動・攻撃・調べる・キャラ切り替えなど）を止めるかどうか
/// </summary>
///
/// 会話・シーン遷移・アイテム画面・イベント演出・メニュー画面の間は止める
/// 止める条件を増やすときはここに足す（各所で個別に書かない）
public static class InputBlock
{
    /// <summary>
    /// プレイヤーの操作を止めるべきか
    /// </summary>
    public static bool IsBlocked =>
        DialogueManager.IsBusy ||
        SceneFader.IsBusy ||
        ItemMenuUI.IsBusy ||
        EventSequence.IsBusy ||
        GameMenu.IsBusy;
}
