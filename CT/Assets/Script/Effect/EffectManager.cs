/*********************************************************************/
// * \file   EffectManager.cs
// * \brief  エフェクトの生成・管理クラス
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

/// <summary>
/// エフェクトを出すクラス
/// SeManager と同様、シーンに配置しなくても初回アクセス時に自動生成され、シーンをまたいで存続する
/// 出したエフェクトは使い回す (プール) ので、何度出しても重くなりにくい
///
/// プランナー向け: コードを書かずに出すなら EffectTrigger コンポーネントを使う
///
/// コードから使う場合:
///   EffectManager.Play(hitFx, transform.position);         // その場に出す
///   EffectManager.Play(hitFx, transform.position, 90.0f);  // 向きを指定して出す
///   EffectHandle h = EffectManager.Play(auraFx, transform); // 対象に追従させる
///   EffectManager.Stop(h);                                  // 止める
///   EffectManager.StopAll();                                // すべて消す
/// </summary>
[AddComponentMenu("Effect/Effect Manager")]
public class EffectManager : MonoBehaviour
{
    //=====================================================================
    // シングルトン
    //=====================================================================

    private static EffectManager instance;
    private static bool isQuitting;

    /// <summary>
    /// インスタンス (存在しなければ自動生成)
    /// </summary>
    public static EffectManager Instance
    {
        get
        {
            if(instance == null && !isQuitting && Application.isPlaying)
            {
                instance = new GameObject(nameof(EffectManager)).AddComponent<EffectManager>();
            }
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    /// <summary>
    /// Playモード開始時に static 変数をリセットする (ドメインリロード無効対策。SeManager と同じ)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;

        // 終了中 (Playモード終了時を含む) に OnDisable などから新しく作られないようにする
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting()
    {
        isQuitting = true;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("シーンを切り替えたとき、出ているエフェクトをすべて消す")]
    private bool clearOnSceneLoad = true;

    [SerializeField, Tooltip("1種類あたりプールにためておく最大数 (超えた分は破棄される)")]
    private int maxPoolSizePerType = 64;

    // スプライト連番用のプール (全エフェクト共通)
    private ObjectPool<EffectInstance> spritePool;

    // Prefab ごとのプール
    private readonly Dictionary<GameObject, ObjectPool<EffectInstance>> prefabPools =
        new Dictionary<GameObject, ObjectPool<EffectInstance>>();

    // 表示中のエフェクト
    private readonly List<EffectInstance> active = new List<EffectInstance>();

    // エフェクトごとの最後に出した時間 (連打対策)
    private readonly Dictionary<EffectData, float> lastPlayTimes = new Dictionary<EffectData, float>();

    // 事前生成済みのエフェクト
    private readonly HashSet<EffectData> preloaded = new HashSet<EffectData>();

    private int serialCounter;

    /// <summary> 表示中のエフェクトの数 (デバッグ用) </summary>
    public int ActiveCount => active.Count;

    //=====================================================================
    // 静的な呼び出し (データが null なら何もしない)
    //=====================================================================

    /// <summary>
    /// 位置を指定してエフェクトを出す
    /// </summary>
    public static EffectHandle Play(EffectData data, Vector3 position, float angle = 0.0f)
    {
        EffectManager manager = GetManager(data);
        return manager != null ? manager.PlayInternal(data, position, Vector2.zero, angle, null, false) : EffectHandle.None;
    }

    /// <summary>
    /// 対象に追従させてエフェクトを出す
    /// </summary>
    public static EffectHandle Play(EffectData data, Transform follow, bool rotateWithTarget = false)
    {
        if(follow == null) { return EffectHandle.None; }
        EffectManager manager = GetManager(data);
        return manager != null ? manager.PlayInternal(data, follow.position, Vector2.zero, 0.0f, follow, rotateWithTarget) : EffectHandle.None;
    }

    /// <summary>
    /// すべての設定を指定してエフェクトを出す
    /// </summary>
    /// <param name="extraOffset"> EffectData の offset に足すずらし </param>
    /// <param name="follow"> 追従する対象 (null なら追従しない) </param>
    public static EffectHandle Play(EffectData data, Vector3 position, Vector2 extraOffset, float angle,
                                    Transform follow, bool rotateWithTarget)
    {
        EffectManager manager = GetManager(data);
        return manager != null ? manager.PlayInternal(data, position, extraOffset, angle, follow, rotateWithTarget) : EffectHandle.None;
    }

    /// <summary>
    /// ハンドルで指定したエフェクトを止める
    /// immediate = false ならパーティクルは自然に消えるのを待つ
    /// </summary>
    public static void Stop(EffectHandle handle, bool immediate = false)
    {
        if(!handle.IsValid || instance == null) { return; }
        if(handle.Instance.Serial != handle.Serial) { return; }// 別のエフェクトに使い回されている
        instance.StopInstance(handle.Instance, immediate);
    }

    /// <summary>
    /// 指定したエフェクトをすべて止める
    /// </summary>
    public static void StopAll(EffectData data, bool immediate = false)
    {
        if(data == null || instance == null) { return; }
        instance.StopWhere(data, false, immediate);
    }

    /// <summary>
    /// 出ているエフェクトをすべて止める
    /// </summary>
    public static void StopAll(bool immediate = true)
    {
        if(instance == null) { return; }
        instance.StopWhere(null, false, immediate);
    }

    /// <summary>
    /// ループエフェクトだけをすべて止める
    /// </summary>
    public static void StopAllLoop(bool immediate = false)
    {
        if(instance == null) { return; }
        instance.StopWhere(null, true, immediate);
    }

    /// <summary>
    /// あらかじめ作っておく (最初に出したときのカクつき対策)
    /// count を省略すると EffectData の preloadCount を使う
    /// </summary>
    public static void Preload(EffectData data, int count = -1)
    {
        EffectManager manager = GetManager(data);
        if(manager == null) { return; }
        manager.PreloadInternal(data, count < 0 ? data.PreloadCount : count);
    }

    private static EffectManager GetManager(EffectData data)
    {
        if(data == null || !Application.isPlaying) { return null; }
        return Instance;
    }

    //=====================================================================
    // 初期化
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

        spritePool = CreatePool(null);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if(instance == this) { instance = null; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(clearOnSceneLoad && mode == LoadSceneMode.Single)
        {
            StopWhere(null, false, true);
            ReleaseFinished();
        }
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void LateUpdate()
    {
        float scaled = Time.deltaTime;
        float unscaled = Time.unscaledDeltaTime;

        for(int i = active.Count - 1; i >= 0; i--)
        {
            EffectInstance fx = active[i];
            if(fx == null) { active.RemoveAt(i); continue; }// 外部から破棄された

            float dt = (fx.Data != null && fx.Data.IgnoreTimeScale) ? unscaled : scaled;
            if(!fx.Tick(dt))
            {
                active.RemoveAt(i);
                Release(fx);
            }
        }
    }

    //=====================================================================
    // 再生
    //=====================================================================

    private EffectHandle PlayInternal(EffectData data, Vector3 position, Vector2 extraOffset, float angle,
                                      Transform follow, bool rotateWithTarget)
    {
        if(!data.IsValid)
        {
            Debug.LogWarning($"EffectData '{data.name}' に画像 (またはPrefab) が設定されていません", data);
            return EffectHandle.None;
        }

        float now = Time.unscaledTime;

        // 連打対策: 間隔
        if(data.MinInterval > 0.0f &&
           lastPlayTimes.TryGetValue(data, out float last) &&
           now - last < data.MinInterval)
        {
            return EffectHandle.None;
        }

        // 連打対策: 同時に出せる数 (超えたら一番古いものを消す)
        if(data.MaxConcurrent > 0)
        {
            int count = 0;
            EffectInstance oldest = null;
            foreach(EffectInstance fx in active)
            {
                if(fx == null || fx.Data != data || fx.IsStopping) { continue; }
                count++;
                if(oldest == null || fx.StartTime < oldest.StartTime) { oldest = fx; }
            }
            if(count >= data.MaxConcurrent && oldest != null)
            {
                StopInstance(oldest, true);
            }
        }

        // 初回は事前生成もしておく
        if(data.PreloadCount > 0 && !preloaded.Contains(data))
        {
            PreloadInternal(data, data.PreloadCount);
        }

        ObjectPool<EffectInstance> pool = GetPool(data);
        EffectInstance instanceFx = pool.Get();

        serialCounter++;
        if(serialCounter == int.MaxValue) { serialCounter = 1; }

        instanceFx.Begin(data, position, extraOffset, angle, follow, rotateWithTarget, serialCounter, now);
        active.Add(instanceFx);
        lastPlayTimes[data] = now;

        // 一緒に鳴らすSE
        if(data.Se != null)
        {
            if(follow != null) { SeManager.PlaySe(data.Se, follow); }
            else               { SeManager.PlaySe(data.Se, instanceFx.transform.position); }
        }

        return new EffectHandle(instanceFx, serialCounter);
    }

    private void StopInstance(EffectInstance fx, bool immediate)
    {
        if(fx == null || !fx.IsPlaying) { return; }
        fx.Stop(immediate);

        // すぐ消す場合はこの場でプールに戻す
        if(immediate)
        {
            active.Remove(fx);
            Release(fx);
        }
    }

    private void StopWhere(EffectData data, bool loopOnly, bool immediate)
    {
        for(int i = active.Count - 1; i >= 0; i--)
        {
            EffectInstance fx = active[i];
            if(fx == null) { continue; }
            if(data != null && fx.Data != data) { continue; }
            if(loopOnly && (fx.Data == null || !fx.Data.IsLoop)) { continue; }
            StopInstance(fx, immediate);
        }
    }

    private void ReleaseFinished()
    {
        active.RemoveAll(fx => fx == null);
    }

    private void PreloadInternal(EffectData data, int count)
    {
        preloaded.Add(data);
        if(count <= 0 || !data.IsValid) { return; }

        ObjectPool<EffectInstance> pool = GetPool(data);
        int need = count - pool.CountInactive;
        if(need <= 0) { return; }

        var temp = new List<EffectInstance>(need);
        for(int i = 0; i < need; i++) { temp.Add(pool.Get()); }
        foreach(EffectInstance fx in temp) { pool.Release(fx); }
    }

    //=====================================================================
    // プール
    //=====================================================================

    private ObjectPool<EffectInstance> GetPool(EffectData data)
    {
        if(data.DisplayMode == EffectDisplayMode.SpriteFrames) { return spritePool; }

        GameObject prefab = data.Prefab;
        if(!prefabPools.TryGetValue(prefab, out ObjectPool<EffectInstance> pool))
        {
            pool = CreatePool(prefab);
            prefabPools.Add(prefab, pool);
        }
        return pool;
    }

    private ObjectPool<EffectInstance> CreatePool(GameObject prefab)
    {
        return new ObjectPool<EffectInstance>(
            createFunc      : () => CreateInstance(prefab),
            actionOnGet     : fx => fx.gameObject.SetActive(true),
            actionOnRelease : fx => fx.gameObject.SetActive(false),
            actionOnDestroy : fx => { if(fx != null) { Destroy(fx.gameObject); } },
            collectionCheck : false,
            defaultCapacity : 8,
            maxSize         : Mathf.Max(1, maxPoolSizePerType));
    }

    private EffectInstance CreateInstance(GameObject prefab)
    {
        GameObject go;
        if(prefab == null)
        {
            go = new GameObject("Fx_Sprite");
            go.AddComponent<SpriteRenderer>();
        }
        else
        {
            go = Instantiate(prefab, transform);
            go.name = prefab.name;
        }

        go.SetActive(false);
        go.transform.SetParent(transform, false);

        EffectInstance fx = go.GetComponent<EffectInstance>();
        if(fx == null) { fx = go.AddComponent<EffectInstance>(); }
        fx.Setup(prefab);
        return fx;
    }

    private void Release(EffectInstance fx)
    {
        if(fx == null) { return; }
        GameObject key = fx.PoolKey;
        fx.ResetForPool();

        if(key == null)
        {
            spritePool.Release(fx);
        }
        else if(prefabPools.TryGetValue(key, out ObjectPool<EffectInstance> pool))
        {
            pool.Release(fx);
        }
        else
        {
            Destroy(fx.gameObject);
        }
    }
}
