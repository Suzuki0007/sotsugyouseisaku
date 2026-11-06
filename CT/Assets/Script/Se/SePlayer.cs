/*********************************************************************/
// * \file   SePlayer.cs
// * \brief  インスペクターから使うSE再生コンポーネント
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// コードを書かずにSEを鳴らすためのコンポーネント
/// ・UnityEvent (ボタンの OnClick など) から Play() / Stop() を呼ぶ
/// ・Animation Event から PlaySe(SeData) を呼ぶ
/// ・有効化時 / 無効化時に自動で鳴らす
/// </summary>
public class SePlayer : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Tooltip("Play() で鳴らすSE")]
    private SeData se;

    [SerializeField, Tooltip("有効化されたときに se を鳴らす")]
    private bool playOnEnable = false;

    [SerializeField, Tooltip("無効化されたときに鳴らすSE (任意)")]
    private SeData seOnDisable;

    [SerializeField, Tooltip("このオブジェクトの位置で鳴らす (3D寄りのSEのとき有効)")]
    private bool followThisObject = true;

    private SeHandle handle;

    //=====================================================================
    // 関数
    //=====================================================================

    private void OnEnable()
    {
        if(playOnEnable) { Play(); }
    }

    private void OnDisable()
    {
        // ループSEは止める
        SeManager.StopSe(handle);
        handle = SeHandle.None;

        if(seOnDisable != null) { SeManager.PlaySe(seOnDisable, transform.position); }
    }

    /// <summary>
    /// 設定されたSEを鳴らす
    /// </summary>
    public void Play()
    {
        PlaySe(se);
    }

    /// <summary>
    /// 指定したSEを鳴らす (Animation Event / UnityEvent から SeData を渡して呼べる)
    /// ※ Animation Event はオーバーロードを嫌うため Play とは別名にしている
    /// </summary>
    public void PlaySe(SeData data)
    {
        if(data == null) { return; }

        SeHandle h = followThisObject
            ? SeManager.PlaySe(data, transform)
            : SeManager.PlaySe(data);

        if(data.Loop) { handle = h; }
    }

    /// <summary>
    /// このコンポーネントで鳴らしたループSEを止める
    /// </summary>
    public void Stop()
    {
        SeManager.StopSe(handle);
        handle = SeHandle.None;
    }
}
