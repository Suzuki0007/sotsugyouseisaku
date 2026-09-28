/*********************************************************************/
// * \file   NoahData.cs
// * \brief  ノアのデータクラス
// *
// * \author 成田悠真
/*********************************************************************/

using UnityEngine;

/// <summary>
/// ノアのデータクラス
/// </summary>
/// 
/// [System.Serializable]とは、Unityのシリアライズ機能を使って、
/// クラスのインスタンスを保存・読み込みできるようにするための属性です。
/// これにより、Unityエディタ上でクラスのフィールドを表示したり、シーンやプレハブに保存したりすることができます。
///
/// たとえるなら、[System.Serializable]は「このクラスは保存可能です」という印のようなものです。
[System.Serializable]
public class NoahData
{
    public bool hasKey = false;
}
