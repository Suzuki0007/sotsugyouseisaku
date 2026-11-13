/*********************************************************************/
// * \file   Room.cs
// * \brief  部屋のまとめ役（名前・ID・種類・部屋の絵。Inspector に部屋の点検の一覧が出る）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 部屋の種類
/// </summary>
public enum RoomKind
{
    [InspectorName("普通の部屋")]
    Normal = 0,

    [InspectorName("戦闘の部屋")]
    Battle = 1,

    [InspectorName("謎解きの部屋")]
    Puzzle = 2,
}

/// <summary>
/// 部屋のまとめ役
/// </summary>
///
/// ・Hierarchy 右クリック > 部屋 > 普通の部屋／戦闘の部屋／謎解きの部屋 で、中身と一緒に作られる
/// ・自分で組み立てた部屋（ティンパニ部屋など）のルートに付けてもよい。子の中身が点検の対象になる
/// ・動きは何もしない（目印とメモと点検のため）。Inspector に点検の一覧（歩ける範囲・出現位置・カメラ・調べる物・オートセーブ）が出る
[AddComponentMenu("マップ/部屋")]
public class Room : MonoBehaviour
{
    [SerializeField, Label("部屋の名前")]
    [Tooltip("わかりやすい名前（メモ用。ゲームには出ない）")]
    private string displayName = "";

    [SerializeField, Label("部屋の ID")]
    [Tooltip("出現位置の ID・戦闘の「1回だけ」の記録などの名前に使う。シーンの中で重ならない英数字（例：Stage2_Hall）")]
    private string roomId = "";

    [SerializeField, Label("種類")]
    private RoomKind kind = RoomKind.Normal;

    [SerializeField, Label("部屋の絵")]
    [Tooltip("この部屋の絵（SpriteRenderer）。点検（歩ける範囲が絵からはみ出していないか）と、カメラの範囲を絵に合わせるボタンに使う。空ならシーンの MapBounds の絵")]
    private SpriteRenderer image;

    [Header("メモ（この部屋でやること・台本の場面など）")]
    [SerializeField, TextArea(2, 5)]
    [Tooltip("メモ（この部屋でやること・台本の場面など）")]
    private string memo = "";

    public string DisplayName => displayName;
    public string RoomId => roomId;
    public RoomKind Kind => kind;
    public string Memo => memo;

    /// <summary> 部屋の絵（空ならシーンの MapBounds の絵） </summary>
    public SpriteRenderer Image
    {
        get
        {
            if(image != null) { return image; }
            MapBounds bounds = FindAnyObjectByType<MapBounds>();
            return bounds != null ? bounds.GetComponent<SpriteRenderer>() : null;
        }
    }

    /// <summary> エディタ用：作るときに中身を入れる </summary>
    public void Setup(string name, string id, RoomKind roomKind, SpriteRenderer roomImage)
    {
        displayName = name;
        roomId = id;
        kind = roomKind;
        image = roomImage;
    }
}
