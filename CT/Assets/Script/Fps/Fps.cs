/*********************************************************************/
// * \file   Fps.cs
// * \brief  Fpsクラス
// *
// * \author 鈴木裕稀
/*********************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Fps : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        const int FPS = 60;

        // fpsを設定できるための処理
        QualitySettings.vSyncCount = 0; // VSyncを無効にする

        Application.targetFrameRate = FPS; // フレームレートを設定する
    }
        void Update()
    {

    }
}