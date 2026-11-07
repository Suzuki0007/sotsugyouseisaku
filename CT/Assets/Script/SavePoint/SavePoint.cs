/*********************************************************************/
// * \file   SavePoint.cs
// * \brief  調べるとセーブできるセーブポイント
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// セーブポイント
/// </summary>
///
/// ・調べる（E）と「セーブしますか？」→ セーブデータ選択画面 → 選んだスロットにセーブ
/// ・「つづきから」で始めると、再開する位置（Resume Offset の位置）から再開する
/// ・Collider2D（Is Trigger）を付けたオブジェクトに付ける（Hierarchy 右クリック > セーブポイント でひな形を作れる）
[RequireComponent(typeof(Collider2D))]
public class SavePoint : Intaractable
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, TextArea(1, 3), Tooltip("最初に出す確認の文")]
    private string question = "セーブしますか？";

    [SerializeField, Tooltip("つづきからで再開する位置（このオブジェクトからのずれ）。セーブポイントに重ならない位置にする")]
    private Vector2 resumeOffset = new Vector2(0f, -1.2f);

    [SerializeField, Tooltip("セーブしたときの SE（空なら共通の UI 効果音「セーブ完了」：Resources/UISounds）")]
    private SeData saveSe;

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary>
    /// つづきからで再開する位置
    /// </summary>
    public Vector2 ResumePosition => (Vector2)transform.position + resumeOffset;

    //=====================================================================
    // 調べたとき
    //=====================================================================

    protected override void Interact()
    {
        if(GameMenu.Instance == null)
        {
            Debug.LogWarning($"{name}: メニュー画面（GameMenu）がないのでセーブできません。Tools > メニュー画面 > メニューのプレハブを作成 で作ってください", this);
            return;
        }

        GameMenu.Instance.OpenSave(gameObject.scene.name, ResumePosition, question, saveSe);
    }

    //=====================================================================
    // デバッグ表示
    //=====================================================================

    private void OnDrawGizmos()
    {
        // 再開する位置
        Gizmos.color = new Color(0.3f, 0.8f, 1f);
        Gizmos.DrawWireSphere(ResumePosition, 0.3f);
        Gizmos.DrawLine(transform.position, ResumePosition);
    }
}
