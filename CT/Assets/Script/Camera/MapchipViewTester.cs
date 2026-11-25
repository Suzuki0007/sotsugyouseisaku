/*********************************************************************/
// * \file   MapchipViewTester.cs
// * \brief  マップチップの見え方を試すシーン用：Pixel Perfect Camera のオン・オフとズームをキーで切り替える
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// マップチップの見え方を試す（試すシーン Test_Mapchip だけで使う）
/// </summary>
///
/// ・P キー：Pixel Perfect Camera のオン・オフ（継ぎ目の出方を見比べる）
/// ・Z キー：ズームを順に切り替える（ゲームにはズームがないので、試すときだけ）
/// ・画面の左下に今の状態（Pixel Perfect Camera・写す高さ・ズーム）を出す
/// ・カメラ（Pixel Perfect Camera の付いた物）に付ける。ふつうのステージには置かない
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MapchipViewTester : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    [Tooltip("Z キーで順に切り替える倍率（1 = ステージと同じ見え方。大きいほど寄る）")]
    private float[] zoomSteps = { 1.0f, 1.25f, 1.5f, 2.0f, 0.75f };

    [SerializeField, Label("状態を画面に出す")]
    [Tooltip("画面の左下に、Pixel Perfect Camera のオン・オフと写す高さを出す")]
    private bool showLabel = true;

    private Camera cam;
    private PixelPerfectCamera pixelPerfect;
    private float baseOrthoSize;
    private int baseRefX;
    private int baseRefY;
    private int zoomIndex;
    private string label = "";
    private float lastOrtho = -1.0f;
    private GUIStyle style;

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        cam = GetComponent<Camera>();
        pixelPerfect = GetComponent<PixelPerfectCamera>();
        baseOrthoSize = cam.orthographicSize;
        if(pixelPerfect != null)
        {
            baseRefX = pixelPerfect.refResolutionX;
            baseRefY = pixelPerfect.refResolutionY;
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if(kb == null) { return; }

        if(kb.pKey.wasPressedThisFrame && pixelPerfect != null)
        {
            pixelPerfect.enabled = !pixelPerfect.enabled;
            ApplyZoom();
        }
        if(kb.zKey.wasPressedThisFrame && zoomSteps.Length > 0)
        {
            zoomIndex = (zoomIndex + 1) % zoomSteps.Length;
            ApplyZoom();
        }
    }

    //=====================================================================
    // ズーム
    //=====================================================================

    /// <summary> 今の倍率（1 = ステージと同じ） </summary>
    public float Zoom => zoomSteps.Length > 0 ? zoomSteps[zoomIndex] : 1.0f;

    /// <summary> Pixel Perfect Camera がオンか </summary>
    public bool PixelPerfectOn => pixelPerfect != null && pixelPerfect.enabled;

    private void ApplyZoom()
    {
        float z = Mathf.Max(0.1f, Zoom);
        if(PixelPerfectOn)
        {
            // Pixel Perfect Camera は写す高さを基準の解像度から決めるので、基準の解像度を変える
            pixelPerfect.refResolutionX = Mathf.Max(2, Mathf.RoundToInt(baseRefX / z / 2.0f) * 2);
            pixelPerfect.refResolutionY = Mathf.Max(2, Mathf.RoundToInt(baseRefY / z / 2.0f) * 2);
        }
        else
        {
            if(pixelPerfect != null)
            {
                pixelPerfect.refResolutionX = baseRefX;
                pixelPerfect.refResolutionY = baseRefY;
            }
            cam.orthographicSize = baseOrthoSize / z;
        }
        lastOrtho = -1.0f;
    }

    //=====================================================================
    // 表示
    //=====================================================================

    private void OnGUI()
    {
        if(!showLabel) { return; }

        // 写す高さが変わったときだけ文を作り直す
        if(!Mathf.Approximately(lastOrtho, cam.orthographicSize))
        {
            lastOrtho = cam.orthographicSize;
            string ppc = pixelPerfect == null ? "なし" : pixelPerfect.enabled ? "オン" : "オフ";
            label = "Pixel Perfect Camera：" + ppc + "（P）　ズーム ×" + Zoom.ToString("0.##") + "（Z）　写す高さ " + (cam.orthographicSize * 2.0f).ToString("0.##") + " マス";
        }

        if(style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45) };
            style.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(12, Screen.height - style.fontSize * 2.2f, Screen.width, style.fontSize * 2.0f), label, style);
    }
}
