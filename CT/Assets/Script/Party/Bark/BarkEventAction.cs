/*********************************************************************/
// * \file   BarkEventAction.cs
// * \brief  イベントの部品「一言」（吹き出しで相棒の一言を出す）
// *
// * \author
/*********************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベント（EventSequence）で一言を出す
/// </summary>
///
/// 例：扉が開いたあとに「開いたな」／ 謎を解いたあとの掛け合い ／ 敵が出てくる前の「来るぞ」
/// ・一言のデータ（BarkData）を選ぶか、ここに直接文を書く
/// ・会話（DialogueManager）と違って、ゲームを止めない
public class BarkEventAction : EventAction
{
    [SerializeField, Label("一言のデータ")]
    [Tooltip("Resources/Barks/ のデータ（候補からランダムに選ぶ）。空なら下の「文」を出す")]
    private BarkData data;

    [SerializeField, Label("データの条件・回数を見る")]
    [Tooltip("オン：データの条件・何回出すか・待ち時間を満たさなければ出さない\nオフ：必ず出す")]
    private bool checkRules = false;

    [SerializeField]
    [Tooltip("データを選ばないときに出す文（上から順に。2行以上で掛け合い）")]
    private List<BarkLine> lines = new List<BarkLine>();

    [SerializeField, Label("終わるまで待つ")]
    [Tooltip("オン：順番に実行のとき、一言が終わってから次の部品へ進む")]
    private bool waitUntilDone = false;

    [SerializeField, Label("優先度")]
    [Tooltip("文を直接書いたときの優先度（自動の一言が割り込めないように、大きめにしておく）")]
    private int priority = 100;

    /// <summary> データか文が入っているか（シーンチェック用） </summary>
    public bool HasContent
    {
        get
        {
            if(data != null) { return true; }
            foreach(BarkLine l in lines) { if(l != null && !string.IsNullOrEmpty(l.text)) { return true; } }
            return false;
        }
    }

    public override void Execute(GameObject gameObject)
    {
        GameObject subject = PlayerSwitchManager.Instance != null ? PlayerSwitchManager.Instance.CurrentPlayer : null;
        BarkContext c = BarkContext.Of(subject, gameObject != null ? gameObject.transform : null);
        if(data != null) { Bark.Play(data, c, checkRules); }
        else             { Bark.PlayLines(lines, c, priority); }
    }

    public override IEnumerator Run(GameObject gameObject)
    {
        Execute(gameObject);
        if(!waitUntilDone) { yield break; }

        yield return null;
        BarkView view = BarkView.Find();
        while(view != null && view.Busy) { yield return null; }
    }
}
