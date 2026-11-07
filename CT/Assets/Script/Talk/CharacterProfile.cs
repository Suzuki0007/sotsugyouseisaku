/*********************************************************************/
// * \file   CharacterProfile.cs
// * \brief  会話に登場するキャラクターの設定（ScriptableObject）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 会話に登場するキャラクターの設定クラス
/// </summary>
///
/// Projectウィンドウで右クリック → Create > Talk > CharacterProfile で作成する
[CreateAssetMenu(menuName = "Talk/CharacterProfile", fileName = "NewCharacter")]
public class CharacterProfile : ScriptableObject
{
    //=====================================================================
    // 型
    //=====================================================================

    /// <summary>
    /// 立ち絵を表示する位置
    /// </summary>
    public enum PortraitSide
    {
        Left,
        Right
    }

    /// <summary>
    /// 表情と画像の組
    /// </summary>
    [System.Serializable]
    public class ExpressionSprite
    {
        public Expression expression = Expression.Normal;
        public Sprite sprite;
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [Header("名前")]

    [Tooltip("名前欄に表示する名前")]
    [SerializeField]
    private string displayName;

    [Tooltip("名前欄の文字色")]
    [SerializeField]
    private Color nameColor = Color.white;

    [Header("立ち絵")]

    [Tooltip("立ち絵を表示する位置")]
    [SerializeField]
    private PortraitSide side = PortraitSide.Left;

    [Tooltip("立ち絵を左右反転して表示するか")]
    [SerializeField]
    private bool flipX = false;

    [Tooltip("表情差分の一覧。指定した表情がない場合はNormal（なければ先頭）を表示する")]
    [SerializeField]
    private List<ExpressionSprite> expressions = new List<ExpressionSprite>();

    //=====================================================================
    // プロパティ
    //=====================================================================

    public string DisplayName => displayName;

    public Color NameColor => nameColor;

    public PortraitSide Side => side;

    public bool FlipX => flipX;

    //=====================================================================
    // 関数
    //=====================================================================

    /// <summary>
    /// 表情に対応する立ち絵を取得する
    /// </summary>
    /// <param name="expression"> 表情 </param>
    /// <returns> 立ち絵（見つからなければNormal、それもなければ先頭、何もなければnull） </returns>
    public Sprite GetSprite(Expression expression)
    {
        Sprite normal = null;
        Sprite first = null;

        foreach(ExpressionSprite e in expressions)
        {
            if(e == null || e.sprite == null) { continue; }

            if(e.expression == expression) { return e.sprite; }

            if(first == null) { first = e.sprite; }
            if(normal == null && e.expression == Expression.Normal) { normal = e.sprite; }
        }

        if(normal != null) { return normal; }

        return first;
    }
}
