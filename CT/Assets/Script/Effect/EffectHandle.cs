/*********************************************************************/
// * \file   EffectHandle.cs
// * \brief  出したエフェクトをあとから操作するためのハンドル
// *
// * \author
/*********************************************************************/

/// <summary>
/// ループエフェクトなどを後から止めるためのハンドル
///   EffectHandle h = EffectManager.Play(aura, transform);
///   EffectManager.Stop(h);
/// </summary>
public readonly struct EffectHandle
{
    internal readonly EffectInstance Instance;
    internal readonly int Serial;

    internal EffectHandle(EffectInstance instance, int serial)
    {
        Instance = instance;
        Serial = serial;
    }

    /// <summary>
    /// 出せたかどうか (何も出なかった場合は false)
    /// </summary>
    public bool IsValid => Serial != 0 && Instance != null;

    /// <summary>
    /// まだ表示中かどうか
    /// </summary>
    public bool IsPlaying => IsValid && Instance.Serial == Serial && Instance.IsPlaying;

    public static readonly EffectHandle None = default;
}

/// <summary>
/// Prefab に付けたスクリプトで終わり方を自分で決めたいときに実装する
/// (ParticleSystem や Animator だけでは終わりを判定できないエフェクト向け)
/// </summary>
public interface IEffectFinishable
{
    /// <summary> エフェクトが出たときに呼ばれる </summary>
    void OnEffectPlay();

    /// <summary> 止めるよう指示されたときに呼ばれる (ループ停止など) </summary>
    void OnEffectStop();

    /// <summary> true を返すとエフェクトが片付けられる </summary>
    bool IsEffectFinished { get; }
}
