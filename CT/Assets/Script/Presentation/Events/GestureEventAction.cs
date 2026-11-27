/*********************************************************************/
// * \file   GestureEventAction.cs
// * \brief  イベントの部品「手の動作を待つ」（ゆっくり回す・長押し・繰り返しを、終わるまで待つ）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// イベントの部品「手の動作を待つ」（別れや決断を、ボタン一回ではなく手の動作にする）
/// </summary>
///
/// ・手触りのプリセット（ゆっくり回す・長押し・繰り返し）の入力が最後まで進むと、次の部品へ進む。失敗・やり直し・時間切れはない
/// ・途中の区切り（例 25%・50%・90%）を超えたとき／繰り返しの 1 回ごとに、イベントの流れを動かす・合図を出す
/// ・進み具合（0〜1）と回数を「演出の数値」に入れる（「演出の数値で動かす」を付けた時計の針・手・墓標などが動く）
/// ・待っている間はプレイヤーの移動・攻撃・メニューを止める（InputBlock）
public class GestureEventAction : EventAction
{
    [System.Serializable]
    public class Step
    {
        [Label("進み具合（0〜1）", 0.0f, 1.0f)]
        [Tooltip("この割合を超えたときに 1 回だけ動く（0.5 で半分）")]
        public float at = 0.5f;

        [Label("動かすイベント")]
        [Tooltip("超えたときに流すイベントの流れ（空でもよい）")]
        public EventSequence sequence;

        [Label("出す合図")]
        [Tooltip("超えたときに出す合図（値に進み具合が入る。空でもよい）")]
        public GameSignal signal;
    }

    [SerializeField, Label("手触りのプリセット")]
    [Tooltip("Assets/Resources/Presentation/Gesture/ の手触り（最後のひと巻き・墓を掘る など）")]
    private GesturePreset preset;

    [SerializeField, Label("案内の文（上書き）")]
    [Tooltip("空ならプリセットの案内")]
    private string promptOverride = "";

    [Header("途中の出来事")]

    [SerializeField]
    [Tooltip("進み具合の区切り（ゆっくり回す・長押し。繰り返しでも回数の割合で効く）")]
    private List<Step> steps = new List<Step>();

    [SerializeField, Label("1 回ごとに動かすイベント")]
    [Tooltip("繰り返し：1 回押すごとに流すイベントの流れ（墓標を 1 つ立てる など）")]
    private EventSequence everyCount;

    [SerializeField, Label("1 回ごとに出す合図")]
    [Tooltip("繰り返し：1 回押すごとに出す合図（値に回数が入る）")]
    private GameSignal everyCountSignal;

    [Header("ほかの演出へ渡す")]

    [SerializeField, Label("進み具合を入れる数値")]
    [Tooltip("演出の数値の名前（例：巻いた割合）。進み具合 0〜1 を入れ続ける")]
    private string progressValue = "";

    [SerializeField, Label("回数を入れる数値")]
    [Tooltip("演出の数値の名前（例：掘った回数）。繰り返しの回数を入れる")]
    private string countValue = "";

    [SerializeField]
    [Tooltip("押している間・押した瞬間に少し震わせる物（手応え。時計・スコップなど）")]
    private List<Transform> shakeTargets = new List<Transform>();

    private static int busyCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { busyCount = 0; }

    /// <summary> 手の動作を待っているか（プレイヤーの操作を止める） </summary>
    public static bool IsBusy => busyCount > 0;

    /// <summary> 確かめ用：今の進み具合 </summary>
    public static float CurrentProgress { get; private set; }

    public GesturePreset Preset => preset;

