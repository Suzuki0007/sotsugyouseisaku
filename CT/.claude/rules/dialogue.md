---
paths:
  - "Assets/Script/Talk/**"
  - "Assets/Data/Dialogue/**"
  - "Assets/Data/Characters/**"
  - "Assets/Prefab/UI/DialogueSystem.prefab"
---

# 会話（会話の仕組み・シナリオのセリフを触るとき）

セリフを足す・直すときは **`dialogue-csv` スキル**（CSV の書き方・取り込み・点検・ユーザーの好み）。

## 仕組み（`Assets/Script/Talk/`）
- `DialogueData`（文の配列 `lines`：`character`・`expression`・`speakerName`・`text`）、`CharacterProfile`（名前・色・左右・立ち絵の表情ごとの絵。`Data/Characters/Noah.asset`・`Sigle.asset`）、`Expression`（enum。**末尾にだけ追加**＝番号でセーブ・CSV に使う）。
- `DialogueManager`（シーンごとのシングルトン）。`Assets/Prefab/UI/DialogueSystem.prefab` を置けば使える（Stage1_1 / SampleScene / Stage_1_3）。Stage1_2 はシーンの Canvas に直接組んだもの（**両方直す**：枠 1600×260・立ち絵 820×1040）。
  - 送りは E・Space・Enter（`extraAdvanceKeys`）。枠の右下の下にキーの説明（`showKeyHint`・`keyHintText`）。話していない方の立ち絵は暗くなる。
- `Dialogue`：会話の中身を持つ部品（Intaractable。**作者は成田さん**）。DialogueManager がないシーンでは旧方式で表示。イベントからは `DialogueEventAction` が子の「会話」に置いた Dialogue を使う。
- `DialogueCsv`（エディタ）：CSV（会話ID, キャラ, 表情, 名前, 本文）⇄ DialogueData。UTF-8 / Shift-JIS、同名は上書き（GUID 維持）。`DialogueCsv.Import(path, showDialog:false)`。Parse などは internal にして `BarkCsv`（相棒の一言）が使い回す。

## シナリオ
- 元は `Assets/Data/Dialogue/Scenario_OP.csv`（OP）・`Scenario_F1.csv`（一層）・`Scenario_F2.csv`（二層）。台本 `セリフ.xlsx`（ユーザーのダウンロード）から変換し、演出は「#」のメモ行に残した。
- 会話ID は `F<層>_<番号>_<場面>`。**直すときは CSV を直して取り込み直す**（DialogueData を直接直さない）。
- ノア・シグル以外は CharacterProfile がなく「名前」の列だけ（ティンパニ・ヴェルツィー・カイロス・？？？）。正体を隠すシグルは キャラ=Sigle・名前=？？？。
- 表情は台本から推測して入れてある。立ち絵は今「通常」だけ（ノア `Images/UI/UI_Noah`・シグル `Images/UI/UI_siguru`）なので、ほかの表情も通常の絵になる（コンソールに「表情 X の立ち絵がありません」が出るのはそのため）。
- まだ置いていない会話：`F1_13_Stairs`（二層への階段。部屋がない）と二層（F2）。`dialogue-csv` の `-Lint` の UNUSED で確かめられる。
