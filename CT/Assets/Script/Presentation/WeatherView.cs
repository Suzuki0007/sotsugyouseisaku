/*********************************************************************/
// * \file   WeatherView.cs
// * \brief  天候の見た目：1つの天候のプリセットを、カメラについてくる粒（ParticleSystem）で出す（窓口が作る）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 天候の見た目（窓口 PresentationDirector が自動で作る。シーンに置かなくてよい）
/// </summary>
///
/// ・粒はカメラの写す範囲より少し広い四角の中に出て、世界に残る（カメラが動いても途切れない）
/// ・量（重み 0〜1）を、指定の秒数でなめらかに増やす・減らす
[AddComponentMenu("")]
public class WeatherView : MonoBehaviour
{
    // 描く順番（同じ並びのレイヤーの中）：床の影 4〜5 ＜ キャラの後ろ 6 ＜ キャラ 99〜101 ＜ キャラの前 150 ＜ かすみ 160 ＜ エフェクト 200
    public const int BehindOrder = 6;
    public const int FrontOrder = 150;
    public const int HazeOrder = 160;

    // カメラの範囲の外にどれだけ広く出すか（マス）
    private const float Margin = 2.0f;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private WeatherPreset preset;
    private ParticleSystem ps;
    private ParticleSystem splashPs;
    private SpriteRenderer haze;
    private MaterialPropertyBlock block;

    private float weight;
    private float target;
    private float speed;
    private float appliedWeight = -1.0f;
    private bool appliedLow;
    private Vector2 appliedSize;
    private bool seStarted;
    private SeHandle seHandle;

    public WeatherPreset Preset => preset;
    public float Weight => weight;
    public float Target => target;
    public bool IsBlending => !Mathf.Approximately(weight, target);

    /// <summary> 消えきって、粒も残っていない </summary>
    public bool IsDone => weight <= 0.0f && target <= 0.0f && (ps == null || ps.particleCount == 0) && (splashPs == null || splashPs.particleCount == 0);

    //=====================================================================
    // 作る
    //=====================================================================

    public static WeatherView Create(WeatherPreset preset, Transform parent, Material litMat, Material unlitMat, Sprite hazeSprite)
    {
        var go = new GameObject("天候 " + preset.name);
        go.transform.SetParent(parent, false);
        WeatherView view = go.AddComponent<WeatherView>();
        view.Build(preset, litMat, unlitMat, hazeSprite);
        return view;
    }

    private void Build(WeatherPreset p, Material litMat, Material unlitMat, Sprite hazeSprite)
    {
        preset = p;
        block = new MaterialPropertyBlock();
        Material mat = p.Lit ? litMat : unlitMat;
        int order = p.DrawOrder == WeatherPreset.Order.Front ? FrontOrder : BehindOrder;

        ps = CreateSystem(gameObject, p.Texture, mat, order);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(p.Lifetime * 0.8f, p.Lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(p.SizeMin, p.SizeMax);
        main.startColor = p.ParticleColor;
        main.maxParticles = p.MaxParticles;
        if(p.Spin) { main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, Mathf.PI * 2.0f); }

        float v = p.SpeedVariation;
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(p.Wind * (1.0f - v), p.Wind * (1.0f + v));
        vel.y = new ParticleSystem.MinMaxCurve(-p.FallSpeed * (1.0f + v), -p.FallSpeed * (1.0f - v));
        vel.z = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);

        if(p.Sway > 0.0f)
        {
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = p.Sway;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        if(p.Spin)
        {
            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2.0f, 2.0f);
        }

        var r = ps.GetComponent<ParticleSystemRenderer>();
        if(p.Stretch > 0.0f)
        {
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = p.Stretch;
            r.lengthScale = 1.0f;
        }

