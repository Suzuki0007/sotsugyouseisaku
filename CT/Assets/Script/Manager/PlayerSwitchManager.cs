/*********************************************************************/
// * \file   PlayerSwitchManager.cs
// * \brief  プレイヤー切り替え管理クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2人の状態
/// </summary>
public enum PartyMode
{
    [InspectorName("一緒（相棒がついてくる。体力は共有）")]
    Together = 0,

    [InspectorName("待機（相棒がその場で待つ。体力は別々）")]
    Waiting = 1,

    [InspectorName("別行動（離れ離れ。体力は別々）")]
    Separated = 2,
}

/// <summary>
/// プレイヤー切り替え管理クラス
/// </summary>
///
/// ・2人の状態（PartyMode）で、切り替えたときの動きと体力の扱いが変わる
///   - 一緒：切り替えると位置が入れ替わり、操作していない方がついてくる。体力は共有（シグルがノアを守っている＝シグルの体力から減る）
///   - 待機・別行動：切り替えても位置はそのまま（カメラが相手へ移る）。操作していない方はその場にいる。体力は別々
public class PlayerSwitchManager : Singleton<PlayerSwitchManager>
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("プレイヤーキャラクター")]

    [SerializeField]
    private GameObject noah;

    [SerializeField]
    private GameObject sigle;

    [Header("カメラ")]

    [SerializeField]
    private FocusCamera focusCamera;

    [Header("設定")]

    [SerializeField]
    private Key switchKey = Key.C;

    [Header("SE")]

    [SerializeField, Tooltip("キャラクターを切り替えたとき")]
    private SeData switchSe;

    [Header("2人の状態")]

    [SerializeField, Label("相棒が歩いて合流する距離", 0.0f)]
    [Tooltip("「一緒」に戻したとき、相棒がこの距離より近ければ歩いて追いつく。遠ければ、操作しているキャラの位置へ移る")]
    private float rejoinWalkDistance = 10.0f;

    private PartyMode mode = PartyMode.Together;

    private Health noahHealth;
    private Health sigleHealth;

    /// <summary> 2人の状態が変わったとき </summary>
    public event System.Action<PartyMode> ModeChanged;

    /// <summary> 操作するキャラが変わったとき（引数：新しく操作するキャラ） </summary>
    public event System.Action<GameObject> Switched;

    private PlayerMovement noahMovement;
    private PlayerMovement sigleMovement;

    private PlayerHistory noahHistory;
    private PlayerHistory sigleHistory;

    private PlayerFollower noahFollower;
    private PlayerFollower sigleFollower;

    // 現在操作しているキャラクター
    private GameObject currentPlayer;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public GameObject CurrentPlayer => currentPlayer;

    public GameObject Noah => noah;

    public GameObject Sigle => sigle;

    /// <summary> 2人の状態 </summary>
    public PartyMode Mode => mode;

    /// <summary> 一緒にいるか（体力を共有しているか） </summary>
    public bool IsTogether => mode == PartyMode.Together;

    /// <summary> 操作していない方のキャラ </summary>
    public GameObject Partner => currentPlayer == noah ? sigle : noah;

    /// <summary>
    /// 今の体力（ライフ表示・ゲームオーバー・条件で見るもの）
    /// 一緒：シグルの体力（2人で共有）／ 待機・別行動：操作しているキャラの体力
    /// </summary>
    public Health PartyHealth
    {
        get
        {
            if(mode == PartyMode.Together) { return sigleHealth; }
            if(currentPlayer == null) { return null; }
            return currentPlayer == noah ? noahHealth : sigleHealth;
        }
    }

    /// <summary> キャラの体力（一緒のときも、そのキャラ自身の値を見るとき用） </summary>
    public Health HealthOf(GameObject character) => character == noah ? noahHealth : (character == sigle ? sigleHealth : null);

    //=====================================================================
    // ワープ（デバッグ用）
    //=====================================================================

    /// <summary>
    /// 2人を指定した位置に移動させる（デバッグウィンドウから使う）
    /// </summary>
    public void Warp(Vector3 noahPosition, Vector3 siglePosition)
    {
        if(noah == null || sigle == null) { return; }

        MoveTo(noah, noahPosition);
        MoveTo(sigle, siglePosition);

        if(noahHistory != null)  { noahHistory.ResetHistory(noah.transform.position); }
        if(sigleHistory != null) { sigleHistory.ResetHistory(sigle.transform.position); }
    }

    private static void MoveTo(GameObject target, Vector3 position)
    {
        position.z = target.transform.position.z;
        target.transform.position = position;

        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if(body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    /// <summary>
    /// プレイヤーの初期化関数
    /// </summary>
    /// <param name="noahObject"></param>
    /// <param name="sigleObject"></param>
    public void InitializePlayers(GameObject noahObject, GameObject sigleObject)
    {
        if(noahObject == null || sigleObject == null)
        { Debug.LogError("NoahまたはSigleのプレハブが設定されていません", this); return; }

        if(noahObject == sigleObject)
        { Debug.LogError("ノアとシグルに同じオブジェクトが設定されています", this); return; }

        noah = noahObject;
        sigle = sigleObject;

        // 初期参照設定、失敗した場合はスクリプトを無効化
        if(!InitializeReferences()) { enabled = false; return; }

        currentPlayer = noah;

        noahMovement.SetControlEnabled(true);
        sigleMovement.SetControlEnabled(false);

        noahFollower.SetTarget(sigle);
        sigleFollower.SetTarget(noah);

        focusCamera.SetTarget(noah.transform);

        mode = PartyMode.Together;
        ApplyFollow();
        ApplyHealthSharing();
    }

    /// <summary>
    /// 初期参照設定
    /// </summary>
    /// <returns></returns>
    private bool InitializeReferences()
    {
        if(noah == null || sigle == null) { Debug.LogError("NoahまたはSigleが設定されていません"); return false; }
        if(noah == sigle) { Debug.LogError("NoahとSigleに同じオブジェクトが設定されています"); return false; }
        if(focusCamera == null) { Debug.LogError("FocusCameraが設定されていません"); return false; }

        noahMovement = noah.GetComponent<PlayerMovement>();
        sigleMovement = sigle.GetComponent<PlayerMovement>();
        if(noahMovement == null || sigleMovement == null) { Debug.LogError("NoahまたはSigleに、PlayerMovementが付いていません"); return false; }

        noahHistory = noah.GetComponent<PlayerHistory>();
        sigleHistory = sigle.GetComponent<PlayerHistory>();
        if(noahHistory == null || sigleHistory == null) { Debug.LogError("NoahまたはSigleにPlayerHistoryが付いていません"); return false; }

        noahFollower = noah.GetComponent<PlayerFollower>();
        sigleFollower = sigle.GetComponent<PlayerFollower>();
        if(noahFollower == null || sigleFollower == null) { Debug.LogError("NoahまたはSigleにPlayerFollowerが付いていません"); return false; }

        noahHealth = noah.GetComponent<Health>();
        sigleHealth = sigle.GetComponent<Health>();
        if(sigleHealth == null) { sigleHealth = sigle.AddComponent<Health>(); }
        if(noahHealth == null)  { Debug.LogError("NoahにHealthが付いていません"); return false; }

        return true;
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(Keyboard.current == null) { return; }

        // 会話中・シーン遷移中はキャラクターを切り替えない
        if(InputBlock.IsBlocked) { return; }

        if(Keyboard.current[switchKey].wasPressedThisFrame)
        {
            SwitchPlayer();
        }
    }

    //=====================================================================
    // プレイヤー切り替え関数
    //=====================================================================

    /// <summary>
    /// プレイヤー切り替え関数
    /// </summary>
    public void SwitchPlayer()
    {
        if(currentPlayer == noah)
        {
            SwitchTo(sigle, noah);
        }
        else
        {
            SwitchTo(noah, sigle);
        }
    }

    /// <summary>
    /// プレイヤー切り替え処理
    /// </summary>
    /// <param name="newPlayer"> 切り替え後のプレイヤー </param>
    /// <param name="oldPlayer"> 切り替え前のプレイヤー </param>
    private void SwitchTo(GameObject newPlayer, GameObject oldPlayer)
    {
        // 一緒のときだけ位置を入れ替える（待機・別行動のときは、それぞれの場所のまま。カメラが相手へ移る）
        if(mode == PartyMode.Together)
        {
            // 2人の現在位置を保存
            Vector3 oldPlayerPosition = oldPlayer.transform.position;
            Vector3 newPlayerPosition = newPlayer.transform.position;

            // 位置を交換
            oldPlayer.transform.position = newPlayerPosition;
            newPlayer.transform.position = oldPlayerPosition;
        }

        // 今の座標を履歴の開始位置にする
        noahHistory.ResetHistory(noah.transform.position);
        sigleHistory.ResetHistory(sigle.transform.position);

        // 操作権を交換
        noahMovement.SetControlEnabled(newPlayer == noah);
        sigleMovement.SetControlEnabled(newPlayer == sigle);

        // 現在の操作キャラクターを更新
        currentPlayer = newPlayer;

        // 追従対象を変更
        noahFollower.SetTarget(sigle);
        sigleFollower.SetTarget(noah);

        // カメラの追従先を変更
        focusCamera.SetTarget(currentPlayer.transform);

        ApplyFollow();

        SeManager.PlaySe(switchSe, currentPlayer.transform.position);
        Switched?.Invoke(currentPlayer);
    }

    //=====================================================================
    // 2人の状態
    //=====================================================================

    /// <summary>
    /// 2人の状態を変える（イベントの部品「2人の状態を変える」・デバッグウィンドウから）
    /// </summary>
    public void SetMode(PartyMode newMode)
    {
        if(newMode == mode) { return; }
        if(noah == null || sigle == null || currentPlayer == null) { mode = newMode; return; }

        PartyMode oldMode = mode;
        mode = newMode;

        // 一緒に戻ったら、相棒が合流する（近ければ歩いて、遠ければ操作しているキャラの位置へ）
        if(newMode == PartyMode.Together && oldMode != PartyMode.Together)
        {
            GameObject partner = Partner;
            PlayerHistory currentHistory = currentPlayer == noah ? noahHistory : sigleHistory;
            PlayerHistory partnerHistory = partner == noah ? noahHistory : sigleHistory;
            PlayerFollower partnerFollower = partner == noah ? noahFollower : sigleFollower;

            float distance = Vector2.Distance(partner.transform.position, currentPlayer.transform.position);
            if(distance <= rejoinWalkDistance)
            {
                currentHistory.SeedPath(partner.transform.position, currentPlayer.transform.position, partnerFollower.FollowFrames);
            }
            else
            {
                MoveTo(partner, currentPlayer.transform.position);
                currentHistory.ResetHistory(currentPlayer.transform.position);
            }
            partnerHistory.ResetHistory(partner.transform.position);
        }

        ApplyFollow();
        ApplyHealthSharing();
        ModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// 操作していない方がついていくか（一緒のときだけ）
    /// </summary>
    private void ApplyFollow()
    {
        if(noahFollower == null || sigleFollower == null) { return; }

        bool follow = mode == PartyMode.Together;
        noahFollower.Hold = !follow;
        sigleFollower.Hold = !follow;
    }

    /// <summary>
    /// 体力の共有を状態に合わせる
    /// ・一緒：ノアが受けたダメージ・回復はシグルの体力へ（シグルがノアを守っている）
    /// ・待機・別行動：それぞれ自分の体力（ノアの体力は、離れたときの値から減っていく）
    /// </summary>
    private void ApplyHealthSharing()
    {
        if(noahHealth == null || sigleHealth == null) { return; }

        sigleHealth.ShareWith(null);
        noahHealth.ShareWith(mode == PartyMode.Together ? sigleHealth : null);
    }
}