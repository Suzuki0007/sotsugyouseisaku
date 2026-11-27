/*********************************************************************/
// * \file   FootprintMaker.cs
// * \brief  キャラの足が着いたときに足跡を置く（足跡が付くシーンで、ノア・シグルに自動で付く）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 足跡を付ける（キャラごと）
/// </summary>
///
/// ・歩くアニメのコマの合図「footprint」が来たら、足元に足跡を置く（左右の足で交互に少しずらす・歩く向きに回す）
/// ・合図のないアニメ（シグルなど）は、決めた距離を歩くごとに置く
/// ・プレイヤーが止められている間（イベント・会話）は置かない
public class FootprintMaker : MonoBehaviour
{
    private SpriteAnimator animator;
    private SpriteRenderer body;

    private Vector2 lastPosition;
    private Vector2 direction = Vector2.down;
    private float distanceSinceStep;
    private float lastMessageTime = -100.0f;
    private float lastMoveTime = -100.0f;
    private bool leftFoot;

    // 合図が来ていれば、合図で置く（この秒数合図がなければ、距離で置く）
    private const float MessageTimeout = 1.5f;

    // 1フレームでこれ以上動いたら、ワープとみなす
    private const float TeleportDistance = 1.0f;

    //=====================================================================
    // 初期化
    //=====================================================================

    private void OnEnable()
    {
        animator = GetComponentInChildren<SpriteAnimator>();
        body = GetComponentInChildren<SpriteRenderer>();
        if(animator != null) { animator.OnMessage -= OnAnimMessage; animator.OnMessage += OnAnimMessage; }
        lastPosition = transform.position;
        distanceSinceStep = 0.0f;
    }

    private void OnDisable()
    {
        if(animator != null) { animator.OnMessage -= OnAnimMessage; }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void LateUpdate()
    {
        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        lastPosition = position;

        float moved = delta.magnitude;
        if(moved >= TeleportDistance) { distanceSinceStep = 0.0f; return; }
        if(moved <= 0.0001f) { return; }

        direction = delta / moved;
        lastMoveTime = Time.time;

        // 合図が来ているキャラは合図だけで置く
        if(Time.time - lastMessageTime < MessageTimeout) { distanceSinceStep = 0.0f; return; }

        Footprints prints = Footprints.Active;
        if(prints == null) { return; }

        distanceSinceStep += moved;
        if(distanceSinceStep >= prints.Settings.stepDistance)
        {
            distanceSinceStep = 0.0f;
            Place(prints);
        }
    }

    private void OnAnimMessage(SpriteAnimator anim, string message)
    {
        if(message != FootprintSettings.Message) { return; }
        lastMessageTime = Time.time;

        // 止まっているのに歩くアニメが残っているときは置かない
        if(Time.time - lastMoveTime > 0.2f) { return; }

        Footprints prints = Footprints.Active;
        if(prints != null) { Place(prints); }
    }

    //=====================================================================
    // 足跡を置く
    //=====================================================================

    private void Place(Footprints prints)
    {
        // イベント・会話の間は置かない（小舟で進む場面・演出で動かされているときなど）
        if(InputBlock.IsBlocked) { return; }
        if(body != null && !body.enabled) { return; }

        FootprintSettings s = prints.Settings;
        Vector2 side = new Vector2(direction.y, -direction.x) * (leftFoot ? -1.0f : 1.0f) * s.stride * 0.5f;
        leftFoot = !leftFoot;

        Vector2 at = (Vector2)transform.position + s.footOffset + side;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90.0f;
        prints.Place(at, angle, body);
    }
}
