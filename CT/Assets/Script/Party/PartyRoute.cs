/*********************************************************************/
// * \file   PartyRoute.cs
// * \brief  相棒が合流するときの道を探す
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 相棒が合流するときの道
/// </summary>
///
/// ・まっすぐ行けるなら、まっすぐ（そのキャラが通り抜けられる物＝狭い通路などは、ふさいでいないとみなす）
/// ・まっすぐ行けなければ、ナビメッシュ（敵の道探しと同じもの）で回り道を探す
/// ・どちらもなければ「来られない」
public static class PartyRoute
{
    /// <summary>
    /// from から to までの道を探す
    /// </summary>
    /// <param name="walker"> 歩くキャラ（そのキャラの当たり判定で調べる） </param>
    /// <param name="route"> 道（from → 曲がり角 → to） </param>
    /// <returns> 道があるか </returns>
    public static bool Find(GameObject walker, Vector2 from, Vector2 to, List<Vector2> route)
    {
        route.Clear();

        if(IsClear(walker, from, to))
        {
            route.Add(from);
            route.Add(to);
            return true;
        }

        NavMesh2D nav = NavMesh2D.Instance;
        var corners = new List<Vector2>();
        if(nav != null && nav.CalculatePath(from, to, EnemySize.Small, corners, out bool complete) && complete)
        {
            route.Add(from);
            route.AddRange(corners);
            route.Add(to);
            return true;
        }
        return false;
    }

    /// <summary>
    /// まっすぐ行けるか（壁・箱などにぶつからないか）
    /// </summary>
    public static bool IsClear(GameObject walker, Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if(distance < 0.01f) { return true; }

        Collider2D body = walker != null ? walker.GetComponent<Collider2D>() : null;
        float radius = body != null ? Mathf.Max(0.05f, Mathf.Min(body.bounds.extents.x, body.bounds.extents.y) * 0.8f) : 0.3f;

        // 歩ける範囲（WalkArea）があるシーンでは、まっすぐの線が範囲の外を通らないか（0.25 マスごとに確かめる）
        if(MapArea.HasWalkShape)
        {
            int steps = Mathf.CeilToInt(distance / 0.25f);
            for(int i = 1; i < steps; i++)
            {
                if(!WalkArea.Contains(Vector2.Lerp(from, to, (float)i / steps))) { return false; }
            }
        }

        foreach(RaycastHit2D hit in Physics2D.CircleCastAll(from, radius, delta / distance, distance))
        {
            Collider2D c = hit.collider;
            if(c == null || c.isTrigger) { continue; }

            // キャラ自身・もう1人のキャラ・敵はふさいでいないとみなす（動くので）
            GameObject root = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
            if(root.CompareTag("Player")) { continue; }
            if(c.GetComponentInParent<Enemy>() != null) { continue; }

            // そのキャラが通り抜けられる物（狭い通路など）
            if(body != null && Physics2D.GetIgnoreCollision(body, c)) { continue; }

            return false;
        }
        return true;
    }
}
