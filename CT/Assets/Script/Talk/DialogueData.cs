/*********************************************************************/
// * \file   DialogueData.cs
// * \brief  会話データ（ScriptableObject）
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 会話データクラス
/// </summary>
///
/// 1つの会話 = 複数の文（Line）
/// Projectウィンドウで右クリック → Create > Talk > DialogueData で作成する
[CreateAssetMenu(menuName = "Talk/DialogueData", fileName = "NewDialogue")]
public class DialogueData : ScriptableObject
{
    //=====================================================================
    // 型
    //=====================================================================

    /// <summary>
    /// 会話の1文
    /// </summary>
    [System.Serializable]
    public class Line
    {
        [Tooltip("話すキャラクター（空なら立ち絵なし。看板やナレーション用）")]
        public CharacterProfile character;

        [Tooltip("このときの表情")]
        public Expression expression = Expression.Normal;

        [Tooltip("名前欄の上書き（空ならキャラクターの名前。例：？？？）")]
        [FormerlySerializedAs("speaker")]
        public string speakerName;

        [Tooltip("本文")]
        [TextArea(2, 4)]
        public string text;

        public Line() { }

        public Line(string speakerName, string text)
        {
            this.speakerName = speakerName;
            this.text = text;
        }

        /// <summary>
        /// 名前欄に表示する名前
        /// </summary>
        public string DisplayName
        {
            get
            {
                if(!string.IsNullOrEmpty(speakerName)) { return speakerName; }
                if(character != null) { return character.DisplayName; }
                return "";
            }
        }
    }

    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField]
    private Line[] lines = new Line[0];

    //=====================================================================
    // プロパティ
    //=====================================================================

    public Line[] Lines => lines;
}
