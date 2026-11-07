/*********************************************************************/
// * \file   CharacterAnimLink.cs
// * \brief  移動・体力に合わせて SpriteAnimator の動きを自動で切り替える
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// キャラクターに付けると、コードを書かずに動きが切り替わる
///   ・動いている → Walk / 止まっている → Idle (向きも移動方向に合わせる)
///   ・ダメージを受けた → Damage を1回流して元に戻る (Health が付いている場合)
///   ・死亡したら切り替えをやめる (死亡アニメは EnemyDead / NoahDead が流す)
///
/// 動いているかどうかは「実際に位置が変わったか」で判断する
/// (追従で動いている仲間や、ほかのスクリプトで動かしている敵でも正しく切り替わる)
/// 操作中のキャラクターは、壁に向かって押しているときも入力の方向を向く
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteAnimator))]
[AddComponentMenu("Animation/Character Anim Link")]
public class CharacterAnimLink : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("止まっているときの動きの名前")]
    [SerializeField]
    private string idleClip = "Idle";

    [Tooltip("動いているときの動きの名前 (無ければ止まっているときの動きのまま)")]
    [SerializeField]
    private string walkClip = "Walk";

    [Tooltip("ダメージを受けたときに1回流す動きの名前 (無ければ何もしない)")]
    [SerializeField]
    private string damageClip = "Damage";

    [Tooltip("この速さ (1秒あたりの距離) より速く動いていたら「動いている」とみなす")]
    [SerializeField, Min(0.0f)]
    private float moveThreshold = 0.1f;

    [Tooltip("止まってから Idle に戻るまでの秒数 (歩きと待機がチラつかないように)")]
    [SerializeField, Min(0.0f)]
    private float stopDelay = 0.1f;

    // 1フレームでこれ以上動いたら、ワープ (位置の入れ替えなど) とみなして無視する
    private const float TeleportDistance = 1.0f;

    private SpriteAnimator anim;
    private CharacterMovement movement;
    private PlayerMovement playerMovement;
    private Health health;

    private Vector3 lastPosition;
    private float movingTimer;
    private string actionClip;// Damage など、1回流している最中の動き

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 動いているとみなしているか </summary>
    public bool IsMoving => movingTimer > 0.0f;

    //=====================================================================
    // Unity
    //=====================================================================

    private void Awake()
    {
        anim = GetComponent<SpriteAnimator>();
        movement = GetComponent<CharacterMovement>();
        playerMovement = movement as PlayerMovement;
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        movingTimer = 0.0f;
        actionClip = null;
        if(health != null) { health.OnDamaged += OnDamaged; }
    }

    private void OnDisable()
    {
        if(health != null) { health.OnDamaged -= OnDamaged; }
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        Vector2 delta = position - lastPosition;
        lastPosition = position;

        // 死亡後は、死亡アニメの邪魔をしない
        if(health != null && health.IsDead) { return; }

        float dt = Time.deltaTime;
        bool teleported = delta.sqrMagnitude > TeleportDistance * TeleportDistance;
        bool moved = !teleported && dt > 0.0f && delta.magnitude / dt > moveThreshold;

        if(moved) { movingTimer = stopDelay; }
        else      { movingTimer -= dt; }

        // 向き : 操作中 (または敵などで移動方向が決まっている) ならその方向、そうでなければ実際に動いた方向
        Vector2 intent = Vector2.zero;
        if(movement != null && (playerMovement == null || playerMovement.CanControl))
        {
            intent = movement.MoveDirection;
        }

        if(intent != Vector2.zero) { anim.SetDirection(intent); }
        else if(moved)             { anim.SetDirection(delta); }

        // Damage などを流している間は、流し終わるまで待つ
        if(actionClip != null)
        {
            if(anim.CurrentClipName == actionClip && !anim.IsCycleComplete) { return; }
            actionClip = null;
        }

        string clip = IsMoving && anim.Has(walkClip) ? walkClip : idleClip;
        anim.Play(clip);
    }

    //=====================================================================
    // 外から呼ぶ関数
    //=====================================================================

    /// <summary>
    /// 1回だけの動き (攻撃など) を流す。流し終わったら Idle / Walk に戻る
    /// その名前の動きが無ければ false
    /// </summary>
    public bool PlayAction(string clipName)
    {
        if(!anim.Play(clipName, true)) { return false; }
        actionClip = anim.CurrentClipName;
        return true;
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private void OnDamaged(int damage)
    {
        // このダメージで死んだときは、死亡アニメを優先する
        if(health.IsDead) { return; }
        if(!string.IsNullOrEmpty(damageClip)) { PlayAction(damageClip); }
    }
}
