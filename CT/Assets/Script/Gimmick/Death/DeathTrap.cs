/*********************************************************************/
// * \file   DeathTrap.cs
// * \brief  即死の罠（範囲に入ると即死。やり直す／ゲームオーバー）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 即死の罠
/// </summary>
///
/// ・Is Trigger の範囲に操作しているキャラが入ると（条件を満たしているとき）、演出のあと即死する
///   例：踏むと天井が落ちてくる床 ／ 開けてはいけない扉の前 ／ 時間切れで閉じる部屋
/// ・即死したあと：やり直す（やり直す位置 RetryPoint から）／ゲームオーバー
/// ・ついてくる相棒・待たせている相棒が入っても反応しない
[RequireComponent(typeof(Collider2D))]
public class DeathTrap : MonoBehaviour
{
    [SerializeField, Label("即死したあと")]
    private DeathResult result = DeathResult.Retry;

    [SerializeField, Label("作動する条件")]
    [Tooltip("満たしているときだけ作動する（空ならいつも）。例：仕掛けが OFF のとき（スイッチで罠を止められる）")]
    private GameCondition condition = new GameCondition();

    [SerializeField, Label("即死の演出")]
    [Tooltip("叫び声・画面を赤く・会話など。終わってから暗転する。空ならすぐ暗転")]
    private EventSequence beforeDeath;

    private Collider2D area;

    public DeathResult Result => result;

    private void Awake()
    {
        area = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if(InstantDeath.Running || InputBlock.IsBlocked) { return; }
        if(condition != null && !condition.IsEmpty && !condition.IsMet()) { return; }

        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        GameObject current = m != null ? m.CurrentPlayer : null;
        if(current == null || !area.OverlapPoint(current.transform.position)) { return; }

        InstantDeath.Kill(this, result, beforeDeath);
    }

    private void OnDrawGizmos()
    {
        Collider2D c = area != null ? area : GetComponent<Collider2D>();
        if(c == null) { return; }
        Gizmos.color = new Color(1.0f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
