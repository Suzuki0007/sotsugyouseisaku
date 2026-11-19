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
    [Tooltip("タイミングよく押せたとき（下の「成功するたびの音」が空のとき・足りないとき）")]
    private SeData successSe;

    [SerializeField]
    [Tooltip("成功するたびの音（1回目・2回目・3回目…の順。音程が上がっていく音を並べる）。回数より少なければ最後の音をくり返す。空なら「成功の音」")]
    private System.Collections.Generic.List<SeData> successSeSteps = new System.Collections.Generic.List<SeData>();

    [SerializeField, Label("締まったときも成功の音を鳴らす")]
    [Tooltip("最後の成功（締まった瞬間）に、成功するたびの音と「締まった音」を重ねて鳴らす")]
    private bool successSeOnTighten = true;

    [SerializeField, Label("失敗の音")]
    private SeData missSe;

    [SerializeField, Label("締まったときの音")]
    private SeData tightenedSe;

    [SerializeField, Label("成功のエフェクト")]
    private EffectData successEffect;

    [SerializeField, Label("締まったときのエフェクト")]
    private EffectData tightenedEffect;

    [Header("初めての説明・案内（空でよい）")]

    [SerializeField, Label("初めて始めるときのイベント")]
    [Tooltip("最初に調べたときだけ、リズムゲームの前に実行する（「よし、直すぞ」と遊び方の説明の会話など）。終わったらそのままリズムゲームが始まる。空なら説明なしですぐ始まる")]
    private EventSequence firstStartEvent;

    [SerializeField, Label("説明を見たフラグ")]
    [Tooltip("説明を見たら ON にするフラグ。ON なら説明を出さない（セーブするフラグにすると、ロードしても繰り返さない）。空なら、このシーンにいる間だけ覚える")]
    private GameFlag firstStartFlag;

    [SerializeField, Label("遊んでいる間の案内の文")]
    [Tooltip("リズムゲームの間、ペグの下に出す文。{n} は「あと何回」に置き換わる。例：輪が重なった瞬間に [E]（あと{n}回）。空なら出さない")]
    private string playingGuide = "";

    [SerializeField, Label("案内の文字（フォント）")]
    [Tooltip("案内の文に使う日本語のフォント（NotoSansJP-Regular SDF など）。空なら TextMeshPro の標準")]
    private TMP_FontAsset guideFont;

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
    private TextMeshPro guide;
    private bool firstStartSeen;
    private Coroutine firstStartRoutine;

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

    // 締まったペグには案内を出さない。リズムゲーム中は「今！」の合図にする
    protected override bool CanPrompt => !IsTightened;
    public override string PromptLabel => playing ? "重なった瞬間に押す" : "締める";

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
        firstStartRoutine = null;
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

        if(firstStartRoutine != null) { return; }

        if(!playing)
        {
            if(NeedsFirstStart())
            {
                firstStartRoutine = StartCoroutine(FirstStartRoutine(user));
                return;
            }
            StartPlaying();
            return;
        }

        Judge();
    }

    // 初めて始めるときの説明がまだか
    private bool NeedsFirstStart()
    {
        if(firstStartEvent == null) { return false; }
        if(firstStartFlag != null) { return !GameFlags.Get(firstStartFlag); }
        return !firstStartSeen;
    }

    // 説明のイベントが終わるのを待って、リズムゲームを始める
    private System.Collections.IEnumerator FirstStartRoutine(GameObject user)
    {
        firstStartSeen = true;
        if(firstStartFlag != null) { GameFlags.Set(firstStartFlag, true); }

        firstStartEvent.Play(user);
        yield return null;
        while(firstStartEvent.IsPlaying || InputBlock.IsBlocked) { yield return null; }

        // 会話を閉じた E がそのまま判定に使われないよう、少し待つ
        yield return new WaitForSeconds(0.25f);

        firstStartRoutine = null;
        GameObject current = CurrentPlayer;
        if(!IsTightened && current != null && restriction.Allows(current) && IsInside(current)) { StartPlaying(); }
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

            SeManager.PlaySe(SuccessSeFor(successCount), transform.position);
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

    /// <summary> 何回目の成功かで鳴らす音（1 から） </summary>
    private SeData SuccessSeFor(int count)
    {
        for(int i = Mathf.Min(count, successSeSteps.Count) - 1; i >= 0; i--)
        {
            if(successSeSteps[i] != null) { return successSeSteps[i]; }
        }
        return successSe;
    }

    /// <summary>
    /// 締まった
    /// </summary>
    private void Tighten()
    {
        playing = false;
        successCount = RequiredSuccess;

        if(successSeOnTighten && tightenedSe != null) { SeManager.PlaySe(SuccessSeFor(successCount), transform.position); }
        SeManager.PlaySe(tightenedSe != null ? tightenedSe : SuccessSeFor(successCount), transform.position);
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

    // Update は使わない：Intaractable（土台）の private な Update が E キーを見ているので、
    // ここで Update を作ると土台の Update が呼ばれず、調べられなくなる
    protected override void LateUpdate()
    {
        base.LateUpdate();

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

        if(!string.IsNullOrEmpty(playingGuide))
        {
            var g = new GameObject("案内");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0.0f, -Mathf.Max(startRadius, 0.6f) - 0.35f, 0.0f);
            guide = g.AddComponent<TextMeshPro>();
            guide.fontSize = 2.4f;
            guide.alignment = TextAlignmentOptions.Center;
            guide.rectTransform.sizeDelta = new Vector2(8.0f, 1.0f);
            guide.sortingOrder = 201;
            guide.outlineWidth = 0.2f;
            guide.outlineColor = Color.black;
            if(guideFont != null) { guide.font = guideFont; }
        }
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

        if(guide != null)
        {
            guide.gameObject.SetActive(playing && !IsTightened);
            guide.text = playingGuide.Replace("{n}", Mathf.Max(0, RequiredSuccess - successCount).ToString());
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
