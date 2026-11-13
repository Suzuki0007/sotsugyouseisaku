/*********************************************************************/
// * \file   MapDebugView.cs
// * \brief  ゲーム中に、歩ける範囲の輪郭（緑・歩けない形は赤）とマップの範囲（水色の四角）を線で見せる（デバッグ用）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム画面に範囲を線で描く（デバッグウィンドウの「歩ける範囲を表示」で切り替える）
/// </summary>
///
/// ・緑：歩ける範囲（WalkArea）の形 ／ 赤：歩けない形 ／ 水色：マップの範囲（MapArea の映す範囲＝マップの絵）
/// ・暗いステージでも見える（光の影響を受けない線）。形が変わる（通路が開く）と描き直す
/// ・自動で作られ、表示をやめると消える
public class MapDebugView : MonoBehaviour
{
    private static MapDebugView instance;

    /// <summary> 表示するか </summary>
    public static bool Show
    {
        get => instance != null;
        set
        {
            if(value == (instance != null)) { return; }
            if(value)
            {
                var go = new GameObject("範囲の表示（デバッグ・自動で作られる）");
                instance = go.AddComponent<MapDebugView>();
            }
            else
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private static readonly Color WalkColor = new Color(0.2f, 1.0f, 0.4f, 0.95f);
    private static readonly Color ExcludeColor = new Color(1.0f, 0.3f, 0.3f, 0.95f);
    private static readonly Color MapColor = new Color(0.3f, 0.85f, 1.0f, 0.8f);

    private readonly List<LineRenderer> pool = new List<LineRenderer>();
    private readonly List<Vector3[]> lines = new List<Vector3[]>();
    private readonly List<bool> excludes = new List<bool>();
    private Material material;
    private int lastSignature = int.MinValue;
    private Rect lastRect;

    private void OnDestroy()
    {
        if(material != null) { Destroy(material); }
        if(instance == this) { instance = null; }
    }

    private void LateUpdate()
    {
        int signature = WalkArea.Signature;
        MapArea.TryGetViewRect(out Rect rect);
        if(signature == lastSignature && rect == lastRect) { return; }
        lastSignature = signature;
        lastRect = rect;
        Rebuild(rect);
    }

    private void Rebuild(Rect mapRect)
    {
        WalkArea.CollectOutlines(lines, excludes);

        int used = 0;
        for(int i = 0; i < lines.Count; i++) { Draw(used++, lines[i], excludes[i] ? ExcludeColor : WalkColor); }

        if(mapRect.width > 0.0f)
        {
            Draw(used++, new[]
            {
                new Vector3(mapRect.xMin, mapRect.yMin), new Vector3(mapRect.xMax, mapRect.yMin),
                new Vector3(mapRect.xMax, mapRect.yMax), new Vector3(mapRect.xMin, mapRect.yMax),
            }, MapColor);
        }

        for(int i = used; i < pool.Count; i++) { pool[i].gameObject.SetActive(false); }
    }

    private void Draw(int index, Vector3[] points, Color color)
    {
        if(index >= pool.Count)
        {
            if(material == null)
            {
                // URP 2D では光の影響を受けないスプライトのシェーダー（なければ標準のもの）
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if(shader == null) { shader = Shader.Find("Sprites/Default"); }
                material = new Material(shader);
            }
            var go = new GameObject("線");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = material;
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.widthMultiplier = 0.06f;
            lr.sortingOrder = 400;
            lr.numCornerVertices = 2;
            pool.Add(lr);
        }

        LineRenderer line = pool[index];
        line.gameObject.SetActive(true);
        line.startColor = color;
        line.endColor = color;
        line.positionCount = points.Length;
        line.SetPositions(points);
    }
}
