/*********************************************************************/
// * \file   GimmickVisual.cs
// * \brief  仕掛けの状態で見た目を変える（絵・色・表示する物・明かりなど）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状態ごとの見た目 1 つ
/// </summary>
[Serializable]
public class GimmickLook
{
    [Label("状態の値", 0)]
    [Tooltip("0 = OFF、1 = ON、ダイヤルなら 0, 1, 2 …")]
    public int value = 1;

    [Label("絵")]
    [Tooltip("この状態のときの絵（空なら変えない）")]
    public Sprite sprite;

    [Label("色を変える")]
    public bool useColor = false;

    [Label("色")]
    public Color color = Color.white;

    [Tooltip("この状態のときだけ表示する物（ほかの状態では隠す）")]
    public List<GameObject> show = new List<GameObject>();

    [Tooltip("この状態のときだけ有効にする部品（LightSource など。ほかの状態では無効）")]
    public List<Behaviour> enable = new List<Behaviour>();
}

/// <summary>
/// 仕掛けの状態で見た目を変える
/// </summary>
///
/// ・レバーの上下の絵、燭台の火（表示する物＋明かり LightSource）、ダイヤルの数字、スイッチの色 など
/// ・どの「状態ごとの見た目」に出てくる物・部品も、今の状態の行にないときは隠す・無効にする
/// ・セーブから戻ったときも、今の状態の見た目になる
public class GimmickVisual : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("仕掛けの状態")]
    [Tooltip("見る仕掛けの状態。空なら同じオブジェクトか親のもの")]
    private GimmickState state;

    [SerializeField, Label("絵を変える SpriteRenderer")]
    [Tooltip("空なら同じオブジェクトのもの")]
    private SpriteRenderer target;

    [SerializeField]
    [Tooltip("状態ごとの見た目。例：値 0 に消えた燭台の絵、値 1 に火のついた絵と「火」のオブジェクト・LightSource")]
    private List<GimmickLook> looks = new List<GimmickLook>();

    private Sprite defaultSprite;
    private Color defaultColor;

    public GimmickState State => state;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        if(state == null) { state = GetComponentInParent<GimmickState>(); }
        if(target == null) { target = GetComponent<SpriteRenderer>(); }
        if(target != null) { defaultSprite = target.sprite; defaultColor = target.color; }
        if(state != null) { state.Changed += OnChanged; }
    }

    private void OnDestroy()
    {
        if(state != null) { state.Changed -= OnChanged; }
    }

    private void Start()
    {
        Apply();
    }

    private void OnChanged(GimmickState s, int value) => Apply();

    /// <summary> 今の状態の見た目にする </summary>
    public void Apply()
    {
        if(state == null) { return; }

        GimmickLook current = looks.Find(l => l != null && l.value == state.Value);

        if(target != null)
        {
            target.sprite = current != null && current.sprite != null ? current.sprite : defaultSprite;
            target.color = current != null && current.useColor ? current.color : defaultColor;
        }

        // ほかの行の物はいったん隠して、今の行の物を出す
        foreach(GimmickLook look in looks)
        {
            if(look == null || look == current) { continue; }
            foreach(GameObject go in look.show) { if(go != null && (current == null || !current.show.Contains(go))) { go.SetActive(false); } }
            foreach(Behaviour b in look.enable) { if(b != null && (current == null || !current.enable.Contains(b))) { b.enabled = false; } }
        }
        if(current != null)
        {
            foreach(GameObject go in current.show) { if(go != null) { go.SetActive(true); } }
            foreach(Behaviour b in current.enable) { if(b != null) { b.enabled = true; } }
        }
    }
}
