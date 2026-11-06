/*********************************************************************/
// * \file   PlayerSpawnPoint.cs
// * \brief  シーン遷移後のプレイヤー出現位置
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// シーン遷移後のプレイヤー出現位置
/// 遷移元の SceneChange に設定した「出現位置ID」と同じIDの地点にプレイヤーを出現させる
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("出現位置ID (遷移元の SceneChange の出現位置IDと合わせる)")]
    private string spawnPointId;

    [SerializeField, Tooltip("Sigleの出現位置 (未設定ならこのオブジェクトの位置)")]
    private Transform sigleSpawnPoint;

    /// <summary>
    /// 次のシーンで使う出現位置ID (シーンをまたいで保持する)
    /// </summary>
    public static string PendingSpawnPointId { get; set; }

    //=====================================================================
    // プロパティ
    //=====================================================================

    public string SpawnPointId => spawnPointId;

    /// <summary>
    /// Noahの出現位置 (このオブジェクト自身)
    /// </summary>
    public Transform NoahPoint => transform;

    /// <summary>
    /// Sigleの出現位置
    /// </summary>
    public Transform SiglePoint => sigleSpawnPoint != null ? sigleSpawnPoint : transform;

    //=====================================================================
    // 検索
    //=====================================================================

    /// <summary>
    /// 保持している出現位置IDに対応する出現位置を取得し、IDをクリアする
    /// </summary>
    /// <returns>見つからなければ null</returns>
    public static PlayerSpawnPoint ConsumePending()
    {
        string id = PendingSpawnPointId;
        PendingSpawnPointId = null;

        if(string.IsNullOrEmpty(id)) { return null; }

        foreach(PlayerSpawnPoint point in FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None))
        {
            if(point.spawnPointId == id) { return point; }
        }

        Debug.LogWarning($"出現位置ID「{id}」のPlayerSpawnPointが見つかりません。デフォルトの位置に出現します");
        return null;
    }

    //=====================================================================
    // デバッグ表示
    //=====================================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(NoahPoint.position, 0.3f);

        if(sigleSpawnPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(sigleSpawnPoint.position, 0.3f);
            Gizmos.DrawLine(NoahPoint.position, sigleSpawnPoint.position);
        }
    }
}
