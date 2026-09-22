using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteAnimation))]
public class kari_player_test : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6.0f;
    [SerializeField] int historyLimit = 300; // 移動履歴の最大数

    SpriteAnimation anim;
    Vector2 facing = Vector2.down; // 初期の向きは下
    readonly List<Vector3> posHistory = new();

    public IReadOnlyList<Vector3> PositionHistory
    {
        get{ return posHistory;}
    }

    static readonly int Idle = Animator.StringToHash("Player_Idle");

    void Awake()
    {
        anim = GetComponent<SpriteAnimation>();
    }

    void Start()
    {
        posHistory.Add(transform.position);
    }

    void Update()
    {
        Vector2 input = ReadInput();
        bool moving = input != Vector2.zero;

        if(moving)
        {
            input = input.normalized;
            transform.Translate(input * (moveSpeed * Time.deltaTime), Space.World);
            facing = ToFacing(input);
            Record();
        }

        if(moving)
        {
            anim.Play(WalkClip(facing));
        }
        else
        {
            anim.Play(Idle);
        }
    }

    Vector2 ReadInput()
    {
        var kb = Keyboard.current;
        if(kb == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;
        if(kb.wKey.isPressed)
        {
            input.y = 1;
        }
        if(kb.sKey.isPressed)
        {
            input.y = -1;
        }
        if(kb.aKey.isPressed)
        {
            input.x = -1;
        }
        if(kb.dKey.isPressed)
        {
            input.x = 1;
        }

        return input;  
    }

    static Vector2 ToFacing(Vector2 input)
    {
        if(Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return new Vector2(Mathf.Sign(input.x), 0);
        }
        else
        {
            return new Vector2(0, Mathf.Sign(input.y));
        }
    }

    static int WalkClip(Vector2 facing)
    {
        if(facing == Vector2.up)
        {
            return Animator.StringToHash("Player_Walk_Up");
        }
        else if(facing == Vector2.down)
        {
            return Animator.StringToHash("Player_Walk_Down");
        }
        else if(facing == Vector2.left)
        {
            return Animator.StringToHash("Player_Walk_Left");
        }
        else if(facing == Vector2.right)
        {
            return Animator.StringToHash("Player_Walk_Right");
        }
        else
        {
            return Idle;
        }
    }

    void Record()
    {
        posHistory.Add(transform.position);
        if(posHistory.Count > historyLimit)
        {
            posHistory.RemoveAt(0);
        }
    }
}
