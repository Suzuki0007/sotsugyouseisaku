/*********************************************************************/
// * \file   RetryPoint.cs
// * \brief  やり直す位置（入ると記録。即死の罠に引っかかったら、ここからやり直す）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// やり直す位置
/// </summary>
///
/// ・Is Trigger の範囲に操作しているキャラが入ると、「やり直す位置」と、そのときの進行を記録する（セーブの確定。ファイルには書かない）
/// ・即死の罠（DeathTrap）・イベントの部品「即死」で「やり直す」になったら、ここからやり直す（その後に拾った物・フラグなどは元に戻る）
/// ・部屋の入口ごとに置く（魔女の家・Ib の「その部屋の入口から」）
[RequireComponent(typeof(Collider2D))]
public class RetryPoint : MonoBehaviour
{
    [SerializeField, Label("やり直す位置")]
    [Tooltip("やり直したときに出る位置（空ならこの範囲の真ん中）")]
    private Transform respawn;

    [SerializeField, Label("記録したときの SE")]
    [Tooltip("空なら鳴らさない（ふつうは気づかせない）")]
    private SeData recordSe;

    private Collider2D area;
    private bool inside;

    //=====================================================================
    // 記録（シーンをまたがない）
    //=====================================================================

    /// <summary> 記録したシーン </summary>
    public static string RecordedScene { get; private set; }

    /// <summary> 記録したやり直す位置 </summary>
    public static Vector2 RecordedPosition { get; private set; }

    /// <summary> 記録したときの、セーブしないフラグの写し </summary>
    public static System.Collections.Generic.HashSet<string> RecordedTemporaryFlags { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { RecordedScene = null; RecordedTemporaryFlags = null; }

    /// <summary> 今のシーンにやり直す位置の記録があるか </summary>
    public static bool HasRecord(string sceneName) => RecordedScene == sceneName;

    /// <summary>
    /// 今の進行と位置を記録する（DeathTrap からも呼べる）
    /// </summary>
    public static void Record(string sceneName, Vector2 position)
    {
        Save.Commit();
        RecordedScene = sceneName;
        RecordedPosition = position;
        RecordedTemporaryFlags = GameFlags.CaptureTemporary();
    }

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        area = GetComponent<Collider2D>();
    }

    private void Update()
    {
        PlayerSwitchManager m = PlayerSwitchManager.Instance;
        GameObject current = m != null ? m.CurrentPlayer : null;
        bool now = current != null && area.OverlapPoint(current.transform.position);

        // 入った瞬間に記録する
        if(now && !inside && !InputBlock.IsBlocked)
        {
            Vector2 pos = respawn != null ? (Vector2)respawn.position : (Vector2)area.bounds.center;
            Record(gameObject.scene.name, pos);
            SeManager.PlaySe(recordSe, transform.position);
        }
        inside = now;
    }

    private void OnDrawGizmos()
    {
        Collider2D c = area != null ? area : GetComponent<Collider2D>();
        if(c == null) { return; }
        Gizmos.color = new Color(0.4f, 1.0f, 0.8f, 0.5f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
        if(respawn != null) { Gizmos.DrawWireSphere(respawn.position, 0.3f); }
    }
}
