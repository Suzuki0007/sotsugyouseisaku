/*********************************************************************/
// * \file   UISoundSettings.cs
// * \brief  UI の効果音の設定（ScriptableObject）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// UI の効果音の設定（Assets/Resources/UISounds.asset に1つだけ置く）
/// </summary>
///
/// タイトル・メニュー・セーブ／ロード画面・確認ダイアログ・アイテム画面で共通に使う
/// 音を差し替えるときは、各欄の SeData を入れ替える（SeData の中の音声ファイルを替えてもよい）
[CreateAssetMenu(menuName = "UI/UISoundSettings", fileName = "UISounds")]
public class UISoundSettings : ScriptableObject
{
    public const string ResourcePath = "UISounds";

    [Tooltip("選択を動かしたとき（↑↓←→）")]
    public SeData select;

    [Tooltip("決定したとき")]
    public SeData decide;

    [Tooltip("戻ったとき・「いいえ」・画面を閉じたとき")]
    public SeData cancel;

    [Tooltip("メニュー（Tab）を開いたとき")]
    public SeData menuOpen;

    [Tooltip("メニュー（Tab）を閉じたとき")]
    public SeData menuClose;

    [Tooltip("セーブが終わったとき")]
    public SeData save;

    [Tooltip("選べない・できないとき（データのないスロットを選んだ、など）")]
    public SeData cannot;

    public SeData Get(UISoundType type)
    {
        switch(type)
        {
            case UISoundType.Select:    return select;
            case UISoundType.Decide:    return decide;
            case UISoundType.Cancel:    return cancel;
            case UISoundType.MenuOpen:  return menuOpen;
            case UISoundType.MenuClose: return menuClose;
            case UISoundType.Save:      return save;
            case UISoundType.Cannot:    return cannot;
        }
        return null;
    }
}
