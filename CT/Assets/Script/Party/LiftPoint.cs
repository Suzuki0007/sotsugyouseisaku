/*********************************************************************/
// * \file   LiftPoint.cs
// * \brief  持ち上げる・飛び移る場所（シグルがノアを持ち上げて段差の上へ など）
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// 持ち上げる場所（踏み台）
/// </summary>
///
/// ・調べると、決めたキャラ（初期値：ノア）が「行き先」へ跳んで移る（段差の上・高い棚・向こう岸）
/// ・「相棒の手伝いが要る」なら、2人がそろっているときだけ（シグルがノアを持ち上げる）。手伝った相棒はその場で待つ
/// ・戻るときは、行き先側にもう1つ置き、行き先をこちらにして「相棒の手伝いが要る」をオフにする（一人で降りる）
/// ・移ったあとは、移ったキャラを操作する
public class LiftPoint : GimmickInteractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("移るキャラ")]
    [Tooltip("行き先へ移るキャラ。シグルがノアを持ち上げるなら ノア")]
    private PlayerCharacter mover = PlayerCharacter.Noah;

    [SerializeField, Label("行き先")]
    [Tooltip("移る先の位置（空のオブジェクト）")]
    private Transform destination;

    [SerializeField, Label("相棒の手伝いが要る")]
    [Tooltip("オン：もう1人がこの場所の近くにいるときだけ使える（持ち上げてもらう）。オフ：一人で使える（降りる・飛び降りる）")]
    private bool requireHelper = true;

    [SerializeField, Label("手伝う相棒の距離", 0.5f)]
    [Tooltip("もう1人がこの場所からこの距離（マス）以内にいるとき")]
    private float helperDistance = 2.0f;

    [SerializeField, Label("移る時間（秒）", 0.05f)]
    private float jumpTime = 0.5f;

    [SerializeField, Label("跳ぶ高さ", 0.0f)]
    [Tooltip("移るときに弧を描く高さ（見た目だけ）")]
    private float jumpHeight = 1.0f;

    [SerializeField, Label("移るときの SE")]
    private SeData jumpSe;

    [SerializeField, Label("移ったあとのイベント")]
    private EventSequence onMoved;

    [SerializeField, Label("手伝いがいないとき")]
    [Tooltip("例：「一人じゃ届かない……シグル、手伝って」。空なら「できない」音だけ")]
    private EventSequence needHelper;

    private bool moving;

    public Transform Destination => destination;

    //=====================================================================
    // 関数
    //=====================================================================

    protected override void OnInteract(GameObject user)
    {
        if(moving || destination == null) { return; }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return; }

        GameObject moverObject = PlayerCharacters.Get(mover);
        GameObject helper = moverObject == manager.Noah ? manager.Sigle : manager.Noah;
        if(moverObject == null) { return; }

        if(requireHelper)
        {
            // 2人とも、この場所の近くにいるとき
            bool moverNear = Vector2.Distance(moverObject.transform.position, transform.position) <= helperDistance;
            bool helperNear = helper != null && Vector2.Distance(helper.transform.position, transform.position) <= helperDistance;
            if(!moverNear || !helperNear)
            {
                Refuse(needHelper);
                return;
            }
        }
        else if(user != moverObject)
        {
            // 一人で使う場所は、移るキャラが調べたときだけ
            Refuse(needHelper);
            return;
        }

        StartCoroutine(Move(manager, moverObject));
    }

    private IEnumerator Move(PlayerSwitchManager manager, GameObject moverObject)
    {
        moving = true;

        // 相棒（手伝った方）はその場で待つ。先に待機にしてから切り替える（一緒のまま切り替えると位置が入れ替わるため）
        if(manager.Mode == PartyMode.Together && requireHelper) { manager.SetMode(PartyMode.Waiting); }
        if(manager.CurrentPlayer != moverObject) { manager.SwitchPlayer(); }

        PlayerMovement movement = moverObject.GetComponent<PlayerMovement>();
        Rigidbody2D body = moverObject.GetComponent<Rigidbody2D>();
        if(movement != null) { movement.Stop(); movement.SetControlEnabled(false); }

        // 当たり判定を切って、弧を描いて移る
        Collider2D[] colliders = moverObject.GetComponents<Collider2D>();
        foreach(Collider2D c in colliders) { c.enabled = false; }

        SeManager.PlaySe(jumpSe, moverObject.transform.position);
        Vector3 from = moverObject.transform.position;
        Vector3 to = new Vector3(destination.position.x, destination.position.y, from.z);
        float elapsed = 0.0f;
        while(elapsed < jumpTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpTime);
            Vector3 p = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * jumpHeight);
            moverObject.transform.position = p;
            if(body != null) { body.position = p; }
            yield return null;
        }
        moverObject.transform.position = to;
        if(body != null) { body.position = to; body.linearVelocity = Vector2.zero; }

        foreach(Collider2D c in colliders) { c.enabled = true; }
        if(movement != null) { movement.SetControlEnabled(manager.CurrentPlayer == moverObject); }

        PlayerHistory history = moverObject.GetComponent<PlayerHistory>();
        if(history != null) { history.ResetHistory(to); }

        if(onMoved != null) { onMoved.Play(gameObject); }
        moving = false;
    }

    private void OnDrawGizmos()
    {
        if(destination == null) { return; }
        Gizmos.color = new Color(0.4f, 1.0f, 0.6f, 0.9f);
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.3f);
    }
}
