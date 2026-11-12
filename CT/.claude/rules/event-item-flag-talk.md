---
paths:
  - "Assets/Script/Event/**"
  - "Assets/Script/Item/**"
  - "Assets/Script/Flag/**"
  - "Assets/Script/Talk/**"
  - "Assets/Script/Common/**"
  - "Assets/Script/Key/**"
  - "Assets/Script/Treasure/**"
  - "Assets/Resources/Items/**"
  - "Assets/Data/Dialogue/**"
  - "Assets/Data/Flags/**"
---

# イベント・アイテム・フラグ・会話・プランナー用ツール（これらを触るとき）

手順書：`Docs/イベントの順番と待ち時間.md`、`Docs/アイテム連動イベントの作り方.md`、`Docs/フラグの使い方.md`、`Docs/プランナー用ツールの使い方.md`。

## イベント（`Assets/Script/Event/`）
- `EventSequence`：`Play Mode` = **順番に実行（初期値）**／すべて同時に実行。`Lock Player` で実行中はプレイヤーの入力を止める（`EventSequence.IsBusy`）。Actions が空なら同じオブジェクトの EventAction を上から順に使う。実行中の再生要求は無視。
- `EventAction`：`Execute` / `Run`（コルーチン。待つ部品がオーバーライド）/ `OnExit` / `Restore`（セーブからの復元。見た目だけ）。
- 部品：Dialogue / Se / SetActive / SpriteChange / Anim（終わるまで待てる）/ Effect / Bgm系 / ItemGive / ItemTake / SetFlag / Wait、ほか各機能のイベントの部品（敵・仕掛け・2人・カメラ・一言）。
- 注意：順番に実行中に、自分（か親）を非表示にすると途中で止まる（シーンチェックで検出）。
- `EventZone` の「1回だけ」はセーブされない（提案⑦で未着手）。

## アイテム連動（`Assets/Script/Item/`）
- `ItemData`（**`Assets/Resources/Items/` に置く必要あり**）、`InventoryManager`（シングルトン、シーンをまたぐ）、`ItemPickup`。
- `ItemInteraction`：きっかけ（調べる / エリアに入る）× ルール（アイテム条件 + フラグ条件、消費、完了）→ EventSequence。Otherwise / After Completed。
- `ItemPossessionSwitch`：所持状況で表示切り替え。
- 古い `Treasure`・`KeyItem`・`NoahData.hasKey` は ItemInteraction に寄せる予定（提案⑦、未着手）。

## フラグ（`Assets/Script/Flag/`）
- `GameFlag`（ScriptableObject、ID・メモ・セーブするか）、`GameFlags`（static。セーブする→`Flag/<id>`、しない→メモリ。`CaptureTemporary` / `RestoreTemporary`）。
- `FlagCondition`、`SetFlagEventAction`、`FlagSwitch`。`BgmFlags` は「セーブしないフラグ」に統合（同じ ID なら同じフラグ）。
- エディタ：Hierarchy 右クリック > フラグ、Tools > フラグ > フラグ一覧（再生中に切り替え可）。

## 会話（`Assets/Script/Talk/`）
- `DialogueData`（文の配列）、`CharacterProfile`（名前・色・立ち絵・表情）、`Expression`（enum。**末尾にだけ追加**）。
- `DialogueManager`（シーンごとのシングルトン）。`Assets/Prefab/UI/DialogueSystem.prefab` を置けば使える。Stage1_1 / SampleScene に配置済み。Stage1_2 はシーンの Canvas に直接組んだもの。
- `Dialogue`：DialogueManager がないシーンでは旧方式。
- `DialogueCsv`（エディタ）：CSV（会話ID, キャラ, 表情, 名前, 本文）⇄ DialogueData。UTF-8 / Shift-JIS、同名は上書き（GUID 維持）。`DialogueCsv.Import(path, showDialog:false)`。Parse などは internal にして `BarkCsv` が使い回す。
- シナリオの会話：`Assets/Data/Dialogue/Scenario_F1.csv`（一層）・`Scenario_F2.csv`（二層）が元（台本 `セリフ.xlsx` から変換。演出は「#」のメモ行）。会話ID は `F<層>_<番号>_<場面>`。**直すときは CSV を直して取り込み直す**（DialogueData を直接直さない）。ノア・シグル以外は CharacterProfile がなく「名前」の列だけ（ティンパニ・ヴェルツィー・カイロス・？？？）。正体を隠すシグルは キャラ=Sigle・名前=？？？。

## プランナー用ツール（`Assets/Script/Common/`）
- `[SceneName]` / `[SpawnPointId(nameof(sceneField))]`：string をドロップダウンに。`SceneLookup` が Build Settings と閉じたシーンのファイルから出現位置 ID を読む（プレハブ内のものは読めない）。
- `SceneChecker` / `SceneCheckWindow`（Tools > シーンチェック）：設定ミスの一覧。**チェックを増やすときは `SceneChecker.CheckScene` / `CheckProject` に足す。**
- `DebugWindow`（Tools > デバッグ）：アイテム増減、フラグ、ワープ（`PlayerSwitchManager.Warp`）、シーン移動、回復、無敵（`Health.PlayerInvincible`）、再生開始時の状態（`DebugStartState`、EditorPrefs）、セーブ操作、敵、仕掛け。
- `GameCondition`：フラグ・アイテム・操作キャラ・パーティーの体力・範囲・仕掛けの状態・2人の状態。`IsMet` / `AllMet` / `AnyMet`。
