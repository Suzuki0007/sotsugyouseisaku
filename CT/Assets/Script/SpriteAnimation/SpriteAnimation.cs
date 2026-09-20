/*********************************************************************/
// * \file   SpriteAnimation.cs
// * \brief  スプライトアニメーションクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using UnityEngine;

[RequireComponent(typeof(Animator))]// Animatorコンポーネントを必須にする
public class SpriteAnimation : MonoBehaviour
{
    Animator anim;
    int currentHash;// 現在のアニメーションのハッシュ値を保持する変数

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Play(int stateHash, bool restart = false)
    {
        if(stateHash == currentHash && !restart)
        {
            return;
        }
        currentHash = stateHash;
        anim.Play(stateHash, 0, 0.0f); // アニメーションを再生する
    }

}
