/*********************************************************************/
// * \file   AttackExecution.cs
// * \brief  攻撃を1回実行する（予告 → 発動 → 後隙）
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
/// ・PatternRunner（敵の EnemyBrain・見えない敵の AttackEmitter）から使う。Run をコルーチンとして回し、途中でやめるときは Cleanup を呼ぶ
/// ・止められている間（会話中・命令で停止など）は、時間を進めない・当てない
/// ・敵の体がない（runner.Enemy が null）ときは、突進・アニメはしない
public class AttackExecution
{
    //=====================================================================
    // 変数
    //=====================================================================

    private readonly PatternRunner runner;
    private readonly Enemy enemy;
    private readonly AttackData data;

    private TelegraphView view;
    private bool charging;
    private GameObject spawnedVisual;

    private Vector2 origin;
    private Vector2 direction;
    private float length;

    // 出し続ける攻撃で、相手ごとに最後に当てた時刻
    private readonly Dictionary<Object, float> lastHitTime = new Dictionary<Object, float>();

    // うず巻きで、撃つたびに回す角度
    private float spiralAngle;

    private enum Phase { Telegraph, Active, Recovery }
    private Phase phase = Phase.Telegraph;

    /// <summary>
    /// 今ひるみでやめてよいか（予告中・発動中・後隙と、攻撃のデータの「ひるみでキャンセルできる」で決まる）
    /// </summary>
    public bool CanCancel
    {
        get
        {
            if(data == null || phase == Phase.Recovery) { return true; }
            switch(data.cancel)
            {
                case AttackCancel.Always:          return true;
                case AttackCancel.DuringTelegraph: return phase == Phase.Telegraph;
                default:                           return false;
            }
        }
    }

    public AttackExecution(PatternRunner runner, AttackData data)
    {
        this.runner = runner;
        this.data = data;
        enemy = runner != null ? runner.Enemy : null;
    }

    private GameObject Attacker => runner != null ? runner.Attacker : null;

    //=====================================================================
    // 実行
    //=====================================================================

