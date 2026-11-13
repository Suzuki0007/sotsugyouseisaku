/*********************************************************************/
// * \file   WalkAreaEditor.cs
// * \brief  WalkArea の Inspector（マスク画像から作る・今の形を書き出す・形の追加ボタン）
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 歩ける範囲（WalkArea）の Inspector
/// </summary>
[CustomEditor(typeof(WalkArea))]
public class WalkAreaEditor : Editor
{
    private string lastMessage;

    public override void OnInspectorGUI()
    {
        var area = (WalkArea)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "マスク画像の作り方：マップの絵と同じ大きさの画像に、歩ける所を白く塗る（透明・黒は歩けない。柱などは塗らずに残すと穴になる）。\n" +
            "→「マスク画像」に入れて「マスク画像から作る」。塗り直したら、もう一度押せば作り直す。\n" +
            "今の形から始めたいときは「今の形をマスク画像に書き出す」で白黒の画像を作り、それを塗り直す。\n" +
            "イベントで開け閉めする通路などは、マスクとは別の子（四角など）にしておく（子を表示・非表示で切り替える）。",
            MessageType.None);

        using(new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = area.MaskTexture != null && !Application.isPlaying;
            if(GUILayout.Button("マスク画像から作る", GUILayout.Height(24)))
            {
                lastMessage = WalkAreaMask.Build(area);
            }
            GUI.enabled = !Application.isPlaying;
            if(GUILayout.Button("今の形をマスク画像に書き出す", GUILayout.Height(24)))
            {
                string scene = area.gameObject.scene.name;
                string path = EditorUtility.SaveFilePanelInProject("マスク画像の書き出し", $"{scene}_{area.name}_walkmask", "png", "書き出す場所を選んでください", "Assets/Images");
                if(!string.IsNullOrEmpty(path))
                {
                    WalkAreaMask.Export(area, path, 2048, out lastMessage);
                    if(area.MaskTexture == null)
                    {
                        Undo.RecordObject(area, "マスク画像を入れる");
                        area.SetMaskTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                    }
                }
            }
            GUI.enabled = true;
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("円を追加", EditorStyles.miniButtonLeft))        { Add(area, "円", false, true); }
            if(GUILayout.Button("四角を追加", EditorStyles.miniButtonMid))      { Add(area, "通路", false, false); }
            if(GUILayout.Button("歩けない円", EditorStyles.miniButtonMid))      { Add(area, "歩けない形（円）", true, true); }
            if(GUILayout.Button("歩けない四角", EditorStyles.miniButtonRight))  { Add(area, "歩けない形（四角）", true, false); }
        }

        if(area.MaskTarget == null)
        {
            EditorGUILayout.HelpBox("合わせるマップの絵が見つかりません。「合わせるマップの絵」に絵（SpriteRenderer）を入れてください。", MessageType.Warning);
        }

        if(!string.IsNullOrEmpty(lastMessage))
        {
            EditorGUILayout.HelpBox(lastMessage, MessageType.Info);
        }
    }

    private static void Add(WalkArea area, string name, bool exclude, bool circle)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "歩ける範囲に形を足す");
        go.transform.SetParent(area.transform, false);
        Vector3 center = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : area.transform.position;
        go.transform.position = new Vector3(center.x, center.y, area.transform.position.z);

        if(circle)
        {
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = exclude ? 1.0f : 3.0f;
        }
        else
        {
            var b = go.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = exclude ? new Vector2(2.0f, 2.0f) : new Vector2(4.0f, 1.5f);
        }
        if(exclude) { go.AddComponent<WalkAreaExclude>(); }
        Selection.activeGameObject = go;
    }
}
