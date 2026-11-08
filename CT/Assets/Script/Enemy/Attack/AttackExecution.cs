/*********************************************************************/
// * \file   AttackExecution.cs
// * \brief  敵の攻撃を1回実行する（予告 → 発動 → 後隙）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃を1回実行する
/// </summary>
///
/// ・EnemyBrain の「攻撃する」行動から使う。Run をコルーチンとして回し、途中でやめるときは Cleanup を呼ぶ
/// ・敵が止められている間（会話中・Paused など）は、時間を進めない・当てない
public class AttackExecution
{
    //=====================================================================
    // 変数
    //=====================================================================

    private readonly EnemyBrain brain;
    private readonly Enemy enemy;
    private readonly AttackData data;

    private TelegraphView view;
    private bool charging;

    private Vector2 origin;
    private Vector2 direction;
    private float length;

    // 出し続ける攻撃で、相手ごとに最後に当てた時刻
    private readonly Dictionary<Object, float> lastHitTime = new Dictionary<Object, float>();

    private static readonly List<Collider2D> found = new List<Collider2D>();

    public AttackExecution(EnemyBrain brain, AttackData data)
    {
        this.brain = brain;
        this.data = data;
        enemy = brain.Enemy;
    }

    //=====================================================================
    // 実行
    //=====================================================================

