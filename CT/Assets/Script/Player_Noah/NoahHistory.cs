/*********************************************************************/
// * \file   NoahHistory.cs
// * \brief  ノアの履歴クラス
// *
// * \author 成田悠真
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 履歴クラス
/// </summary>
public class NoahHistory : CharaBase
{
    public List<Vector3> posHistory = new List<Vector3>();

    /// <summary>
    /// 最初に一度だけ呼ばれる関数
    /// </summary>
    void Start()
    {
        posHistory.Add(transform.position);
    }

    /// <summary>
    /// 位置を履歴に記録する関数
    /// </summary>
    public void RecordPosition(Vector3 pos)
    {
        // 位置が変化した場合のみ履歴に追加
        if(posHistory.Count == 0 || posHistory[posHistory.Count - 1] != pos)
        {
            posHistory.Add(pos);
        }
    }
}
