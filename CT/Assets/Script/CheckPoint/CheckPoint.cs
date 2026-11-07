/*********************************************************************/
// * \file   CheckPoint.cs
// * \brief  チェックポイントクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/


using UnityEngine;

/// <summary>
/// チェックポイント（触れるとセーブする）
/// </summary>
///
/// Collider2D（Is Trigger）を付けたオブジェクトに付ける
/// 「つづきから」で始めると、最後に触れたチェックポイントの位置から再開する
public class CheckPoint : ObjectBase
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            // 再開する場所としてこの位置を記録する
            GameSession.RecordResumePosition(gameObject.scene.name, transform.position);

            Save.Commit(); // 仮置きを確定する
            Save.Write(); // セーブデータを書き込む

            Debug.Log("セーブしました。");
        }
    }
}
