/*********************************************************************/
// * \file   TimpaniPeg.cs
// * \brief  ティンパニのペグ（調べるとリズムゲームが始まり、何回か成功すると締まる）
// *
// * \author
/*********************************************************************/

using System;
using TMPro;
using UnityEngine;

/// <summary>
/// ティンパニのペグ
/// </summary>
///
/// ・調べる（E）とリズムゲームが始まる：外側の輪が縮み、中心の輪に重なる瞬間にもう一度 E を押すと「成功」
/// ・決めた回数だけ成功すると締まる（同じオブジェクトの GimmickState が ON になる）
/// ・ゲームは止まらない（敵の攻撃は続く）。ペグから離れる・キャラを切り替えると中断する（進んだ分は残る）
/// ・使えるキャラ（初期値：ノアだけ）。使えないキャラで近づくと、使えるキャラの目印が出る
/// ・締まったかどうかは、ペグの色・上に出る印（◆◇）でわかる
/// ・1台のティンパニのペグが全部締まったかは、答え合わせ（PuzzleCheck「全部 ON」）でペグの GimmickState を見る
[RequireComponent(typeof(GimmickState))]
public class TimpaniPeg : GimmickInteractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("使えるキャラ")]

    [SerializeField, Label("使えるキャラ")]
    [Tooltip("ペグを締められるキャラ。使えないキャラで近づくと、使えるキャラの目印が出る")]
    private CharacterRestriction restriction = CharacterRestriction.NoahOnly;

    [SerializeField, Label("使えないキャラで調べたとき")]
    [Tooltip("例：「俺には無理だ。ノアに任せる」の会話。空なら「できない」音だけ")]
    private EventSequence wrongCharacter;

    [Header("リズムゲーム（難しさ）")]

    [SerializeField, Label("成功する回数", 1)]
    [Tooltip("この回数だけタイミングよく押すと締まる")]
    private int requiredSuccess = 3;

    [SerializeField, Label("輪が縮む時間（秒）", 0.2f)]
    [Tooltip("外側の輪が縮みきるまでの時間。短いほど難しい")]
    private float shrinkSeconds = 1.3f;

    [SerializeField, Label("成功するたびに速くなる割合", 0.0f, 0.5f)]
    [Tooltip("0.1 なら、成功するたびに 10% ずつ速くなる。0 なら変わらない")]
    private float speedUpPerSuccess = 0.1f;

    [SerializeField, Label("判定の甘さ（マス）", 0.02f)]
    [Tooltip("縮む輪と中心の輪の大きさの差が、これ以内のときに押すと成功。大きいほど簡単")]
    private float tolerance = 0.16f;

    [SerializeField, Label("失敗で戻る回数", 0)]
    [Tooltip("タイミングを外したとき、成功の回数をいくつ減らすか。0 なら減らない")]
    private int missPenalty = 0;

    [SerializeField, Label("失敗のあと押せない時間（秒）", 0.0f)]
    [Tooltip("連打で成功しないように、外したあと少しの間は押せない")]
    private float missLockSeconds = 0.35f;

    [Header("見た目")]

    [SerializeField, Label("ペグの絵")]
    [Tooltip("色を変える SpriteRenderer（空なら自分か子から探す）")]
    private SpriteRenderer body;

    [SerializeField, Label("ゆるんでいる色")]
    private Color looseColor = new Color(0.95f, 0.3f, 0.2f);

    [SerializeField, Label("締まった色")]
    private Color tightColor = new Color(0.3f, 0.9f, 0.5f);

    [SerializeField, Label("外側の輪の大きさ（マス）", 0.3f)]
    [Tooltip("縮み始めの輪の半径")]
    private float startRadius = 1.6f;

    [SerializeField, Label("中心の輪の大きさ（マス）", 0.1f)]
    [Tooltip("ここに重なる瞬間に押す")]
    private float targetRadius = 0.55f;

    [SerializeField, Label("輪の色")]
    private Color ringColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);

    [SerializeField, Label("重なっているときの色")]
    [Tooltip("今押せば成功、というときの輪の色")]
    private Color hitColor = new Color(1.0f, 0.9f, 0.2f, 1.0f);

    [Header("音・エフェクト（空でもよい）")]

    [SerializeField, Label("始めたときの音")]
    private SeData startSe;

    [SerializeField, Label("成功の音")]
    private SeData successSe;

    [SerializeField, Label("失敗の音")]
    private SeData missSe;

    [SerializeField, Label("締まったときの音")]
    private SeData tightenedSe;

    [SerializeField, Label("成功のエフェクト")]
    private EffectData successEffect;

    [SerializeField, Label("締まったときのエフェクト")]
    private EffectData tightenedEffect;

    [Header("イベント")]

    [SerializeField, Label("締まったときのイベント")]
    [Tooltip("このペグが締まったときに実行する（空でよい）")]
    private EventSequence onTightened;

    private GimmickState state;

    private int successCount;
    private bool playing;
    private float ringTime;
    private float lockTimer;
    private float pulse;

    private LineRenderer targetRing;
    private LineRenderer shrinkRing;
    private TextMeshPro marks;

    //=====================================================================
    // プロパティ・イベント
    //=====================================================================

    /// <summary> 締まっているか </summary>
    public bool IsTightened => state != null && state.IsOn;

    /// <summary> リズムゲーム中か </summary>
    public bool IsPlaying => playing;

    /// <summary> 成功した回数 </summary>
    public int SuccessCount => successCount;

    /// <summary> 必要な成功の回数 </summary>
    public int RequiredSuccess => Mathf.Max(1, requiredSuccess);

    /// <summary> タイミングよく押せたとき（締まったときも含む） </summary>
    public static event Action<TimpaniPeg> Succeeded;

    /// <summary> タイミングを外したとき </summary>
    public static event Action<TimpaniPeg> Missed;

    /// <summary> 締まったとき </summary>
    public static event Action<TimpaniPeg> Tightened;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Succeeded = null;
        Missed = null;
        Tightened = null;
    }

    protected override CharacterRestriction Restriction => restriction;

    //=====================================================================
    // 初期化
    //=====================================================================

    protected override void Awake()
    {
        base.Awake();
        state = GetComponent<GimmickState>();
        if(body == null) { body = GetComponentInChildren<SpriteRenderer>(); }
    }

    private void Start()
    {
        // セーブから戻ったとき・最初から ON のとき
        if(IsTightened) { successCount = RequiredSuccess; }
        BuildVisuals();
        RefreshVisuals();
    }

    private void OnDisable()
    {
        StopPlaying();
    }

    //=====================================================================
    // 調べる
    //=====================================================================

    protected override void OnInteract(GameObject user)
    {
        if(IsTightened) { return; }

        if(!restriction.Allows(user))
        {
            RefuseCharacter(wrongCharacter);
            return;
        }

        if(!playing)
        {
            StartPlaying();
            return;
        }

        Judge();
    }

    protected override void OnPlayerExit(GameObject exitedPlayer)
    {
        // 離れたら中断（進んだ分は残る）
        StopPlaying();
    }

    private void StartPlaying()
    {
        playing = true;
        ringTime = 0.0f;
        lockTimer = 0.0f;
        SeManager.PlaySe(startSe, transform.position);
        RefreshVisuals();
    }

    private void StopPlaying()
    {
        if(!playing) { return; }
        playing = false;
        RefreshVisuals();
    }

    /// <summary>
    /// 押した瞬間の判定
    /// </summary>
    private void Judge()
    {
        if(lockTimer > 0.0f) { return; }

        if(Mathf.Abs(CurrentRadius() - targetRadius) <= tolerance)
        {
            successCount++;
            pulse = 1.0f;
            ringTime = 0.0f;

            if(successCount >= RequiredSuccess)
            {
                Tighten();
                return;
            }

            SeManager.PlaySe(successSe, transform.position);
            if(successEffect != null) { EffectManager.Play(successEffect, transform.position, Vector2.zero, 0.0f, null, false); }
            Succeeded?.Invoke(this);
        }
        else
        {
            successCount = Mathf.Max(0, successCount - Mathf.Max(0, missPenalty));
            lockTimer = missLockSeconds;
            ringTime = 0.0f;
            SeManager.PlaySe(missSe, transform.position);
            if(missSe == null) { UISound.Play(UISoundType.Cannot); }
            Missed?.Invoke(this);
        }

        RefreshVisuals();
    }

    /// <summary>
    /// 締まった
    /// </summary>
    private void Tighten()
    {
        playing = false;
        successCount = RequiredSuccess;

        SeManager.PlaySe(tightenedSe != null ? tightenedSe : successSe, transform.position);
        if(tightenedEffect != null)      { EffectManager.Play(tightenedEffect, transform.position, Vector2.zero, 0.0f, null, false); }
        else if(successEffect != null)   { EffectManager.Play(successEffect, transform.position, Vector2.zero, 0.0f, null, false); }

        state.TurnOn();
        RefreshVisuals();

        Succeeded?.Invoke(this);
        Tightened?.Invoke(this);
        if(onTightened != null) { onTightened.Play(gameObject); }
    }

    /// <summary>
    /// 音や演出なしで締まった状態にする（クリアしたあとに戻ってきたとき）
    /// </summary>
    public void SetTightenedSilently()
    {
        if(state == null) { state = GetComponent<GimmickState>(); }
        playing = false;
        successCount = RequiredSuccess;
        state.Set(1, true, false);
        RefreshVisuals();
    }

    /// <summary>
    /// 最初の状態に戻す（戦闘のやり直し・デバッグ用）
    /// </summary>
    public void ResetPeg()
    {
        playing = false;
        successCount = 0;
        if(state != null) { state.TurnOff(true); }
        RefreshVisuals();
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        if(pulse > 0.0f) { pulse = Mathf.Max(0.0f, pulse - Time.deltaTime * 3.0f); ApplyBodyLook(); }

        if(!playing) { return; }

        // 会話・メニューの間は止めておく。キャラを切り替えた・使えないキャラになったら中断
        if(InputBlock.IsBlocked) { return; }

        GameObject current = CurrentPlayer;
        if(current == null || !restriction.Allows(current) || !IsInside(current))
        {
            StopPlaying();
            return;
        }

        if(lockTimer > 0.0f) { lockTimer -= Time.deltaTime; }

        ringTime += Time.deltaTime / CurrentShrinkSeconds();
        if(ringTime >= 1.0f) { ringTime = 0.0f; }

        float radius = CurrentRadius();
        SetCircle(shrinkRing, radius);

        bool hit = Mathf.Abs(radius - targetRadius) <= tolerance && lockTimer <= 0.0f;
        Color c = lockTimer > 0.0f ? new Color(ringColor.r, ringColor.g, ringColor.b, 0.3f) : (hit ? hitColor : ringColor);
        shrinkRing.startColor = c;
        shrinkRing.endColor = c;
        targetRing.startColor = hit ? hitColor : ringColor;
        targetRing.endColor = targetRing.startColor;
    }

    private float CurrentShrinkSeconds()
    {
        float speed = 1.0f + speedUpPerSuccess * successCount;
        return Mathf.Max(0.2f, shrinkSeconds / speed);
    }

    // 輪は外側の大きさから、中心の輪より少し小さくなるまで縮む
    private float CurrentRadius()
    {
        return Mathf.Lerp(startRadius, Mathf.Max(0.0f, targetRadius - tolerance * 2.0f), ringTime);
    }

    //=====================================================================
    // 見た目
    //=====================================================================

    private void BuildVisuals()
    {
        targetRing = CreateRing("中心の輪", 0.07f);
        shrinkRing = CreateRing("縮む輪", 0.05f);
        SetCircle(targetRing, targetRadius);

        var go = new GameObject("印");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.0f, 0.75f, 0.0f);
        marks = go.AddComponent<TextMeshPro>();
        marks.fontSize = 3.0f;
        marks.alignment = TextAlignmentOptions.Center;
        marks.rectTransform.sizeDelta = new Vector2(4.0f, 1.0f);
        marks.sortingOrder = 201;
    }

    private LineRenderer CreateRing(string ringName, float width)
    {
        var go = new GameObject(ringName);
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.widthMultiplier = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingOrder = 200;
        line.startColor = ringColor;
        line.endColor = ringColor;
        return line;
    }

    private static void SetCircle(LineRenderer line, float radius)
    {
        if(line == null) { return; }

        int count = line.positionCount;
        for(int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2.0f / count;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0.0f));
        }
    }

    private void RefreshVisuals()
    {
        if(targetRing != null) { targetRing.gameObject.SetActive(playing); }
        if(shrinkRing != null) { shrinkRing.gameObject.SetActive(playing); }

        if(marks != null)
        {
            if(IsTightened)
            {
                marks.text = "♪";
                marks.color = tightColor;
            }
            else
            {
                int need = RequiredSuccess;
                marks.text = new string('◆', Mathf.Clamp(successCount, 0, need)) + new string('◇', Mathf.Clamp(need - successCount, 0, need));
                marks.color = Color.white;
            }

            // 締める前で、まだ触っていないペグは印を出さない
            marks.gameObject.SetActive(IsTightened || playing || successCount > 0);
        }

        ApplyBodyLook();
    }

    private void ApplyBodyLook()
    {
        if(body == null) { return; }

        float progress = IsTightened ? 1.0f : (float)successCount / RequiredSuccess;
        Color c = Color.Lerp(looseColor, tightColor, progress);
        body.color = Color.Lerp(c, Color.white, pulse * 0.7f);
    }
}
