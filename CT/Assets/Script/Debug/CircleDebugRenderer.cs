/*********************************************************************/
// * \file   CircleDebugRenderer.cs
// * \brief  円のデバッグ描画クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

/// <summary>
/// 円のデバッグ描画クラス
/// </summary>
/// 
/// RequireComponent属性を使用して、LineRendererコンポーネントがアタッチされていることを保証する
/// このクラスは円を描画するためにLineRendererコンポーネントを使用するため、必ずLineRendererが必要だから
[RequireComponent(typeof(LineRenderer))]
public class CircleDebugRenderer : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("円の設定")]

    [SerializeField]
    private Transform focusTargetPos;

    [SerializeField]
    private float radius = 1f;

    [SerializeField, Range(16, 128)]
    private int segments = 32;

    private LineRenderer lineRenderer;

    [Header("描画設定")]

    [Tooltip("線の太さ")]
    [Min(0.001f)]
    [SerializeField]
    private float lineWidth = 0.03f;

    [Tooltip("円の色")]
    [SerializeField]
    private Color lineColor = Color.red;

    [Tooltip("円を表示するか")]
    [SerializeField]
    private bool showCircle = true;

    [Tooltip("Z座標の描画オフセット")]
    [SerializeField]
    private float zOffset = 0f;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if(lineRenderer == null) { Debug.LogError("LineRendererがアタッチされていません"); }

        // LineRendererの初期設定
        SetupLineRenderer();
    }

    /// <summary>
    /// LineRendererの初期設定
    /// </summary>
    private void SetupLineRenderer()
    {
        // ワールド座標で描画
        lineRenderer.useWorldSpace = true;

        // 円を閉じる
        lineRenderer.loop = true;

        // 塗りつぶしは行わず、線だけを描画
        lineRenderer.positionCount = segments;

        // 線の太さを設定
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // 線の色を設定
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // ShadowCastingModeは、オブジェクトが影を落とすかどうかを制御するための設定
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // 表示状態を反映
        lineRenderer.enabled = showCircle;
    }

    private void LateUpdate()
    {
        if(!showCircle) { return; }

        UpdateCircle();

        if(focusTargetPos == null) { Debug.LogError("ターゲットが設定されていません"); return; }

        lineRenderer.positionCount = segments;

        // 円の各頂点の座標を計算してLineRendererに設定する
        for(int i = 0; i < segments; i++) 
        {
            float angle = i * Mathf.PI * 2f / segments;

            // 円の頂点座標を計算する
            Vector2 point = focusTargetPos.position + new Vector3(
                Mathf.Cos(angle) * radius, 
                Mathf.Sin(angle) * radius,
                0f
            );

            lineRenderer.SetPosition(i, point);
        }
    }

    private void UpdateCircle()
    {
        if(lineRenderer == null) { return; }

        // 円の中心を取得
        Vector3 center = focusTargetPos != null
            ? focusTargetPos.position
            : transform.position;

        // 分割数を反映
        if(lineRenderer.positionCount != segments)
        {
            lineRenderer.positionCount = segments;
        }

        // 線の設定を反映
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // 円周上の点を計算
        for(int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;

            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            Vector3 position = center + new Vector3(
                x,
                y,
                zOffset
            );

            lineRenderer.SetPosition(i, position);
        }
    }

    //=====================================================================
    // 外部操作用の関数
    //=====================================================================

    /// <summary>
    /// 円の半径を変更する
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0f, newRadius);
    }

    /// <summary>
    /// 円の色を変更する
    /// </summary>
    public void SetColor(Color newColor)
    {
        lineColor = newColor;
    }

    /// <summary>
    /// 円の表示・非表示を切り替える
    /// </summary>
    public void SetVisible(bool visible)
    {
        showCircle = visible;

        if(lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
    }

    /// <summary>
    /// 円の追従対象を変更する
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        focusTargetPos = newTarget;
    }

    /// <summary>
    /// 円の半径を取得する
    /// </summary>
    public float GetRadius()
    {
        return radius;
    }
}
