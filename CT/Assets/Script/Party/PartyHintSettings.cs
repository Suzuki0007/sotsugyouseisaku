/*********************************************************************/
// * \file   PartyHintSettings.cs
// * \brief  相棒の目印の見た目の設定（Resources/PartyHints.asset）
// *
// * \author
/*********************************************************************/

using TMPro;
using UnityEngine;

/// <summary>
/// 相棒の目印の見た目の設定（Resources/PartyHints.asset）
/// </summary>
[CreateAssetMenu(menuName = "仕掛け/相棒の目印の設定", fileName = "PartyHints")]
public class PartyHintSettings : ScriptableObject
{
    [Label("出す")]
    [Tooltip("オフにすると、目印を出さない")]
    public bool enabled = true;

    [Label("近づいたら出す距離", 0.0f)]
    [Tooltip("操作しているキャラが、使えない仕掛けにこの距離まで近づいたら出す")]
    public float showDistance = 1.8f;

    [Label("仕掛けからの高さ")]
    public float height = 1.0f;

    [Label("背景の絵")]
    public Sprite background;

    [Label("背景の大きさ")]
    public Vector2 backgroundSize = new Vector2(1.1f, 0.42f);

    [Label("文字のフォント")]
    public TMP_FontAsset font;

    [Label("文字の大きさ", 0.1f)]
    public float fontSize = 3.0f;

    [Label("ノアの文字")]
    public string noahText = "ノア";

    [Label("ノアの色")]
    public Color noahColor = new Color(1.0f, 0.55f, 0.75f, 0.95f);

    [Label("シグルの文字")]
    public string sigleText = "シグル";

    [Label("シグルの色")]
    public Color sigleColor = new Color(0.45f, 0.7f, 1.0f, 0.95f);

    [Label("描く順番")]
    public int sortingOrder = 200;
}
