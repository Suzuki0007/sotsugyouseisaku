---
name: event-part
description: CT プロジェクトでイベントの部品（EventAction を継いだクラス。「画面を揺らす」「オートセーブ」「合図を出す」のような、EventSequence の「流れ」に並べる物）を1つ足すときの型。クラスの書き方（日本語の Label・Tooltip・InspectorName・summary）、待つ部品の Run、ロード時の Restore、EventActionCatalog の表、シーンチェック、確かめ方（今すぐ実行・event.ps1）、早見表・手順書・code-map の更新までを順番に行う。新しい演出・進行の部品を作る・今の部品に項目を足すときに使う。
---

# event-part：イベントの部品を足す

部品は `EventAction`（成田さんの土台。`Assets/Script/Event/EventProcess/EventAction.cs`）を継ぐ MonoBehaviour。**土台は変えない**（変えるなら先に方針を見せる。`rules/team-code.md`）。
仕組みの説明は `rules/event-item-flag-talk.md`、書き方は `rules/coding-style.md`。手本は `Event/Actions/FaceDirectionEventAction.cs`（すぐ終わる）、`Camera/ShakeEventAction.cs`（待つ）、`Save/AutoSaveEventAction.cs`（進行）。

## 0. 決める（作る前に一言伝える）
- 日本語名（Inspector の分類メニューに出る）と分類：`会話・演出`／`物・キャラ`／`進行`／`仕掛け・敵`。
- **すぐ終わる**か、**終わるまで次を待たせる**か（待つなら `Run`）。
- **ロードしたとき見た目を戻す必要があるか**（物の位置・表示・暗さなど → `Restore`。音・会話は戻さない）。
- 置き場所：その機能のフォルダ（カメラなら `Camera/`、セーブなら `Save/`）、どれでもなければ `Event/Actions/`。クラス名は `〜EventAction`。

## 1. クラスを書く
```csharp
/*********************************************************************/
// * \file   XxxEventAction.cs
// * \brief  イベントの部品「日本語名」
// *
// * \author
/*********************************************************************/

using System.Collections;
using UnityEngine;

/// <summary>
/// イベントの部品「日本語名」（プランナー向けの一言。早見表の見出しになる）
/// </summary>
///
/// ・何が起きるか（早見表の説明になる。プランナーの言葉で）
/// ・注意（止まっている間どうなるか・ロードしたときどうなるか）
public class XxxEventAction : EventAction
{
    [SerializeField, Label("項目名")]
    [Tooltip("何を入れるか・空ならどうなるか")]
    private float seconds = 1.0f;

    public override void Execute(GameObject gameObject) { /* すぐ終わる処理 */ }

    // 待つ部品だけ：終わるまで次の部品へ進まない
    public override IEnumerator Run(GameObject gameObject) { Execute(gameObject); yield return new WaitForSeconds(seconds); }

    // ロードで見た目だけ戻す部品だけ（音・会話・ダメージは出さない）
    public override void Restore(GameObject gameObject) { }
}
```
- enum の表示名は `[InspectorName("日本語")]`。セーブや番号で保存される enum は**末尾にだけ足す**。
- `[Label("名前", min, max)]` で最小・最大。Range と一緒に使わない。
- ノア・シグルは `PlayerSwitchManager.Instance.Noah`／`.Sigle`／`.CurrentPlayer` で**実行のときに**探す（Inspector に直接入れさせない。プレイヤーは生成される）。
- static を持ったら `SubsystemRegistration` でリセット。毎フレームの処理（コルーチンの中のループ）で Find・GetComponent・new をしない。
- 入力を止めるのは `EventSequence` の `Lock Player`（部品で `InputBlock` を直接いじらない）。

## 2. つなぐ
- [ ] `Event/EventProcess/Editor/EventActionCatalog.cs` の表に1行：`{ "XxxEventAction", ("分類", "日本語名") },`（ユーザーのファイル）。要約が「項目名 値」の3つで足りなければ `Summary` に専用の要約を足す（空欄の警告は「⚠ 〜が空欄（〜しない）」の形）。
- [ ] 設定ミスが起きやすい（空欄だと何も起きない・壁の中に動かす等）なら `SceneChecker.CheckScene` に確認を足す。
- [ ] 素材（音・絵・エフェクト）の欄があるなら、`asset-audit` の `audit.ps1 -NewSlots` で空き欄が出るか見る。
- [ ] シーンに置くなら `scene-recipe` のレシピで（手で置いた物は作り直しで消える）。

## 3. 確かめる
1. `unity-check`（再コンパイル・コンソール）。`perf-review` の `scan.ps1`（変更したファイル）。
2. 再生して `unity-playtest`：テスト用のシーン（`Assets/__ClaudeTest/`、最後に消す）か今のシーンで、EventSequence の Inspector「今すぐ実行」、または `event.ps1 -Run "親/子"` → `-State`・`-Capture` で結果を見る。
3. `Restore` を書いたなら：部品を実行 → オートセーブ → `event.ps1 -Reload` → 見た目が戻っているか。
4. 見た目・音は撮影と状態でしか確かめられない → 報告に「目と耳での確認はお願いします」と書く。

## 4. 文書
- [ ] 早見表（自動）：`Invoke-UnityEval -Code 'return QuickReferenceBuilder.Build();'`。
- [ ] `Docs/イベントの順番と待ち時間.md` の部品の表、`Docs/プランナー向け作業手順書.md`（付録の早見表）。
- [ ] `Docs/code-map.md` の「3. スクリプト一覧」、`rules/event-item-flag-talk.md` の部品の一覧。
- [ ] `finish-task` で締める。