    public IEnumerator Run()
    {
        if(data == null || enemy == null) { yield break; }

        enemy.Stop();

        // ---- 予告 ----
        UpdatePose(true);

        enemy.PlayAction(data.telegraphAnimation);
        SeManager.PlaySe(data.telegraphSe, enemy.transform.position);

        if(data.telegraphTime > 0.0f && data.showTelegraph)
        {
            view = TelegraphView.Show(data, origin, direction, length);
        }

        float elapsed = 0.0f;
        while(elapsed < data.telegraphTime)
        {
            if(brain.CanAct)
            {
                elapsed += Time.deltaTime;
                if(IsTracking) { UpdatePose(false); }
            }
            if(view != null) { view.SetProgress(elapsed / data.telegraphTime); }
            yield return null;
        }

        // ---- 発動 ----
        enemy.PlayAction(data.activeAnimation);
        SeManager.PlaySe(data.activeSe, enemy.transform.position);

        Vector2 center = data.shape.CenterOf(origin, direction, length);
        EffectManager.Play(data.activeEffect, center, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        switch(data.activation)
        {
            case AttackActivation.Instant:
                HideView();
                if(data.motion == AttackMotion.Charge) { yield return Charge(); }
                else                                   { HitTargets(false); }
                break;

            case AttackActivation.Sustained:
                yield return Sustain();
                HideView();
                break;

            case AttackActivation.Bullet:
                HideView();
                yield return FireBullets();
                break;
        }

        // ---- 後隙 ----
        yield return brain.Wait(data.recoveryTime);
    }

    /// <summary>
    /// 途中でやめたときの片づけ（予告を消す・突進をやめる）
    /// </summary>
    public void Cleanup()
    {
        HideView();
        if(charging && enemy != null)
        {
            enemy.ClearOverrideVelocity();
            SetIgnorePlayers(false);
        }
        charging = false;
    }

    private void HideView()
    {
        if(view != null) { view.Hide(); }
        view = null;
    }

    //=====================================================================
    // 位置と向き
    //=====================================================================

    private bool IsTracking => data.origin == AttackOrigin.PlayerTracking || (data.trackDuringTelegraph && data.aim == AttackAim.Player);

    /// <summary>
    /// 起点・向き・長さを決める（first：予告を出す最初）
    /// </summary>
    private void UpdatePose(bool first)
    {
        GameObject target = enemy.Target;
        Vector2 enemyPos = enemy.transform.position;
        Vector2 targetPos = target != null ? (Vector2)target.transform.position : enemyPos + enemy.Facing;

        // 起点
        if(first || data.origin == AttackOrigin.PlayerTracking)
        {
            origin = data.origin == AttackOrigin.Enemy ? enemyPos : targetPos;
        }
        else if(data.origin == AttackOrigin.Enemy)
        {
            origin = enemyPos;
        }

        // 向き
        if(first || (data.trackDuringTelegraph && data.aim == AttackAim.Player))
        {
            direction = ComputeDirection(enemyPos, targetPos);
        }

        float oldLength = length;
        length = ComputeLength(PlacedOrigin, direction);

        if(view == null) { return; }

        // 長方形の長さが壁で変わったときは作り直す（それ以外は位置と向きだけ）
        if(data.shape.type == AttackShapeType.Rectangle && Mathf.Abs(oldLength - length) > 0.05f)
        {
            view.Hide();
            view = TelegraphView.Show(data, PlacedOrigin, direction, length);
        }
        else
        {
            view.SetPose(PlacedOrigin, direction);
        }
    }

    private Vector2 ComputeDirection(Vector2 enemyPos, Vector2 targetPos)
    {
        switch(data.aim)
        {
            case AttackAim.Facing:
                return enemy.Facing;

            case AttackAim.Fixed:
                float rad = data.fixedAngle * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            default:
                Vector2 toTarget = targetPos - enemyPos;
                return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : enemy.Facing;
        }
    }

    private Vector2 PlacedOrigin => origin + direction * data.forwardOffset;

    /// <summary>
    /// 長方形の長さ（壁で止める設定なら、壁までの距離）
    /// </summary>
    private float ComputeLength(Vector2 from, Vector2 dir)
    {
        float max = data.shape.length;
        if(data.shape.type != AttackShapeType.Rectangle || !data.stopAtWall) { return max; }

        float wall = DistanceToWall(from, dir, max);

        // 突進は体の大きさの分、手前で止まる
        if(data.motion == AttackMotion.Charge && wall < max) { wall = Mathf.Max(0.0f, wall - enemy.BodyRadius); }
        return Mathf.Min(max, wall);
    }

    /// <summary>
    /// 壁（動かない当たり判定）までの距離
    /// </summary>
    public static float DistanceToWall(Vector2 from, Vector2 dir, float maxDistance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(from, dir, maxDistance);
        float nearest = maxDistance;
        foreach(RaycastHit2D hit in hits)
        {
            if(hit.collider == null || !NavMesh2D.IsObstacle(hit.collider)) { continue; }
            if(hit.distance < nearest) { nearest = hit.distance; }
        }
        return nearest;
    }

    //=====================================================================
    // 当てる
    //=====================================================================

    /// <summary>
    /// 範囲に入っている相手に当てる
    /// </summary>
    /// <param name="repeating"> 出し続ける攻撃か（当たる間隔を守る） </param>
    private void HitTargets(bool repeating)
    {
        Vector2 placed = PlacedOrigin;
        Vector2 center = data.shape.CenterOf(placed, direction, length);
        float searchRadius = data.shape.OuterRadius(length) + 1.0f;

        found.Clear();
        found.AddRange(Physics2D.OverlapCircleAll(center, searchRadius));

        var hitThisCheck = new HashSet<Object>();
        foreach(Collider2D col in found)
        {
            if(!IsTarget(col)) { continue; }
            if(!data.shape.Overlaps(col, placed, direction, length)) { continue; }

            Object key = DamageUtility.HitKey(col);
            if(key == null) { key = col; }
            if(!hitThisCheck.Add(key)) { continue; }

            if(repeating)
            {
                if(lastHitTime.TryGetValue(key, out float last) && Time.time - last < data.hitInterval) { continue; }
                lastHitTime[key] = Time.time;
            }

            Vector2 point = col.ClosestPoint(center);
            DamageUtility.Apply(col, DamageInfo.Create(data.damage, data.damageKind, enemy.gameObject, point));
        }
    }

    /// <summary>
    /// 当てる相手か（当たる相手・効かないキャラ・自分自身）
    /// </summary>
    private bool IsTarget(Collider2D col)
    {
        if(col == null) { return false; }

        // トリガーは、攻撃を受ける部品（DamageReceiver）が付いているものだけ
        if(col.isTrigger && col.GetComponent<DamageReceiver>() == null) { return false; }

        if(col.CompareTag("Player"))
        {
            if(!data.HitsPlayer) { return false; }
            return !data.Ignores(PlayerCharacters.Of(col.gameObject));
        }

        if(col.CompareTag("Enemies"))
        {
            if(!data.HitsEnemies) { return false; }
            Enemy other = col.GetComponentInParent<Enemy>();
            return other != enemy;
        }

        // タグがなくても、攻撃を受ける部品があればギミックとして当てる（敵に当たる攻撃のとき）
        return data.HitsEnemies && col.GetComponent<DamageReceiver>() != null;
    }

    //=====================================================================
    // 出し続ける（ビーム）
    //=====================================================================

    private IEnumerator Sustain()
    {
        if(view == null && data.showTelegraph)
        {
            view = TelegraphView.Show(data, PlacedOrigin, direction, length);
        }
        if(view != null) { view.SetActiveLook(); }

        float elapsed = 0.0f;
        while(elapsed < data.sustainTime)
        {
            if(brain.CanAct)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // 回る・敵について動く
                if(data.rotateSpeed != 0.0f)
                {
                    direction = Quaternion.Euler(0.0f, 0.0f, data.rotateSpeed * dt) * direction;
                }
                if(data.origin == AttackOrigin.Enemy) { origin = enemy.transform.position; }

                float oldLength = length;
                length = ComputeLength(PlacedOrigin, direction);
                if(view != null)
                {
                    if(data.shape.type == AttackShapeType.Rectangle && Mathf.Abs(oldLength - length) > 0.05f)
                    {
                        // 壁で長さが変わったので作り直す
                        view.Hide();
                        view = TelegraphView.Show(data, PlacedOrigin, direction, length);
                        view.SetActiveLook();
                    }
                    else
                    {
                        view.SetPose(PlacedOrigin, direction);
                    }
                }

                HitTargets(true);
            }
            yield return null;
        }
    }

