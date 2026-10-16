/*********************************************************************/
// * \file   SceneManager.cs
// * \brief  シーンを管理するクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private FadeManager _fade;
    public SceneBase CurrentScene { get; private set; }

    private bool _transitioning = false;// シーン遷移中かどうか

    public bool IsTransitioning
    {
        get { return _transitioning; }
    }

    // RuntimeInitializeOnLoadMethodとは、 ゲームが開始される前に特定のメソッドを自動的に呼び出すために使用されます。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if(Instance != null)
        {
            return;
        }

        //Resourcesとは、Unityのリソース管理システムで、プロジェクト内の特定のフォルダに配置されたアセットを動的にロードするための仕組みです。指定したパスからアセットをロードできます。
        GameManager prefab = Resources.Load<GameManager>("GameManager");

        if(prefab == null)
        {
            Debug.LogError("フォルダが見つかりません");
            return;
        }

        Instantiate(prefab);// GameManagerのインスタンスを生成
        Save.Read();// セーブデータの読み込み
    }

    // シーンを登録する
    public void RegisterScene(SceneBase scene)
    {
        CurrentScene = scene;
    }

    // シーンを登録解除する
    public void UnregisterScene(SceneBase scene)
    {
        if(CurrentScene == scene)
        {
            CurrentScene = null;
        }
    }

    // シーン遷移を開始する
    public void ChangeScene(SceneData data)
    {
        StartTransition(data.SceneName);
    }

    // シーンをリロードする
    public void ReloadScene()
    {
        StartTransition(SceneManager.GetActiveScene().name);
    }

    private void StartTransition(string sceneName)
    {
        if(_transitioning)
        {
            return;
        }

        // シーンがロードできるかどうかを確認する
        // Application.CanStreamedLevelBeLoadedとは、指定されたシーンがロード可能かどうかを確認するためのメソッドです。
        if(!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"シーン {sceneName} はロードできません");
            return;
        }

        StartCoroutine(Run(sceneName));
    }

    private IEnumerator Run(string sceneName)
    {
        _transitioning = true;
        Time.timeScale = 1.0f; // シーン遷移中はポーズを解除する
        yield return _fade.Out();

        // AsyncOperationとは、非同期操作を表すクラスで、シーンのロードやアンロードなどの非同期処理を管理するために使用されます。
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while(!op.isDone)
        {
            yield return null;
        }

        yield return _fade.In();
        _transitioning = false;
    }

}