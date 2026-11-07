/*********************************************************************/
// * \file   EffectData.cs
// * \brief  エフェクト1種類分の設定データ
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// エフェクトの表示方法
/// </summary>
public enum EffectDisplayMode
{
    [InspectorName("スプライト連番 (画像を並べるだけ)")]
    SpriteFrames,

    [InspectorName("Prefab (パーティクルなど)")]
    Prefab,
}

/// <summary>
/// エフェクトがいつ消えるか
/// </summary>
public enum EffectLifetime
{
    [InspectorName("自動 (アニメ・パーティクルが終わったら)")]
    Auto,

    [InspectorName("秒数を指定")]
    Duration,

    [InspectorName("ループ (止めるまで出し続ける)")]
    Loop,
}

/// <summary>
/// エフェクト1種類分の設定 (ScriptableObject)
///
/// 作り方:
///   ・画像を選択して右クリック → Create → Effect → EffectData (〜から作成)
///   ・または 右クリック → Create → Effect → EffectData (空) で作って、あとから画像を入れる
///
/// 出し方:
///   ・コードを書かない場合 : 出したいオブジェクトに EffectTrigger を付けて、このデータを指定する
///   ・コードから出す場合   : EffectManager.Play(effectData, transform.position);
/// </summary>
[CreateAssetMenu(menuName = "Effect/EffectData (空)", fileName = "Fx_New", order = 10)]
public class EffectData : ScriptableObject
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("エフェクトの見た目の作り方\n" +
             "・スプライト連番 : 画像を並べてパラパラ漫画のように再生する (おすすめ)\n" +
             "・Prefab : パーティクルなど、作り込んだエフェクトを出す")]
    [SerializeField]
    private EffectDisplayMode displayMode = EffectDisplayMode.SpriteFrames;

    // --- スプライト連番 ---

    [Tooltip("再生する画像 (上から順に再生される)")]
    [SerializeField]
    private Sprite[] frames = new Sprite[0];

    [Tooltip("1秒に何枚進めるか (大きいほど速い)")]
    [SerializeField, Min(0.1f)]
    private float fps = 12.0f;

    [Tooltip("色 (白ならそのままの色。透明度もここで変えられる)")]
    [SerializeField]
    private Color color = Color.white;

    [Tooltip("マテリアル (加算合成で光らせたいときなどに設定。空ならふつうの表示)")]
    [SerializeField]
    private Material material;

    // --- Prefab ---

    [Tooltip("出すPrefab (ParticleSystem や Animator が付いていると、終わりを自動で判定できる)")]
    [SerializeField]
    private GameObject prefab;

    // --- 消え方 ---

    [Tooltip("いつ消えるか\n" +
             "・自動 : アニメ(またはパーティクル)が1回終わったら消える\n" +
             "・秒数を指定 : 指定した秒数で消える (アニメは繰り返す)\n" +
             "・ループ : 止めるまで出し続ける (追従している相手が消えたら自動で止まる)")]
    [SerializeField]
    private EffectLifetime lifetime = EffectLifetime.Auto;

    [Tooltip("消えるまでの秒数")]
    [SerializeField, Min(0.01f)]
    private float duration = 1.0f;

    // --- 位置・大きさ・向き ---

    [Tooltip("出す位置のずらし (例: Y を 0.5 にすると少し上に出る)")]
    [SerializeField]
    private Vector2 offset = Vector2.zero;

    [Tooltip("大きさ (1 = そのまま)")]
    [SerializeField, Min(0.01f)]
    private float scale = 1.0f;

    [Tooltip("大きさのばらつき (例: 0.2 なら ±20% の範囲でランダム)")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float scaleRandom = 0.0f;

    [Tooltip("出すたびに向きをランダムに回転させる")]
    [SerializeField]
    private bool randomRotation = false;

    [Tooltip("出すたびに左右反転をランダムにする")]
    [SerializeField]
    private bool randomFlipX = false;

    // --- 表示順 ---

    [Tooltip("表示順を上書きする (キャラクターの前や後ろに出したいとき)")]
    [SerializeField]
    private bool overrideSorting = true;

    [Tooltip("Sorting Layer")]
    [SerializeField, SortingLayer]
    private int sortingLayerID = 0;

    [Tooltip("Order in Layer (大きいほど手前。キャラクターは 99〜101 あたり)")]
    [SerializeField]
    private int sortingOrder = 200;

    // --- 音 ---

    [Tooltip("エフェクトと一緒に鳴らすSE (任意)")]
    [SerializeField]
    private SeData se;

    // --- 時間 ---

    [Tooltip("ポーズ中 (Time.timeScale = 0) でも動かす。UIのエフェクトなどで使う")]
    [SerializeField]
    private bool ignoreTimeScale = false;

    // --- 連打対策 ---

    [Tooltip("同じエフェクトを再び出せるまでの最短間隔(秒)。0なら制限なし")]
    [SerializeField, Min(0.0f)]
    private float minInterval = 0.0f;

    [Tooltip("同じエフェクトを同時に出せる最大数 (超えたら一番古いものを消す)。0なら制限なし")]
    [SerializeField, Min(0)]
    private int maxConcurrent = 0;

    [Tooltip("あらかじめ作っておく数 (よく出すエフェクトは増やすと最初のカクつきが減る)")]
    [SerializeField, Min(0)]
    private int preloadCount = 0;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public EffectDisplayMode DisplayMode => displayMode;
    public Sprite[] Frames => frames;
    public float Fps => fps;
    public Color Color => color;
    public Material Material => material;
    public GameObject Prefab => prefab;
    public EffectLifetime Lifetime => lifetime;
    public float Duration => duration;
    public Vector2 Offset => offset;
    public bool RandomRotation => randomRotation;
    public bool RandomFlipX => randomFlipX;
    public bool OverrideSorting => overrideSorting;
    public int SortingLayerID => sortingLayerID;
    public int SortingOrder => sortingOrder;
    public SeData Se => se;
    public bool IgnoreTimeScale => ignoreTimeScale;
    public float MinInterval => minInterval;
    public int MaxConcurrent => maxConcurrent;
    public int PreloadCount => preloadCount;

    public bool IsLoop => lifetime == EffectLifetime.Loop;

    /// <summary>
    /// スプライト連番1周の秒数
    /// </summary>
    public float FrameCycleTime => HasFrames ? frames.Length / Mathf.Max(0.1f, fps) : 0.0f;

    public bool HasFrames => frames != null && frames.Length > 0;

    /// <summary>
    /// 表示するものが設定されているか
    /// </summary>
    public bool IsValid => displayMode == EffectDisplayMode.SpriteFrames ? HasFrames : prefab != null;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 出すたびの大きさを決める
    /// </summary>
    public float PickScale()
    {
        if(scaleRandom <= 0.0f) { return scale; }
        return scale * (1.0f + Random.Range(-scaleRandom, scaleRandom));
    }
}

/// <summary>
/// int の変数を Sorting Layer の選択欄で表示するための属性
/// </summary>
public class SortingLayerAttribute : PropertyAttribute { }
