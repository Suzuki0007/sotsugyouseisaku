/*********************************************************************/
// * \file   PlayerHistory.cs
// * \brief  プレイヤーの履歴を管理するクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// プレイヤーの履歴を管理するクラス
/// </summary>
public class PlayerHistory : CharaBase
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("履歴設定")]
    [SerializeField] 
    private int maxHistoryCount = 1000;

    private readonly List<Vector3> posHistory = new List<Vector3>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    public IReadOnlyList<Vector3> PosHistory => posHistory;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Start()
    {
        RecordPosition(transform.position);
    }

    //=====================================================================
    // 更新関数
    //=====================================================================

    private void LateUpdate()
    {
        // 移動後の位置を記録する
        RecordPosition(transform.position);
    }

    //=====================================================================
    // 記録関数
    //=====================================================================

    /// <summary>
    /// 位置を記録する関数
    /// </summary>
    /// <param name="pos"></param>
    public void RecordPosition(Vector3 pos)
    {
        // 同じ位置は重複して記録しない
        if(posHistory.Count > 0 && posHistory[posHistory.Count - 1] == pos) { return; }

        posHistory.Add(pos);

        // 古い履歴を削除
        if(posHistory.Count > maxHistoryCount) { posHistory.RemoveAt(0); }
    }

    //=====================================================================
    // 履歴リセット関数
    //=====================================================================

    public void ResetHistory(Vector3 position)
    {
        //posHistory.Clear();
        //RecordPosition(position);

        posHistory.Clear();
        for(int i = 0; i < maxHistoryCount; i++) { posHistory.Add(position); }
    }
}