    public override void Execute(GameObject gameObject)
    {
        // すぐ終わる使い方はない（順番に実行で待つ部品）。同時に実行されたときも動くように、自分で走らせる
        StartCoroutine(Run(gameObject));
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        if(preset == null) { Debug.LogWarning("手の動作を待つ：手触りのプリセットが空です", this); yield break; }

        busyCount++;
        GestureView view = GestureView.Get();
        string prompt = string.IsNullOrEmpty(promptOverride) ? preset.PromptText : promptOverride;
        var fired = new bool[steps.Count];
        var basePos = new Vector3[shakeTargets.Count];
        for(int i = 0; i < shakeTargets.Count; i++) { if(shakeTargets[i] != null) { basePos[i] = shakeTargets[i].localPosition; } }

        float progress = 0.0f;
        int count = 0;
        float sinceStart = 0.0f;
        float idle = 0.0f;
        float sinceRepeat = 999.0f;
        float pulse = 0.0f;
        bool frozen = false;
        float savedScale = 1.0f;
        CurrentProgress = 0.0f;
        WriteValues(progress, count);

        // 部品を始めたキー（調べた E など）は数えない
        yield return null;

        while(progress < 1.0f)
        {
            float dt = Time.unscaledDeltaTime;
            sinceStart += dt;
            sinceRepeat += dt;
            Keyboard kb = Keyboard.current;
            bool held = preset.IsHeld(kb);
            bool pressed = preset.WasPressed(kb);
            bool moved = false;

            if(preset.Type == GesturePreset.Kind.Repeat)
            {
                if(pressed && sinceRepeat >= preset.RepeatInterval)
                {
                    sinceRepeat = 0.0f;
                    count++;
                    progress = Mathf.Clamp01((float)count / preset.Count);
                    pulse = 0.25f;
                    moved = true;
                    if(everyCount != null) { everyCount.Play(gameObject); }
                    if(everyCountSignal != null) { Signals.Send(everyCountSignal, SignalContext.From(gameObject, count)); }
                }
            }
            else if(preset.TapMode)
            {
                if(pressed) { progress = Mathf.Clamp01(progress + preset.TapStep); pulse = 0.2f; moved = true; }
            }
            else if(held)
            {
                progress = Mathf.Clamp01(progress + dt / preset.Seconds * preset.WeightAt(progress));
                moved = true;
            }
            else if(preset.ReleaseMode == GesturePreset.Release.Return && progress > 0.0f)
            {
                progress = Mathf.Max(0.0f, progress - dt / preset.ReturnSeconds);
            }

            if(moved && preset.FreezeWorld && !frozen)
            {
                frozen = true;
                savedScale = Time.timeScale;
                Time.timeScale = 0.0f;
                AudioListener.pause = true;
            }

            // 区切り
            for(int i = 0; i < steps.Count; i++)
            {
                Step s = steps[i];
                if(s == null || fired[i] || progress < s.at) { continue; }
                fired[i] = true;
                if(s.sequence != null) { s.sequence.Play(gameObject); }
                if(s.signal != null) { Signals.Send(s.signal, SignalContext.From(gameObject, progress)); }
            }

            CurrentProgress = progress;
            WriteValues(progress, count);

            // 案内：最初の 3 秒と、2.5 秒止まっているとき
            idle = moved || held ? 0.0f : idle + dt;
            bool showPrompt = sinceStart < 3.0f || idle > 2.5f;
            view.SetPrompt(prompt, showPrompt ? 1.0f : 0.0f);
            view.SetRing(preset.ProgressLook == GesturePreset.Look.Ring, progress);

            // 手応え：押している間と、押した瞬間に震わせる
            pulse = Mathf.Max(0.0f, pulse - dt);
            float amp = preset.Shake * (held && preset.Type != GesturePreset.Kind.Repeat ? 0.6f : 0.0f) + preset.Shake * (pulse > 0.0f ? 1.0f : 0.0f);
            if(PresentationEase.IsLowQuality) { amp *= 0.5f; }
            for(int i = 0; i < shakeTargets.Count; i++)
            {
                Transform tr = shakeTargets[i];
                if(tr == null) { continue; }
                tr.localPosition = basePos[i] + (amp > 0.0f ? (Vector3)(Random.insideUnitCircle * amp) : Vector3.zero);
            }
            yield return null;
        }

        // 片付け
        for(int i = 0; i < shakeTargets.Count; i++) { if(shakeTargets[i] != null) { shakeTargets[i].localPosition = basePos[i]; } }
        if(frozen) { Time.timeScale = savedScale; AudioListener.pause = false; }
        view.SetPrompt(null, 0.0f);
        view.SetRing(false, 1.0f);
        CurrentProgress = 1.0f;
        busyCount = Mathf.Max(0, busyCount - 1);
    }

    private void WriteValues(float progress, int count)
    {
        if(!string.IsNullOrEmpty(progressValue)) { PresentationValues.Set(progressValue, progress); }
        if(!string.IsNullOrEmpty(countValue)) { PresentationValues.Set(countValue, count); }
    }
}
