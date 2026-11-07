/*********************************************************************/
// * \file   ReturnToTitleButton.cs
// * \brief  タイトル画面に戻るボタン用のコンポーネント
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// タイトル画面に戻る（ボタンの OnClick に ReturnToTitle を登録して使う）
/// </summary>
///
/// 確定済みのセーブは保存してから戻る。まだ確定していない進行（最後のセーブ以降）は保存されない
public class ReturnToTitleButton : MonoBehaviour
{
    /// <summary>
    /// タイトル画面に戻る
    /// </summary>
    public void ReturnToTitle()
    {
        GameSession.ReturnToTitle();
    }
}
