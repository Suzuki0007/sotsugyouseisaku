/*********************************************************************/
// * \file   InventoryManager.cs
// * \brief  インベントリ管理クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// インベントリ管理クラス
/// </summary>
public class InventoryManager : Singleton<InventoryManager>
{
    /// <summary>
    /// ノアのデータ
    /// </summary>
    /// 
    /// getはこのクラスのインスタンスのHasKeyプロパティの値を返す
    /// setはこのクラスのインスタンスのHasKeyプロパティに値を代入する
    /// たとえるなら、ゲッターとセッターが合体したようなもの
    /// 
    /// setをprivateにすることで、外部からはnoahDataプロパティの値を変更できなくする
    public NoahData NoahData { get; private set; }

    /// <summary>
    /// 鍵を持っているかどうか
    /// </summary>
    public bool HasKey { get; set; }

    protected override void Awake()
    {
        // 重複したInventoryManagerが存在する場合は破棄する
        if(Instance != null && Instance != this)
        {
            Debug.Log("重複したInventoryManagerを削除します");
            Destroy(gameObject);
            return;
        }
        base.Awake();

        // NoahDataの生成
        NoahData = new NoahData();

        //SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// シーンがロードされたときに呼び出されるメソッド
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    /// 
    /// シーンがロードされたときにログに出力する。
    /// 現在のシーン名、
    /// ロードモード、
    /// InventoryManagerのID、
    /// NoahDataのハッシュコード、
    /// 鍵を持っているかどうかを
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(
            $"========== SCENE LOAD ==========\n" +
            $"Scene : {scene.name}\n" +
            $"Mode : {mode}\n" +
            $"Manager ID : {GetEntityId()}\n" +
            $"Data ID     : {NoahData.GetHashCode()}\n" +
            $"HasKey : {NoahData.hasKey}\n" +
            $"================================"
        );
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
