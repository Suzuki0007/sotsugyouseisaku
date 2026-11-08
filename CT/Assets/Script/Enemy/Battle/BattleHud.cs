/*********************************************************************/
// * \file   BattleHud.cs
// * \brief  戦闘の画面表示（目的・残り時間・残りの敵・始まり／勝ち／負けのメッセージ）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;

/// <summary>
/// 戦闘の画面表示
/// </summary>
///
/// ・戦闘エリア（BattleArea）が使う。シーンに置かなくてよい（Resources/UI/BattleHud.prefab から自動で作られ、シーンをまたいで残る）
/// ・上の帯：目的と残り時間・残りの敵。戦闘中だけ出る
/// ・真ん中：「逃げろ！」「逃げ切った！」「捕まった！」などのメッセージ（少しの間だけ出る）
/// ・見た目はプレハブで調整できる（Tools > 敵 > 戦闘の画面表示のプレハブを作る で作り直せる）
public class BattleHud : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    public const string PrefabResourcePath = "UI/BattleHud";

    [SerializeField, Label("上の帯")]
    [Tooltip("目的と残り時間を出す帯（背景ごと出し入れする）")]
    private GameObject infoPanel;

    [SerializeField, Label("上の帯の文字")]
    private TMP_Text infoText;

    [SerializeField, Label("メッセージ")]
    private TMP_Text messageText;

    [SerializeField, Label("ふつうの文字の色")]
    private Color normalColor = Color.white;

    [SerializeField, Label("残りわずかの色")]
    [Tooltip("残り時間が「残りわずか」の秒数を切ったときの色")]
    private Color warningColor = new Color(1.0f, 0.35f, 0.3f);

    [SerializeField, Label("残りわずか（秒）", 0.0f)]
    private float warningSeconds = 10.0f;

    private static BattleHud instance;
    private static bool warned;

    private float messageTime;
    private float messageDuration;
    private Object owner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        instance = null;
        warned = false;
    }

    /// <summary>
    /// 表示（なければプレハブから作る。プレハブがなければ null）
    /// </summary>
    private static BattleHud Get()
    {
        if(instance != null) { return instance; }

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if(prefab == null)
        {
            if(!warned) { Debug.LogWarning($"戦闘の画面表示のプレハブ Resources/{PrefabResourcePath} がありません。Tools > 敵 > 戦闘の画面表示のプレハブを作る で作成してください"); }
            warned = true;
            return null;
        }

        GameObject go = Instantiate(prefab);
        go.name = prefab.name;
        DontDestroyOnLoad(go);
        instance = go.GetComponent<BattleHud>();
        if(instance != null) { instance.HideAll(); }
        return instance;
    }

    //=====================================================================
    // 外から使う関数
    //=====================================================================

    /// <summary>
    /// 上の帯を出す（owner：出している戦闘エリア。別のエリアが出したものを消さないため）
    /// </summary>
    /// <param name="text"> 目的と残り時間など </param>
    /// <param name="warning"> 残りわずかの色にする </param>
    public static void SetInfo(Object owner, string text, bool warning)
    {
        BattleHud hud = Get();
        if(hud == null) { return; }

        hud.owner = owner;
        if(hud.infoPanel != null && !hud.infoPanel.activeSelf) { hud.infoPanel.SetActive(true); }
        if(hud.infoText != null)
        {
            hud.infoText.text = text;
            hud.infoText.color = warning ? hud.warningColor : hud.normalColor;
        }
    }

    /// <summary>
    /// 上の帯を消す
    /// </summary>
    public static void HideInfo(Object owner)
    {
        if(instance == null || instance.owner != owner) { return; }
        if(instance.infoPanel != null) { instance.infoPanel.SetActive(false); }
        instance.owner = null;
    }

    /// <summary>
    /// 真ん中にメッセージを出す（空なら何もしない）
    /// </summary>
    public static void ShowMessage(string text, float seconds)
    {
        if(string.IsNullOrEmpty(text)) { return; }

        BattleHud hud = Get();
        if(hud == null || hud.messageText == null) { return; }

        hud.messageText.text = text;
        hud.messageText.gameObject.SetActive(true);
        hud.messageTime = 0.0f;
        hud.messageDuration = Mathf.Max(0.1f, seconds);
        hud.SetMessageAlpha(1.0f);
    }

    /// <summary> 残りわずかとみなす秒数 </summary>
    public static float WarningSeconds => instance != null ? instance.warningSeconds : 10.0f;

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(messageText == null || !messageText.gameObject.activeSelf) { return; }

        // メニューで止めていても消えるように、止まらない時間で数える
        messageTime += Time.unscaledDeltaTime;

        // 最後の 0.3 秒で消えていく
        float remain = messageDuration - messageTime;
        SetMessageAlpha(Mathf.Clamp01(remain / 0.3f));
        if(remain <= 0.0f) { messageText.gameObject.SetActive(false); }
    }

    private void SetMessageAlpha(float alpha)
    {
        Color c = messageText.color;
        c.a = alpha;
        messageText.color = c;
    }

    private void HideAll()
    {
        if(infoPanel != null) { infoPanel.SetActive(false); }
        if(messageText != null) { messageText.gameObject.SetActive(false); }
    }
}
