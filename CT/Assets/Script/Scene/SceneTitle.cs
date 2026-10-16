using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SceneTitle : SceneBase
{
    [SerializeField, Header("スタートボタン")]
    private Button _startButton;
    protected override void Start()
    {
        base.Start();
        // スタートボタンを選択状態にする
        EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
    }
    
}
