/*********************************************************************/
// * \file   GuardGauge.cs
// * \brief  守り（弾消し）のゲージ：使える時間とリチャージ。シーンに置いたときだけ制限がかかる
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 守り（弾消し・Q キー）のゲージ
/// </summary>
///
/// ・弾消しを出している間はゲージが減り、出していない間はたまる（リチャージ）
/// ・ゲージが空になると、決めた量までたまるまで弾消しを出せない
/// ・シーンに置いたときだけ制限がかかる（置いていないシーンは今までどおり、いつでも出せる）
/// ・画面の左上（HP の下）にゲージを出す。名前の表示は「シグルの守り」（シグルが守っている、という演出）
/// ・アイテムなどから、ゲージをためる（Refill）・しばらくリチャージを速くする（BoostRecharge）
public class GuardGauge : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("ゲージ")]

    [SerializeField, Label("出していられる時間（秒）", 0.2f)]
    [Tooltip("ゲージが満タンのとき、弾消しを出し続けられる時間")]
    private float maxSeconds = 2.0f;

    [SerializeField, Label("空から満タンまでの時間（秒）", 0.2f)]
    [Tooltip("リチャージにかかる時間。アイテムで短くできる")]
    private float rechargeSeconds = 6.0f;

    [SerializeField, Label("たまり始めるまでの間（秒）", 0.0f)]
    [Tooltip("弾消しをやめてから、たまり始めるまでの間")]
    private float rechargeDelay = 0.4f;

    [SerializeField, Label("空になったら出せるようになる量", 0.05f, 1.0f)]
    [Tooltip("空になったあと、ゲージがこの割合までたまるまで出せない（1 なら満タンまで）")]
    private float resumeRatio = 0.5f;

    [Header("表示")]

    [SerializeField, Label("名前")]
    private string label = "シグルの守り（Q）";

    [SerializeField, Label("ゲージの色")]
    private Color fillColor = new Color(0.35f, 0.75f, 1.0f, 1.0f);

    [SerializeField, Label("リチャージ中の色")]
    [Tooltip("空になって出せない間の色")]
    private Color rechargeColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);

    [SerializeField, Label("速くたまっている間の色")]
    [Tooltip("アイテムでリチャージが速くなっている間の色")]
    private Color boostColor = new Color(1.0f, 0.85f, 0.3f, 1.0f);

    [SerializeField, Label("画面の位置（左上から）")]
    private Vector2 screenOffset = new Vector2(28.0f, -96.0f);

    [Header("音（空でもよい）")]

    [SerializeField, Label("空になったときの音")]
    private SeData emptySe;

    [SerializeField, Label("また出せるようになったときの音")]
    private SeData readySe;

    private static GuardGauge active;

    private float amount = 1.0f;
    private bool exhausted;
    private float delayTimer;
    private int lastUseFrame = -1;
    private float boostTimer;
    private float boostMultiplier = 1.0f;

    private Image fill;
    private TextMeshProUGUI text;
    private GameObject canvasRoot;

    //=====================================================================
    // プロパティ・イベント
    //=====================================================================

    /// <summary> シーンのゲージ（置いていなければ null） </summary>
    public static GuardGauge Active => active;

    /// <summary> 残り（0〜1） </summary>
    public float Amount => amount;

    /// <summary> 空になって出せない状態か </summary>
    public bool Exhausted => exhausted;

    /// <summary> 空になったとき（一言のきっかけ） </summary>
    public static event System.Action Emptied;

    /// <summary> また出せるようになったとき </summary>
    public static event System.Action Ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
        Emptied = null;
        Ready = null;
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    private void Awake()
    {
        active = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if(active == this) { active = null; }
        if(canvasRoot != null) { Destroy(canvasRoot); }
    }

    //=====================================================================
    // 使う（弾消しの側から毎フレーム呼ぶ）
    //=====================================================================

    /// <summary>
    /// 弾消しを出してよいか（出したいフレームに呼ぶ）。ゲージがないシーンでは、いつでも true
    /// </summary>
    public static bool Use(float deltaTime)
    {
        if(active == null || !active.isActiveAndEnabled) { return true; }
        return active.UseInternal(deltaTime);
    }

    private bool UseInternal(float deltaTime)
    {
        if(exhausted) { return false; }

        lastUseFrame = Time.frameCount;
        delayTimer = rechargeDelay;
        amount -= deltaTime / Mathf.Max(0.1f, maxSeconds);

        if(amount <= 0.0f)
        {
            amount = 0.0f;
            exhausted = true;
            SeManager.PlaySe(emptySe, Vector3.zero);
            if(emptySe == null) { UISound.Play(UISoundType.Cannot); }
            Emptied?.Invoke();
            return false;
        }
        return true;
    }

    /// <summary>
    /// ゲージをためる（アイテムなど）。ratio = 1 で満タン
    /// </summary>
    public void Refill(float ratio)
    {
        amount = Mathf.Clamp01(amount + ratio);
        CheckReady();
    }

    /// <summary>
    /// しばらくの間、リチャージを速くする（アイテムなど）。multiplier = 2 なら 2 倍速
    /// </summary>
    public void BoostRecharge(float multiplier, float seconds)
    {
        boostMultiplier = Mathf.Max(1.0f, multiplier);
        boostTimer = Mathf.Max(boostTimer, seconds);
    }

    /// <summary> 満タンにして、速くたまる状態も消す（戦闘のやり直しなど） </summary>
    public void ResetGauge()
    {
        amount = 1.0f;
        exhausted = false;
        boostTimer = 0.0f;
        delayTimer = 0.0f;
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        float dt = Time.deltaTime;
        if(boostTimer > 0.0f) { boostTimer -= dt; }

        // このフレームに使っていなければたまる（会話中などで止まっている間もたまる）
        if(lastUseFrame != Time.frameCount)
        {
            if(delayTimer > 0.0f) { delayTimer -= dt; }
            else if(amount < 1.0f)
            {
                float speed = boostTimer > 0.0f ? boostMultiplier : 1.0f;
                amount = Mathf.Min(1.0f, amount + dt * speed / Mathf.Max(0.1f, rechargeSeconds));
                CheckReady();
            }
        }

        RefreshUI();
    }

    private void CheckReady()
    {
        if(!exhausted || amount < resumeRatio) { return; }

        exhausted = false;
        SeManager.PlaySe(readySe, Vector3.zero);
        Ready?.Invoke();
    }

    //=====================================================================
    // 表示
    //=====================================================================

    private void BuildUI()
    {
        canvasRoot = new GameObject("守りのゲージ（自動で作られる）");
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        var rootObj = new GameObject("ゲージ", typeof(RectTransform));
        rootObj.transform.SetParent(canvasRoot.transform, false);
        var rootRect = (RectTransform)rootObj.transform;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.0f, 1.0f);
        rootRect.pivot = new Vector2(0.0f, 1.0f);
        rootRect.anchoredPosition = screenOffset;
        rootRect.sizeDelta = new Vector2(240.0f, 50.0f);

        text = CreateText(rootRect);
        Image back = CreateImage(rootRect, "下地", new Color(0.0f, 0.0f, 0.0f, 0.6f), new Vector2(0.0f, -26.0f), new Vector2(240.0f, 16.0f));
        fill = CreateImage((RectTransform)back.transform, "残り", fillColor, new Vector2(2.0f, -2.0f), new Vector2(236.0f, 12.0f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
    }

    private TextMeshProUGUI CreateText(RectTransform parent)
    {
        var go = new GameObject("名前", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(320.0f, 26.0f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = 20.0f;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static Image CreateImage(RectTransform parent, string imageName, Color color, Vector2 position, Vector2 size)
    {
        var go = new GameObject(imageName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.0f, 1.0f);
        rect.pivot = new Vector2(0.0f, 1.0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void RefreshUI()
    {
        if(fill == null) { return; }

        fill.fillAmount = amount;
        fill.color = exhausted ? rechargeColor : (boostTimer > 0.0f ? boostColor : fillColor);
        text.text = exhausted ? label + "　回復中…" : label;
    }
}
