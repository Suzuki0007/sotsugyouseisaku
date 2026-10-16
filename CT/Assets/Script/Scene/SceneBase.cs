/*********************************************************************/
// * \file   SceneBase.cs
// * \brief  シーンのベースクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using UnityEngine;

public abstract class SceneBase : MonoBehaviour
{ 
    protected virtual void Awake()
    {
        GameManager.Instance.RegisterScene(this);
    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {
    }

    protected virtual void OnDestroy()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterScene(this);
        }
    }
}
