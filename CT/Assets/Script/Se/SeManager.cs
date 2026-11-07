/*********************************************************************/
// * \file   SeManager.cs
// * \brief  SE(効果音)再生管理クラス
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// ループSEなどを後から止めるためのハンドル
/// </summary>
public readonly struct SeHandle
{
    internal readonly int Slot;
    internal readonly int Serial;

    internal SeHandle(int slot, int serial)
    {
        Slot = slot;
        Serial = serial;
    }

    /// <summary>
    /// 再生できたかどうか (何も鳴らなかった場合は false)
    /// </summary>
    public bool IsValid => Serial != 0;

    public static readonly SeHandle None = default;
}

/// <summary>
/// SEを再生するクラス
/// BgmManager と同様、シーンに配置しなくても初回アクセス時に自動生成され、シーンをまたいで存続する
/// 設定 (音量・出力先など) を変えたい場合はシーンに配置してもよい
///
/// 使い方:
///   SeManager.PlaySe(se);                       // 2D で鳴らす
///   SeManager.PlaySe(se, transform.position);   // 位置を指定して鳴らす (SeData の spatialBlend が 3D寄りのとき有効)
///   SeHandle h = SeManager.PlaySe(loopSe);      // ループSE
///   SeManager.StopSe(h);                        // ループSEを止める
/// </summary>
public class SeManager : MonoBehaviour
{
    //=====================================================================
    // シングルトン
    //=====================================================================

    private static SeManager instance;
    private static bool isQuitting;

