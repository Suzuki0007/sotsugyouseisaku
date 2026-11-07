/*********************************************************************/
// * \file   GameFlag.cs
// * \brief  ゲーム進行フラグ（ScriptableObject）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// ゲーム進行フラグ（ON / OFF）
/// </summary>
///
/// 「NPCと話した」「ボスを倒した」「スイッチを押した」など、アイテム以外の進行状態を表す
/// Projectウィンドウで右クリック → Create > Flag > GameFlag で作成する
/// （どのフォルダに置いてもよい。おすすめは Assets/Data/Flags/）
///
/// 名前を手入力しないで、アセットをドラッグして使うので打ち間違いが起きない
[CreateAssetMenu(menuName = "Flag/GameFlag", fileName = "NewFlag")]
public class GameFlag : ScriptableObject
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("セーブ用のID。空ならアセット名を使う（他のフラグと重ならないようにする）")]
    [SerializeField]
    private string id;

    [Tooltip("メモ（どこで立つフラグか、何に使うかなど。動作には影響しない）")]
    [TextArea(2, 4)]
    [SerializeField]
    private string memo;

    [Tooltip("セーブする。オフにすると、ゲームを起動し直すと OFF に戻る（ボス戦中だけ BGM を変える、など一時的なもの）")]
    [SerializeField]
    private bool saved = true;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public string Id => string.IsNullOrEmpty(id) ? name : id;

    public string Memo => memo;

    public bool Saved => saved;

    /// <summary>
    /// 現在の値（スクリプトからは flag.Value = true のように使える）
    /// </summary>
    public bool Value
    {
        get { return GameFlags.Get(this); }
        set { GameFlags.Set(this, value); }
    }
}
