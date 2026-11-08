/*********************************************************************/
// * \file   BarkArea.cs
// * \brief  一言の範囲（操作しているキャラが入ると一言を出す）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 一言の範囲
/// </summary>
///
/// 例：暗い部屋の入口で「暗いな…」／ 血の跡のそばで「見るな」／ 長い廊下の途中のつぶやき
/// ・Is Trigger の Collider2D に、操作しているキャラが入ると出す
/// ・出すかどうかは一言のデータの「条件」「何回出すか」「次に出せるまで」「出る確率」で決まる
[RequireComponent(typeof(Collider2D))]
public class BarkArea : MonoBehaviour
{
    [SerializeField, Label("一言のデータ")]
    [Tooltip("Resources/Barks/ のデータ。きっかけは「イベント・範囲からだけ」にしておくとよい")]
    private BarkData data;

    [SerializeField, Label("入るキャラ")]
    [Tooltip("このキャラを操作して入ったときだけ出す")]
    private CharacterRestriction who = CharacterRestriction.Both;

    public BarkData Data => data;

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if(c != null) { c.isTrigger = true; }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        if(m == null || data == null || other.gameObject != m.CurrentPlayer) { return; }
        if(!who.Allows(other.gameObject)) { return; }
        Bark.Play(data, BarkContext.Of(other.gameObject, transform), true);
    }

    private void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if(c == null) { return; }
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.4f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
