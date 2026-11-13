/*********************************************************************/
// * \file   SceneChangeEventAction.cs
// * \brief  イベントの部品「シーンを移る」
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントの部品「シーンを移る」（会話のあとで次のマップへ、など）
/// </summary>
///
/// ・出口（SceneChange）にぶつかる代わりに、イベントの最後でシーンを移る
/// ・暗転・セーブの確定は、出口と同じ（シーンの SceneChange を使う）
/// ・この部品のあとの部品は実行されない（シーンが変わるため）。一番最後に置く
public class SceneChangeEventAction : EventAction
{
    [SerializeField, SceneName, Tooltip("移る先のシーン（Build Settings に入っているものから選ぶ）")]
    private string sceneName;

    [SerializeField, SpawnPointId(nameof(sceneName)), Tooltip("移った先での出現位置（移る先のシーンの PlayerSpawnPoint から選ぶ）")]
    private string spawnPointId;

    public override void Execute(GameObject gameObject)
    {
        if(string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"{name}: 移る先のシーンが入っていません", this);
            return;
        }

        SceneChange sceneChange = FindAnyObjectByType<SceneChange>();
        if(sceneChange == null)
        {
            Debug.LogError($"{name}: シーンに SceneChange がありません（OtherSystem/SceneChange を置いてください）", this);
            return;
        }

        sceneChange.ChangeScene(sceneName, spawnPointId);
    }
}
