/*********************************************************************/
// * \file   AttackHits.cs
// * \brief  攻撃の範囲に入った相手に当てる処理（攻撃の実行・置いた爆弾で共通）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃の範囲に入った相手に当てる
/// </summary>
public static class AttackHits
{
    private static readonly List<Collider2D> found = new List<Collider2D>();

    /// <summary>
    /// 範囲に入っている相手に当てる
    /// </summary>
    /// <param name="data"> 攻撃のデータ（当たる相手・効かないキャラ・ダメージ・種類） </param>
    /// <param name="origin"> 形の起点 </param>
    /// <param name="direction"> 形の向き </param>
    /// <param name="length"> 長方形の長さ（壁で止めたもの） </param>
    /// <param name="attacker"> 攻撃した人（記録用。自分自身には当てない） </param>
    /// <param name="lastHitTime"> 出し続ける攻撃のとき、相手ごとに最後に当てた時刻（null なら間隔を見ない） </param>
    public static void Hit(AttackData data, Vector2 origin, Vector2 direction, float length, GameObject attacker, Dictionary<Object, float> lastHitTime)
    {
        Vector2 center = data.shape.CenterOf(origin, direction, length);
        float searchRadius = data.shape.OuterRadius(length) + 1.0f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, searchRadius);
        DamageUtility.SortByPriority(hits);
        found.Clear();
        found.AddRange(hits);

        var hitThisCheck = new HashSet<Object>();
        foreach(Collider2D col in found)
        {
            if(!IsTarget(data, col, attacker)) { continue; }
            if(!data.shape.Overlaps(col, origin, direction, length)) { continue; }

            Object key = DamageUtility.HitKey(col);
            if(key == null) { key = col; }
            if(!hitThisCheck.Add(key)) { continue; }

            if(lastHitTime != null)
            {
                if(lastHitTime.TryGetValue(key, out float last) && Time.time - last < data.hitInterval) { continue; }
                lastHitTime[key] = Time.time;
            }

            DamageUtility.Apply(col, DamageInfo.Create(data.damage, data.damageKind, attacker, col.ClosestPoint(center)));
        }
    }

    /// <summary>
    /// 円の範囲（突進の体のまわりなど）に当てる。1回の攻撃で同じ相手には1回だけ（hitOnce）
    /// </summary>
    public static void HitCircle(AttackData data, Vector2 center, float radius, GameObject attacker, HashSet<Object> hitOnce)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        DamageUtility.SortByPriority(hits);
        foreach(Collider2D col in hits)
        {
            if(!IsTarget(data, col, attacker)) { continue; }

            Object key = DamageUtility.HitKey(col);
            if(key == null) { key = col; }
            if(!hitOnce.Add(key)) { continue; }

            DamageUtility.Apply(col, DamageInfo.Create(data.damage, data.damageKind, attacker, col.ClosestPoint(center)));
        }
    }

    /// <summary>
    /// 当てる相手か（当たる相手・効かないキャラ・自分自身）
    /// </summary>
    public static bool IsTarget(AttackData data, Collider2D col, GameObject attacker)
    {
        if(col == null) { return false; }

        bool hasReceiver = col.GetComponent<DamageReceiver>() != null;

        // トリガーは、攻撃を受ける部品（DamageReceiver）が付いているものだけ
        if(col.isTrigger && !hasReceiver) { return false; }

        // 自分自身（と自分の部位）には当てない
        if(attacker != null && col.transform.IsChildOf(attacker.transform)) { return false; }

        if(col.CompareTag("Player"))
        {
            if(!data.HitsPlayer) { return false; }
            return !data.Ignores(PlayerCharacters.Of(col.gameObject));
        }

        if(col.CompareTag("Enemies"))
        {
            return data.HitsEnemies;
        }

        // タグのない「攻撃を受ける部品」は、ギミック（攻撃で作動するスイッチなど）として、いつでも当てる
        // （反応するかどうかはスイッチ側の条件で決める）
        return hasReceiver;
    }
}
