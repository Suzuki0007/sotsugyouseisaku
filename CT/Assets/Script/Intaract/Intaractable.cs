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
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private Key interactKeyInput = Key.E;

    protected GameObject player;

    //=====================================================================
    // 関数
    //=====================================================================

    void Update()
    {
        if(!player) { return; }
        if(player == null) { return; }
        if(Keyboard.current == null) { return; }

        // 会話中（閉じた直後も含む。同じキーで会話が開き直すのを防ぐ）・シーン遷移中・イベント演出中・
        // アイテム画面・メニュー画面の間は調べられない
        if(InputBlock.IsBlocked) { return; }

        // インタラクトキーが押されたときに処理を呼び出す
        if(Keyboard.current[interactKeyInput].wasPressedThisFrame)
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

        OnPlayerEnter(player);
    }

    /// <summary>
    /// OnTriggerExit2D関数は、他のオブジェクトがトリガーから出たときに呼ばれる
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerExit2D(Collider2D other)
    {
        if(!IsPlayerTag(other)) { return; }

        player = null;

        OnPlayerExit(other.gameObject);
    }

    /// <summary>
    /// プレイヤーが範囲に入ったときに呼ばれる仮想関数（継承先で必要なら使う）
    /// </summary>
    protected virtual void OnPlayerEnter(GameObject enteredPlayer) { }

    /// <summary>
    /// プレイヤーが範囲から出たときに呼ばれる仮想関数（継承先で必要なら使う）
    /// </summary>
    protected virtual void OnPlayerExit(GameObject exitedPlayer) { }

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
