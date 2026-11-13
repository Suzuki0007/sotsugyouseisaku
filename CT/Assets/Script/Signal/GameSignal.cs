/*********************************************************************/
// * \file   GameSignal.cs
// * \brief  合図（出来事の名前。ScriptableObject）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 合図（「ペグが締まった」「スイッチが押された」など、出来事に付けた名前）
/// </summary>
///
/// ・Project ウィンドウで右クリック → Create > 合図 > 合図 で作る（おすすめの置き場所は Assets/Data/Signals/）
/// ・出す側：イベントの部品「合図を出す」、またはゲームが自動で出す「標準の合図」（Assets/Data/Signals/標準/）
/// ・受ける側：「合図を受けたら」（SignalListener）、相棒の一言のきっかけ「合図」
/// ・フラグと同じく、名前を手で書かずにアセットをドラッグして使うので打ち間違いが起きない
/// ・合図は「その瞬間に起きたこと」を伝えるだけで、セーブはしない（状態を残したいならフラグ）
[CreateAssetMenu(menuName = "合図/合図", fileName = "Signal_New")]
public class GameSignal : ScriptableObject
{
    [Tooltip("合図の ID。空ならアセット名を使う。標準の合図は決まった ID（Timpani.PegTightened など）なので変えない")]
    [SerializeField]
    private string id;

    [Tooltip("メモ（いつ出る合図か、何に使うかなど。動作には影響しない）")]
    [TextArea(2, 4)]
    [SerializeField]
    private string memo;

    /// <summary> 合図の ID（空ならアセット名） </summary>
    public string Id => string.IsNullOrEmpty(id) ? name : id;

    public string Memo => memo;

    /// <summary> エディタ用：ID とメモを入れる（標準の合図を作るとき） </summary>
    public void Setup(string newId, string newMemo) { id = newId; memo = newMemo; }

    /// <summary> この合図を出す（スクリプトから） </summary>
    public void Send(GameObject source = null) { Signals.Send(this, SignalContext.From(source)); }
}
