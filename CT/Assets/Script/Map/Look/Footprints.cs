/*********************************************************************/
// * \file   Footprints.cs
// * \brief  足跡を置いて、時間がたったら薄くして消す（足跡が付くシーンに自動で作られる）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 足跡（置く・薄くして消す）
/// </summary>
///
/// ・`Resources/FootprintSettings.asset` の「足跡が付くシーン」に入ったとき、自動で作られる（シーンに置かなくてよい）
/// ・ノアとシグルに FootprintMaker を付け、足が着いたら Place で足跡を置く
/// ・足跡は使い回す（いちどに残る数を超えたら古い物から使う）
public class Footprints : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    /// <summary> 今のシーンの足跡（足跡が付かないシーンでは null） </summary>
    public static Footprints Active { get; private set; }

    private FootprintSettings settings;
    private Sprite placeholder;
    private Texture2D placeholderTexture;

    private struct Print
    {
        public SpriteRenderer renderer;
        public float born;
    }

    private readonly List<Print> prints = new List<Print>();
    private int next;
    private float attachTimer;

    //=====================================================================
    // 自動で作る
    //=====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Active = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(mode != LoadSceneMode.Single) { return; }

        FootprintSettings s = Resources.Load<FootprintSettings>("FootprintSettings");
        if(s == null || !s.AppliesTo(scene.name)) { return; }
        if(Active != null && Active.gameObject.scene == scene) { return; }

        var go = new GameObject("足跡");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<Footprints>().settings = s;
    }

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        Active = this;
    }

    private void OnDestroy()
    {
        if(Active == this) { Active = null; }
        if(placeholderTexture != null) { Destroy(placeholderTexture); }
        if(placeholder != null) { Destroy(placeholder); }
    }

    private void Update()
    {
        // ノアとシグルに、足跡を付ける部品を付ける（プレイヤーはシーンが始まってから作られるので、ときどき見る）
        attachTimer -= Time.deltaTime;
        if(attachTimer <= 0.0f)
        {
            attachTimer = 0.5f;
            PlayerSwitchManager party = PlayerSwitchManager.Instance;
            if(party != null)
            {
                Attach(party.Noah);
                Attach(party.Sigle);
            }
        }

        FadePrints();
    }

    private static void Attach(GameObject character)
    {
        if(character == null) { return; }
        if(character.GetComponent<FootprintMaker>() == null) { character.AddComponent<FootprintMaker>(); }
    }

    //=====================================================================
    // 足跡
    //=====================================================================

    /// <summary> 設定（FootprintMaker が使う） </summary>
    public FootprintSettings Settings => settings;

    /// <summary>
    /// 足跡を1つ置く
    /// </summary>
    /// <param name="angle"> 回す角度（度。0 でつま先が上） </param>
    /// <param name="like"> 見本の絵（キャラの絵。明かりの受け方・並びの層をそろえる） </param>
    public void Place(Vector2 position, float angle, SpriteRenderer like)
    {
        SpriteRenderer r = NextRenderer(like);
        r.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0.0f), Quaternion.Euler(0.0f, 0.0f, angle));
        r.transform.localScale = Vector3.one * settings.size;
        r.color = settings.color;
        r.gameObject.SetActive(true);

        Print p = prints[next];
        p.born = Time.time;
        prints[next] = p;
        next = (next + 1) % Mathf.Max(1, settings.maxPrints);
    }

    // 使う足跡（足りなければ作る。いちどに残る数に達したら古い物を使い回す）
    private SpriteRenderer NextRenderer(SpriteRenderer like)
    {
        if(next >= prints.Count)
        {
            var go = new GameObject("足跡");
            go.transform.SetParent(transform, false);
            SpriteRenderer created = go.AddComponent<SpriteRenderer>();
            created.sprite = settings.sprite != null ? settings.sprite : Placeholder();
            created.sortingOrder = settings.sortingOrder;
            if(like != null)
            {
                created.sharedMaterial = like.sharedMaterial;
                created.sortingLayerID = like.sortingLayerID;
            }
            prints.Add(new Print { renderer = created, born = Time.time });
        }
        return prints[next].renderer;
    }

    // 時間がたった足跡を薄くして、消えきったら隠す
    private void FadePrints()
    {
        float now = Time.time;
        float stay = settings.stayTime;
        float fade = Mathf.Max(0.01f, settings.fadeTime);
        float baseAlpha = settings.color.a;

        for(int i = 0; i < prints.Count; i++)
        {
            SpriteRenderer r = prints[i].renderer;
            if(r == null || !r.gameObject.activeSelf) { continue; }

            float age = now - prints[i].born;
            if(age < stay) { continue; }

            float k = 1.0f - (age - stay) / fade;
            if(k <= 0.0f) { r.gameObject.SetActive(false); continue; }

            Color c = r.color;
            c.a = baseAlpha * k;
            r.color = c;
        }
    }

    // 仮の足跡の絵（縦長の、ふちのぼけた楕円）
    private Sprite Placeholder()
    {
        if(placeholder != null) { return placeholder; }

        const int w = 12, h = 20;
        placeholderTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        placeholderTexture.filterMode = FilterMode.Bilinear;
        placeholderTexture.wrapMode = TextureWrapMode.Clamp;
        for(int y = 0; y < h; y++)
        {
            for(int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2.0f - 1.0f;
                float dy = (y + 0.5f) / h * 2.0f - 1.0f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1.0f - d) / 0.35f);
                placeholderTexture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, a));
            }
        }
        placeholderTexture.Apply();

        // 100 ピクセルで 1 マス → 0.12 × 0.2 マス（足の大きさ）
        placeholder = Sprite.Create(placeholderTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100.0f);
        return placeholder;
    }
}
