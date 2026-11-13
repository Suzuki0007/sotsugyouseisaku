/*********************************************************************/
// * \file   RoomEditor.cs
// * \brief  部屋（Room）の Inspector（点検の一覧・カメラの範囲を絵に合わせる）
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 部屋の Inspector
/// </summary>
[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var room = (Room)target;
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("点検", EditorStyles.boldLabel);

        foreach(RoomChecks.Result r in RoomChecks.Check(room))
        {
            string mark = r.level == RoomChecks.Level.Ok ? "✓" : r.level == RoomChecks.Level.Info ? "ℹ" : r.level == RoomChecks.Level.Warning ? "⚠" : "✖";
            Color old = GUI.color;
            GUI.color = r.level == RoomChecks.Level.Ok ? new Color(0.6f, 1.0f, 0.6f) :
                        r.level == RoomChecks.Level.Error ? new Color(1.0f, 0.55f, 0.55f) :
                        r.level == RoomChecks.Level.Warning ? new Color(1.0f, 0.9f, 0.5f) : Color.white;
            using(new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(mark + " " + r.text, EditorStyles.wordWrappedLabel);
                if(r.level != RoomChecks.Level.Ok && r.target != null && r.target != room && GUILayout.Button("選ぶ", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    Selection.activeObject = r.target is Component c ? c.gameObject : r.target;
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
            }
            GUI.color = old;
        }

        EditorGUILayout.Space();
        using(new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !Application.isPlaying;
            if(GUILayout.Button("カメラの範囲を部屋の絵に合わせる")) { FitCamera(room); }
            if(GUILayout.Button("歩ける範囲を選ぶ"))
            {
                WalkArea area = room.GetComponentInChildren<WalkArea>(true);
                if(area != null) { Selection.activeGameObject = area.gameObject; }
            }
            GUI.enabled = true;
        }
    }

    private static void FitCamera(Room room)
    {
        SpriteRenderer image = room.Image;
        if(image == null || image.sprite == null) { EditorUtility.DisplayDialog("部屋", "部屋の絵がありません", "OK"); return; }

        CameraArea area = room.GetComponentInChildren<CameraArea>(true);
        if(area == null)
        {
            var go = new GameObject("カメラの範囲");
            Undo.RegisterCreatedObjectUndo(go, "カメラの範囲");
            go.transform.SetParent(room.transform, false);
            var b = go.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            area = go.AddComponent<CameraArea>();
        }

        BoxCollider2D box = area.GetComponent<BoxCollider2D>();
        Undo.RecordObject(area.transform, "カメラの範囲を絵に合わせる");
        Undo.RecordObject(box, "カメラの範囲を絵に合わせる");
        area.transform.position = new Vector3(image.bounds.center.x, image.bounds.center.y, area.transform.position.z);
        area.transform.rotation = Quaternion.identity;
        Vector3 s = area.transform.lossyScale;
        box.offset = Vector2.zero;
        box.size = new Vector2(image.bounds.size.x / Mathf.Max(0.0001f, s.x), image.bounds.size.y / Mathf.Max(0.0001f, s.y));
    }
}
