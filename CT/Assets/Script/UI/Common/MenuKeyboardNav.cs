/*********************************************************************/
// * \file   MenuKeyboardNav.cs
// * \brief  ボタンの並びを、キーボード（↑↓・Enter・Space）で選べるようにする
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ボタンの並びをキーボードで選べるようにする（ゲームオーバー画面などに使う）
/// </summary>
///
/// ・開いたときに最初のボタンを選ぶ。↑↓（並びが横なら ←→）で選択が動き、端から反対の端へ回る
/// ・マウスでどこかをクリックして選択が外れても、キーを押せば最初のボタンに戻る
/// ・選ばれているボタンを色で目立たせる（「選ばれたときの色」）
/// ・決定（Enter・Space）と選択の音は UISubmitKeys が全画面に足している
[AddComponentMenu("UI/キーボードで選ぶ（ボタンの並び）")]
public class MenuKeyboardNav : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [Tooltip("上から（左から）順に並べたボタン。空なら子のボタンを並び順で使う")]
    [SerializeField]
    private List<Button> buttons = new List<Button>();

    [SerializeField, Label("並びが横")]
    [Tooltip("オンで ←→ で動く（オフなら ↑↓）")]
    private bool horizontal;

    [SerializeField, Label("端で反対に回る")]
    [Tooltip("いちばん下で ↓ を押すといちばん上に戻る")]
    private bool wrap = true;

    [SerializeField, Label("選ばれたときの色を変える")]
    [Tooltip("オンで、選ばれているボタンを「選ばれたときの色」にする")]
    private bool applySelectedColor = true;

    [SerializeField, Label("選ばれたときの色")]
    [Tooltip("選ばれているボタンの色（ボタンの絵にかける色）")]
    private Color selectedColor = new Color(1.0f, 0.85f, 0.4f, 1.0f);

    //=====================================================================
    // 初期化
    //=====================================================================

    private void OnEnable()
    {
        if(buttons.Count == 0) { buttons.AddRange(GetComponentsInChildren<Button>()); }
        ApplyNavigation();
        SelectFirst();
    }

    // シーンの始めは EventSystem がまだ用意されていないことがあるので、もう一度選ぶ
    private void Start()
    {
        SelectFirst();
    }

    private void Update()
    {
        // 選択が外れていたら、キーを押したときに最初のボタンへ戻す
        EventSystem es = EventSystem.current;
        if(es == null) { return; }

        GameObject selected = es.currentSelectedGameObject;
        bool lost = selected == null || !selected.activeInHierarchy;
        if(lost && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) { SelectFirst(); }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    /// <summary>
    /// ボタンどうしの移動先をはっきり決める（自動だと思わぬボタンに飛ぶことがある）
    /// </summary>
    private void ApplyNavigation()
    {
        List<Button> list = buttons.FindAll(b => b != null);
        for(int i = 0; i < list.Count; i++)
        {
            Button prev = i > 0 ? list[i - 1] : (wrap ? list[list.Count - 1] : null);
            Button next = i < list.Count - 1 ? list[i + 1] : (wrap ? list[0] : null);

            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            if(horizontal) { nav.selectOnLeft = prev; nav.selectOnRight = next; }
            else           { nav.selectOnUp = prev;   nav.selectOnDown = next; }
            list[i].navigation = nav;

            if(applySelectedColor)
            {
                ColorBlock colors = list[i].colors;
                colors.selectedColor = selectedColor;
                colors.highlightedColor = selectedColor;
                list[i].colors = colors;
            }
        }
    }

    /// <summary>
    /// 最初の押せるボタンを選ぶ
    /// </summary>
    public void SelectFirst()
    {
        EventSystem es = EventSystem.current;
        if(es == null) { return; }

        foreach(Button b in buttons)
        {
            if(b == null || !b.interactable || !b.gameObject.activeInHierarchy) { continue; }
            es.SetSelectedGameObject(b.gameObject);
            return;
        }
    }
}
