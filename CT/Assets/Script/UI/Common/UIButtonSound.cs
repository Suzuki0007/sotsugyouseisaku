/*********************************************************************/
// * \file   UIButtonSound.cs
// * \brief  ボタンを押したときに UI の効果音を鳴らすコンポーネント
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンを押したときに UI の効果音を鳴らす
/// </summary>
///
/// Button と同じオブジェクトに付けるだけでよい（OnClick に登録しなくてよい）
/// タイトル・メニュー・セーブ／ロード画面は最初から鳴るので、それ以外の画面のボタン（ゲームオーバー画面など）に使う
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [SerializeField, Tooltip("鳴らす音の種類")]
    private UISoundType sound = UISoundType.Decide;

    [SerializeField, Tooltip("このボタンだけ別の音にしたいとき（空なら共通の UI 効果音）")]
    private SeData overrideSe;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => UISound.Play(overrideSe, sound));
    }
}
