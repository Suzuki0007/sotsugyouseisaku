/*********************************************************************/
// * \file   PushBlock.cs
// * \brief  押せる箱（1 マスずつ動く。穴に落とすと埋まる）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 押せる箱
/// </summary>
///
/// ・操作しているキャラが、箱に向かって少しの間歩き続けると、1 マス（マスの大きさ）押せる
/// ・行き先に壁・ほかの箱・キャラ・敵があると動かない。埋まっていない穴（GimmickHole）なら、落ちて穴を埋める
/// ・押せるキャラ（両方・ノアだけ・シグルだけ）を決められる（重い箱はシグルだけ）
/// ・位置と、穴に落ちたかをセーブする（Block/<シーン>/<ID か階層パス>）。動けなくなったときは「押せる箱を元に戻す」で戻せる
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PushBlock : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("押せるキャラ")]
    [Tooltip("重い箱はシグルだけ、など")]
    private CharacterRestriction user = CharacterRestriction.Both;

    [SerializeField, Label("2人で押す")]
    [Tooltip("オン：相棒が箱のそば（2 マス以内）にいるときだけ押せる（とても重い扉・大きな岩）。「一緒」なら相棒はついてくるので、そのまま押せる")]
    private bool requireBoth = false;

    [SerializeField, Label("1人で押したとき")]
    [Tooltip("「2人で押す」で相棒がそばにいないとき。例：「一人じゃ動かない……」。空なら「できない」音だけ")]
    private EventSequence needPartner;

    [SerializeField, Label("マスの大きさ", 0.1f)]
    [Tooltip("1 回押したときに動く距離（マス）")]
    private float cellSize = 1.0f;

    [SerializeField, Label("押し始めるまで（秒）", 0.0f)]
    [Tooltip("箱に向かってこの秒数歩き続けると動く（かすっただけで動かないように）")]
    private float pushDelay = 0.25f;

    [SerializeField, Label("動く時間（秒）", 0.01f)]
    private float moveTime = 0.2f;

    [SerializeField, Label("押したときの SE")]
    private SeData pushSe;

    [SerializeField, Label("穴に落ちたときの SE")]
    private SeData fallSe;

    [SerializeField, Label("押せないキャラで押したとき")]
    [Tooltip("例：ノアで押したら「重くて動かない……」。空なら「できない」音だけ")]
    private EventSequence wrongCharacter;

    [Header("セーブ")]

    [SerializeField, Label("セーブする")]
    [Tooltip("オフにすると、シーンに入るたびに置いた位置に戻る")]
    private bool save = true;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する")]
    private string saveId = "";

    private BoxCollider2D box;
    private Rigidbody2D rb;
    private Vector2 home;
    private string saveKey;
    private bool moving;
    private bool fallen;
    private float pushTimer;
    private float refuseCooldown;

    private static readonly List<PushBlock> all = new List<PushBlock>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 穴に落ちたか </summary>
    public bool Fallen => fallen;

    /// <summary> 動いている途中か </summary>
    public bool Moving => moving;

    public CharacterRestriction User => user;
    public float CellSize => cellSize;

    /// <summary> シーンの押せる箱（デバッグ・元に戻す用） </summary>
    public static IReadOnlyList<PushBlock> All => all;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { all.Clear(); }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0.0f;
        rb.freezeRotation = true;

        home = transform.position;
        saveKey = "Block/" + gameObject.scene.name + "/" + (string.IsNullOrEmpty(saveId) ? ScenePath(transform) : saveId);

        if(save)
        {
            if(Save.Get(saveKey + "/Fallen", false))
            {
                fallen = true;
                gameObject.SetActive(false);
                return;
            }
            float x = Save.Get(saveKey + "/X", home.x);
            float y = Save.Get(saveKey + "/Y", home.y);
            SetPosition(new Vector2(x, y));
        }
    }

    private void OnEnable()  { if(!all.Contains(this)) { all.Add(this); } }
    private void OnDisable() { all.Remove(this); }

    //=====================================================================
    // 押す
    //=====================================================================

    private void Update()
    {
        if(refuseCooldown > 0.0f) { refuseCooldown -= Time.deltaTime; }
        if(moving || fallen) { return; }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        GameObject player = manager != null ? manager.CurrentPlayer : null;
        if(player == null || InputBlock.IsBlocked) { pushTimer = 0.0f; return; }

        // 押せないキャラが近づいたら、押せるキャラの目印
        if(!user.Allows(player))
        {
            PartyHintSettings hint = PartyHint.Settings;
            if(hint != null && Vector2.Distance(player.transform.position, transform.position) <= hint.showDistance) { PartyHint.Request(transform, user, player.transform.position); }
        }

        Vector2 direction;
        if(!IsPushedBy(player, out direction)) { pushTimer = 0.0f; return; }

        if(!user.Allows(player))
        {
            pushTimer = 0.0f;
            if(refuseCooldown <= 0.0f)
            {
                refuseCooldown = 1.5f;
                if(wrongCharacter != null) { wrongCharacter.Play(gameObject); }
                else                       { UISound.Play(UISoundType.Cannot); }
            }
            return;
        }

        // 2人で押す箱は、相棒がそばにいないと押せない
        if(requireBoth && manager != null && manager.Partner != null && Vector2.Distance(manager.Partner.transform.position, transform.position) > cellSize * 2.0f + 0.5f)
        {
            pushTimer = 0.0f;
            if(refuseCooldown <= 0.0f)
            {
                refuseCooldown = 1.5f;
                if(needPartner != null) { needPartner.Play(gameObject); }
                else                    { UISound.Play(UISoundType.Cannot); }
            }
            return;
        }

        pushTimer += Time.deltaTime;
        if(pushTimer < pushDelay) { return; }

        pushTimer = 0.0f;
        TryMove(direction);
    }

    /// <summary>
    /// キャラが箱に向かって歩いているか（向きは上下左右のどれか）
    /// </summary>
    private bool IsPushedBy(GameObject player, out Vector2 direction)
    {
        direction = Vector2.zero;

        CharacterMovement movement = player.GetComponent<CharacterMovement>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if(movement == null || playerCollider == null) { return false; }

        Vector2 move = movement.MoveDirection;
        if(move.sqrMagnitude < 0.01f) { return false; }

        direction = Mathf.Abs(move.x) >= Mathf.Abs(move.y) ? new Vector2(Mathf.Sign(move.x), 0.0f) : new Vector2(0.0f, Mathf.Sign(move.y));

        // 触れているか（すき間が少しならよい）
        if(box.Distance(playerCollider).distance > 0.08f) { return false; }

        // 箱がキャラの進む向きの側にあるか
        Vector2 toBlock = (Vector2)transform.position - (Vector2)player.transform.position;
        return toBlock.sqrMagnitude > 0.0001f && Vector2.Dot(toBlock.normalized, direction) >= 0.6f;
    }

    /// <summary>
    /// 1 マス動かす（行き先がふさがっていたら動かない）
    /// </summary>
    /// <returns> 動いたか </returns>
    public bool TryMove(Vector2 direction)
    {
        if(moving || fallen) { return false; }

        Vector2 from = transform.position;
        Vector2 to = from + direction.normalized * cellSize;

        GimmickHole hole;
        if(!CanMoveTo(to, out hole)) { return false; }

        StartCoroutine(MoveRoutine(from, to, hole));
        return true;
    }

    private bool CanMoveTo(Vector2 to, out GimmickHole hole)
    {
        hole = null;
        Vector2 size = Vector2.Scale(box.size, transform.lossyScale) * 0.9f;
        Vector2 center = to + Vector2.Scale(box.offset, transform.lossyScale);

        foreach(Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0.0f))
        {
            if(hit == null || hit.isTrigger) { continue; }
            if(hit.transform.IsChildOf(transform)) { continue; }

            // 2人で押すときは、ついてくる相棒が行き先にいても押せる（相棒は押した人の後ろを歩く）
            if(requireBoth)
            {
                GameObject root = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
                if(root.CompareTag("Player")) { continue; }
            }

            GimmickHole h = hit.GetComponentInParent<GimmickHole>();
            if(h != null && !h.Filled) { hole = h; continue; }

            return false;    // 壁・箱・キャラ・敵
        }
        return true;
    }

    private IEnumerator MoveRoutine(Vector2 from, Vector2 to, GimmickHole hole)
    {
        moving = true;
        SeManager.PlaySe(pushSe, transform.position);

        float elapsed = 0.0f;
        while(elapsed < moveTime)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / moveTime)));
        }
        SetPosition(to);
        moving = false;

        if(save)
        {
            Save.SetPending(saveKey + "/X", to.x);
            Save.SetPending(saveKey + "/Y", to.y);
        }

        if(hole != null && !hole.Filled) { yield return FallInto(hole); }
    }

    /// <summary>
    /// 穴に落ちて埋める
    /// </summary>
    private IEnumerator FallInto(GimmickHole hole)
    {
        fallen = true;
        if(save) { Save.SetPending(saveKey + "/Fallen", true); }
        SeManager.PlaySe(fallSe, transform.position);
        hole.Fill();

        // 小さくなりながら沈む
        Vector3 scale = transform.localScale;
        float elapsed = 0.0f;
        while(elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = scale * Mathf.Lerp(1.0f, 0.6f, elapsed / 0.3f);
            yield return null;
        }
        gameObject.SetActive(false);
        transform.localScale = scale;
    }

    //=====================================================================
    // 元に戻す
    //=====================================================================

    /// <summary>
    /// 置いた位置に戻す（穴に落ちた箱はそのまま）
    /// </summary>
    public void ResetToHome()
    {
        if(fallen) { return; }

        StopAllCoroutines();
        moving = false;
        SetPosition(home);
        if(save)
        {
            Save.SetPending(saveKey + "/X", home.x);
            Save.SetPending(saveKey + "/Y", home.y);
        }
    }

    private void SetPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        if(rb != null) { rb.position = position; }
    }

    private static string ScenePath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent) { path = parent.name + "/" + path; }
        return path;
    }
}
