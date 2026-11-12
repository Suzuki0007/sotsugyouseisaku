/*********************************************************************/
// * \file   MapCameraAspect.cs
// * \brief  マップを写すカメラを画面の中央の 4:3 にして、残りを黒い帯にする
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// マップを写すカメラの画面比率
/// </summary>
///
/// ・昔のゲームを今のモニターで遊んだときのように、マップは画面の中央の 4:3 だけに写し、左右（上下）は帯にする
/// ・UI（Screen Space - Overlay の Canvas）は画面全体を使える（帯の上にも出せる）
/// ・FocusCamera が付いているカメラには、シーンが始まったときに自動で付く（シーンに置かなくてよい）
///   比率や帯の色を変えたいときだけ、カメラに付けて Inspector で変える
/// ・カメラの写す範囲（cam.aspect）が 4:3 になるので、カメラが止まるマップの端・マウスの位置・画面の外の判定もそれに合う
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class MapCameraAspect : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("横の比率", 1.0f)]
    [Tooltip("マップを写す範囲の横の比率（4:3 なら 4）")]
    private float aspectWidth = 4.0f;

    [SerializeField, Label("縦の比率", 1.0f)]
    [Tooltip("マップを写す範囲の縦の比率（4:3 なら 3）")]
    private float aspectHeight = 3.0f;

    [SerializeField, Label("帯の色")]
    [Tooltip("マップの外（左右の帯）の色")]
    private Color barColor = Color.black;

    // 帯は UI より奥に出す（ほかの Canvas はすべて帯より手前）
    private const int BarSortingOrder = -1000;

    private Camera cam;
    private GameObject barRoot;
    private RectTransform left, right, bottom, top;

    private int lastWidth = -1;
    private int lastHeight = -1;

    //=====================================================================
    // 自動で付ける
    //=====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToMapCameras();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToMapCameras();
    }

    /// <summary>
    /// FocusCamera が付いているカメラ（マップを写すカメラ）に付ける
    /// </summary>
    private static void AttachToMapCameras()
    {
        foreach(FocusCamera focus in Object.FindObjectsByType<FocusCamera>())
        {
            if(focus.GetComponent<Camera>() == null) { continue; }
            if(focus.GetComponent<MapCameraAspect>() == null) { focus.gameObject.AddComponent<MapCameraAspect>(); }
        }
    }

    //=====================================================================
    // 初期化・更新
    //=====================================================================

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if(barRoot == null) { CreateBars(); }
        barRoot.SetActive(true);
        lastWidth = -1;
        Apply();
    }

    private void OnDisable()
    {
        if(cam != null) { cam.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f); }
        if(barRoot != null) { barRoot.SetActive(false); }
    }

    private void OnDestroy()
    {
        if(barRoot != null) { Destroy(barRoot); }
    }

    private void LateUpdate()
    {
        // ウィンドウの大きさが変わったときだけ計算し直す
        if(Screen.width != lastWidth || Screen.height != lastHeight) { Apply(); }
    }

    private void OnValidate()
    {
        lastWidth = -1;
    }

    //=====================================================================
    // 画面比率
    //=====================================================================

    /// <summary>
    /// カメラの写す範囲を画面の中央の指定の比率にし、残りに帯を出す
    /// </summary>
    private void Apply()
    {
        if(cam == null || Screen.height <= 0) { return; }

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float target = aspectWidth / Mathf.Max(0.01f, aspectHeight);
        float screen = (float)Screen.width / Screen.height;

        Rect rect;
        if(screen > target)
        {
            // 横長の画面：左右に帯
            float w = target / screen;
            rect = new Rect((1.0f - w) * 0.5f, 0.0f, w, 1.0f);
        }
        else
        {
            // 縦長の画面：上下に帯
            float h = screen / target;
            rect = new Rect(0.0f, (1.0f - h) * 0.5f, 1.0f, h);
        }

        cam.rect = rect;

        SetAnchors(left,   new Vector2(0.0f, 0.0f),       new Vector2(rect.xMin, 1.0f));
        SetAnchors(right,  new Vector2(rect.xMax, 0.0f),  new Vector2(1.0f, 1.0f));
        SetAnchors(bottom, new Vector2(0.0f, 0.0f),       new Vector2(1.0f, rect.yMin));
        SetAnchors(top,    new Vector2(0.0f, rect.yMax),  new Vector2(1.0f, 1.0f));
    }

    private void CreateBars()
    {
        barRoot = new GameObject("MapAspectBars");
        barRoot.transform.SetParent(transform, false);

        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = BarSortingOrder;

        left = CreateBar("Left");
        right = CreateBar("Right");
        bottom = CreateBar("Bottom");
        top = CreateBar("Top");
    }

    private RectTransform CreateBar(string barName)
    {
        var go = new GameObject(barName);
        go.transform.SetParent(barRoot.transform, false);

        Image image = go.AddComponent<Image>();
        image.color = barColor;
        image.raycastTarget = false;

        RectTransform rt = image.rectTransform;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        if(rt == null) { return; }

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.GetComponent<Image>().color = barColor;

        // 帯がないとき（同じ比率の画面）は消す
        rt.gameObject.SetActive(max.x - min.x > 0.0001f && max.y - min.y > 0.0001f);
    }
}
