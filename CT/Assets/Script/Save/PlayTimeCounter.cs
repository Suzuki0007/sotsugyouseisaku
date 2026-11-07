/*********************************************************************/
// * \file   PlayTimeCounter.cs
// * \brief  プレイ時間を数えるコンポーネント（GameSession が自動で作る）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// プレイ時間を数えるコンポーネント
/// </summary>
///
/// GameSession が起動時に自動で作る（シーンに置く必要はない）
[AddComponentMenu("")]
public class PlayTimeCounter : MonoBehaviour
{
    private void Update()
    {
        GameSession.Tick(Time.unscaledDeltaTime);
    }
}
