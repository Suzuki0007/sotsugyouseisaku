/*********************************************************************/
// * \file   EffectInstance.cs
// * \brief  出ているエフェクト1つ分の制御クラス (EffectManager が自動で付ける)
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 出ているエフェクト1つ分の制御 (再生・終了判定・追従)
/// EffectManager が自動で付けて管理するので、自分で付ける必要はない
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]// コンポーネント追加メニューには出さない
public class EffectInstance : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    private enum State { Idle, Playing, Stopping }

    private State state = State.Idle;

    // 生成時にキャッシュするもの
    private bool spriteMode;
    private SpriteRenderer spriteRenderer;
    private Material defaultSpriteMaterial;
    private ParticleSystem[] particles = new ParticleSystem[0];
    private Animator[] animators = new Animator[0];
    private IEffectFinishable[] finishables = new IEffectFinishable[0];
    private Renderer[] renderers = new Renderer[0];
    private int[] originalLayers = new int[0];
    private int[] originalOrders = new int[0];
    private Vector3 baseScale = Vector3.one;

    // 再生ごとに変わるもの
    private float elapsed;
    private Transform follow;
    private bool hasFollow;
    private bool followRotation;
    private Vector3 offset;
    private float angle;
    private bool useDuration;// 終了判定できるものがないとき、秒数で消す
    private bool stopImmediately;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public EffectData Data { get; private set; }

    /// <summary> プールの識別用 (スプライト連番なら null) </summary>
    internal GameObject PoolKey { get; private set; }

    internal int Serial { get; private set; }

    internal float StartTime { get; private set; }

    public bool IsPlaying => state != State.Idle;

    public bool IsStopping => state == State.Stopping;

    //=====================================================================
    // 初期化 (EffectManager から呼ばれる)
    //=====================================================================

    /// <summary>
    /// 生成直後に1回だけ呼ばれる
    /// </summary>
    internal void Setup(GameObject poolKey)
    {
        PoolKey = poolKey;
        spriteMode = poolKey == null;
        baseScale = transform.localScale;

        if(spriteMode)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultSpriteMaterial = spriteRenderer.sharedMaterial;
            renderers = new Renderer[] { spriteRenderer };
        }
        else
        {
            particles = GetComponentsInChildren<ParticleSystem>(true);
            animators = GetComponentsInChildren<Animator>(true);
            finishables = GetComponentsInChildren<IEffectFinishable>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        originalLayers = new int[renderers.Length];
        originalOrders = new int[renderers.Length];
        for(int i = 0; i < renderers.Length; i++)
        {
            originalLayers[i] = renderers[i].sortingLayerID;
            originalOrders[i] = renderers[i].sortingOrder;
        }
    }

    /// <summary>
    /// 再生を始める
    /// </summary>
    internal void Begin(EffectData data, Vector3 position, Vector2 extraOffset, float rotation,
                        Transform followTarget, bool rotateWithTarget, int serial, float now)
    {
        Data = data;
        Serial = serial;
        StartTime = now;
        elapsed = 0.0f;
        state = State.Playing;

        follow = followTarget;
        hasFollow = followTarget != null;
        followRotation = rotateWithTarget && hasFollow;
        offset = (Vector3)(data.Offset + extraOffset);
        angle = rotation + (data.RandomRotation ? Random.Range(0.0f, 360.0f) : 0.0f);

        // 大きさ・反転
        bool flip = data.RandomFlipX && Random.value < 0.5f;
        Vector3 s = baseScale * data.PickScale();
        if(flip && !spriteMode) { s.x = -s.x; }
        transform.localScale = s;

        // 位置・向き
        ApplyTransform(hasFollow ? follow.position : position);

        // 表示順
        for(int i = 0; i < renderers.Length; i++)
        {
            if(renderers[i] == null) { continue; }
            renderers[i].sortingLayerID = data.OverrideSorting ? data.SortingLayerID : originalLayers[i];
            renderers[i].sortingOrder   = data.OverrideSorting ? data.SortingOrder   : originalOrders[i];
        }

        if(spriteMode)
        {
            spriteRenderer.flipX = flip;
            spriteRenderer.color = data.Color;
            spriteRenderer.sharedMaterial = data.Material != null ? data.Material : defaultSpriteMaterial;
            spriteRenderer.sprite = data.HasFrames ? data.Frames[0] : null;
            useDuration = false;
        }
        else
        {
            foreach(ParticleSystem ps in particles)
            {
                if(ps == null) { continue; }
                ParticleSystem.MainModule main = ps.main;
                main.useUnscaledTime = data.IgnoreTimeScale;
                ps.Clear(false);
                ps.Play(false);
            }

            foreach(Animator animator in animators)
            {
                if(animator == null) { continue; }
                animator.updateMode = data.IgnoreTimeScale ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
                animator.Rebind();
                animator.Update(0.0f);
            }

            foreach(IEffectFinishable f in finishables) { f.OnEffectPlay(); }

            // 終わりを判定できるものが何もなければ、秒数で消す
            useDuration = particles.Length == 0 && animators.Length == 0 && finishables.Length == 0;
        }
    }

    /// <summary>
    /// プールに戻すときの片付け
    /// </summary>
    internal void ResetForPool()
    {
        state = State.Idle;
        Data = null;
        Serial = 0;
        follow = null;
        hasFollow = false;
        if(spriteMode) { spriteRenderer.sprite = null; }
    }

    //=====================================================================
    // 更新 (EffectManager から毎フレーム呼ばれる)
    //=====================================================================

    /// <summary>
    /// 1フレーム進める。終わったら false を返す
    /// </summary>
    internal bool Tick(float deltaTime)
    {
        if(state == State.Idle || Data == null) { return false; }

        elapsed += deltaTime;

        // 追従
        if(hasFollow)
        {
            if(follow == null || !follow.gameObject.activeInHierarchy)
            {
                // 追従相手が消えた : ループならフェードアウト、そうでなければその場で最後まで再生
                hasFollow = false;
                follow = null;
                if(Data.IsLoop) { Stop(false); }
            }
            else
            {
                ApplyTransform(follow.position);
            }
        }

        if(state == State.Stopping) { return !IsStopFinished(); }

        // 秒数指定
        if(Data.Lifetime == EffectLifetime.Duration && elapsed >= Data.Duration)
        {
            Stop(false);
            return state != State.Idle && !IsStopFinished();
        }

        return spriteMode ? TickSprite() : TickPrefab();
    }

    private bool TickSprite()
    {
        Sprite[] frames = Data.Frames;
        if(frames == null || frames.Length == 0) { return false; }

        // 「自動」は1回で終わり、それ以外 (秒数指定・ループ) は繰り返す
        bool loop = Data.Lifetime != EffectLifetime.Auto;
        int index = SpriteFrameTiming.FrameAt(elapsed, frames.Length, Data.Fps, loop, out bool finished);
        if(finished) { return false; }

        spriteRenderer.sprite = frames[index];
        return true;
    }

    private bool TickPrefab()
    {
        if(Data.Lifetime != EffectLifetime.Auto) { return true; }

        // 判定できるものがない場合は秒数で消す
        if(useDuration) { return elapsed < Data.Duration; }

        // 最初の1フレームは終了判定しない (再生開始直後は判定が不安定なため)
        if(elapsed <= 0.0f) { return true; }

        return !(AreParticlesDone() && AreAnimatorsDone() && AreFinishablesDone());
    }

    //=====================================================================
    // 停止
    //=====================================================================

    /// <summary>
    /// 止める
    /// immediate = false ならパーティクルの放出だけ止めて、残りが消えるのを待つ
    /// </summary>
    internal void Stop(bool immediate)
    {
        if(state == State.Idle) { return; }

        if(state == State.Playing)
        {
            foreach(IEffectFinishable f in finishables) { f.OnEffectStop(); }
        }

        if(immediate || (particles.Length == 0 && finishables.Length == 0))
        {
            // すぐ消す (呼び出し側の EffectManager がプールに戻す)
            foreach(ParticleSystem ps in particles)
            {
                if(ps != null) { ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); }
            }
            state = State.Stopping;
            stopImmediately = true;
            return;
        }

        // パーティクルは放出だけ止めて、出ている粒が消えるのを待つ
        foreach(ParticleSystem ps in particles)
        {
            if(ps != null) { ps.Stop(false, ParticleSystemStopBehavior.StopEmitting); }
        }
        state = State.Stopping;
        stopImmediately = false;
    }

    private bool IsStopFinished()
    {
        if(stopImmediately) { return true; }
        return AreParticlesDone() && AreFinishablesDone();
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private void ApplyTransform(Vector3 basePosition)
    {
        if(followRotation)
        {
            transform.SetPositionAndRotation(
                basePosition + follow.rotation * offset,
                follow.rotation * Quaternion.Euler(0.0f, 0.0f, angle));
        }
        else
        {
            transform.SetPositionAndRotation(basePosition + offset, Quaternion.Euler(0.0f, 0.0f, angle));
        }
    }

    private bool AreParticlesDone()
    {
        foreach(ParticleSystem ps in particles)
        {
            if(ps != null && ps.IsAlive(false)) { return false; }
        }
        return true;
    }

    private bool AreAnimatorsDone()
    {
        foreach(Animator animator in animators)
        {
            if(animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) { continue; }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if(info.loop || info.normalizedTime < 1.0f || animator.IsInTransition(0)) { return false; }
        }
        return true;
    }

    private bool AreFinishablesDone()
    {
        foreach(IEffectFinishable f in finishables)
        {
            if(f != null && !f.IsEffectFinished) { return false; }
        }
        return true;
    }
}
