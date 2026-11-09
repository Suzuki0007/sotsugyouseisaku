/*********************************************************************/
// * \file   PartyRejoinArea.cs
// * \brief  合流の範囲（離れていた2人がそろって入ると、一緒に戻る）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 合流の範囲
/// </summary>
///
/// ・Is Trigger の Collider2D の中に、ノアとシグルの2人がそろうと、2人の状態を「一緒」に戻し、イベントを実行する
///   例：離れ離れのステージの出口で再会する（再会の会話）／ 同時押しの部屋を抜けたところ
/// ・「1回だけ」ならセーブする（Rejoin/<シーン>/<ID か階層パス>）
/// ・「待て／ついてこい」を使えなくしていたら、使えるように戻すこともできる
[RequireComponent(typeof(Collider2D))]
public class PartyRejoinArea : MonoBehaviour
{
    //=====================================================================
    // 変数
    //=====================================================================

    [SerializeField, Label("合流したときのイベント")]
    [Tooltip("例：再会の会話・SE")]
    private EventSequence onRejoined;

    [SerializeField, Label("待て／ついてこいを使えるようにする")]
    [Tooltip("オン：合流したら、イベントで使えなくしていた「待て／ついてこい」のキーを使えるように戻す")]
    private bool unlockWaitKey = true;

    [SerializeField, Label("1回だけ（セーブする）")]
    private bool once = true;

    [SerializeField, Label("セーブ用の ID")]
    [Tooltip("空なら階層のパスで記録する")]
    private string saveId = "";

    private Collider2D area;
    private string saveKey;
    private bool done;

    public bool Done => done;

    //=====================================================================
    // 関数
    //=====================================================================

    private void Awake()
    {
        area = GetComponent<Collider2D>();
        saveKey = "Rejoin/" + gameObject.scene.name + "/" + (string.IsNullOrEmpty(saveId) ? ScenePath(transform) : saveId);
    }

    private void Start()
    {
        if(once && Save.Get(saveKey, false))
        {
            done = true;
            if(onRejoined != null) { onRejoined.Restore(gameObject); }
        }
    }

    private void Update()
    {
        if(done && once) { return; }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null || manager.Noah == null || manager.Sigle == null) { return; }
        if(manager.IsTogether) { return; }

        if(!area.OverlapPoint(manager.Noah.transform.position) || !area.OverlapPoint(manager.Sigle.transform.position)) { return; }

        // 2人がそろった
        manager.SetMode(PartyMode.Together);
        if(unlockWaitKey) { PlayerSwitchManager.WaitCommandLocked = false; }
        if(once)
        {
            done = true;
            Save.SetPending(saveKey, true);
        }
        if(onRejoined != null) { onRejoined.Play(gameObject); }
    }

    private static string ScenePath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent) { path = parent.name + "/" + path; }
        return path;
    }

    private void OnDrawGizmos()
    {
        Collider2D c = area != null ? area : GetComponent<Collider2D>();
        if(c == null) { return; }
        Gizmos.color = new Color(1.0f, 0.75f, 0.3f, 0.5f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
