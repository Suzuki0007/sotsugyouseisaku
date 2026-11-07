/*********************************************************************/
// * \file   EffectTrigger.cs
// * \brief  コードを書かずにエフェクトを出すためのコンポーネント
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エフェクトを出すタイミング
/// </summary>
public enum EffectTiming
{
    [InspectorName("出現したとき (最初の1回)")]
    OnStart,

    [InspectorName("表示されたとき (毎回)")]
    OnEnable,

    [InspectorName("消えたとき・非表示になったとき")]
    OnDisable,

    [InspectorName("ダメージを受けたとき (Health)")]
    OnDamaged,

    [InspectorName("回復したとき (Health)")]
    OnHealed,

    [InspectorName("死亡したとき (Health)")]
    OnDied,

    [InspectorName("何かが入ってきたとき (Trigger)")]
    OnTriggerEnter,

    [InspectorName("何かが出ていったとき (Trigger)")]
    OnTriggerExit,

    [InspectorName("何かにぶつかったとき (Collision)")]
    OnCollisionEnter,

    [InspectorName("手動 (UnityEvent・アニメーションイベントから呼ぶ)")]
    Manual,
}

/// <summary>
/// エフェクトを出す場所
/// </summary>
public enum EffectSpawnAt
{
    [InspectorName("このオブジェクト")]
    Self,

    [InspectorName("触れた相手")]
    Other,

    [InspectorName("触れた場所 (接触点)")]
    ContactPoint,
}

/// <summary>
/// EffectTrigger に登録する1行分の設定
/// </summary>
[System.Serializable]
public class EffectTriggerEntry
{
    [Tooltip("メモ・名前 (手動で出すときは、この名前で呼び出せる)")]
    public string name = "";

    [Tooltip("いつ出すか")]
    public EffectTiming timing = EffectTiming.OnStart;

    [Tooltip("出すエフェクト")]
    public EffectData effect;

    [Tooltip("どこに出すか")]
    public EffectSpawnAt spawnAt = EffectSpawnAt.Self;

    [Tooltip("位置のずらし (EffectData のずらしに足される)")]
    public Vector2 offset = Vector2.zero;

    [Tooltip("出した後も対象にくっついて動く")]
    public bool follow = false;

    [Tooltip("対象の回転にも合わせる")]
    public bool rotateWithTarget = false;

    [Tooltip("このタグの相手のときだけ出す (空ならすべての相手)")]
    public string targetTag = "";

    [Tooltip("相手が出ていったら、このエフェクト (ループ) を止める")]
    public bool stopOnExit = true;

    [Tooltip("1回だけ出す (2回目以降は出さない)")]
    public bool onlyOnce = false;

    [System.NonSerialized] internal bool played;
    [System.NonSerialized] internal readonly List<EffectHandle> loops = new List<EffectHandle>();

    /// <summary> 触れたときに関係する設定か </summary>
    public static bool IsContactTiming(EffectTiming t)
    {
        return t == EffectTiming.OnTriggerEnter || t == EffectTiming.OnTriggerExit || t == EffectTiming.OnCollisionEnter;
    }

    /// <summary> Health を使う設定か </summary>
    public static bool IsHealthTiming(EffectTiming t)
    {
        return t == EffectTiming.OnDamaged || t == EffectTiming.OnHealed || t == EffectTiming.OnDied;
    }
}

