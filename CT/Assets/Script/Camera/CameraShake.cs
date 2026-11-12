/*********************************************************************/
// * \file   CameraShake.cs
// * \brief  画面（マップを写すカメラ）を揺らす
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 画面を揺らす（地響き・衝撃・頭痛の演出など）
/// </summary>
///
/// ・描画するときだけカメラの位置をずらし、描画が終わったら戻す
///   （FocusCamera が決めたカメラの位置は変えないので、追いかける動きとぶつからない）
/// ・UI（Canvas）は揺れない
public static class CameraShake
{
    private static float strength;
    private static float duration;
    private static float startTime = -1.0f;
    private static float frequency = 25.0f;

    private static Camera shakingCamera;
    private static Vector3 appliedOffset;
    private static bool hooked;

    /// <summary> 揺れているか </summary>
    public static bool IsShaking => startTime >= 0.0f && Time.time - startTime < duration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        startTime = -1.0f;
        shakingCamera = null;
        appliedOffset = Vector3.zero;
        if(hooked)
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            hooked = false;
        }
    }

    /// <summary>
    /// 画面を揺らす
    /// </summary>
    /// <param name="newStrength"> 揺れの大きさ（マス。0.1 で小さく、0.4 で大きく） </param>
    /// <param name="newDuration"> 揺れる時間（秒）。だんだん弱くなって止まる </param>
    /// <param name="newFrequency"> 1秒あたりの揺れの速さ </param>
    public static void Shake(float newStrength, float newDuration, float newFrequency = 25.0f)
    {
        if(newStrength <= 0.0f || newDuration <= 0.0f) { return; }

        // 揺れている途中なら、強い方を使う
        if(IsShaking) { newStrength = Mathf.Max(newStrength, CurrentStrength()); }

        strength = newStrength;
        duration = newDuration;
        frequency = Mathf.Max(1.0f, newFrequency);
        startTime = Time.time;

        if(!hooked)
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
            hooked = true;
        }
    }

    private static float CurrentStrength()
    {
        if(!IsShaking) { return 0.0f; }
        float t = (Time.time - startTime) / duration;
        return strength * (1.0f - t);
    }

    private static void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        if(!IsShaking || cam != Camera.main) { return; }

        float s = CurrentStrength();
        float time = Time.time * frequency;
        appliedOffset = new Vector3(
            (Mathf.PerlinNoise(time, 0.0f) - 0.5f) * 2.0f * s,
            (Mathf.PerlinNoise(0.0f, time) - 0.5f) * 2.0f * s,
            0.0f);

        shakingCamera = cam;
        cam.transform.position += appliedOffset;
    }

    private static void OnEndCamera(ScriptableRenderContext context, Camera cam)
    {
        if(cam != shakingCamera || cam == null) { return; }

        cam.transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
        shakingCamera = null;
    }
}
