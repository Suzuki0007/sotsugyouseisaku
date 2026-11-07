/*********************************************************************/
// * \file   SpriteFrameTiming.cs
// * \brief  スプライト連番の「今何コマ目か」を計算する共通処理
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スプライト連番 (パラパラ漫画) のコマ計算
/// エフェクト (EffectInstance / EffectDataEditor) とアニメーション (SpriteAnimator) で共通で使う
/// </summary>
public static class SpriteFrameTiming
{
    /// <summary>
    /// i 番目のコマの長さの倍率 (未設定・0以下なら 1)
    /// </summary>
    public static float LengthScale(IReadOnlyList<float> lengths, int index)
    {
        if(lengths == null || index < 0 || index >= lengths.Count) { return 1.0f; }
        float v = lengths[index];
        return v > 0.0f ? v : 1.0f;
    }

    /// <summary>
    /// 1周の秒数
    /// </summary>
    public static float CycleTime(int frameCount, float fps, IReadOnlyList<float> lengths = null)
    {
        if(frameCount <= 0) { return 0.0f; }
        float f = Mathf.Max(0.1f, fps);
        if(lengths == null || lengths.Count == 0) { return frameCount / f; }

        float total = 0.0f;
        for(int i = 0; i < frameCount; i++) { total += LengthScale(lengths, i); }
        return total / f;
    }

    /// <summary>
    /// time 秒の時点で表示するコマ番号 (0始まり) を返す
    /// loop = false で最後まで再生し終わっていたら finished = true にして、最後のコマを返す
    /// コマが無ければ -1
    /// </summary>
    public static int FrameAt(float time, int frameCount, float fps, bool loop, out bool finished,
                              IReadOnlyList<float> lengths = null)
    {
        finished = false;
        if(frameCount <= 0) { return -1; }

        float f = Mathf.Max(0.1f, fps);

        // すべて同じ長さ (いちばんよくある形) は掛け算だけで求める
        if(lengths == null || lengths.Count == 0)
        {
            int index = Mathf.FloorToInt(time * f);
            if(index < 0) { return 0; }
            if(loop) { return index % frameCount; }
            if(index >= frameCount) { finished = true; return frameCount - 1; }
            return index;
        }

        float cycle = CycleTime(frameCount, f, lengths);
        if(loop)
        {
            time = cycle > 0.0f ? Mathf.Repeat(time, cycle) : 0.0f;
        }
        else if(time >= cycle)
        {
            finished = true;
            return frameCount - 1;
        }

        float t = Mathf.Max(0.0f, time) * f;// 「1コマ分」を 1 とした単位
        for(int i = 0; i < frameCount; i++)
        {
            float len = LengthScale(lengths, i);
            if(t < len) { return i; }
            t -= len;
        }
        return frameCount - 1;
    }
}
