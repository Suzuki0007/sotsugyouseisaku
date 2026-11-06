/*********************************************************************/
// * \file   SeData.cs
// * \brief  SE(効果音)1種類分の設定データ
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// SE1種類分の設定 (ScriptableObject)
/// Projectウィンドウで 右クリック → Create → Audio → SeData で作成する
///
/// 使い方:
///   スクリプトに [SerializeField] private SeData shotSe; を用意し、
///   SeManager.PlaySe(shotSe); で鳴らす (未設定(null)なら何もしない)
/// </summary>
[CreateAssetMenu(menuName = "Audio/SeData", fileName = "Se_New")]
public class SeData : ScriptableObject
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Header("音源")]

    [SerializeField, Tooltip("鳴らす音 (複数ある場合はランダムに選ぶ。同じ音の連続はなるべく避ける)")]
    private AudioClip[] clips = new AudioClip[0];

    [Header("音量・ピッチ")]

    [SerializeField, Range(0.0f, 1.0f)]
    private float volume = 1.0f;

    [SerializeField, Tooltip("ピッチの最小値 (最大値と同じならランダムなし)")]
    [Range(0.1f, 3.0f)]
    private float pitchMin = 1.0f;

    [SerializeField, Tooltip("ピッチの最大値")]
    [Range(0.1f, 3.0f)]
    private float pitchMax = 1.0f;

    [Header("再生方法")]

    [SerializeField, Tooltip("ループ再生する (SeManager.Stop で止める)")]
    private bool loop = false;

    [SerializeField, Tooltip("0 = 2D(どこでも同じ音量)  1 = 3D(位置によって聞こえ方が変わる)")]
    [Range(0.0f, 1.0f)]
    private float spatialBlend = 0.0f;

    [Header("連打対策")]

    [SerializeField, Tooltip("同じSEを再び鳴らせるまでの最短間隔(秒)。0なら制限なし")]
    private float minInterval = 0.03f;

    [SerializeField, Tooltip("同じSEを同時に鳴らせる最大数。0なら制限なし")]
    private int maxConcurrent = 4;

    [Header("出力先")]

    [SerializeField, Tooltip("このSEだけ別のAudioMixerGroupに出す場合に設定 (任意)")]
    private AudioMixerGroup outputGroup;

    // 直前に鳴らしたクリップの番号 (同じ音の連続を避けるため)
    [System.NonSerialized]
    private int lastClipIndex = -1;

    //=====================================================================
    // プロパティ
    //=====================================================================

    public float Volume => volume;
    public bool Loop => loop;
    public float SpatialBlend => spatialBlend;
    public float MinInterval => minInterval;
    public int MaxConcurrent => maxConcurrent;
    public AudioMixerGroup OutputGroup => outputGroup;

    public bool HasClip => clips != null && clips.Length > 0;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 鳴らすクリップを1つ選ぶ
    /// </summary>
    public AudioClip PickClip()
    {
        if(!HasClip) { return null; }
        if(clips.Length == 1) { return clips[0]; }

        int index = Random.Range(0, clips.Length);
        if(index == lastClipIndex)
        {
            index = (index + Random.Range(1, clips.Length)) % clips.Length;
        }
        lastClipIndex = index;
        return clips[index];
    }

    /// <summary>
    /// 鳴らすピッチを決める
    /// </summary>
    public float PickPitch()
    {
        return Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));
    }
}
