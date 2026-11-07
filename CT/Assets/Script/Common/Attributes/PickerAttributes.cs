/*********************************************************************/
// * \file   PickerAttributes.cs
// * \brief  文字列の欄を選択式にするための属性
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// string の欄を「Build Settings に入っているシーン」から選ぶドロップダウンにする
/// </summary>
///
/// 例：[SerializeField, SceneName] private string sceneName;
public class SceneNameAttribute : PropertyAttribute { }

/// <summary>
/// string の欄を「指定したシーンにある PlayerSpawnPoint の ID」から選ぶドロップダウンにする
/// </summary>
///
/// 例：[SerializeField, SpawnPointId(nameof(sceneName))] private string spawnPointId;
/// sceneField には、シーン名が入っている同じクラスの string の欄の名前を渡す
public class SpawnPointIdAttribute : PropertyAttribute
{
    public readonly string SceneField;

    public SpawnPointIdAttribute(string sceneField)
    {
        SceneField = sceneField;
    }
}
