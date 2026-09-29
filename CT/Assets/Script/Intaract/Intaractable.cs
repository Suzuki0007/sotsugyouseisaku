/*********************************************************************/
// * \file   Intaractable.cs
// * \brief  インタラクト可能オブジェクトの基底クラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// インタラクト可能オブジェクトの抽象クラス
/// </summary>
/// 
/// 鍵や宝箱など、プレイヤーが調べることができるオブジェクトに継承させる
public abstract class Intaractable : MonoBehaviour
{
    protected GameObject player;

    void Update()
    {
        if(!player) { return; }

        // Eキーが押されたときにインタラクト処理を呼び出す
        if(Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    /// <summary>
    /// OnTriggerEnter2D関数は、他のオブジェクトがトリガーに入ったときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if(!IsPlayerTag(collision)) { return; }
        
        player = collision.gameObject;

        Debug.Log($"{gameObject.name}を調べられるまする");
    }

    /// <summary>
    /// OnTriggerExit2D関数は、他のオブジェクトがトリガーから出たときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerExit2D(Collider2D other)
    {
        if(!IsPlayerTag(other)) { return; }

        player = null;
    }

    /// <summary>
    /// プレイヤーのタグを判定するメソッド
    /// </summary>
    /// <param name="other"></param>
    /// <returns> trueならプレイヤーのタグ、falseならそれ以外 </returns>
    public bool IsPlayerTag(Collider2D other)
    {
        return other.CompareTag("Player");
    }

    /// <summary>
    /// インタラクト処理を実装する抽象メソッド
    /// </summary>
    protected abstract void Interact();
}
