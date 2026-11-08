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
    [Tooltip("イベントで「一緒」に戻したとき、道があってこの距離より近ければ歩いて追いつく。遠い・道がなければ、操作しているキャラの位置へ移る")]
    private float rejoinWalkDistance = 10.0f;

    [Header("離れているとき")]

    [SerializeField, Label("切り替えの演出")]
    [Tooltip("待機・別行動のときに C で切り替えたときのカメラ：すぐ／スライド（なめらかに移る）／暗転（別のエリアにいるとき向き）")]
    private ApartSwitchStyle apartSwitchStyle = ApartSwitchStyle.Slide;

    [SerializeField, Label("敵が狙う相手")]
    [Tooltip("待機・別行動のときに敵が狙う相手：操作しているキャラだけ（待っている相棒は狙われない）／近い方（待っている相棒も狙われる）")]
    private ApartEnemyTarget apartEnemyTarget = ApartEnemyTarget.Current;

    public enum ApartSwitchStyle
    {
        [InspectorName("すぐ")]
        Instant = 0,

        [InspectorName("スライド（なめらかに移る）")]
        Slide = 1,

        [InspectorName("暗転")]
        Fade = 2,
    }

    public enum ApartEnemyTarget
    {
        [InspectorName("操作しているキャラだけ")]
        Current = 0,

        [InspectorName("近い方（待っている相棒も狙われる）")]
        Nearest = 1,
    }

    /// <summary>
    /// 敵が狙う相手（敵の位置から）
    /// ・一緒のとき・「操作しているキャラだけ」のときは操作しているキャラ
    /// ・離れていて「近い方」のときは、ノアとシグルの近い方
    /// </summary>
    public GameObject EnemyTargetFrom(Vector2 position)
    {
        if(currentPlayer == null) { return null; }
        if(mode == PartyMode.Together || apartEnemyTarget == ApartEnemyTarget.Current) { return currentPlayer; }

        GameObject partner = Partner;
        if(partner == null) { return currentPlayer; }

        float toCurrent = Vector2.Distance(position, currentPlayer.transform.position);
        float toPartner = Vector2.Distance(position, partner.transform.position);
        return toPartner < toCurrent ? partner : currentPlayer;
    }

    [Header("待て／ついてこい")]

    [SerializeField, Label("待て／ついてこいのキー")]
    [Tooltip("押すと、相棒をその場で待たせる（待機）／呼び戻す（一緒）。別行動のときは使えない")]
    private Key waitKey = Key.F;

    [SerializeField, Label("待たせたときの SE")]
    private SeData waitSe;

    [SerializeField, Label("呼び戻したときの SE")]
    private SeData followSe;

    [SerializeField, Label("相棒が来られないときの SE")]
    [Tooltip("呼んでも道がない（狭い通路の向こう・壁の向こう）・使えないときに鳴らす")]
    private SeData cannotSe;

    [SerializeField, Label("相棒が来られないときのイベント")]
    [Tooltip("例：「そっちには行けない……」の会話。空なら音だけ")]
    private EventSequence cannotFollow;

    /// <summary>
    /// 待て／ついてこいのキーを使えなくする（イベントの部品「2人の状態を変える」から。追跡中・会話の流れなど）
    /// </summary>
    public static bool WaitCommandLocked { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { WaitCommandLocked = false; }

    private PartyMode mode = PartyMode.Together;
    private readonly System.Collections.Generic.List<Vector2> route = new System.Collections.Generic.List<Vector2>();

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

        if(waitKey != Key.None && Keyboard.current[waitKey].wasPressedThisFrame)
        {
            ToggleWait();
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
        GameObject next = currentPlayer == noah ? sigle : noah;
        GameObject prev = currentPlayer == noah ? noah : sigle;

        // 離れているときに「暗転」なら、暗転している間に切り替える
        if(mode != PartyMode.Together && apartSwitchStyle == ApartSwitchStyle.Fade && !SceneFader.IsBusy)
        {
            SceneFader.Instance.FadeThrough(() => SwitchTo(next, prev));
            return;
        }

        SwitchTo(next, prev);
    }

    /// <summary>
    /// プレイヤー切り替え処理
    /// </summary>
    /// <param name="newPlayer"> 切り替え後のプレイヤー </param>
    /// <param name="oldPlayer"> 切り替え前のプレイヤー </param>
    /// <summary>
    /// 演出・SE なしで、操作するキャラを決める（セーブからの復元。離れているときに使う）
    /// </summary>
    public void SelectImmediate(GameObject target)
    {
        if(target == null || target == currentPlayer) { return; }
        SwitchTo(target, currentPlayer, false);
        focusCamera.SetTarget(currentPlayer.transform, false);
    }

    private void SwitchTo(GameObject newPlayer, GameObject oldPlayer, bool playSe = true)
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

        // カメラの追従先を変更（離れているときに「スライド」なら、なめらかに移る）
        focusCamera.SetTarget(currentPlayer.transform, mode != PartyMode.Together && apartSwitchStyle == ApartSwitchStyle.Slide);

        ApplyFollow();

        if(playSe) { SeManager.PlaySe(switchSe, currentPlayer.transform.position); }
        Switched?.Invoke(currentPlayer);
    }

    //=====================================================================
    // 2人の状態
    //=====================================================================

    /// <summary>
    /// 待て／ついてこい（キーから）
    /// ・一緒 → 待機（相棒がその場で待つ）
    /// ・待機 → 一緒（相棒が歩いて合流する。道がなければ来られない）
    /// ・別行動のとき・使えなくしているときは使えない
    /// </summary>
    /// <returns> 変わったか </returns>
    public bool ToggleWait()
    {
        if(noah == null || sigle == null || currentPlayer == null) { return false; }

        if(WaitCommandLocked || mode == PartyMode.Separated)
        {
            RefuseFollow();
            return false;
        }

        if(mode == PartyMode.Together)
        {
            SetMode(PartyMode.Waiting);
            SeManager.PlaySe(waitSe, Partner.transform.position);
            return true;
        }

        if(!TrySetMode(PartyMode.Together, true))
        {
            RefuseFollow();
            return false;
        }
        SeManager.PlaySe(followSe, Partner.transform.position);
        return true;
    }

    private void RefuseFollow()
    {
        if(cannotFollow != null) { cannotFollow.Play(gameObject); }
        else                     { SeManager.PlaySe(cannotSe, currentPlayer != null ? currentPlayer.transform.position : transform.position); }
    }

    /// <summary>
    /// 相棒が今の場所から操作キャラのところまで歩いて来られるか
    /// </summary>
    public bool CanPartnerReach()
    {
        if(noah == null || sigle == null || currentPlayer == null) { return false; }
        return PartyRoute.Find(Partner, Partner.transform.position, currentPlayer.transform.position, route);
    }

    /// <summary>
    /// 2人の状態を変える（イベントの部品「2人の状態を変える」・デバッグウィンドウから）
    /// 「一緒」に戻すとき、道がなければ相棒は操作キャラの位置へ移る
    /// </summary>
    public void SetMode(PartyMode newMode)
    {
        TrySetMode(newMode, false);
    }

    /// <summary>
    /// 2人の状態を変える
    /// </summary>
    /// <param name="requireRoute"> 「一緒」に戻すとき、相棒が歩いて来られないなら変えない（待て／ついてこいのキー） </param>
    /// <returns> 変わったか </returns>
    public bool TrySetMode(PartyMode newMode, bool requireRoute)
    {
        if(newMode == mode) { return false; }
        if(noah == null || sigle == null || currentPlayer == null) { mode = newMode; return true; }

        // 一緒に戻ったら、相棒が合流する（道があれば歩いて、なければ操作しているキャラの位置へ）
        if(newMode == PartyMode.Together)
        {
            GameObject partner = Partner;
            PlayerHistory currentHistory = currentPlayer == noah ? noahHistory : sigleHistory;
            PlayerHistory partnerHistory = partner == noah ? noahHistory : sigleHistory;
            PlayerFollower partnerFollower = partner == noah ? noahFollower : sigleFollower;

            Vector2 from = partner.transform.position;
            Vector2 to = currentPlayer.transform.position;
            bool hasRoute = PartyRoute.Find(partner, from, to, route);
            if(requireRoute && !hasRoute) { return false; }

            if(hasRoute && (requireRoute || RouteLength() <= rejoinWalkDistance))
            {
                currentHistory.SeedPolyline(route, partnerFollower.FollowFrames);
            }
            else
            {
                MoveTo(partner, currentPlayer.transform.position);
                currentHistory.ResetHistory(currentPlayer.transform.position);
            }
            partnerHistory.ResetHistory(partner.transform.position);
        }

        mode = newMode;

        ApplyFollow();
        ApplyHealthSharing();
        ModeChanged?.Invoke(mode);
        return true;
    }

    private float RouteLength()
    {
        float length = 0.0f;
        for(int i = 1; i < route.Count; i++) { length += Vector2.Distance(route[i - 1], route[i]); }
        return length;
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

        ApplyBodyCollision(noah);
        ApplyBodyCollision(sigle);
    }

    // プレハブでの当たり判定の有効・無効（シグルはプレハブで無効になっている＝ついてくるときにノアに引っかからないため）
    private readonly System.Collections.Generic.Dictionary<Collider2D, bool> originalBody = new System.Collections.Generic.Dictionary<Collider2D, bool>();

    /// <summary>
    /// 体の当たり判定：ついてきている間はプレハブのまま。操作しているとき・待っているとき・別行動のときは有効
    /// （有効でないと、壁で止まらない・調べる範囲やイベントの範囲に入っても反応しない・狭い通路などの仕掛けが効かない）
    /// </summary>
    private void ApplyBodyCollision(GameObject character)
    {
        if(character == null) { return; }

        bool following = mode == PartyMode.Together && character != currentPlayer;
        foreach(Collider2D body in character.GetComponents<Collider2D>())
        {
            if(body.isTrigger) { continue; }
            if(!originalBody.ContainsKey(body)) { originalBody[body] = body.enabled; }
            body.enabled = following ? originalBody[body] : true;
        }
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