/*********************************************************************/
// * \file   EnemyBlock.cs
// * \brief  敵（攻撃の発生源を含む）を止めるかどうかの判定をまとめたクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 敵を止めるかどうか
/// </summary>
///
/// ・会話・暗転・メニュー・アイテム画面・プレイヤーを止めるイベントの間（InputBlock）は、敵も止める
///   （会話中に撃たれてやられる、をなくすため。メニューとアイテム画面は timeScale = 0 でもともと止まる）
/// ・デバッグウィンドウの「敵を止める」
/// 止める条件を増やすときはここに足す（敵の各部品で個別に書かない）。1体だけ止めるのは Enemy.Paused
public static class EnemyBlock
{
    /// <summary> すべての敵を止める（デバッグ用。再生を止めると戻る） </summary>
    public static bool StopAll { get; set; }

    /// <summary> 敵を止めるべきか </summary>
    public static bool IsBlocked => InputBlock.IsBlocked || StopAll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        StopAll = false;
    }
}
