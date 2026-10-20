/*********************************************************************/
// * \file   SpawnedObject.cs
// * \brief  オブジェクトがスポーンしたときに追加されるコンポーネント
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using System;

/// <summary>
/// オブジェクトがスポーンしたときに追加されるコンポーネント
/// </summary>
public class SpawnedObject : MonoBehaviour
{
    // オブジェクトの破棄通知
    public Action OnDestroyed; 
    
    private void OnDestroy() { OnDestroyed?.Invoke(); }
}
