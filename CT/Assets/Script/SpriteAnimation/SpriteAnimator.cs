/*********************************************************************/
// * \file   SpriteAnimator.cs
// * \brief  SpriteAnimData を再生するコンポーネント
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// SpriteAnimData のアニメーションを SpriteRenderer で再生する
///
/// コードを書かない場合:
///   ・付けて SpriteAnimData を指定するだけで「最初に流す動き」がループ再生される (置物・NPC・背景など)
///   ・キャラクターは CharacterAnimLink も付けると、移動に合わせて Idle / Walk が切り替わる
///
/// コードから:
///   animator.Play("Attack");            // 動きを流す (同じ動きを流し中なら何もしない)
///   animator.Play("Attack", true);      // 最初から流し直す
///   animator.SetDirection(moveDir);     // 向きを変える
///   yield return animator.WaitForCycle(); // 1回流し終わるまで待つ
///
/// ※ Animator コンポーネントと一緒に付けると、Animator が画像を上書きしてしまうので外すこと
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("Animation/Sprite Animator")]
public class SpriteAnimator : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("再生するアニメーションデータ")]
    [SerializeField]
    private SpriteAnimData data;

    [Tooltip("表示されたときに自動で再生を始める")]
    [SerializeField]
    private bool playOnEnable = true;

    [Tooltip("最初に流す動き (空ならデータの「最初に流す動き」)")]
    [SerializeField]
    private string startClip = "";

    [Tooltip("最初の向き")]
    [SerializeField]
    private SpriteAnimFacing startFacing = SpriteAnimFacing.Down;

    [Tooltip("再生速度の倍率 (1 = そのまま)")]
    [SerializeField, Min(0.0f)]
    private float speed = 1.0f;

    [Tooltip("ポーズ中 (Time.timeScale = 0) でも動かす")]
    [SerializeField]
    private bool ignoreTimeScale = false;

    private SpriteRenderer spriteRenderer;
    private bool baseFlipX;
    private bool initialized;

    private SpriteAnimClip current;
    private Sprite[] frames;
    private bool flip;
    private float time;
    private int lastFrame = -1;
    private bool finished;

    private SpriteAnimFacing facing;
    private bool lastHorizontalLeft;

    private EffectTrigger effectTrigger;
    private bool effectTriggerSearched;

    //=====================================================================
    // イベント
    //=====================================================================

    /// <summary> 「1回」の動きを流し終わったとき (引数: 動きの名前) </summary>
    public event System.Action<SpriteAnimator, string> OnClipFinished;

    /// <summary> コマごとのイベントの「合図」が来たとき (引数: 合図の文字) </summary>
    public event System.Action<SpriteAnimator, string> OnMessage;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public SpriteAnimData Data => data;

    /// <summary> 今流している動きの名前 (無ければ空) </summary>
    public string CurrentClipName => current != null ? current.name : "";

    public SpriteAnimClip CurrentClip => current;

    /// <summary> 「1回」の動きを流し終わって止まっているか </summary>
    public bool IsFinished => finished;

    public bool IsPlaying => current != null && !finished;

    /// <summary> 今の動きを1周以上流したか (ループの動きでも1周したら true) </summary>
    public bool IsCycleComplete => current == null || finished || time >= current.CycleTime(frames);

    public SpriteAnimFacing Facing => facing;

    public float Speed { get => speed; set => speed = Mathf.Max(0.0f, value); }

    //=====================================================================
    // Unity
    //=====================================================================

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        if(playOnEnable && current == null) { PlayStart(); }
    }

    private void Update()
    {
        if(current == null || finished) { return; }

        float dt = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        time += dt * speed;
        Evaluate(true);
    }

    //=====================================================================
    // 外から呼ぶ関数
    //=====================================================================

    /// <summary>
    /// 動きを流す。その名前の動きが無ければ false
    /// </summary>
    /// <param name="restart"> false なら、同じ動きを流している最中は何もしない </param>
    public bool Play(string clipName, bool restart = false)
    {
        Init();
        if(data == null) { return false; }

        SpriteAnimClip clip = data.Find(clipName);
        if(clip == null) { return false; }

        if(clip == current && !restart) { return true; }

        StartClip(clip);
        return true;
    }

    /// <summary>
    /// この名前の動きがあるか
    /// </summary>
    public bool Has(string clipName) => data != null && data.Has(clipName);

    /// <summary>
    /// 動きを「流し終わった状態」にする (セーブからの復元用。音やエフェクトは出さない)
    ///   ・1回 → 最後のコマで止める
    ///   ・1回 → 次の動き → 次の動きを流す
    ///   ・ループ → そのまま流す
    /// </summary>
    public bool JumpToEnd(string clipName)
    {
        Init();
        if(data == null) { return false; }

        SpriteAnimClip clip = data.Find(clipName);
        if(clip == null) { return false; }

        if(clip.playMode == SpriteAnimPlayMode.Loop) { return Play(clipName, true); }

        if(clip.playMode == SpriteAnimPlayMode.OnceThenNext && data.Has(clip.nextClip))
        {
            return Play(clip.nextClip, true);
        }

        current = clip;
        RefreshFrames();
        time = clip.CycleTime(frames);
        finished = true;
        Evaluate(false);
        return true;
    }

    /// <summary>
    /// アニメーションデータを入れ替える
    /// </summary>
    public void SetData(SpriteAnimData newData, bool restart = true)
    {
        data = newData;
        current = null;
        if(restart) { PlayStart(); }
    }

    /// <summary>
    /// 再生を止める (今のコマのまま)
    /// </summary>
    public void Stop()
    {
        current = null;
        finished = false;
    }

    /// <summary>
    /// 移動方向などから向きを変える (ゼロなら変えない)
    /// 斜めのときは左右を優先する
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        if(direction.sqrMagnitude < 0.0001f) { return; }

        SpriteAnimFacing f;
        if(Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            f = direction.x < 0.0f ? SpriteAnimFacing.Left : SpriteAnimFacing.Right;
        }
        else
        {
            f = direction.y < 0.0f ? SpriteAnimFacing.Down : SpriteAnimFacing.Up;
        }
        SetFacing(f);
    }

    /// <summary>
    /// 向きを変える
    /// </summary>
    public void SetFacing(SpriteAnimFacing newFacing)
    {
        Init();
        if(newFacing == facing) { return; }

        facing = newFacing;
        if(facing == SpriteAnimFacing.Left)  { lastHorizontalLeft = true; }
        if(facing == SpriteAnimFacing.Right) { lastHorizontalLeft = false; }

        if(current == null) { return; }

        // 向きが変わっても、流れている時間はそのまま (歩きが途切れないように)
        RefreshFrames();
        Evaluate(false);
    }

    /// <summary>
    /// 今の動きを1周流し終わるまで待つ (コルーチン用)
    /// 途中で別の動きに変わったら、そこで待つのをやめる
    /// </summary>
    public IEnumerator WaitForCycle(float timeout = 10.0f)
    {
        SpriteAnimClip waiting = current;
        float waited = 0.0f;

        while(waiting != null && current == waiting && !IsCycleComplete && waited < timeout)
        {
            waited += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private void Init()
    {
        if(initialized) { return; }
        initialized = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        baseFlipX = spriteRenderer.flipX;
        facing = startFacing;
        lastHorizontalLeft = startFacing == SpriteAnimFacing.Left;
    }

    private void PlayStart()
    {
        Init();
        if(data == null) { return; }

        SpriteAnimClip clip = data.Find(startClip) ?? data.DefaultClip;
        if(clip != null) { StartClip(clip); }
    }

    private void StartClip(SpriteAnimClip clip)
    {
        current = clip;
        time = 0.0f;
        lastFrame = -1;
        finished = false;
        RefreshFrames();
        Evaluate(true);// 1コマ目のイベントもここで出す
    }

    private void RefreshFrames()
    {
        frames = current != null ? current.GetFrames(facing, lastHorizontalLeft, out flip) : null;
    }

    /// <summary>
    /// 今の時間のコマを表示する
    /// </summary>
    private void Evaluate(bool fireEvents)
    {
        if(current == null) { return; }

        if(frames == null || frames.Length == 0)
        {
            return;
        }

        int index = SpriteFrameTiming.FrameAt(time, frames.Length, current.fps, current.IsLoop, out bool done, current.frameLengths);

        if(fireEvents && index != lastFrame) { FireEvents(lastFrame, index); }
        lastFrame = index;

        spriteRenderer.sprite = frames[index];
        spriteRenderer.flipX = baseFlipX ^ flip;

        if(done && !finished)
        {
            finished = true;
            SpriteAnimClip ended = current;
            OnClipFinished?.Invoke(this, ended.name);

            // 「1回 → 次の動きへ」 (イベントの中で別の動きに変えられていなければ)
            if(current == ended && ended.playMode == SpriteAnimPlayMode.OnceThenNext && !string.IsNullOrEmpty(ended.nextClip))
            {
                Play(ended.nextClip, true);
            }
        }
    }

    /// <summary>
    /// from の次のコマから to のコマまでのイベントを出す (ループで先頭に戻った場合も含む)
    /// </summary>
    private void FireEvents(int from, int to)
    {
        if(current.events == null || current.events.Count == 0) { return; }

        if(to > from)
        {
            for(int i = from + 1; i <= to; i++) { FireFrame(i); }
        }
        else
        {
            // ループで先頭に戻った
            for(int i = from + 1; i < frames.Length; i++) { FireFrame(i); }
            for(int i = 0; i <= to; i++) { FireFrame(i); }
        }
    }

    private void FireFrame(int index)
    {
        foreach(SpriteAnimFrameEvent ev in current.events)
        {
            if(ev == null || ev.frame - 1 != index) { continue; }

            if(ev.se != null) { SeManager.PlaySe(ev.se, transform.position); }

            if(ev.effect != null)
            {
                Vector2 offset = ev.effectOffset;
                if(flip) { offset.x = -offset.x; }// 反転しているときは、ずらしも反転する
                EffectManager.Play(ev.effect, transform.position, offset, 0.0f, ev.effectFollow ? transform : null, false);
            }

            if(!string.IsNullOrEmpty(ev.effectTriggerName))
            {
                if(!effectTriggerSearched) { effectTrigger = GetComponent<EffectTrigger>(); effectTriggerSearched = true; }
                if(effectTrigger != null) { effectTrigger.Play(ev.effectTriggerName); }
                else { Debug.LogWarning($"{name}: EffectTrigger が付いていないため「{ev.effectTriggerName}」を出せません", this); }
            }

            if(!string.IsNullOrEmpty(ev.message)) { OnMessage?.Invoke(this, ev.message); }
        }
    }
}
