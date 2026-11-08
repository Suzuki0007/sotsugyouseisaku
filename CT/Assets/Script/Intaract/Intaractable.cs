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

    /// <summary>
    /// 範囲の中にいる「操作しているキャラ」（いなければ null）
    /// ついてくる相棒・待たせている相棒が範囲にいても、ここには入らない
    /// </summary>
    protected GameObject player;

    // 範囲の中にいるプレイヤー（ノア・シグル。操作していない方も含む）
    private readonly System.Collections.Generic.HashSet<GameObject> inside = new System.Collections.Generic.HashSet<GameObject>();

    //=====================================================================
    // 関数
    //=====================================================================

    void Update()
    {
        // キャラを切り替えたときに、範囲の中の操作キャラを入れ直す
        RefreshPlayer();

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

        GameObject entered = collision.gameObject;
        inside.Add(entered);

        // 反応するのは操作しているキャラだけ（ついてくる相棒が先に入っても反応しない）
        if(!IsControlled(entered)) { return; }

        player = entered;

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

        GameObject exited = other.gameObject;
        inside.Remove(exited);

        // 相棒が出ただけなら何もしない（操作しているキャラはまだ中にいるかもしれない）
        if(exited != player) { return; }

        player = null;

        OnPlayerExit(exited);
    }

    /// <summary>
    /// 操作しているキャラか（切り替えの仕組みがないシーンでは、どのプレイヤーも操作キャラとして扱う）
    /// </summary>
    protected static bool IsControlled(GameObject target)
    {
        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null || manager.CurrentPlayer == null) { return true; }
        return manager.CurrentPlayer == target;
    }

    /// <summary>
    /// 範囲の中の操作キャラを入れ直す（キャラを切り替えたとき・相棒が出入りしたとき）
    /// </summary>
    private void RefreshPlayer()
    {
        if(inside.Count == 0) { player = null; return; }

        inside.RemoveWhere(go => go == null || !go.activeInHierarchy);

        GameObject controlled = null;
        foreach(GameObject go in inside) { if(IsControlled(go)) { controlled = go; break; } }
        player = controlled;
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
