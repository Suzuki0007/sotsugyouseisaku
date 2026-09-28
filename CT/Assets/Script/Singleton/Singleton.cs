/*********************************************************************/
// * \file   Singleton.cs
// * \brief  シングルトンクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    [SerializeField] bool persistAcrossScenes;// シーンをまたいで存続するかどうか

    protected virtual void Awake()
    {
        // 2つ目以降あれば破棄する
        if(Instance != null && Instance != this)
        {
            Debug.Log($"重複した{typeof(T).Name}を削除します");
            Destroy(gameObject);
            return;
        }

        Instance = (T)(object)this;
        
        if(persistAcrossScenes)
        {
            transform.SetParent(null);// 親を外す
            DontDestroyOnLoad(gameObject);// シーンをまたいで存続する
        }
    }

    protected virtual void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
