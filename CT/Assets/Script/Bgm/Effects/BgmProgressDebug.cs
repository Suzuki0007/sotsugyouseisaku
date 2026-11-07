/*********************************************************************/
// * \file   BgmProgressDebug.cs
// * \brief  BGM進行度の調整用デバッグクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 再生中にインスペクタのスライダーで進行度を動かし、音の変化を確認するためのクラス
/// 調整が終わったら外す
/// </summary>
public class BgmProgressDebug : MonoBehaviour
{
    [SerializeField, Tooltip("チェック中はスライダーの値で進行度を上書きする")]
    private bool overrideProgress = true;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float progress;

    [SerializeField, Tooltip("現在の進行度 (表示用)")]
    private float currentValue;

    private void Update()
    {
        if(overrideProgress) { BgmProgress.Set(progress); }
        currentValue = BgmProgress.Value;
    }
}
