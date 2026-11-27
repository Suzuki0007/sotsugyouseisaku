/*********************************************************************/
// * \file   FootprintSettings.cs
// * \brief  足跡の設定（どのシーンで付けるか・絵・色・大きさ・消え方）
// *
// * \author
/*********************************************************************/

using UnityEngine;

/// <summary>
/// 足跡の設定（砂の上を歩くと、足が着くたびに足跡が残る）
/// </summary>
///
/// ・`Resources/FootprintSettings.asset` を1つ置くだけで効く（シーンに何も置かなくてよい）
/// ・「足跡が付くシーン」に入れたシーンでは、ノアとシグルが歩くと足跡が残り、しばらくしてから薄くなって消える
/// ・足が着く瞬間は、歩くアニメ（SpriteAnimData）のコマの合図「footprint」で決める。合図のないアニメ（シグルなど）は、決めた距離ごと
/// ・イベント・会話の間（プレイヤーが止められている間）は付けない（小舟で海の上を進む場面など）
[CreateAssetMenu(menuName = "CT/足跡の設定", fileName = "FootprintSettings")]
public class FootprintSettings : ScriptableObject
{
    /// <summary> 歩くアニメのコマに入れる合図の名前（この合図のコマで足跡を付ける） </summary>
    public const string Message = "footprint";

    [Tooltip("足跡が付くシーンの名前（砂浜・砂漠など、外の砂の所）")]
    public string[] scenes = { "Stage1_0", "Stage1_1" };

    [Label("足跡の絵")]
    [Tooltip("足跡の絵（上向き＝つま先が上）。空なら仮の丸い影")]
    public Sprite sprite;

    [Label("色")]
    [Tooltip("足跡の色。右の濃さ（A）がいちばん濃いときの濃さ")]
    public Color color = new Color(0.22f, 0.16f, 0.1f, 0.5f);

    [Label("大きさ", 0.05f)]
    [Tooltip("足跡の大きさ（マス）。仮の絵は 0.2 くらいで足の大きさ")]
    public float size = 1.0f;

    [Label("残る時間（秒）", 0.0f)]
    [Tooltip("付いてから、薄くなり始めるまでの秒数")]
    public float stayTime = 4.0f;

    [Label("消える時間（秒）", 0.0f)]
    [Tooltip("薄くなり始めてから、消えきるまでの秒数")]
    public float fadeTime = 2.0f;

    [Label("足元の位置")]
    [Tooltip("キャラの中心から、足元までのずれ（マス）")]
    public Vector2 footOffset = new Vector2(0.0f, -0.62f);

    [Label("左右の足の間（マス）", 0.0f)]
    [Tooltip("左右の足跡を、歩く向きと直角にずらす量")]
    public float stride = 0.1f;

    [Label("合図がないときの歩幅（マス）", 0.05f)]
    [Tooltip("歩くアニメに合図「footprint」がないキャラ（シグルなど）は、この距離を歩くごとに足跡を付ける")]
    public float stepDistance = 0.55f;

    [Label("並び順")]
    [Tooltip("足跡を描く並び順（床の絵より上、キャラより下）")]
    public int sortingOrder = 3;

    [Label("いちどに残る数", 10)]
    [Tooltip("これより多くなったら、古い足跡から消す")]
    public int maxPrints = 160;

    /// <summary> このシーンで足跡を付けるか </summary>
    public bool AppliesTo(string sceneName)
    {
        if(scenes == null) { return false; }
        foreach(string s in scenes) { if(s == sceneName) { return true; } }
        return false;
    }
}