    /// <summary>
    /// インスタンス (存在しなければ自動生成)
    /// </summary>
    public static SeManager Instance
    {
        get
        {
            if(instance == null && !isQuitting)
            {
                instance = new GameObject(nameof(SeManager)).AddComponent<SeManager>();
            }
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    /// <summary>
    /// Playモード開始時に static 変数をリセットする
    /// このプロジェクトは「Enter Play Mode Options」でドメインリロードを無効にしているため、
    /// リセットしないと前回のPlay終了時の isQuitting = true が残り、2回目以降のPlayでSEが一切鳴らなくなる
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("SE全体の音量")]
    [Range(0.0f, 1.0f)]
    private float masterVolume = 1.0f;

    [SerializeField, Tooltip("出力先のAudioMixerGroup (任意)")]
    private AudioMixerGroup outputGroup;

    [SerializeField, Tooltip("同時に鳴らせるSEの数 (足りなくなったら一番古いものを止める)")]
    private int poolSize = 16;

    private class Slot
    {
        public AudioSource source;
        public SeData data;
        public Transform follow;
        public bool hasFollow;
        public int serial;
        public float baseVolume;
        public float startTime;
    }

    private Slot[] slots;
    private int serialCounter;

    // SEごとの最後に鳴らした時間 (連打対策)
    private readonly Dictionary<SeData, float> lastPlayTimes = new Dictionary<SeData, float>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }
    }

    //=====================================================================
    // 静的な呼び出し (データが null なら何もしない)
    //=====================================================================

    /// <summary>
    /// SEを鳴らす (2D)
    /// </summary>
    public static SeHandle PlaySe(SeData data)
    {
        if(data == null || !Application.isPlaying) { return SeHandle.None; }
        SeManager manager = Instance;
        return manager != null ? manager.Play(data, Vector3.zero, null) : SeHandle.None;
    }

    /// <summary>
    /// 位置を指定してSEを鳴らす
    /// </summary>
    public static SeHandle PlaySe(SeData data, Vector3 position)
    {
        if(data == null || !Application.isPlaying) { return SeHandle.None; }
        SeManager manager = Instance;
        return manager != null ? manager.Play(data, position, null) : SeHandle.None;
    }

    /// <summary>
    /// 対象に追従しながらSEを鳴らす (ループSE向け)
    /// </summary>
    public static SeHandle PlaySe(SeData data, Transform follow)
    {
        if(data == null || !Application.isPlaying) { return SeHandle.None; }
        SeManager manager = Instance;
        Vector3 position = follow != null ? follow.position : Vector3.zero;
        return manager != null ? manager.Play(data, position, follow) : SeHandle.None;
    }

    /// <summary>
    /// ハンドルで指定したSEを止める
    /// </summary>
    public static void StopSe(SeHandle handle)
    {
        if(!handle.IsValid || instance == null) { return; }
        instance.Stop(handle);
    }

    /// <summary>
    /// 指定したSEをすべて止める
    /// </summary>
    public static void StopSe(SeData data)
    {
        if(data == null || instance == null) { return; }
        instance.StopAll(data);
    }

    /// <summary>
    /// 鳴っているSEをすべて止める
    /// </summary>
    public static void StopAllSe()
    {
        if(instance == null) { return; }
        instance.StopAll(null);
    }

    /// <summary>
    /// ループSEだけをすべて止める (シーン遷移時など。鳴り切りのSEはそのまま)
    /// </summary>
    public static void StopAllLoopSe()
    {
        if(instance == null || instance.slots == null) { return; }

        foreach(Slot slot in instance.slots)
        {
            if(slot.data == null || !slot.source.loop) { continue; }
            slot.source.Stop();
            Release(slot);
        }
    }

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        poolSize = Mathf.Max(1, poolSize);
        slots = new Slot[poolSize];
        for(int i = 0; i < slots.Length; i++)
        {
            GameObject child = new GameObject($"SeSource{i}");
            child.transform.SetParent(transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = outputGroup;

            slots[i] = new Slot { source = source };
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if(instance == this)
        {
            instance = null;
        }
    }

    private void LateUpdate()
    {
        if(slots == null) { return; }

        foreach(Slot slot in slots)
        {
            if(slot.data == null) { continue; }

            // 鳴り終わったスロットを空ける
            if(!slot.source.isPlaying)
            {
                Release(slot);
                continue;
            }

            if(!slot.hasFollow) { continue; }

            // 追従対象が消えた・無効になったらSEも止める
            if(slot.follow == null || !slot.follow.gameObject.activeInHierarchy)
            {
                slot.source.Stop();
                Release(slot);
                continue;
            }
            slot.source.transform.position = slot.follow.position;
        }
    }

    //=====================================================================
    // 再生関数
    //=====================================================================

    /// <summary>
    /// SEを鳴らす
    /// </summary>
    public SeHandle Play(SeData data, Vector3 position, Transform follow)
    {
        if(data == null || slots == null) { return SeHandle.None; }

        AudioClip clip = data.PickClip();
        if(clip == null)
        {
            Debug.LogWarning($"SeData '{data.name}' に音声ファイルが設定されていません", data);
            return SeHandle.None;
        }

        // 「Preload Audio Data」がオフの音声は、ここで読み込んでおく
        if(clip.loadState == AudioDataLoadState.Unloaded) { clip.LoadAudioData(); }

        float now = Time.unscaledTime;

        // 連打対策: 間隔
        if(data.MinInterval > 0.0f &&
           lastPlayTimes.TryGetValue(data, out float last) &&
           now - last < data.MinInterval)
        {
            return SeHandle.None;
        }

        // 連打対策: 同時再生数 (超えたら一番古いものを止める)
        if(data.MaxConcurrent > 0)
        {
            int count = 0;
            Slot oldest = null;
            foreach(Slot s in slots)
            {
                if(s.data != data || !s.source.isPlaying) { continue; }
                count++;
                if(oldest == null || s.startTime < oldest.startTime) { oldest = s; }
            }
            if(count >= data.MaxConcurrent && oldest != null)
            {
                oldest.source.Stop();
                Release(oldest);
            }
        }

        Slot slot = FindFreeSlot();
        lastPlayTimes[data] = now;

        AudioSource source = slot.source;
        source.Stop();
        source.clip = clip;
        source.loop = data.Loop;
        source.pitch = data.PickPitch();
        source.spatialBlend = data.SpatialBlend;
        source.outputAudioMixerGroup = data.OutputGroup != null ? data.OutputGroup : outputGroup;
        source.transform.position = position;

        slot.data = data;
        slot.follow = follow;
        slot.hasFollow = follow != null;
        slot.baseVolume = data.Volume;
        slot.startTime = now;
        slot.serial = ++serialCounter;
        if(serialCounter == int.MaxValue) { serialCounter = 0; }

        source.volume = slot.baseVolume * masterVolume;
        source.Play();

        return new SeHandle(System.Array.IndexOf(slots, slot), slot.serial);
    }

    /// <summary>
    /// ハンドルで指定したSEを止める
    /// </summary>
    public void Stop(SeHandle handle)
    {
        if(!handle.IsValid || slots == null) { return; }
        if(handle.Slot < 0 || handle.Slot >= slots.Length) { return; }

        Slot slot = slots[handle.Slot];
        // 別のSEに使い回されていたら止めない
        if(slot.serial != handle.Serial) { return; }

        slot.source.Stop();
        Release(slot);
    }

    /// <summary>
    /// 指定したSEをすべて止める (null ならすべて)
    /// </summary>
    public void StopAll(SeData data)
    {
        if(slots == null) { return; }

        foreach(Slot slot in slots)
        {
            if(slot.data == null) { continue; }
            if(data != null && slot.data != data) { continue; }

            slot.source.Stop();
            Release(slot);
        }
    }

    //=====================================================================
    // 内部処理
    //=====================================================================

    private Slot FindFreeSlot()
    {
        Slot oldest = slots[0];
        foreach(Slot slot in slots)
        {
            if(slot.data == null || !slot.source.isPlaying) { return slot; }

            // ループSEはなるべく止めない
            bool better = (oldest.source.loop && !slot.source.loop) ||
                          (oldest.source.loop == slot.source.loop && slot.startTime < oldest.startTime);
            if(better) { oldest = slot; }
        }

        oldest.source.Stop();
        Release(oldest);
        return oldest;
    }

    private static void Release(Slot slot)
    {
        slot.data = null;
        slot.follow = null;
        slot.hasFollow = false;
        slot.serial = 0;
        slot.source.clip = null;
        slot.source.loop = false;
    }

    private void ApplyVolumes()
    {
        if(slots == null) { return; }
        foreach(Slot slot in slots)
        {
            if(slot.data == null) { continue; }
            slot.source.volume = slot.baseVolume * masterVolume;
        }
    }
}
