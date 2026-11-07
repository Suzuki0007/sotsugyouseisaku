/*********************************************************************/
// * \file   GameFlagEditor.cs
// * \brief  GameFlag の Inspector 表示
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;

/// <summary>
/// GameFlag の Inspector に、再生中の現在の値と切り替えボタンを出す
/// </summary>
[CustomEditor(typeof(GameFlag))]
public class GameFlagEditor : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        GameFlag flag = (GameFlag)target;

        if(!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("再生中は、ここに現在の値が出て、切り替えて確認できます。\n一覧は Tools > フラグ > フラグ一覧", MessageType.Info);
            return;
        }

        bool value = flag.Value;
        bool next = EditorGUILayout.Toggle("現在の値（再生中）", value);
        if(next != value) { flag.Value = next; }
    }
}
