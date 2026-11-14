/*********************************************************************/
// * \file   FootstepSe.cs
// * \brief  歩行時に足音SEを鳴らすクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// キャラクターが歩くと足音SEを鳴らす
/// </summary>
///
/// ・ノア・シグルのプレハブに付いている。操作中と、ついてきているときで別の音を使える
/// ・鳴らし方「一歩ずつ」：一定の距離を進むごとに1歩の音を鳴らす（音は1歩分の短い音にする。複数入れてピッチを少し揺らすと単調になりにくい）
/// ・鳴らし方「歩いている間ループ」：歩き始めたら音をループで鳴らし、止まったら止める（歩き続ける音を録った長い音のとき。SeData の「ループ」をオンにする）
/// ・実際の移動量で判定するので、速さが変わっても自然に合う。ワープ・キャラ切り替えは歩いたことにしない
public class FootstepSe : MonoBehaviour
{
    public enum PlayMode
    {
        [InspectorName("一歩ずつ")]
        PerStep = 0,

        [InspectorName("歩いている間ループ")]
        LoopWhileWalking = 1,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("鳴らし方")]
    [Tooltip("一歩ずつ：1歩分の短い音を、決めた距離ごとに鳴らす\n歩いている間ループ：歩き続ける音（長い音）を、歩いている間だけループで鳴らす（SeData の「ループ」をオンに）")]
    private PlayMode playMode = PlayMode.PerStep;

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

    // 「歩いている間ループ」で鳴らしている音
    private SeHandle loopHandle;
    private SeData loopData;

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

    private void OnDisable()
    {
        StopLoop();
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
            if(stillTime >= stopDelay && isWalking)
            {
                isWalking = false;
                StopLoop();
            }
            return;
        }

        stillTime = 0.0f;

        // 歩き始め
        if(!isWalking)
        {
            isWalking = true;
            distanceSinceStep = playOnStart ? stepDistance : 0.0f;
        }

        if(playMode == PlayMode.LoopWhileWalking)
        {
            UpdateLoop();
            return;
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
        SeManager.PlaySe(CurrentSe(), transform.position);
    }

    /// <summary> 操作中か、ついてきているかで使う音 </summary>
    private SeData CurrentSe()
    {
        bool isControlled = playerMovement == null || playerMovement.CanControl;
        return isControlled ? footstepSe : followerFootstepSe;
    }

    /// <summary>
    /// 歩いている間のループ：鳴らしていなければ鳴らす。操作中／ついてくるが切り替わったら音を替える
    /// </summary>
    private void UpdateLoop()
    {
        SeData se = CurrentSe();
        if(se == loopData && loopHandle.IsValid) { return; }

        StopLoop();
        if(se == null) { return; }

        loopHandle = SeManager.PlaySe(se, transform);
        loopData = se;
    }

    private void StopLoop()
    {
        if(loopHandle.IsValid) { SeManager.StopSe(loopHandle); }
        loopHandle = SeHandle.None;
        loopData = null;
    }

    private void ResetState()
    {
        lastPosition = transform.position;
        distanceSinceStep = 0.0f;
        stillTime = stopDelay;
        isWalking = false;
        StopLoop();
    }
}
