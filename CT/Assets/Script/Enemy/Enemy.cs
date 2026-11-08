/*********************************************************************/
// * \file   Enemy.cs
// * \brief  敵の本体（データの反映・移動・経路・体当たり・倒されたとき）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 敵の本体
/// </summary>
///
/// ・共通プレハブ Enemy_Base に付いている。「敵のデータ」（EnemyData）を入れてシーンに置くだけで動く
///   （スポナーがなくても、狙う相手は操作中のキャラを自分で探す）
/// ・移動は Rigidbody2D（Dynamic）で行う。壁で止まり、マップ（PlayerSceneContext の Tilemap）の外には出ない
/// ・MoveTo で目的地を渡すと、NavMesh2D で経路を探して壁を回り込む
/// ・会話・イベント中など（EnemyBlock）と、Paused の間は止まる
/// 行動パターン（いつ・どこへ動くか、いつ攻撃するか）は次の段階で足す。今は MoveTo / Stop を外から呼ぶ
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("敵のデータ")]
    [Tooltip("体力・速さ・見た目などをまとめたデータ（Assets/Data/Enemies/ の EnemyData）")]
    private EnemyData data;

    // 経路を探し直す間隔（秒）
    private const float RepathInterval = 0.25f;

    // 目的地がこれ以上動いたら、間隔を待たずに探し直す
    private const float RepathDistance = 0.5f;

    // 曲がり角に着いたとみなす距離
    private const float CornerReachDistance = 0.1f;

    // ほかの敵と重ならないように押し合う距離と強さ
    private const float SeparationRadius = 0.8f;
    private const float SeparationStrength = 0.6f;

    private static readonly List<Enemy> all = new List<Enemy>();

    private Rigidbody2D rb;
    private Health health;
    private SpriteAnimator anim;
    private Tilemap limitTilemap;

    private bool hasDestination;
    private Vector2 destination;
    private float stopDistance;
    private Vector2 lastPathTarget;
    private float repathTimer;
    private readonly List<Vector2> corners = new List<Vector2>();
    private int cornerIndex;
    private bool pathComplete;

    private float contactTimer;
    private bool dying;

    private bool touchingPlayer;
    private Vector2 playerContactNormal;

    private bool hasOverrideVelocity;
    private Vector2 overrideVelocity;
    private CharacterAnimLink animLink;
    private EnemyBrain brain;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 今いる敵（倒されて消えたものは含まない） </summary>
    public static IReadOnlyList<Enemy> All => all;

    public EnemyData Data => data;

    public Health Health => health;

    public bool IsDead => dying || (health != null && health.IsDead);

    /// <summary> この敵だけを止める（ギミックの命令などで使う。EnemyBlock とは別） </summary>
    public bool Paused { get; set; }

    /// <summary> 移動の速さの倍率（行動パターンで一時的に速くするなど） </summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary> 動ける状態か（止められていない・倒れていない） </summary>
    public bool CanAct => !IsDead && !Paused && !EnemyBlock.IsBlocked;

    /// <summary> 目的地へ向かっているか </summary>
    public bool IsMoving => hasDestination;

    /// <summary> 目的地に着いたか（MoveTo してから） </summary>
    public bool HasArrived { get; private set; }

    /// <summary> 目的地までの経路が見つからなかったか（ナビメッシュの外・まったく行けない） </summary>
    public bool PathFailed { get; private set; }

    /// <summary> 今の経路の曲がり角（デバッグ表示用） </summary>
    public IReadOnlyList<Vector2> PathCorners => corners;

    public int PathIndex => cornerIndex;

    /// <summary> 狙う相手（今操作しているキャラ） </summary>
    public GameObject Target
    {
        get
        {
            PlayerSwitchManager manager = PlayerSwitchManager.Instance;
            return manager != null ? manager.CurrentPlayer : null;
        }
    }

    public EnemySize Size => data != null ? data.Size : EnemySize.Medium;

    /// <summary> 向いている方向（最後に動いた方向。最初はプレイヤーの方か下） </summary>
    public Vector2 Facing { get; private set; } = Vector2.down;

    /// <summary> 体の半径（当たり判定のだいたいの大きさ。突進の当たり判定・壁の手前で止まる距離に使う） </summary>
    public float BodyRadius
    {
        get
        {
            Collider2D col = GetComponent<Collider2D>();
            if(col == null) { return 0.5f; }
            Vector3 size = col.bounds.size;
            return Mathf.Max(0.1f, Mathf.Max(size.x, size.y) * 0.5f);
        }
    }

    /// <summary> 行動パターンを実行している部品（なければ null） </summary>
    public EnemyBrain Brain => brain;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        all.Clear();
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    // Spawn で出すときに使うデータ（Awake より前に決める必要があるため）
    private static EnemyData pendingSpawnData;

    /// <summary>
    /// 共通プレハブから敵を出す（仲間を呼ぶ・卵など）。プレハブは Resources/Enemies/Enemy_Base
    /// </summary>
    public static Enemy Spawn(EnemyData data, Vector2 position)
    {
        GameObject prefab = Resources.Load<GameObject>("Enemies/Enemy_Base");
        if(prefab == null) { Debug.LogError("Resources/Enemies/Enemy_Base.prefab がないので、敵を出せません"); return null; }

        pendingSpawnData = data;
        GameObject go = Instantiate(prefab, new Vector3(position.x, position.y, 0.0f), Quaternion.identity);
        pendingSpawnData = null;
        return go.GetComponent<Enemy>();
    }

    //=====================================================================
    // ギミックからの命令
    //=====================================================================

    private Coroutine pauseRoutine;
    private Color? colorBeforePause;

    /// <summary>
    /// 止める・動かす（止めている間は色を変えられる。tint が null なら色はそのまま）
    /// </summary>
    public void SetPaused(bool paused, Color? tint = null)
    {
        if(pauseRoutine != null) { StopCoroutine(pauseRoutine); pauseRoutine = null; }
        ApplyPaused(paused, tint);
    }

    /// <summary>
    /// 秒数だけ止める（止められている間は数える。会話中などは数えない）
    /// </summary>
    public void PauseFor(float seconds, Color? tint = null)
    {
        if(pauseRoutine != null) { StopCoroutine(pauseRoutine); }
        ApplyPaused(true, tint);
        pauseRoutine = StartCoroutine(PauseRoutine(seconds));
    }

    private System.Collections.IEnumerator PauseRoutine(float seconds)
    {
        float elapsed = 0.0f;
        while(elapsed < seconds)
        {
            if(!EnemyBlock.IsBlocked) { elapsed += Time.deltaTime; }
            yield return null;
        }
        pauseRoutine = null;
        ApplyPaused(false, null);
    }

    private void ApplyPaused(bool paused, Color? tint)
    {
        Paused = paused;
        if(paused && brain != null) { brain.Interrupt(); }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if(sr == null) { return; }

        if(paused && tint.HasValue)
        {
            if(!colorBeforePause.HasValue) { colorBeforePause = sr.color; }
            sr.color = tint.Value;
        }
        else if(!paused && colorBeforePause.HasValue)
        {
            sr.color = colorBeforePause.Value;
            colorBeforePause = null;
        }
    }

    /// <summary>
    /// 倒された演出なしで消す
    /// </summary>
    public void Remove()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 倒す（効き方の設定に関係なく。倒されたときの演出あり）
    /// </summary>
    public void Kill()
    {
        if(IsDead || health == null) { return; }
        var filter = health.DamageFilter;
        health.DamageFilter = null;
        health.Damage(DamageInfo.Create(health.CurrentHealth, DamageKind.Other, null, transform.position));
        health.DamageFilter = filter;
    }

    private void Awake()
    {
        if(pendingSpawnData != null) { data = pendingSpawnData; }
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        anim = GetComponent<SpriteAnimator>();
        animLink = GetComponent<CharacterAnimLink>();
        brain = GetComponent<EnemyBrain>();

        if(data == null) { Debug.LogError($"{name}: 敵のデータ（EnemyData）が入っていません", this); }

        ApplyData();

        // 攻撃の種類・キャラごとの効き方
        health.DamageFilter = info => data != null ? info.WithAmount(data.ApplyDamageRules(info)) : info;
    }

    /// <summary>
    /// データの内容を、体力・物理・見た目に反映する
    /// </summary>
    private void ApplyData()
    {
        // 物理：重力なし・回転しない・壁にぶつかって止まる
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0.0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if(data == null) { return; }

        health.SetMaxHealth(data.MaxHealth, true);

        // 押されにくい敵は重くする（プレイヤーの重さは 1）
        rb.mass = data.Heavy ? 1000.0f : 1.0f;

        if(anim != null && data.Animation != null) { anim.SetData(data.Animation, true); }
        else if(data.Sprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if(sr != null) { sr.sprite = data.Sprite; }
        }
    }

    private void OnEnable()
    {
        if(!all.Contains(this)) { all.Add(this); }
        if(health != null) { health.OnDied += OnDied; }
    }

    private void OnDisable()
    {
        all.Remove(this);
        if(health != null) { health.OnDied -= OnDied; }
    }

    private void Start()
    {
        // マップの外に出ないように、プレイヤーと同じ範囲を使う
        PlayerSceneContext context = FindAnyObjectByType<PlayerSceneContext>();
        if(context != null) { limitTilemap = context.PlayerLimitTilemap; }

        // 最初はプレイヤーの方を向く
        GameObject target = Target;
        if(target != null)
        {
            Vector2 toTarget = target.transform.position - transform.position;
            if(toTarget.sqrMagnitude > 0.0001f) { Facing = toTarget.normalized; }
        }
    }

    //=====================================================================
    // 外から使う関数
    //=====================================================================

    /// <summary>
    /// 経路を使わずに、この速度で動かす（突進など）。ClearOverrideVelocity で戻す
    /// </summary>
    public void SetOverrideVelocity(Vector2 velocity)
    {
        hasOverrideVelocity = true;
        overrideVelocity = velocity;
        if(velocity.sqrMagnitude > 0.0001f) { Facing = velocity.normalized; }
    }

    public void ClearOverrideVelocity()
    {
        hasOverrideVelocity = false;
        overrideVelocity = Vector2.zero;
        if(rb != null) { rb.linearVelocity = Vector2.zero; }
    }

    /// <summary>
    /// 1回だけの動き（構え・攻撃など）を流す。名前が空・その動きがなければ何もしない
    /// </summary>
    public bool PlayAction(string clipName)
    {
        if(string.IsNullOrEmpty(clipName) || IsDead) { return false; }
        if(animLink != null) { return animLink.PlayAction(clipName); }
        return anim != null && anim.Play(clipName, true);
    }

    //=====================================================================
    // 移動の指示
    //=====================================================================

    /// <summary>
    /// 目的地へ向かう（壁は回り込む）
    /// </summary>
    /// <param name="target"> 目的地（ワールド座標） </param>
    /// <param name="arriveDistance"> この距離まで近づいたら着いたことにする </param>
    public void MoveTo(Vector2 target, float arriveDistance = 0.1f)
    {
        bool far = !hasDestination || Vector2.Distance(target, lastPathTarget) > RepathDistance;

        hasDestination = true;
        destination = target;
        stopDistance = Mathf.Max(0.05f, arriveDistance);
        HasArrived = false;

        if(far) { repathTimer = 0.0f; }
    }

    /// <summary>
    /// 止まる（目的地を忘れる）
    /// </summary>
    public void Stop()
    {
        hasDestination = false;
        corners.Clear();
        cornerIndex = 0;
        if(rb != null) { rb.linearVelocity = Vector2.zero; }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void FixedUpdate()
    {
        if(contactTimer > 0.0f) { contactTimer -= Time.fixedDeltaTime; }

        if(!CanAct)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 突進など、速度を直接決められているとき
        if(hasOverrideVelocity)
        {
            rb.linearVelocity = overrideVelocity;
            return;
        }

        Vector2 velocity = Vector2.zero;
        if(hasDestination) { velocity = FollowPath(); }

        // ほかの敵と重ならないように少し離れる（動いているときだけ）
        if(velocity != Vector2.zero) { velocity += Separation() * (data != null ? data.MoveSpeed : 1.0f); }

        // プレイヤーに触れているときは、プレイヤーの方へは進まない（重い敵がプレイヤーを押し出して、イベントの範囲などに押し込まないように）
        if(touchingPlayer)
        {
            float into = Vector2.Dot(velocity, playerContactNormal);
            if(into < 0.0f) { velocity -= playerContactNormal * into; }
            touchingPlayer = false;
        }

        rb.linearVelocity = velocity;
        if(velocity.sqrMagnitude > 0.01f) { Facing = velocity.normalized; }
    }

    private void LateUpdate()
    {
        ClampToMap();
    }

    /// <summary>
    /// 経路に沿って進む速度
    /// </summary>
    private Vector2 FollowPath()
    {
        Vector2 position = rb.position;

        // 着いた
        if(Vector2.Distance(position, destination) <= stopDistance)
        {
            hasDestination = false;
            HasArrived = true;
            corners.Clear();
            return Vector2.zero;
        }

        // 経路を探し直す
        repathTimer -= Time.fixedDeltaTime;
        if(repathTimer <= 0.0f || Vector2.Distance(destination, lastPathTarget) > RepathDistance)
        {
            repathTimer = RepathInterval;
            Repath(position);
        }

        Vector2 next = destination;
        if(corners.Count > 0)
        {
            // 着いた曲がり角は飛ばす
            while(cornerIndex < corners.Count - 1 && Vector2.Distance(position, corners[cornerIndex]) <= CornerReachDistance) { cornerIndex++; }
            next = corners[cornerIndex];

            // 最後の曲がり角（行ける所まで）に着いたが、目的地には届かない
            if(cornerIndex == corners.Count - 1 && !pathComplete && Vector2.Distance(position, next) <= CornerReachDistance)
            {
                return Vector2.zero;
            }
        }

        Vector2 direction = next - position;
        if(direction.sqrMagnitude < 0.0001f) { return Vector2.zero; }

        float speed = (data != null ? data.MoveSpeed : 2.0f) * Mathf.Max(0.0f, SpeedMultiplier);

        // 行き過ぎないように、最後の1歩は距離に合わせる
        float step = speed * Time.fixedDeltaTime;
        float distance = direction.magnitude;
        if(step > distance && Time.fixedDeltaTime > 0.0f) { speed = distance / Time.fixedDeltaTime; }

        return direction.normalized * speed;
    }

    private void Repath(Vector2 position)
    {
        lastPathTarget = destination;
        cornerIndex = 0;

        NavMesh2D nav = NavMesh2D.Instance;
        if(nav != null && nav.CalculatePath(position, destination, Size, corners, out pathComplete))
        {
            PathFailed = false;

            // 最初の点は今いる場所の近くなので飛ばす
            if(corners.Count > 1) { cornerIndex = 1; }
            return;
        }

        // 経路が見つからないときは、まっすぐ向かう（壁があればそこで止まる）
        PathFailed = true;
        pathComplete = false;
        corners.Clear();
    }

    private Vector2 Separation()
    {
        Vector2 push = Vector2.zero;
        Vector2 position = rb.position;

        foreach(Enemy other in all)
        {
            if(other == this || other == null) { continue; }

            Vector2 diff = position - (Vector2)other.transform.position;
            float distance = diff.magnitude;
            if(distance <= 0.0001f || distance >= SeparationRadius) { continue; }

            push += diff / distance * (1.0f - distance / SeparationRadius);
        }

        return push * SeparationStrength;
    }

    /// <summary>
    /// マップ（Tilemap の範囲）の外に出ないようにする（PlayerMapLimit と同じ範囲）
    /// </summary>
    private void ClampToMap()
    {
        if(limitTilemap == null) { return; }

        BoundsInt cells = limitTilemap.cellBounds;
        Vector3 min = limitTilemap.CellToWorld(cells.min);
        Vector3 max = limitTilemap.CellToWorld(cells.max);

        Vector2 position = rb.position;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(position.x, Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
            Mathf.Clamp(position.y, Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)));

        if(clamped != position) { rb.position = clamped; }
    }

    //=====================================================================
    // 体当たり
    //=====================================================================

    private void OnCollisionStay2D(Collision2D collision)
    {
        // プレイヤーに触れている向き（次の FixedUpdate で、そちらへ押さないようにする）
        if(collision.collider.CompareTag("Player") && collision.contactCount > 0)
        {
            touchingPlayer = true;
            playerContactNormal = collision.GetContact(0).normal;
        }

        if(data == null || data.ContactDamage <= 0) { return; }
        if(contactTimer > 0.0f) { return; }
        if(!CanAct) { return; }
        if(!collision.collider.CompareTag("Player")) { return; }

        Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)collision.transform.position;
        if(DamageUtility.Apply(collision.collider, DamageInfo.Create(data.ContactDamage, DamageKind.Contact, gameObject, point)))
        {
            contactTimer = data.ContactInterval;
        }
    }

    //=====================================================================
    // 倒されたとき
    //=====================================================================

    private void OnDied()
    {
        if(dying) { return; }
        dying = true;

        Stop();
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 当たり判定を消す（倒れている間に攻撃が当たったり、ぶつかったりしないように）
        foreach(Collider2D col in GetComponentsInChildren<Collider2D>()) { col.enabled = false; }

        if(data != null) { SeManager.PlaySe(data.DeathSe, transform.position); }

        // 死亡アニメがあれば、流し終わってから消える
        string clip = data != null ? data.DeathAnimation : "";
        if(anim != null && !string.IsNullOrEmpty(clip) && anim.Play(clip, true))
        {
            StartCoroutine(VanishAfterAnimation());
            return;
        }

        Vanish();
    }

    private IEnumerator VanishAfterAnimation()
    {
        yield return anim.WaitForCycle();
        Vanish();
    }

    private void Vanish()
    {
        if(data != null) { EffectManager.Play(data.DeathEffect, transform.position); }
        Destroy(gameObject);
    }

    //=====================================================================
    // Gizmos
    //=====================================================================

    private void OnDrawGizmosSelected()
    {
        if(corners.Count == 0) { return; }

        Gizmos.color = Color.yellow;
        Vector3 prev = transform.position;
        for(int i = cornerIndex; i < corners.Count; i++)
        {
            Gizmos.DrawLine(prev, corners[i]);
            prev = corners[i];
        }
    }
}
