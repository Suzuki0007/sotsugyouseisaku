/*********************************************************************/
// * \file   GroundFog.cs
// * \brief  床をはう霧：ふちのぼけた霧の絵を、ゆっくり行ったり来たりさせ、濃さも少しずつ変える
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 床をはう霧（遺跡・洞窟・水辺の空気が動いて見える）
/// </summary>
///
/// ・ふちのぼけた霧の絵が、決まった向きにゆっくり行ったり来たりし、濃さも少しずつ変わる
/// ・霧の絵を 2〜4 枚、少しずつずらして重ねると自然（同じ設定でも、置いた場所で動きの時間がずれる）
/// ・Hierarchy 右クリック > マップの見た目 > 床をはう霧 で作れる。大きさは Transform の Scale
/// ・床・小物より上、キャラより下（並び順 6）。キャラの足もとは隠れない
/// ・再生中だけ動く
[AddComponentMenu("マップの見た目/床をはう霧")]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class GroundFog : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("濃さ", 0.0f, 1.0f)]
    [Tooltip("霧の濃さ（0＝見えない）。0.2〜0.35 くらい")]
    private float strength = 0.28f;

    [SerializeField, Label("濃さのゆれ", 0.0f, 1.0f)]
    [Tooltip("濃さがどれだけゆっくり変わるか（濃さに対する割合。0.3 で ±30%）")]
    private float breathe = 0.3f;

    [SerializeField, Label("動く向き")]
    [Tooltip("行ったり来たりする向き（(1, 0.1) でほぼ横）。長さは使わない")]
    private Vector2 direction = new Vector2(1.0f, 0.1f);

    [SerializeField, Label("動く幅（マス）", 0.0f, 10.0f)]
    [Tooltip("真ん中から片側に何マス動くか")]
    private float amplitude = 1.2f;

    [SerializeField, Label("1 往復の秒数", 2.0f, 120.0f)]
    [Tooltip("行って戻るまでの秒数。長いほどゆっくり（20〜30 秒くらい）")]
    private float cycleSeconds = 24.0f;

    private const float PixelSize = 1.0f / 64.0f;

    private Vector3 basePosition;
    private float phase;
    private SpriteRenderer spriteRenderer;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        basePosition = transform.position;
        // 置いた場所で動きの時間をずらす（並べても同じ動きにならない）
        phase = Mathf.Repeat(basePosition.x * 0.37f + basePosition.y * 0.71f, 1.0f) * Mathf.PI * 2.0f;
        SetAlpha(strength);
    }

    private void OnValidate()
    {
        if(spriteRenderer == null) { spriteRenderer = GetComponent<SpriteRenderer>(); }
        SetAlpha(strength);
    }

    private void OnDisable()
    {
        if(spriteRenderer != null) { transform.position = basePosition; SetAlpha(strength); }
    }

    private void SetAlpha(float a)
    {
        if(spriteRenderer == null) { return; }
        Color c = spriteRenderer.color;
        c.a = a;
        spriteRenderer.color = c;
    }

    private void LateUpdate()
    {
        float w = Mathf.PI * 2.0f / Mathf.Max(0.1f, cycleSeconds);
        float t = Time.time * w + phase;
        Vector2 d = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 o = d * (Mathf.Sin(t) * amplitude);
        o.x = Mathf.Round(o.x / PixelSize) * PixelSize;
        o.y = Mathf.Round(o.y / PixelSize) * PixelSize;
        transform.position = new Vector3(basePosition.x + o.x, basePosition.y + o.y, basePosition.z);
        // 濃さは動きと違う速さでゆっくり変える
        SetAlpha(strength * (1.0f + Mathf.Sin(t * 0.63f + 1.7f) * breathe));
    }
}
