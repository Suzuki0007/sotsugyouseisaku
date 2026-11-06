/*********************************************************************/
// * \file   FootstepSe.cs
// * \brief  歩行時に足音SEを鳴らすクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// キャラクターが一定距離進むごとに足音SEを鳴らす
/// 実際の移動量で判定するので、操作中のキャラにも追従中のキャラにも使える
/// (移動速度が変わっても足音の間隔が自然に合う)
///
/// ノア・シグルのプレハブにそれぞれ付けて、SeData を設定する
/// 足音の SeData は、複数の音を入れてピッチを少し揺らす(例: 0.9〜1.1)と単調になりにくい
/// </summary>
public class FootstepSe : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("SE")]

    [SerializeField, Tooltip("操作中に鳴らす足音")]
    private SeData footstepSe;

    [SerializeField, Tooltip("追従中(操作していないとき)に鳴らす足音。空なら追従中は鳴らさない\n" +
                             "操作中と同じ音を小さい音量で作ると、2人で歩いている感じが出る")]
    private SeData followerFootstepSe;

    [Header("間隔")]

    [SerializeField, Tooltip("この距離(ワールド単位)進むごとに1歩鳴らす")]
    private float stepDistance = 0.8f;

    [SerializeField, Tooltip("歩き始めた瞬間に1歩目を鳴らす")]
    private bool playOnStart = true;

    [Header("判定")]

    [SerializeField, Tooltip("1フレームの移動量がこれ以上なら瞬間移動とみなして無視する (キャラ切り替え・ワープなど)")]
    private float teleportThreshold = 1.0f;

    [SerializeField, Tooltip("この時間(秒)動かなければ止まったとみなす")]
    private float stopDelay = 0.1f;

    private PlayerMovement playerMovement;

    private Vector3 lastPosition;
    private float distanceSinceStep;
    private float stillTime;
    private bool isWalking;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        float moved = Vector2.Distance(position, lastPosition);
        lastPosition = position;

        if(Time.deltaTime <= 0.0f) { return; }

        // 瞬間移動は歩行に数えない
        if(moved >= teleportThreshold)
        {
            ResetState();
            return;
        }

        // 止まっている
        if(moved <= 0.0001f)
        {
            stillTime += Time.deltaTime;
            if(stillTime >= stopDelay) { isWalking = false; }
            return;
        }

        stillTime = 0.0f;

        // 歩き始め
        if(!isWalking)
        {
            isWalking = true;
            distanceSinceStep = playOnStart ? stepDistance : 0.0f;
        }

        distanceSinceStep += moved;

        if(distanceSinceStep >= stepDistance)
        {
            distanceSinceStep = Mathf.Repeat(distanceSinceStep, Mathf.Max(0.01f, stepDistance));
            PlayStep();
        }
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private void PlayStep()
    {
        bool isControlled = playerMovement == null || playerMovement.CanControl;
        SeData se = isControlled ? footstepSe : followerFootstepSe;

        SeManager.PlaySe(se, transform.position);
    }

    private void ResetState()
    {
        lastPosition = transform.position;
        distanceSinceStep = 0.0f;
        stillTime = stopDelay;
        isWalking = false;
    }
}
