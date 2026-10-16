/*********************************************************************/
// * \file   SceneData.cs
// * \brief  シーンのデータを保持するクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using UnityEngine;

// CreateAssetMenuとは、ScriptableObjectを作成するためのメニューをUnityエディタに追加すること。
// ScriptableObjectとは、Unityでデータを保存するためのクラスで、インスタンス化せずに使用できる。
[CreateAssetMenu(menuName = "Game/SceneData")]
public class SceneData : ScriptableObject
{
    [SerializeField, Header("シーン名")]
    private string _sceneName;

    public string SceneName
    {
        get { return _sceneName; }
    }
}
