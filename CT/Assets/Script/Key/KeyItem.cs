/*********************************************************************/
// * \file   KeyItem.cs
// * \brief  キーのアイテムクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// キーのアイテムクラス
/// </summary>
public class KeyItem : MonoBehaviour
{
    private bool playerInRange = false;

    private NoahInventory noahInventory;

    /// <summary>
    /// OnTriggerEnter2D関数は、プレイヤーがトリガーに入ったときに呼ばれる
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            playerInRange = true;

            noahInventory = collision.GetComponent<NoahInventory>();

            Debug.Log("鍵を拾える範囲に入りました");
        }
    }

    /// <summary>
    /// OnTriggerExit2D関数は、プレイヤーがトリガーから出たときに呼ばれる
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            playerInRange = false;
            noahInventory = null;

            Debug.Log("鍵から離れました");
        }
    }

    private void Update()
    {
        if(!playerInRange) { return; }

        // プレイヤーがEキーを押したときにInteract関数を呼び出す
        if(Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    /// <summary>
    /// Interact関数は、プレイヤーが鍵を拾うときに呼ばれる
    /// </summary>
    private void Interact()
    {
        if(!playerInRange){ return;  }

        if(noahInventory == null){ return; }

        noahInventory.GetKey();

        // 鍵のオブジェクトを非表示にする
        //
        // SetActive(false)を使用して、鍵のオブジェクトを非表示にする
        //
        // この関数は、UnityのGameObjectクラスのメソッドであり、オブジェクトを非表示にするために使用される
        gameObject.SetActive(false);

        Debug.Log("鍵を拾いました");
    }
}