        // 地面のはね（強い雨）
        if(p.Splash)
        {
            var child = new GameObject("はね");
            child.transform.SetParent(transform, false);
            splashPs = CreateSystem(child, p.SplashTexture != null ? p.SplashTexture : p.Texture, mat, BehindOrder);
            ParticleSystem.MainModule sm = splashPs.main;
            sm.startLifetime = 0.3f;
            sm.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            Color sc = p.ParticleColor; sc.a = Mathf.Min(1.0f, sc.a * 1.5f);
            sm.startColor = sc;
            sm.maxParticles = Mathf.Max(10, p.MaxParticles / 4);
            ParticleSystem.SizeOverLifetimeModule size = splashPs.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0.0f, 0.3f, 1.0f, 1.0f));
        }

        // 画面のかすみ
        if(p.Haze > 0.0f && hazeSprite != null)
        {
            var hz = new GameObject("かすみ");
            hz.transform.SetParent(transform, false);
            haze = hz.AddComponent<SpriteRenderer>();
            haze.sprite = hazeSprite;
            haze.sharedMaterial = p.Lit ? litMat : unlitMat;
            haze.sortingOrder = HazeOrder;
            haze.color = Color.clear;
        }

        ps.Play();
        if(splashPs != null) { splashPs.Play(); }
    }

    private ParticleSystem CreateSystem(GameObject go, Texture2D texture, Material mat, int order)
    {
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 5.0f;
        main.startSpeed = 0.0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule em = system.emission;
        em.rateOverTime = 0.0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10.0f, 10.0f, 0.01f);

        ParticleSystem.ColorOverLifetimeModule col = system.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                  new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) });
        col.color = g;

        var r = system.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = order;
        r.maxParticleSize = 5.0f;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        if(texture != null)
        {
            r.GetPropertyBlock(block);
            block.SetTexture(MainTexId, texture);
            r.SetPropertyBlock(block);
        }
        return system;
    }

    //=====================================================================
    // 量
    //=====================================================================

    /// <summary>
    /// 量を変える（1＝プリセットのまま、0＝止める）。秒数 0 ならすぐ（増やすときは最初から降っている状態にする）
    /// </summary>
    public void SetTarget(float value, float seconds)
    {
        target = Mathf.Clamp01(value);
        if(seconds <= 0.0f)
        {
            bool fill = target > weight;
            weight = target;
            speed = 0.0f;
            ApplyWeight();
            if(fill && ps != null)
            {
                ps.Simulate(preset.Lifetime, true, true, true);
                ps.Play(true);
            }
            if(target <= 0.0f && ps != null) { ps.Clear(true); }
            return;
        }
        speed = Mathf.Abs(target - weight) / seconds;
    }

    /// <summary>
    /// 毎フレーム（窓口の LateUpdate から）：カメラについていき、量を進める
    /// </summary>
    public void Tick(Camera cam, float dt)
    {
        if(cam != null)
        {
            Vector3 c = cam.transform.position;
            transform.position = new Vector3(c.x, c.y, 0.0f);
            float h = cam.orthographicSize * 2.0f;
            var size = new Vector2(h * cam.aspect, h);
            if(size != appliedSize) { ApplySize(size); }
        }

        if(weight != target)
        {
            weight = Mathf.MoveTowards(weight, target, speed * dt);
        }
        ApplyWeight();
    }

    private void ApplySize(Vector2 size)
    {
        appliedSize = size;
        var scale = new Vector3(size.x + Margin * 2.0f, size.y + Margin * 2.0f, 0.01f);
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.scale = scale;
        if(splashPs != null) { ParticleSystem.ShapeModule s2 = splashPs.shape; s2.scale = new Vector3(size.x, size.y, 0.01f); }
        if(haze != null)
        {
            Vector2 spriteSize = haze.sprite.bounds.size;
            haze.transform.localScale = new Vector3((size.x + 1.0f) / spriteSize.x, (size.y + 1.0f) / spriteSize.y, 1.0f);
        }
    }

    private void ApplyWeight()
    {
        bool low = PresentationEase.IsLowQuality;
        if(Mathf.Approximately(weight, appliedWeight) && low == appliedLow) { return; }
        appliedWeight = weight;
        appliedLow = low;

        float q = low ? 0.5f : 1.0f;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = preset.Rate * weight * q;
        ParticleSystem.MainModule main = ps.main;
        main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(preset.MaxParticles * q));

        if(splashPs != null)
        {
            ParticleSystem.EmissionModule se = splashPs.emission;
            se.rateOverTime = preset.Rate * 0.12f * weight * q;
        }
        if(haze != null)
        {
            Color c = preset.HazeColor;
            c.a = preset.Haze * weight;
            haze.color = c;
            haze.enabled = c.a > 0.001f;
        }

        // 環境音
        bool wantSe = preset.LoopSe != null && target > 0.0f;
        if(wantSe && !seStarted) { seHandle = SeManager.PlaySe(preset.LoopSe); seStarted = true; }
        else if(!wantSe && seStarted) { SeManager.StopSe(seHandle); seStarted = false; }
    }

    private void OnDestroy()
    {
        if(seStarted) { SeManager.StopSe(seHandle); seStarted = false; }
    }
}
