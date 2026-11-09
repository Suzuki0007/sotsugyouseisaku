/*********************************************************************/
// * \file   CharacterBarrier.cs
// * \brief  キャラによって通れる・通れない壁（ノアだけ通れる狭い通路など）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キャラによって通れる壁
/// </summary>
///
/// ・当たり判定（Is Trigger オフの Collider2D）を、決めたキャラだけ通り抜けられるようにする
///   例：ノアだけ通れる狭い通路（シグルにとっては壁）／ シグルだけ通れる重い扉の隙間
/// ・「一緒」のときに通れるキャラが入ると、相棒（通れない方）は入口で自動で待つ（待機になる）
/// ・敵・押せる箱にとっては、ふつうの壁
public class CharacterBarrier : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("通れるキャラ")]
    [Tooltip("ノアだけ：狭い通路 ／ シグルだけ：シグルにしか抜けられない所")]
    private CharacterRestriction passable = CharacterRestriction.NoahOnly;

    [SerializeField, Label("入ったら相棒を待たせる")]
    [Tooltip("オン：一緒のときに通れるキャラが入ると、相棒はその場で待つ（呼び戻すには 待て／ついてこい のキー。通路を出てから）")]
    private bool autoWait = true;

    [SerializeField, Label("相棒を待たせたときの SE")]
    private SeData waitSe;

    private Collider2D[] colliders;
    private GameObject appliedNoah;
    private GameObject appliedSigle;
    private float reapplyTimer;

    public CharacterRestriction Passable => passable;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>();
    }

    private void Update()
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return; }

        // プレイヤーはシーンが始まってから作られるので、見つかったら当たらないようにする
        // （キャラの当たり判定は切り替えで有効・無効が変わるので、ときどきかけ直す）
        reapplyTimer -= Time.deltaTime;
        if(manager.Noah != appliedNoah || manager.Sigle != appliedSigle || reapplyTimer <= 0.0f)
        {
            reapplyTimer = 0.5f;
            appliedNoah = manager.Noah;
            appliedSigle = manager.Sigle;
            Apply(manager.Noah, passable.Allows(PlayerCharacter.Noah));
            Apply(manager.Sigle, passable.Allows(PlayerCharacter.Sigle));
        }

        // 通れるキャラが入ったら、相棒を待たせる
        if(autoWait && manager.IsTogether && manager.CurrentPlayer != null && passable.Allows(manager.CurrentPlayer) && passable != CharacterRestriction.Both)
        {
            if(Contains(manager.CurrentPlayer.transform.position))
            {
                manager.SetMode(PartyMode.Waiting);
                SeManager.PlaySe(waitSe, transform.position);
            }
        }

        // 通れないキャラが触れていたら、通れるキャラの目印を出す
        GameObject current = manager.CurrentPlayer;
        if(current != null && !passable.Allows(current) && IsNear(current)) { PartyHint.Request(transform, passable, current.transform.position); }
    }

    private void Apply(GameObject character, bool pass)
    {
        if(character == null) { return; }
        foreach(Collider2D body in character.GetComponentsInChildren<Collider2D>())
        {
            if(body.isTrigger) { continue; }
            foreach(Collider2D wall in colliders)
            {
                if(wall != null && !wall.isTrigger) { Physics2D.IgnoreCollision(wall, body, pass); }
            }
        }
    }

    private bool Contains(Vector2 point)
    {
        foreach(Collider2D c in colliders) { if(c != null && c.enabled && !c.isTrigger && c.OverlapPoint(point)) { return true; } }
        return false;
    }

    private bool IsNear(GameObject character)
    {
        Collider2D body = character.GetComponent<Collider2D>();
        if(body == null) { return false; }
        foreach(Collider2D c in colliders)
        {
            if(c != null && c.enabled && !c.isTrigger && c.Distance(body).distance < 0.3f) { return true; }
        }
        return false;
    }
}
