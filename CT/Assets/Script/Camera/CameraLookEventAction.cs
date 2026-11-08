/*********************************************************************/
// * \file   CameraLookEventAction.cs
// * \brief  イベントでカメラを決めた場所へ向け、少し見せてから戻すアクション
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// カメラを向ける
/// </summary>
///
/// 例：スイッチを入れたら、離れた場所（相棒のエリア）の扉が開くところを見せる ／ ボスの登場を見せる
/// ・「順番に実行」の EventSequence では、見せ終わる（戻す）まで次の部品を待つ
/// ・見せている間、プレイヤーを止めたいときは EventSequence の Lock Player をオン
public class CameraLookEventAction : EventAction
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("見せる場所")]
    [Tooltip("カメラを向ける場所（扉など。空のオブジェクトでもよい）")]
    private Transform target;

    [SerializeField, Label("動く時間（秒）", 0.0f)]
    [Tooltip("カメラがなめらかに移る時間。0 なら一瞬")]
    private float moveTime = 0.6f;

    [SerializeField, Label("見せる時間（秒）", 0.0f)]
    [Tooltip("着いてから、この秒数見せる")]
    private float holdTime = 1.2f;

    [SerializeField, Label("戻す")]
    [Tooltip("オン：見せたあと、操作しているキャラにカメラを戻す／オフ：向けたまま（あとで別の「カメラを向ける」で戻す）")]
    private bool returnAfter = true;

    //=====================================================================
    // アクション関数
    //=====================================================================

    public override void Execute(GameObject gameObject)
    {
        // すべて同時に実行のときは、向けるだけ（戻すのは Run）
        FocusCamera cam = FindAnyObjectByType<FocusCamera>();
        if(cam == null || target == null) { return; }
        cam.LookAt(target.position, moveTime);
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        FocusCamera cam = FindAnyObjectByType<FocusCamera>();
        if(cam == null || target == null) { yield break; }

        cam.LookAt(target.position, moveTime);
        yield return WaitSeconds(moveTime);
        yield return WaitSeconds(holdTime);

        if(!returnAfter) { yield break; }

        cam.LookAt(null, moveTime);
        yield return WaitSeconds(moveTime);
    }

    private static IEnumerator WaitSeconds(float seconds)
    {
        float t = 0.0f;
        while(t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