    //=====================================================================
    // 突進
    //=====================================================================

    private IEnumerator Charge()
    {
        float distance = length;
        float traveled = 0.0f;
        float stuckTime = 0.0f;
        bool hitWall = false;

        var hitOnce = new HashSet<Object>();
        Vector2 last = enemy.transform.position;

        charging = true;
        SetIgnorePlayers(true);
        enemy.SetOverrideVelocity(direction * data.chargeSpeed);

        while(traveled < distance - 0.05f)
        {
            yield return new WaitForFixedUpdate();
            if(!brain.CanAct) { last = enemy.transform.position; continue; }

            Vector2 now = enemy.transform.position;
            float step = Vector2.Distance(now, last);
            last = now;
            traveled += step;

            // ほとんど進めていない = 壁や重いものにぶつかった
            if(step < data.chargeSpeed * Time.fixedDeltaTime * 0.2f) { stuckTime += Time.fixedDeltaTime; }
            else                                                     { stuckTime = 0.0f; }
            if(stuckTime > 0.1f) { hitWall = true; break; }

            // 体のまわり（突進の幅）に当たった相手
            HitAround(now, hitOnce);
        }

        enemy.ClearOverrideVelocity();

        // プレイヤーと重なったまま当たり判定を戻すと、プレイヤーが弾き飛ばされるので、離れるまで待つ（最長 1 秒）
        float waitSeparate = 0.0f;
        while(waitSeparate < 1.0f && OverlapsPlayer())
        {
            waitSeparate += Time.deltaTime;
            yield return null;
        }
        SetIgnorePlayers(false);
        charging = false;

        // 予告の長さ（壁まで）を走りきった場合も、壁が目の前なら「ぶつかった」とみなす
        if(!hitWall && data.stopAtWall && length < data.shape.length - 0.01f) { hitWall = true; }

        if(hitWall && data.chargeStunTime > 0.0f)
        {
            enemy.PlayAction("Stun");
            yield return brain.Wait(data.chargeStunTime);
        }
    }

