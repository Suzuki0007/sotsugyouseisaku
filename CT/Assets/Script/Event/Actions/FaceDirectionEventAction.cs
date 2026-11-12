/*********************************************************************/
// * \file   FaceDirectionEventAction.cs
// * \brief  イベントの部品「キャラの向きを変える」
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// イベントの部品「キャラの向きを変える」（ノアが右を向く、など）
/// </summary>
///
/// ・止まっている間はその向きのまま。歩き出すと、歩く向きに戻る
/// ・プレイヤー以外（SpriteAnimator の付いた物）も「ほかの物」で向きを変えられる
public class FaceDirectionEventAction : EventAction
{
    public enum Who
    {
        [InspectorName("ノア")]
        Noah = 0,

        [InspectorName("シグル")]
        Sigle = 1,

        [InspectorName("操作中のキャラ")]
        Current = 2,

        [InspectorName("ほかの物（下で選ぶ）")]
        Other = 3,
    }

    public enum Direction
    {
        [InspectorName("上")]
        Up = 0,

        [InspectorName("下")]
        Down = 1,

        [InspectorName("左")]
        Left = 2,

        [InspectorName("右")]
        Right = 3,
    }

    [SerializeField, Label("だれ")]
    private Who who = Who.Noah;

    [SerializeField, Label("ほかの物")]
    [Tooltip("「ほかの物」のとき、向きを変える物（SpriteAnimator が付いている物）")]
    private GameObject other;

    [SerializeField, Label("向き")]
    private Direction direction = Direction.Right;

    public override void Execute(GameObject gameObject)
    {
        GameObject target = FindTarget();
        if(target == null) { return; }

        SpriteAnimator anim = target.GetComponentInChildren<SpriteAnimator>();
        if(anim != null) { anim.SetDirection(ToVector(direction)); }
    }

    private GameObject FindTarget()
    {
        if(who == Who.Other) { return other; }

        PlayerSwitchManager manager = PlayerSwitchManager.Instance;
        if(manager == null) { return null; }

        switch(who)
        {
            case Who.Noah:  return manager.Noah;
            case Who.Sigle: return manager.Sigle;
            default:        return manager.CurrentPlayer;
        }
    }

    private static Vector2 ToVector(Direction d)
    {
        switch(d)
        {
            case Direction.Up:   return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            default:             return Vector2.right;
        }
    }
}
