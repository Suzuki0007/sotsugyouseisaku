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

    /// <summary>
    /// 「from から to まで歩いてきた」履歴にする
    /// </summary>
    /// <param name="from"> 歩き始めの位置（追従するキャラがここに出る） </param>
    /// <param name="to"> 今の位置 </param>
    /// <param name="steps"> from から to までの記録の数（追従するキャラの遅れのフレーム数に合わせる） </param>
    ///
    /// 追従するキャラを別の位置に出したいときに使う（ワープせず、そこから歩いて付いてくる）
    public void SeedPath(Vector3 from, Vector3 to, int steps)
    {
        steps = Mathf.Clamp(steps, 1, maxHistoryCount - 1);

        posHistory.Clear();
        for(int i = 0; i < maxHistoryCount - steps; i++) { posHistory.Add(from); }
        for(int i = 1; i <= steps; i++) { posHistory.Add(Vector3.Lerp(from, to, (float)i / steps)); }
    }

    /// <summary>
    /// 「points を順にたどって歩いてきた」履歴にする（曲がり角のある道。最初の点に追従するキャラがいる）
    /// </summary>
    /// <param name="points"> 歩き始め → 今の位置 の順の点（2 つ以上） </param>
    /// <param name="steps"> 道全体の記録の数（追従するキャラの遅れのフレーム数に合わせる） </param>
    public void SeedPolyline(IReadOnlyList<Vector2> points, int steps)
    {
        if(points == null || points.Count < 2) { return; }
        steps = Mathf.Clamp(steps, 1, maxHistoryCount - 1);

        float total = 0.0f;
        for(int i = 1; i < points.Count; i++) { total += Vector2.Distance(points[i - 1], points[i]); }

        float z = transform.position.z;
        posHistory.Clear();
        for(int i = 0; i < maxHistoryCount - steps; i++) { posHistory.Add(new Vector3(points[0].x, points[0].y, z)); }

        for(int s = 1; s <= steps; s++)
        {
            float d = total * s / steps;
            Vector2 p = points[points.Count - 1];
            for(int i = 1; i < points.Count; i++)
            {
                float seg = Vector2.Distance(points[i - 1], points[i]);
                if(d <= seg || i == points.Count - 1)
                {
                    p = Vector2.Lerp(points[i - 1], points[i], seg > 0.0001f ? Mathf.Clamp01(d / seg) : 1.0f);
                    break;
                }
                d -= seg;
            }
            posHistory.Add(new Vector3(p.x, p.y, z));
        }
    }

    public void ResetHistory(Vector3 position)
    {
        //posHistory.Clear();
        //RecordPosition(position);

        posHistory.Clear();
        for(int i = 0; i < maxHistoryCount; i++) { posHistory.Add(position); }
    }
}
