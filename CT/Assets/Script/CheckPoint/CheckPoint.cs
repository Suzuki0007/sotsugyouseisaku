/*********************************************************************/
// * \file   CheckPoint.cs
// * \brief  チェックポイントクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/


using UnityEngine;

public class CheckPoint : ObjectBase
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            Save.Commit(); // 仮置きを確定する
            Save.Write(); // セーブデータを書き込む

            Debug.Log("セーブしました。");
        }
    }
}