    /// <summary>
    /// 突進の間はプレイヤーをすり抜ける（ぶつかって止まると「壁にぶつかった」と区別できず、プレイヤーを押してしまうため。ダメージは当たり判定で与える）
    /// </summary>
    private void SetIgnorePlayers(bool ignore)
    {
        if(enemy == null) { return; }

        Collider2D[] mine = enemy.GetComponentsInChildren<Collider2D>();
        foreach(PlayerCharacter c in new[] { PlayerCharacter.Noah, PlayerCharacter.Sigle })
        {
            GameObject player = PlayerCharacters.Get(c);
            if(player == null) { continue; }
            foreach(Collider2D theirs in player.GetComponentsInChildren<Collider2D>())
            {
                foreach(Collider2D col in mine) { if(col != null && theirs != null) { Physics2D.IgnoreCollision(col, theirs, ignore); } }
            }
        }
    }

    private bool OverlapsPlayer()
    {
        Collider2D[] mine = enemy.GetComponentsInChildren<Collider2D>();
        foreach(PlayerCharacter c in new[] { PlayerCharacter.Noah, PlayerCharacter.Sigle })
        {
            GameObject player = PlayerCharacters.Get(c);
            if(player == null) { continue; }
            foreach(Collider2D theirs in player.GetComponentsInChildren<Collider2D>())
            {
                foreach(Collider2D col in mine)
                {
                    if(col != null && theirs != null && Physics2D.Distance(col, theirs).isOverlapped) { return true; }
                }
            }
        }
        return false;
    }

    private void HitAround(Vector2 position, HashSet<Object> hitOnce)
    {
        float radius = Mathf.Max(enemy.BodyRadius, data.shape.width * 0.5f);
        foreach(Collider2D col in Physics2D.OverlapCircleAll(position, radius))
        {
            if(!IsTarget(col)) { continue; }

            Object key = DamageUtility.HitKey(col);
            if(key == null) { key = col; }
            if(!hitOnce.Add(key)) { continue; }

            DamageUtility.Apply(col, DamageInfo.Create(data.damage, data.damageKind, enemy.gameObject, col.ClosestPoint(position)));
        }
    }

    //=====================================================================
    // 弾
    //=====================================================================

    private IEnumerator FireBullets()
    {
        EnemyBulletPool pool = brain.BulletPool;
        if(pool == null)
        {
            Debug.LogWarning($"{enemy.name}: 敵の弾のプール（EnemyBulletPool）がシーンにないので、弾を撃てません", enemy);
            yield break;
        }

        int volleys = Mathf.Max(1, data.bulletVolleys);
        for(int v = 0; v < volleys; v++)
        {
            while(!brain.CanAct) { yield return null; }

            // 向きは撃つたびに決め直す（プレイヤーを狙う場合）
            Vector2 enemyPos = enemy.transform.position;
            GameObject target = enemy.Target;
            Vector2 baseDir = ComputeDirection(enemyPos, target != null ? (Vector2)target.transform.position : enemyPos + enemy.Facing);

            int count = Mathf.Max(1, data.bulletCount);
            for(int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0.0f : Mathf.Lerp(-data.bulletSpread * 0.5f, data.bulletSpread * 0.5f, (float)i / (count - 1));
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, angle) * baseDir;

                EnemyBullet bullet = pool.GetBullet(enemyPos + dir * data.forwardOffset, Quaternion.identity);
                if(bullet == null) { continue; }
                bullet.SetDirection(dir);
                bullet.SetShooter(enemy.gameObject);
            }

            if(v > 0) { SeManager.PlaySe(data.activeSe, enemyPos); }
            if(v < volleys - 1) { yield return brain.Wait(data.bulletVolleyInterval); }
        }
    }
}
