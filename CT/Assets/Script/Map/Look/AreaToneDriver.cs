/*********************************************************************/
// * \file   AreaToneDriver.cs
// * \brief  場所の色味（AreaTone）を Global Light 2D に反映する（自動で作られる）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 場所の色味を Global Light 2D に反映する（場所の色味があるシーンで自動で作られる。シーンに置かなくてよい）
/// </summary>
[AddComponentMenu("")]
public class AreaToneDriver : MonoBehaviour
{
    private static AreaToneDriver instance;

    private Light2D globalLight;
    private Color baseColor;
    private float baseIntensity;
    private bool disabledByDarkness;

    private Color currentColor;
    private float currentIntensity;
    private Color fromColor;
    private float fromIntensity;
    private AreaTone currentArea;
    private float blendTime;
    private float blendDuration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    /// <summary> 再生中なら、シーンに1つだけ作る </summary>
    public static void Ensure()
    {
        if(!Application.isPlaying || instance != null) { return; }
        var go = new GameObject("AreaToneDriver（場所の色味・自動）");
        instance = go.AddComponent<AreaToneDriver>();
    }

    private void Start()
    {
        foreach(Light2D l in FindObjectsByType<Light2D>())
        {
            if(l.lightType == Light2D.LightType.Global && l.isActiveAndEnabled) { globalLight = l; break; }
        }
        disabledByDarkness = FindAnyObjectByType<StageDarkness>() != null;
        if(globalLight == null || disabledByDarkness)
        {
            if(globalLight == null) { Debug.LogWarning("場所の色味：シーンに Global Light 2D がないので、色味を変えられません"); }
            enabled = false;
            return;
        }
        baseColor = globalLight.color;
        baseIntensity = globalLight.intensity;
        currentColor = fromColor = baseColor;
        currentIntensity = fromIntensity = baseIntensity;
        blendDuration = 0.0f;
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
    }

    private void LateUpdate()
    {
        if(globalLight == null) { return; }

        // 今いる場所（優先度の大きいほう）
        AreaTone best = null;
        PlayerSwitchManager psm = PlayerSwitchManager.Instance;
        GameObject player = psm != null ? psm.CurrentPlayer : null;
        if(player != null)
        {
            Vector2 p = player.transform.position;
            IReadOnlyList<AreaTone> list = AreaTone.Active;
            for(int i = 0; i < list.Count; i++)
            {
                AreaTone a = list[i];
                if(a.Contains(p) && (best == null || a.Priority > best.Priority)) { best = a; }
            }
        }

        // 場所が変わったら、今の色からなめらかに移る
        if(best != currentArea)
        {
            fromColor = currentColor;
            fromIntensity = currentIntensity;
            blendDuration = best != null ? best.BlendSeconds : (currentArea != null ? currentArea.BlendSeconds : 0.0f);
            blendTime = 0.0f;
            currentArea = best;
        }

        Color toColor = best != null ? best.ToneColor : baseColor;
        float toIntensity = best != null ? best.ToneIntensity : baseIntensity;
        blendTime += Time.unscaledDeltaTime;
        float t = blendDuration > 0.0f ? Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(blendTime / blendDuration)) : 1.0f;
        currentColor = Color.Lerp(fromColor, toColor, t);
        currentIntensity = Mathf.Lerp(fromIntensity, toIntensity, t);

        globalLight.color = currentColor;
        globalLight.intensity = currentIntensity;
    }
}
