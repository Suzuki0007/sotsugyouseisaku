/*********************************************************************/
// * \file   PuzzleUISettings.cs
// * \brief  謎解きの画面（番号錠・アイテムを選ぶ・メモを読む）の見た目の設定（Resources/PuzzleUI.asset）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;

/// <summary>
/// 謎解きの画面の見た目の設定
/// </summary>
[CreateAssetMenu(menuName = "仕掛け/謎解きの画面の設定", fileName = "PuzzleUI")]
public class PuzzleUISettings : ScriptableObject
{
    [Label("文字のフォント")]
    [Tooltip("日本語の出るフォント（NotoSansJP-Regular SDF など）")]
    public TMP_FontAsset font;

    [Label("背景を暗くする色")]
    public Color dimColor = new Color(0.0f, 0.0f, 0.0f, 0.6f);

    [Label("箱の色")]
    public Color boxColor = new Color(0.08f, 0.08f, 0.12f, 0.96f);

    [Label("文字の色")]
    public Color textColor = new Color(0.95f, 0.95f, 0.95f, 1.0f);

    [Label("選んでいる物の色")]
    public Color selectedColor = new Color(0.95f, 0.8f, 0.3f, 1.0f);

    [Label("選んでいない物の色")]
    public Color normalColor = new Color(0.25f, 0.25f, 0.32f, 1.0f);

    [Label("メモの紙の色")]
    public Color paperColor = new Color(0.93f, 0.9f, 0.82f, 1.0f);

    [Label("メモの文字の色")]
    public Color paperTextColor = new Color(0.15f, 0.12f, 0.1f, 1.0f);
}