/// <summary>
/// 出したいオブジェクトに付けるだけでエフェクトを出せるコンポーネント
/// 「いつ」「どのエフェクトを」「どこに」をリストで登録する
///
/// 例:
///   ・敵に付けて「ダメージを受けたとき → ヒットエフェクト」「死亡したとき → 爆発」
///   ・宝箱に付けて「何かが入ってきたとき(タグ: Player) → キラキラ」
///   ・アニメーションイベントで Play("足煙") を呼んで、足元に煙を出す
/// </summary>
[AddComponentMenu("Effect/Effect Trigger")]
public class EffectTrigger : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private List<EffectTriggerEntry> entries = new List<EffectTriggerEntry>();

    private Health health;
    private bool subscribed;

    public IReadOnlyList<EffectTriggerEntry> Entries => entries;

    //=====================================================================
    // Unity イベント
    //=====================================================================

    private void Awake()
    {
        if(Uses(EffectTriggerEntry.IsHealthTiming))
        {
            health = GetComponentInParent<Health>();
            if(health == null)
            {
                Debug.LogWarning($"{name}: EffectTrigger に Health のタイミングが設定されていますが、Health が見つかりません", this);
            }
        }
    }

    private void Start()
    {
        Fire(EffectTiming.OnStart, null);
    }

    private void OnEnable()
    {
        if(health != null && !subscribed)
        {
            health.OnDamaged += OnDamaged;
            health.OnHealed  += OnHealed;
            health.OnDied    += OnDied;
            subscribed = true;
        }

        Fire(EffectTiming.OnEnable, null);
    }

    private void OnDisable()
    {
        if(health != null && subscribed)
        {
            health.OnDamaged -= OnDamaged;
            health.OnHealed  -= OnHealed;
            health.OnDied    -= OnDied;
            subscribed = false;
        }

        // このオブジェクトが出していたループは止める
        StopLoops();

        Fire(EffectTiming.OnDisable, null);
    }

    private void OnDamaged(int damage)
    {
        // 死亡したときは「死亡」のほうだけ出す
        if(health != null && health.IsDead) { return; }
        Fire(EffectTiming.OnDamaged, null);
    }

    private void OnHealed(int amount) => Fire(EffectTiming.OnHealed, null);

    private void OnDied() => Fire(EffectTiming.OnDied, null);

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector3 point = other.ClosestPoint(transform.position);
        Fire(EffectTiming.OnTriggerEnter, new Contact(other.gameObject, point));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 入ってきたときに出したループを止める
        foreach(EffectTriggerEntry entry in entries)
        {
            if(entry.timing != EffectTiming.OnTriggerEnter || !entry.stopOnExit) { continue; }
            if(!MatchesTag(entry, other.gameObject)) { continue; }
            StopLoops(entry);
        }

        Vector3 point = other.ClosestPoint(transform.position);
        Fire(EffectTiming.OnTriggerExit, new Contact(other.gameObject, point));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 point = collision.contactCount > 0 ? (Vector3)collision.GetContact(0).point : collision.transform.position;
        Fire(EffectTiming.OnCollisionEnter, new Contact(collision.gameObject, point));
    }

    //=====================================================================
    // 外部から呼ぶ関数 (UnityEvent・アニメーションイベントからも呼べる)
    //=====================================================================

    /// <summary>
    /// タイミングが「手動」のエフェクトをすべて出す
    /// </summary>
    public void Play()
    {
        Fire(EffectTiming.Manual, null);
    }

    /// <summary>
    /// 指定した名前のエフェクトを出す (タイミングに関係なく出る)
    /// アニメーションイベントの String に名前を入れて呼ぶと便利
    /// </summary>
    public void Play(string entryName)
    {
        bool found = false;
        foreach(EffectTriggerEntry entry in entries)
        {
            if(entry.name != entryName) { continue; }
            PlayEntry(entry, null);
            found = true;
        }

        if(!found)
        {
            Debug.LogWarning($"{name}: EffectTrigger に「{entryName}」という名前のエフェクトがありません", this);
        }
    }

    /// <summary>
    /// リストの番号を指定してエフェクトを出す (0から数える)
    /// </summary>
    public void PlayIndex(int index)
    {
        if(index < 0 || index >= entries.Count) { return; }
        PlayEntry(entries[index], null);
    }

    /// <summary>
    /// このコンポーネントが出したループエフェクトをすべて止める
    /// </summary>
    public void StopLoops()
    {
        foreach(EffectTriggerEntry entry in entries) { StopLoops(entry); }
    }

    /// <summary>
    /// 指定した名前のループエフェクトを止める
    /// </summary>
    public void StopLoops(string entryName)
    {
        foreach(EffectTriggerEntry entry in entries)
        {
            if(entry.name == entryName) { StopLoops(entry); }
        }
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private readonly struct Contact
    {
        public readonly GameObject other;
        public readonly Vector3 point;

        public Contact(GameObject other, Vector3 point)
        {
            this.other = other;
            this.point = point;
        }
    }

    private void Fire(EffectTiming timing, Contact? contact)
    {
        if(!Application.isPlaying) { return; }

        foreach(EffectTriggerEntry entry in entries)
        {
            if(entry.timing != timing) { continue; }
            if(contact.HasValue && !MatchesTag(entry, contact.Value.other)) { continue; }
            PlayEntry(entry, contact);
        }
    }

    private void PlayEntry(EffectTriggerEntry entry, Contact? contact)
    {
        if(entry.effect == null) { return; }
        if(entry.onlyOnce && entry.played) { return; }

        // 出す場所
        Transform target = transform;
        Vector3 position = transform.position;
        if(contact.HasValue)
        {
            if(entry.spawnAt == EffectSpawnAt.Other)
            {
                target = contact.Value.other.transform;
                position = target.position;
            }
            else if(entry.spawnAt == EffectSpawnAt.ContactPoint)
            {
                target = null;
                position = contact.Value.point;
            }
        }

        // 非表示になったときは自分に追従できない
        bool canFollow = entry.follow && target != null && target.gameObject.activeInHierarchy;

        EffectHandle handle = EffectManager.Play(
            entry.effect, position, entry.offset, 0.0f,
            canFollow ? target : null, entry.rotateWithTarget);

        if(!handle.IsValid) { return; }
        entry.played = true;

        if(entry.effect.IsLoop)
        {
            entry.loops.RemoveAll(h => !h.IsPlaying);
            entry.loops.Add(handle);
        }
    }

    private static void StopLoops(EffectTriggerEntry entry)
    {
        foreach(EffectHandle h in entry.loops) { EffectManager.Stop(h); }
        entry.loops.Clear();
    }

    private static bool MatchesTag(EffectTriggerEntry entry, GameObject other)
    {
        if(!EffectTriggerEntry.IsContactTiming(entry.timing)) { return true; }
        if(string.IsNullOrEmpty(entry.targetTag)) { return true; }
        return other != null && other.CompareTag(entry.targetTag);
    }

    private bool Uses(System.Func<EffectTiming, bool> predicate)
    {
        foreach(EffectTriggerEntry entry in entries)
        {
            if(predicate(entry.timing)) { return true; }
        }
        return false;
    }

    //=====================================================================
    // ギズモ (シーンビューで出す位置を表示)
    //=====================================================================

    private void OnDrawGizmosSelected()
    {
        foreach(EffectTriggerEntry entry in entries)
        {
            if(entry.effect == null) { continue; }
            if(entry.spawnAt != EffectSpawnAt.Self && EffectTriggerEntry.IsContactTiming(entry.timing)) { continue; }

            Vector3 p = transform.position + (Vector3)(entry.offset + entry.effect.Offset);
            Gizmos.color = new Color(1.0f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(p, 0.15f);
            Gizmos.DrawLine(transform.position, p);
        }
    }
}
