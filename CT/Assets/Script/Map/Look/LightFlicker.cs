/*********************************************************************/
// * \file   LightFlicker.cs
// * \brief  明かり（Light 2D）の明るさと大きさを、たいまつの火・水面のきらめきのようにゆらす
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 明かりのゆらぎ（たいまつ・水面のきらめき）
/// </summary>
///
/// ・Light 2D に付けると、明るさと光の届く大きさが少しずつゆれる（決まった間隔ではなく、自然な揺れ方）
/// ・たいまつ：ゆれの強さ 0.15・速さ 3。水面のきらめき：強さ 0.4・速さ 0.7 くらい
/// ・同じ設定の明かりを並べても、それぞれ違う揺れ方になる
/// ・Hierarchy 右クリック > マップの見た目 > 明かり（ゆらぐ）で、明かりとまとめて作れる
[AddComponentMenu("マップの見た目/明かりのゆらぎ（LightFlicker）")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("ゆれの強さ", 0.0f, 1.0f)]
    [Tooltip("明るさがどれだけゆれるか（元の明るさに対する割合。0.15 で ±15%）")]
    private float strength = 0.15f;

    [SerializeField, Label("ゆれの速さ", 0.05f, 20.0f)]
    [Tooltip("1 秒あたりのゆれの細かさ（たいまつ 3、水面 0.7 くらい）")]
    private float speed = 3.0f;

    [SerializeField, Label("大きさもゆらす", 0.0f, 0.5f)]
    [Tooltip("光の届く大きさのゆれ（元の大きさに対する割合。0 でゆらさない）")]
    private float radiusStrength = 0.05f;

    private Light2D light2D;
    private float baseIntensity;
    private float baseOuterRadius;
    private float seed;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        baseOuterRadius = light2D.pointLightOuterRadius;
        // 位置から決まる種（同じ設定でも明かりごとに違う揺れ）
        seed = Mathf.Repeat(transform.position.x * 12.9898f + transform.position.y * 78.233f, 1000.0f);
    }

    private void OnDisable()
    {
        if(light2D == null) { return; }
        light2D.intensity = baseIntensity;
        light2D.pointLightOuterRadius = baseOuterRadius;
    }

    private void Update()
    {
        float t = Time.time * speed;
        // 2 つの速さのノイズを重ねる（なめらかだが、決まった周期にならない）
        float n = (Mathf.PerlinNoise(seed, t) * 0.7f + Mathf.PerlinNoise(seed + 37.0f, t * 2.3f) * 0.3f) * 2.0f - 1.0f;
        light2D.intensity = Mathf.Max(0.0f, baseIntensity * (1.0f + n * strength));
        if(radiusStrength > 0.0f && light2D.lightType == Light2D.LightType.Point)
        {
            light2D.pointLightOuterRadius = baseOuterRadius * (1.0f + n * radiusStrength);
        }
    }
}
