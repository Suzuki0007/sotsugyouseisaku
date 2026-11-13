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
/// 倒したことをセーブした敵が、シーンに戻ったときの姿
/// </summary>
public enum DefeatReturn
{
    [InspectorName("倒されたあとと同じ（消える敵はいない・残る敵は倒れた姿）")]
    SameAsDefeat = 0,

    [InspectorName("いない")]
    Gone = 1,

    [InspectorName("復活する（また戦う）")]
    Revive = 2,
}

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

    [Header("この敵だけの設定（倒したとき）")]

    [SerializeField, Label("倒したときのイベント")]
    [Tooltip("倒した瞬間に実行する（扉が開く・会話 など）。セーブから戻ったときは、見た目だけ実行後の状態にする")]
    private EventSequence onDefeated;

    [SerializeField, Label("倒したら ON にするフラグ")]
    private GameFlag defeatFlag;

    [SerializeField, Label("倒した後に調べたとき")]
    [Tooltip("敵のデータの「倒されたあと」が「残る」「おとなしくなる」のとき、E で調べると実行する（会話・アイテムをもらう など）")]
    private EventSequence afterTalk;

    [SerializeField, Label("倒したことをセーブする")]
    [Tooltip("オン：倒したことを覚えておき、シーンに戻ったときに「戻ったときの姿」にする（ボス・1 回だけの敵）")]
    private bool saveDefeat = false;

    [SerializeField, Label("戻ったときの姿")]
    [Tooltip("倒したことをセーブしたとき、シーンに戻ったらどうするか")]
    private DefeatReturn onReturn = DefeatReturn.SameAsDefeat;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する（名前や親を変えると記録がリセットされる）")]
    private string saveId = "";

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

    /// <summary> どれかの敵が倒されたとき（相棒の一言など） </summary>
    public static event System.Action<Enemy> Defeated;

    public EnemyData Data => data;

    public Health Health => health;

    public bool IsDead => dying || (health != null && health.IsDead);

    /// <summary> この敵だけを止める（ギミックの命令などで使う。EnemyBlock とは別） </summary>
    public bool Paused { get; set; }

    /// <summary> ひるんでいる間 </summary>
    public bool Staggered => staggerTimer > 0.0f;

    /// <summary> 止められているか（命令で止めた・ひるんでいる） </summary>
    public bool Stopped => Paused || Staggered;

    /// <summary> 止めている間の色（命令で指定したとき。見た目の変化 EnemyLook が使う） </summary>
    public Color? PausedTint { get; private set; }

    /// <summary> ひるみのたまり具合（敵のデータの「ひるむまでのダメージ」でひるむ） </summary>
    public float StaggerGauge => staggerGauge;

    /// <summary> 壊れた部位の名前 </summary>
    public IReadOnlyCollection<string> BrokenParts => brokenParts;

    /// <summary> ひるんだとき </summary>
    public event System.Action OnStaggered;

    /// <summary> 部位が壊れたとき </summary>
    public event System.Action<EnemyPart> OnPartBroken;

    private float staggerGauge;
    private float staggerTimer;
    private readonly HashSet<string> brokenParts = new HashSet<string>();
    private readonly HashSet<AttackData> disabledAttacks = new HashSet<AttackData>();

    /// <summary> 移動の速さの倍率（行動パターンで一時的に速くするなど） </summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary> 動ける状態か（止められていない・ひるんでいない・倒れていない） </summary>
    public bool CanAct => !IsDead && !Stopped && !EnemyBlock.IsBlocked;

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
            return manager != null ? manager.EnemyTargetFrom(transform.position) : null;
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
        Defeated = null;
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

        // 色は見た目の変化（EnemyLook）がまとめて付ける
        PausedTint = paused ? tint : null;
    }

    //=====================================================================
    // ひるみ
    //=====================================================================

    /// <summary>
    /// ひるみをためる（攻撃を受けたとき。部位からも呼ばれる）
    /// </summary>
    /// <param name="amount"> たまる量（ダメージ × 部位のひるみやすさ） </param>
    /// <param name="info"> 攻撃の情報（敵のデータの「ひるみやすさ」の倍率を選ぶのに使う） </param>
    public void AddStagger(float amount, DamageInfo info)
    {
        if(data == null || data.StaggerThreshold <= 0.0f || IsDead || Staggered) { return; }

        float multiplier = data.StaggerMultiplier(info);
        if(multiplier <= 0.0f || amount <= 0.0f) { return; }

        staggerGauge += amount * multiplier;
        if(staggerGauge >= data.StaggerThreshold) { staggerPending = true; }
        TryStagger();
    }

    // ひるむだけたまったが、攻撃の途中でキャンセルできないので待っている
    private bool staggerPending;

    /// <summary>
    /// たまっていればひるむ（攻撃の途中でキャンセルできないときは、できるようになるまで待つ）
    /// </summary>
    private void TryStagger()
    {
        if(data == null || !staggerPending || Staggered || IsDead) { return; }
        if(brain != null && brain.CurrentAttack != null && !brain.CurrentAttack.CanCancel) { return; }

        Stagger(data.StaggerTime);
    }

    /// <summary>
    /// ひるませる（今の行動をやめて、秒数だけ動けなくなる）。ギミックの命令からも使う
    /// </summary>
    public void Stagger(float seconds)
    {
        if(IsDead) { return; }

        staggerGauge = 0.0f;
        staggerPending = false;
        staggerTimer = Mathf.Max(0.05f, seconds);
        if(brain != null) { brain.Interrupt(); }
        Stop();

        if(data != null)
        {
            PlayAction(data.StaggerAnimation);
            EffectManager.Play(data.StaggerEffect, transform.position);
            SeManager.PlaySe(data.StaggerSe, transform.position);
        }
        OnStaggered?.Invoke();
    }

    private void Update()
    {
        if(IsDead) { return; }

        // ひるみの時間（会話中などは減らない）
        if(staggerTimer > 0.0f && !EnemyBlock.IsBlocked) { staggerTimer -= Time.deltaTime; }

        // ひるむ順番を待っている間は減らさない（攻撃が終わったらすぐひるむ）
        if(staggerPending) { TryStagger(); return; }

        // たまったひるみは少しずつ減る
        if(data != null && staggerGauge > 0.0f && !Staggered)
        {
            staggerGauge = Mathf.Max(0.0f, staggerGauge - data.StaggerDecay * Time.deltaTime);
        }
    }

    //=====================================================================
    // 部位
    //=====================================================================

    /// <summary> 部位が壊れたことを知らせる（EnemyPart から） </summary>
    public void NotifyPartBroken(EnemyPart part)
    {
        if(part == null) { return; }
        brokenParts.Add(part.PartName);
        OnPartBroken?.Invoke(part);
    }

    /// <summary> この部位が壊れたか </summary>
    public bool IsPartBroken(string partName) => !string.IsNullOrEmpty(partName) && brokenParts.Contains(partName);

    /// <summary> この攻撃を使えなくする（部位が壊れたときなど） </summary>
    public void DisableAttack(AttackData attack)
    {
        if(attack != null) { disabledAttacks.Add(attack); }
    }

    /// <summary> この攻撃が使えなくなっているか </summary>
    public bool IsAttackDisabled(AttackData attack) => attack != null && disabledAttacks.Contains(attack);

    /// <summary>
    /// 指定した位置へ瞬間移動する（戦闘エリアのやり直しなど）
    /// </summary>
    public void Teleport(Vector2 position)
    {
        Stop();
        ClearOverrideVelocity();
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        if(rb != null) { rb.position = position; rb.linearVelocity = Vector2.zero; }
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

        // 効き方・無敵（グループのバリアなど）に関係なく倒す
        var filter = health.DamageFilter;
        bool invincible = health.Invincible;
        health.DamageFilter = null;
        health.Invincible = false;
        health.Damage(DamageInfo.Create(health.CurrentHealth, DamageKind.Other, null, transform.position));
        health.DamageFilter = filter;
        health.Invincible = invincible;
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

        // 本体に当たった攻撃でひるみをためる（部位から渡されたものは、部位で数え済み）
        health.OnDamagedInfo += info => { if(!info.noStagger) { AddStagger(info.amount, info); } };

        // 見た目の変化（体力・攻撃・部位・段階・ひるみ・止まっている）
        if(GetComponent<EnemyLook>() == null) { gameObject.AddComponent<EnemyLook>(); }

        // 倒したことをセーブしてあれば、戻ったときの姿にする
        RestoreDefeat();
    }

    //=====================================================================
    // 倒したことのセーブ
    //=====================================================================

    private string SaveKey
    {
        get
        {
            string name = saveId;
            if(string.IsNullOrEmpty(name))
            {
                name = gameObject.name;
                for(Transform p = transform.parent; p != null; p = p.parent) { name = p.name + "/" + name; }
            }
            return "Enemy/" + gameObject.scene.name + "/" + name;
        }
    }

    /// <summary> 倒したことがセーブされているか </summary>
    public bool DefeatSaved => saveDefeat && Save.Get(SaveKey, false);

    private bool restoredDefeat;

    private void RestoreDefeat()
    {
        if(!DefeatSaved || onReturn == DefeatReturn.Revive) { return; }

        restoredDefeat = true;
        if(onDefeated != null) { onDefeated.Restore(gameObject); }

        DefeatStyle style = data != null ? data.DefeatStyle : DefeatStyle.Vanish;
        bool gone = onReturn == DefeatReturn.Gone || (onReturn == DefeatReturn.SameAsDefeat && style == DefeatStyle.Vanish);
        if(gone)
        {
            dying = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        // 倒れた姿のまま（またはおとなしくなった姿で）置いておく
        dying = true;
        health.SetDeadSilently();
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
        if(restoredDefeat)
        {
            // 戻ったときの姿（倒れた姿・おとなしくなった姿）
            DefeatStyle style = data != null ? data.DefeatStyle : DefeatStyle.Remain;
            EnterDefeatedPose(style == DefeatStyle.Pacify, true);
            return;
        }

        // マップの外に出ないようにする範囲は MapArea（プレイヤーと同じ範囲。ClampToMap）

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
    /// 歩ける範囲（MapArea：歩ける範囲 → マップの絵 → Tilemap。プレイヤーと同じ範囲）の外に出ないようにする
    /// </summary>
    private void ClampToMap()
    {
        // 止められている間（イベントで上から降らせる・画面の外から入れる など）は収めない
        if(Paused) { return; }

        Vector2 position = rb.position;
        Vector2 clamped = MapArea.Clamp(position);
        if((clamped - position).sqrMagnitude > 0.000001f) { rb.position = clamped; }
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

        DefeatStyle style = data != null ? data.DefeatStyle : DefeatStyle.Vanish;

        // 当たり判定を消す（倒れている間に攻撃が当たったり、ぶつかったりしないように）
        // おとなしくなる敵は、体の当たり判定を残す（その場に立っている NPC のように）
        foreach(Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if(style == DefeatStyle.Pacify && col.gameObject == gameObject && !col.isTrigger) { continue; }
            col.enabled = false;
        }

        if(data != null) { SeManager.PlaySe(data.DeathSe, transform.position); }

        // 倒したときのイベント・フラグ・セーブ・落とすアイテム
        if(defeatFlag != null) { defeatFlag.Value = true; }
        if(saveDefeat) { Save.SetPending(SaveKey, true); }
        DropItems();
        if(onDefeated != null) { onDefeated.Play(gameObject); }
        Defeated?.Invoke(this);

        if(style == DefeatStyle.Pacify)
        {
            EffectManager.Play(data != null ? data.DeathEffect : null, transform.position);
            EnterDefeatedPose(true, false);
            return;
        }

        // 死亡アニメがあれば、流し終わってから消える（残る）
        string clip = data != null ? data.DeathAnimation : "";
        if(anim != null && !string.IsNullOrEmpty(clip) && anim.Play(clip, true))
        {
            StartCoroutine(AfterDeathAnimation(style));
            return;
        }

        if(style == DefeatStyle.Remain)
        {
            EffectManager.Play(data != null ? data.DeathEffect : null, transform.position);
            EnterDefeatedPose(false, false);
            return;
        }
        Vanish();
    }

    private IEnumerator AfterDeathAnimation(DefeatStyle style)
    {
        yield return anim.WaitForCycle();
        if(style == DefeatStyle.Remain)
        {
            EffectManager.Play(data != null ? data.DeathEffect : null, transform.position);
            EnterDefeatedPose(false, false);
        }
        else
        {
            Vanish();
        }
    }

    /// <summary>
    /// 倒れた姿・おとなしくなった姿にする（調べられるようにする）
    /// </summary>
    /// <param name="pacify"> おとなしくなる（立ったまま） </param>
    /// <param name="restoring"> セーブから戻したとき（演出なし） </param>
    private void EnterDefeatedPose(bool pacify, bool restoring)
    {
        all.Remove(this);
        Stop();
        if(rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.linearVelocity = Vector2.zero; }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if(pacify)
        {
            if(anim != null) { anim.Play("Idle", true); }
        }
        else
        {
            // 倒れた姿：死亡アニメの最後の絵で止め、色を変える
            string clip = data != null ? data.DeathAnimation : "";
            if(restoring && anim != null && !string.IsNullOrEmpty(clip)) { anim.JumpToEnd(clip); }
            if(sr != null && data != null) { sr.color = data.RemainTint; }
            foreach(Collider2D col in GetComponentsInChildren<Collider2D>()) { col.enabled = false; }
        }

        // 部位の見た目は隠す（倒れたあとに弱点の目印などが残らないように）
        foreach(EnemyPart part in GetComponentsInChildren<EnemyPart>())
        {
            foreach(Renderer r in part.GetComponentsInChildren<Renderer>()) { r.enabled = false; }
        }

        if(afterTalk != null) { DefeatedTalk.Attach(this, afterTalk, Mathf.Max(1.0f, BodyRadius + 0.6f)); }
    }

    /// <summary>
    /// 敵のデータの「落とすアイテム」を落とす
    /// </summary>
    private void DropItems()
    {
        if(data == null) { return; }

        int index = 0;
        foreach(EnemyDrop drop in data.Drops)
        {
            if(drop == null || drop.item == null) { continue; }
            if(UnityEngine.Random.value > drop.chance) { continue; }

            if(drop.direct)
            {
                if(InventoryManager.Instance != null) { InventoryManager.Instance.AddItem(drop.item, Mathf.Max(1, drop.count)); }
                continue;
            }

            // 少しずつずらして置く
            float angle = index * 2.4f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (index == 0 ? 0.0f : 0.5f);
            DroppedItem.Drop(drop.item, drop.count, (Vector2)transform.position + offset);
            index++;
        }
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
