/*********************************************************************/
// * \file   SpriteFrameBlend.cs
// * \brief  コマを混ぜるアニメ：連番アニメのコマとコマの間をなめらかに混ぜ（線形補間）、水のようにゆらす（波・水面・炎など）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// コマを混ぜるアニメ（水・炎など）
/// </summary>
///
/// ・アニメのデータ（SpriteAnimData）の動き（Idle など）のコマを順に流し、コマが替わる間を少しずつ混ぜる（パッと切り替わらない）
/// ・水のゆれ：絵を横・縦に少しずつずらして、水面がゆらいでいるように見せる（0 にすると止まる）
/// ・シェーダー「CT/Sprite Frame Blend Wave」のマテリアル（Assets/Shaders/Mat_SpriteFrameBlendWave）を使う。明かり（Light 2D）には反応しない
/// ・同じオブジェクトの SpriteAnimator は止める（絵を替える部品が2つあるとぶつかるため）
[AddComponentMenu("見た目/コマを混ぜるアニメ（水・炎など）")]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameBlend : MonoBehaviour
{
    public enum BlendCurve
    {
        [InspectorName("一定の速さ（直線）")]
        Linear = 0,

        [InspectorName("なめらか（ゆっくり始まりゆっくり終わる）")]
        Smooth = 1,
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("アニメのデータ")]
    [Tooltip("コマの並び・速さ（Fps）はこのデータの動きのものを使う（並びを変えるときはデータの Down の欄を並べ替える）")]
    private SpriteAnimData data;

    [SerializeField, Label("動きの名前")]
    [Tooltip("データの中の動きの名前（ふつうは Idle）")]
    private string clipName = "Idle";

    [SerializeField, Label("速さ", 0.0f)]
    [Tooltip("1 でデータの Fps のまま。0.5 で半分の速さ")]
    private float speed = 1.0f;

    [Header("コマの間を混ぜる")]

    [SerializeField, Label("混ぜる")]
    [Tooltip("オフにすると、ふつうの連番アニメ（パッと切り替わる）")]
    private bool blend = true;

    [SerializeField, Label("混ぜ方")]
    private BlendCurve curve = BlendCurve.Smooth;

    [SerializeField, Label("そのまま見せる割合", 0.0f, 0.9f)]
    [Tooltip("1コマの時間のうち、混ぜずにそのコマだけを見せる割合（0 ならずっと混ざっている。0.3 なら 3 割はそのコマ、残りで次へ移る）")]
    private float hold = 0.0f;

    [SerializeField, Label("水の高さを合わせて動かす")]
    [Tooltip("混ぜている間、今のコマを次のコマの水の高さへ上下に動かす（波が寄せて引くように見える。二重に見えにくい）。高さは「コマごとの水の高さ」")]
    private bool followWaterline = true;

    [SerializeField]
    [Tooltip("コマごとの水の高さ（絵の一番上の色がある所。0〜1）。データを替えたら右上の︙ > 「水の高さを測り直す」。空ならエディタで自動で測る")]
    private float[] frameTops = new float[0];

    [Header("水のゆれ")]

    [SerializeField, Label("横のゆれ（ピクセル）", 0.0f)]
    [Tooltip("絵を横にずらす大きさ（絵のピクセル）。0 なら横にゆれない")]
    private float waveX = 3.0f;

    [SerializeField, Label("縦のゆれ（ピクセル）", 0.0f)]
    [Tooltip("絵を縦にずらす大きさ（絵のピクセル）。0 なら縦にゆれない")]
    private float waveY = 1.5f;

    [SerializeField, Label("波の細かさ", 0.1f)]
    [Tooltip("大きいほど細かい波（絵の高さに入る波の数の目安）")]
    private float waveFrequency = 3.0f;

    [SerializeField, Label("ゆれる速さ", 0.0f)]
    private float waveSpeed = 1.5f;

    [Header("その他")]

    [SerializeField, Label("シーンの明るさに合わせる")]
    [Tooltip("シーンの全体の明かり（Global Light 2D）の色と強さで暗くする（夜のマップなど）。このシェーダーは明かりに反応しないので、ほかの絵と明るさをそろえるため")]
    private bool followGlobalLight = true;

    [SerializeField, Label("使うマテリアル")]
    [Tooltip("Assets/Shaders/Mat_SpriteFrameBlendWave（シェーダー CT/Sprite Frame Blend Wave）。空なら今のマテリアルのまま（混ぜ・ゆれは出ない）")]
    private Material material;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock block;
    private Sprite[] frames;
    private float fps = 8.0f;
    private float time;

    private static readonly int NextTexId = Shader.PropertyToID("_NextTex");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int UvAId = Shader.PropertyToID("_UvA");
    private static readonly int UvBId = Shader.PropertyToID("_UvB");
    private static readonly int WaveId = Shader.PropertyToID("_Wave");
    private static readonly int WaveTimeId = Shader.PropertyToID("_WaveTime");
    private static readonly int ShiftId = Shader.PropertyToID("_Shift");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private UnityEngine.Rendering.Universal.Light2D globalLight;
    private bool globalLightSearched;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        block = new MaterialPropertyBlock();
        if(material != null) { spriteRenderer.sharedMaterial = material; }

        // 絵を替える部品がもう1つあるとぶつかるので止める
        SpriteAnimator other = GetComponent<SpriteAnimator>();
        if(other != null) { other.enabled = false; }

        LoadFrames();
    }

    private void LoadFrames()
    {
        SpriteAnimClip clip = data != null ? data.Find(clipName) : null;
        frames = clip != null ? clip.down : null;
        fps = clip != null ? Mathf.Max(0.01f, clip.fps) : 8.0f;
    }

    private void Update()
    {
        time += Time.deltaTime * speed;
        EvaluateAt(time);
    }

    /// <summary>
    /// その時間の見た目にする（確かめ用にも使う）
    /// </summary>
    public void EvaluateAt(float seconds)
    {
        if(spriteRenderer == null) { Awake(); }
        if(frames == null || frames.Length == 0) { return; }

        float pos = seconds * fps;
        int index = Mathf.FloorToInt(pos) % frames.Length;
        if(index < 0) { index += frames.Length; }
        int next = (index + 1) % frames.Length;

        float t = 0.0f;
        if(blend)
        {
            float f = pos - Mathf.Floor(pos);
            t = f <= hold ? 0.0f : (f - hold) / Mathf.Max(0.0001f, 1.0f - hold);
            if(curve == BlendCurve.Smooth) { t = t * t * (3.0f - 2.0f * t); }
        }

        Sprite a = frames[index];
        Sprite b = frames[next] != null ? frames[next] : a;
        if(a == null) { return; }
        spriteRenderer.sprite = a;

        spriteRenderer.GetPropertyBlock(block);
        block.SetTexture(NextTexId, b.texture);
        block.SetVector(UvAId, UvRect(a));
        block.SetVector(UvBId, UvRect(b));
        block.SetFloat(BlendId, t);
        block.SetVector(WaveId, new Vector4(waveX, waveY, waveFrequency, waveSpeed));
        block.SetFloat(WaveTimeId, seconds);

        // 水の高さを合わせて動かす：今のコマは d×t 上へ、次のコマは -d×(1-t) から 0 へ（d＝次の高さ − 今の高さ）
        Vector4 shift = Vector4.zero;
        if(followWaterline && blend && frameTops != null && frameTops.Length == frames.Length)
        {
            float d = frameTops[next] - frameTops[index];
            // z：下の端から、低い方の水の高さの半分までで動きを 0→1 にする（深い所は動かさず、白い波の帯は丸ごと動かす）
            float ramp = Mathf.Min(frameTops[index], frameTops[next]) * 0.5f;
            shift = new Vector4(d * t, -d * (1.0f - t), ramp, 0.0f);
        }
        block.SetVector(ShiftId, shift);
        block.SetColor(ColorId, LightTint());
        spriteRenderer.SetPropertyBlock(block);
    }

    /// <summary> シーンの全体の明かり（Global Light 2D）の色×強さ。なければ白 </summary>
    private Color LightTint()
    {
        if(!followGlobalLight) { return Color.white; }
        if(!globalLightSearched || (globalLight == null && Application.isPlaying == false))
        {
            globalLightSearched = true;
            globalLight = null;
            foreach(var l in FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>())
            {
                if(l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global && l.gameObject.scene == gameObject.scene && l.isActiveAndEnabled) { globalLight = l; break; }
            }
        }
        if(globalLight == null || !globalLight.isActiveAndEnabled) { return Color.white; }
        Color c = globalLight.color * globalLight.intensity;
        c.a = 1.0f;
        return c;
    }

    /// <summary> 絵がテクスチャのどこにあるか（uv の左下と大きさ） </summary>
    private static Vector4 UvRect(Sprite s)
    {
        Rect r = s.textureRect;
        float w = s.texture.width;
        float h = s.texture.height;
        return new Vector4(r.x / w, r.y / h, r.width / w, r.height / h);
    }

    /// <summary> 確かめ用：今のコマ・次のコマ・混ぜ具合 </summary>
    public string Describe(float seconds)
    {
        if(frames == null || frames.Length == 0) { return "コマがない"; }
        float pos = seconds * fps;
        int index = Mathf.FloorToInt(pos) % frames.Length;
        return frames[index].name + " → " + frames[(index + 1) % frames.Length].name;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // マテリアルとアニメのデータを、置いたときに入れておく
        material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Mat_SpriteFrameBlendWave.mat");
        SpriteAnimator anim = GetComponent<SpriteAnimator>();
        if(anim != null && anim.Data != null) { data = anim.Data; }
        MeasureWaterline();
    }

    private void OnValidate()
    {
        // コマの数と高さの数が合わないときだけ測り直す（データを替えたとき）
        SpriteAnimClip clip = data != null ? data.Find(clipName) : null;
        int n = clip != null && clip.down != null ? clip.down.Length : 0;
        if(n > 0 && (frameTops == null || frameTops.Length != n)) { UnityEditor.EditorApplication.delayCall += () => { if(this != null) { MeasureWaterline(); } }; }
        frames = null;
        spriteRenderer = null;
    }

    /// <summary>
    /// コマごとの水の高さ（一番上の色がある行）を、元の画像ファイルから測る
    /// </summary>
    [ContextMenu("水の高さを測り直す")]
    public void MeasureWaterline()
    {
        SpriteAnimClip clip = data != null ? data.Find(clipName) : null;
        if(clip == null || clip.down == null) { return; }
        var tops = new float[clip.down.Length];
        var cache = new System.Collections.Generic.Dictionary<string, Texture2D>();
        for(int i = 0; i < tops.Length; i++)
        {
            Sprite s = clip.down[i];
            if(s == null) { continue; }
            string path = UnityEditor.AssetDatabase.GetAssetPath(s.texture);
            if(!cache.TryGetValue(path, out Texture2D src))
            {
                src = new Texture2D(2, 2);
                ImageConversion.LoadImage(src, System.IO.File.ReadAllBytes(path));
                cache[path] = src;
            }
            // 取り込みで縮んでいても、元の画像の大きさに直して読む
            float scale = (float)src.width / s.texture.width;
            Rect r = s.textureRect;
            int x0 = Mathf.RoundToInt(r.x * scale), y0 = Mathf.RoundToInt(r.y * scale);
            int w = Mathf.RoundToInt(r.width * scale), h = Mathf.RoundToInt(r.height * scale);
            int top = 0;
            for(int y = h - 1; y >= 0 && top == 0; y--)
            {
                for(int x = 0; x < w; x += 4) { if(src.GetPixel(x0 + x, y0 + y).a > 0.5f) { top = y; break; } }
            }
            tops[i] = h > 0 ? (float)top / h : 0.0f;
        }
        foreach(Texture2D t in cache.Values) { DestroyImmediate(t); }
        UnityEditor.Undo.RecordObject(this, "水の高さを測る");
        frameTops = tops;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
