/*********************************************************************/
// * \file   UISubmitKeys.cs
// * \brief  UI の「決定」に Space キーを足すクラス
// *
// * \author
/*********************************************************************/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// UI のボタンを Space キーでも決定できるようにする（標準では Enter だけ）。
/// また、キー操作（↑↓←→）で選択が動いたときに「選択」の SE を鳴らす（UISound）
/// </summary>
///
/// ・タイトル・メニュー・セーブ／ロード画面・確認ダイアログ・ゲームオーバー画面など、すべての UI に効く
/// ・EventSystem（InputSystemUIInputModule）の「決定（Submit）」に、起動中だけ Space を足す
///   （パッケージの設定ファイルは書き換えない。シーンごと・自動で作られた EventSystem にも足す）
/// ・起動時に自動で動く（シーンに置く必要はない）
[AddComponentMenu("")]
public class UISubmitKeys : MonoBehaviour
{
    /// <summary>
    /// 決定に足すキー
    /// </summary>
    private static readonly string[] ExtraSubmitBindings = { "<Keyboard>/space" };

    // 最後に設定した入力モジュール（同じものに何度も足さないため）
    private InputSystemUIInputModule applied;

    // 選択を動かす入力（↑↓←→）があったフレームと、前のフレームで選ばれていたもの（選択の SE 用）
    private InputAction moveAction;
    private int lastMoveFrame = -10;
    private GameObject lastSelected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var go = new GameObject(nameof(UISubmitKeys));
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<UISubmitKeys>();
    }

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if(es == null) { return; }

        var module = es.currentInputModule as InputSystemUIInputModule;
        if(module == null || module == applied) { return; }

        Apply(module);
        applied = module;

        // 選択を動かす入力を見張る（選択の SE を、キー操作で動かしたときだけ鳴らすため）
        if(moveAction != null) { moveAction.performed -= OnMove; }
        moveAction = module.move != null ? module.move.action : null;
        if(moveAction != null) { moveAction.performed += OnMove; }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        lastMoveFrame = Time.frameCount;
    }

    /// <summary>
    /// キー操作で選択が変わったら、選択の SE を鳴らす
    /// （画面を開いたときなど、プログラムで選択を変えたときは鳴らさない）
    /// </summary>
    private void LateUpdate()
    {
        EventSystem es = EventSystem.current;
        GameObject selected = es != null ? es.currentSelectedGameObject : null;

        if(selected != lastSelected && selected != null && lastSelected != null &&
           Time.frameCount - lastMoveFrame <= 1)
        {
            UISound.Play(UISoundType.Select);
        }

        lastSelected = selected;
    }

    private void OnDestroy()
    {
        if(moveAction != null) { moveAction.performed -= OnMove; }
    }

    /// <summary>
    /// 入力モジュールの「決定」に Space を足す（すでにあれば何もしない）
    /// </summary>
    public static void Apply(InputSystemUIInputModule module)
    {
        if(module == null || module.submit == null) { return; }

        InputAction submit = module.submit.action;
        if(submit == null) { return; }

        foreach(string path in ExtraSubmitBindings)
        {
            bool exists = false;
            foreach(InputBinding b in submit.bindings)
            {
                if(b.path == path) { exists = true; break; }
            }
            if(exists) { continue; }

            // 有効なままでは足せないので、いったん止めてから足す
            InputActionMap map = submit.actionMap;
            bool mapEnabled = map != null && map.enabled;
            bool actionEnabled = submit.enabled;
            if(mapEnabled) { map.Disable(); } else if(actionEnabled) { submit.Disable(); }

            submit.AddBinding(path);

            if(mapEnabled) { map.Enable(); } else if(actionEnabled) { submit.Enable(); }
        }
    }
}