    public IEnumerator Run()
    {
        if(data == null || runner == null) { yield break; }

        if(enemy != null) { enemy.Stop(); }

        // ---- 予告 ----
        UpdatePose(true);

        runner.PlayAction(data.telegraphAnimation);
        SeManager.PlaySe(data.telegraphSe, runner.Position);

        if(data.telegraphTime > 0.0f && data.showTelegraph && data.activation != AttackActivation.Place)
        {
            view = TelegraphView.Show(data, PlacedOrigin, direction, length);
        }

        float elapsed = 0.0f;
        while(elapsed < data.telegraphTime)
        {
            if(runner.CanAct)
            {
                elapsed += Time.deltaTime;
                if(IsTracking) { UpdatePose(false); }
            }
            if(view != null) { view.SetProgress(elapsed / data.telegraphTime); }

            // 上から落ちる：予告の最後に物が落ちてくる
            if(data.activation == AttackActivation.Drop) { UpdateDrop(data.telegraphTime - elapsed); }

            yield return null;
        }

        // ---- 発動 ----
        phase = Phase.Active;
        runner.PlayAction(data.activeAnimation);
        SeManager.PlaySe(data.activeSe, runner.Position);
        if(data.activeShake > 0.0f) { CameraShake.Shake(data.activeShake, 0.4f); }

        Vector2 center = data.shape.CenterOf(PlacedOrigin, direction, length);

        // 発動したときに出す物（落石と一緒に落ちてくるアイテムなど）。歩ける範囲があるシーンでは、その中のときだけ
        if(data.spawnOnActive != null && data.activation != AttackActivation.Bullet &&
           Random.value < data.spawnChance && WalkArea.Contains(center))
        {
            Object.Instantiate(data.spawnOnActive, new Vector3(center.x, center.y, 0.0f), Quaternion.identity);
        }
        if(data.activation != AttackActivation.Place && data.activation != AttackActivation.Bullet)
        {
            EffectManager.Play(data.activeEffect, center, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        switch(data.activation)
        {
            case AttackActivation.Instant:
                HideView();
                if(data.motion == AttackMotion.Charge && enemy != null) { yield return Charge(); }
                else                                                    { HitNow(); }
                break;

            case AttackActivation.Sustained:
                yield return Sustain();
                HideView();
                break;

            case AttackActivation.Bullet:
                HideView();
                EffectManager.Play(data.activeEffect, runner.Position, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                yield return FireBullets();
                break;

            case AttackActivation.Drop:
                HideView();
                DestroyVisual();
                HitNow();
                break;

            case AttackActivation.Rise:
                HideView();
                yield return Rise();
                break;

            case AttackActivation.Place:
                HideView();
                PlacedBomb.Place(data, PlacedOrigin, direction, length, Attacker);
                break;
        }

        // ---- 後隙 ----
        phase = Phase.Recovery;
        yield return runner.Wait(data.recoveryTime);
    }

    /// <summary>
    /// 途中でやめたときの片づけ（予告・出した物を消す・突進をやめる）
    /// </summary>
    public void Cleanup()
    {
        HideView();
        DestroyVisual();
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

    private void DestroyVisual()
    {
        if(spawnedVisual != null) { Object.Destroy(spawnedVisual); }
        spawnedVisual = null;
    }

    private void HitNow()
    {
        AttackHits.Hit(data, PlacedOrigin, direction, length, Attacker, null);
    }

    //=====================================================================
    // 位置と向き
    //=====================================================================

    private bool IsTracking => data.origin == AttackOrigin.PlayerTracking || (data.trackDuringTelegraph && data.aim == AttackAim.Player);

    private Vector2 PlacedOrigin => origin + direction * data.forwardOffset;

    /// <summary>
    /// 起点・向き・長さを決める（first：予告を出す最初）
    /// </summary>
    private void UpdatePose(bool first)
    {
        GameObject target = runner.Target;
        Vector2 selfPos = runner.Position;
        Vector2 targetPos = target != null ? (Vector2)target.transform.position : selfPos + runner.Facing;

        // 起点
        if(first || data.origin == AttackOrigin.PlayerTracking)
        {
            origin = data.origin == AttackOrigin.Enemy ? selfPos : targetPos;
        }
        else if(data.origin == AttackOrigin.Enemy)
        {
            origin = selfPos;
        }

        // 向き
        if(first || (data.trackDuringTelegraph && data.aim == AttackAim.Player))
        {
            direction = ComputeDirection(selfPos, targetPos);
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

    private Vector2 ComputeDirection(Vector2 selfPos, Vector2 targetPos)
    {
        switch(data.aim)
        {
            case AttackAim.Facing:
                return runner.Facing.sqrMagnitude > 0.0001f ? runner.Facing.normalized : Vector2.right;

            case AttackAim.Fixed:
                float rad = data.fixedAngle * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            default:
                Vector2 toTarget = targetPos - selfPos;
                return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : (runner.Facing.sqrMagnitude > 0.0001f ? runner.Facing.normalized : Vector2.right);
        }
    }

    /// <summary>
    /// 長方形の長さ（壁で止める設定なら、壁までの距離）
    /// </summary>
    private float ComputeLength(Vector2 from, Vector2 dir)
    {
        float max = data.shape.length;
        if(data.shape.type != AttackShapeType.Rectangle || !data.stopAtWall) { return max; }

        float wall = DistanceToWall(from, dir, max);

        // 突進は体の大きさの分、手前で止まる
        if(data.motion == AttackMotion.Charge && enemy != null && wall < max) { wall = Mathf.Max(0.0f, wall - enemy.BodyRadius); }
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
            if(runner.CanAct)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // 回る・敵について動く
                if(data.rotateSpeed != 0.0f)
                {
                    direction = Quaternion.Euler(0.0f, 0.0f, data.rotateSpeed * dt) * direction;
                }
                if(data.origin == AttackOrigin.Enemy) { origin = runner.Position; }

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

                AttackHits.Hit(data, PlacedOrigin, direction, length, Attacker, lastHitTime);
            }
            yield return null;
        }
    }

    //=====================================================================
    // 上から落ちる
    //=====================================================================

    /// <summary>
    /// 予告の最後（残り dropTime 秒）に、上から物を落とす見た目
    /// </summary>
    private void UpdateDrop(float remaining)
    {
        if(data.dropObject == null) { return; }

        float fall = Mathf.Max(0.05f, data.dropTime);
        if(remaining > fall) { return; }

        Vector2 landing = data.shape.CenterOf(PlacedOrigin, direction, length);
        if(spawnedVisual == null)
        {
            spawnedVisual = Object.Instantiate(data.dropObject, landing + Vector2.up * data.dropHeight, Quaternion.identity);
            DisableColliders(spawnedVisual);
        }

        float t = 1.0f - Mathf.Clamp01(remaining / fall);
        spawnedVisual.transform.position = Vector2.Lerp(landing + Vector2.up * data.dropHeight, landing, t * t);
    }

    //=====================================================================
    // 下から突き出る
    //=====================================================================

    private IEnumerator Rise()
    {
        Vector2 center = data.shape.CenterOf(PlacedOrigin, direction, length);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if(data.riseObject != null)
        {
            spawnedVisual = Object.Instantiate(data.riseObject, center, Quaternion.Euler(0.0f, 0.0f, data.shape.type == AttackShapeType.Rectangle ? angle : 0.0f));
        }

        HitNow();

        // 残っている間（当たり判定が付いていれば壁になる。ナビメッシュも自動で作り直される）
        float elapsed = 0.0f;
        while(elapsed < data.riseLinger)
        {
            if(runner.CanAct)
            {
                elapsed += Time.deltaTime;
                if(data.riseHitWhileLingering) { AttackHits.Hit(data, PlacedOrigin, direction, length, Attacker, lastHitTime); }
            }
            yield return null;
        }

        DestroyVisual();
    }

    private static void DisableColliders(GameObject go)
    {
        foreach(Collider2D col in go.GetComponentsInChildren<Collider2D>()) { col.enabled = false; }
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
            if(!runner.CanAct) { last = enemy.transform.position; continue; }

            Vector2 now = enemy.transform.position;
            float step = Vector2.Distance(now, last);
            last = now;
            traveled += step;

            // ほとんど進めていない = 壁や重いものにぶつかった
            if(step < data.chargeSpeed * Time.fixedDeltaTime * 0.2f) { stuckTime += Time.fixedDeltaTime; }
            else                                                     { stuckTime = 0.0f; }
            if(stuckTime > 0.1f) { hitWall = true; break; }

            // 体のまわり（突進の幅）に当たった相手
            AttackHits.HitCircle(data, now, Mathf.Max(enemy.BodyRadius, data.shape.width * 0.5f), Attacker, hitOnce);
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
            yield return runner.Wait(data.chargeStunTime);
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

    //=====================================================================
    // 弾
    //=====================================================================

    private IEnumerator FireBullets()
    {
        EnemyBulletPool pool = runner.BulletPool;
        if(pool == null)
        {
            Debug.LogWarning($"{runner.name}: 敵の弾のプール（EnemyBulletPool）がシーンにないので、弾を撃てません", runner);
            yield break;
        }

        int volleys = Mathf.Max(1, data.bulletVolleys);
        for(int v = 0; v < volleys; v++)
        {
            while(!runner.CanAct) { yield return null; }

            // 向きは撃つたびに決め直す（プレイヤーを狙う場合）
            Vector2 selfPos = runner.Position;
            GameObject target = runner.Target;
            Vector2 baseDir = ComputeDirection(selfPos, target != null ? (Vector2)target.transform.position : selfPos + runner.Facing);

            int count = Mathf.Max(1, data.bulletCount);
            for(int i = 0; i < count; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, BulletAngle(i, count)) * baseDir;

                EnemyBullet bullet = pool.GetBullet(selfPos + dir * data.forwardOffset, Quaternion.identity);
                if(bullet == null) { continue; }
                bullet.Configure(data.bulletSpeed, data.damage);
                bullet.SetRules(data.ignoreNoah, data.ignoreSigle, data.bulletDeletable);
                bullet.SetDirection(dir);
                bullet.SetShooter(Attacker);
            }

            spiralAngle += data.bulletSpiralStep;

            if(v > 0) { SeManager.PlaySe(data.activeSe, selfPos); }
            if(v < volleys - 1) { yield return runner.Wait(data.bulletVolleyInterval); }
        }
    }

    /// <summary>
    /// i 発目の弾の、基準の向きからの角度
    /// </summary>
    private float BulletAngle(int i, int count)
    {
        switch(data.bulletPattern)
        {
            case BulletPattern.Circle:
                return 360.0f * i / count;

            case BulletPattern.Spiral:
                return spiralAngle + 360.0f * i / count;

            case BulletPattern.Random:
                return UnityEngine.Random.Range(-data.bulletSpread * 0.5f, data.bulletSpread * 0.5f);

            default:
                return count == 1 ? 0.0f : Mathf.Lerp(-data.bulletSpread * 0.5f, data.bulletSpread * 0.5f, (float)i / (count - 1));
        }
    }
}
