---
paths:
  - "Assets/Script/Event/**"
  - "Assets/Script/Item/**"
  - "Assets/Script/Flag/**"
  - "Assets/Script/Common/**"
  - "Assets/Script/Key/**"
  - "Assets/Script/Treasure/**"
  - "Assets/Resources/Items/**"
  - "Assets/Data/Flags/**"
---

# イベント・アイテム・フラグ・会話・プランナー用ツール（これらを触るとき）

手順書：`Docs/イベントの順番と待ち時間.md`、`Docs/アイテム連動イベントの作り方.md`、`Docs/フラグの使い方.md`、`Docs/プランナー用ツールの使い方.md`。

## イベント（`Assets/Script/Event/`）
- `EventSequence`：`Play Mode` = **順番に実行（初期値）**／すべて同時に実行。`Lock Player` で実行中はプレイヤーの入力を止める（`EventSequence.IsBusy`）。Actions が空なら同じオブジェクトの EventAction を上から順に使う。実行中の再生要求は無視。
- `EventAction`：`Execute` / `Run`（コルーチン。待つ部品がオーバーライド）/ `OnExit` / `Restore`（セーブからの復元。見た目だけ）。
- 部品：Dialogue / Se / SetActive / SpriteChange / Anim（終わるまで待てる）/ Effect / Bgm系 / ItemGive / ItemTake / SetFlag / Wait、ほか各機能のイベントの部品（敵・仕掛け・2人・カメラ・一言）。
- 注意：順番に実行中に、自分（か親）を非表示にすると途中で止まる（シーンチェックで検出）。
- `ScreenFadeEventAction`（画面の暗転）：`ScreenCurtain`（シーンごとの Canvas、並び順 90 = 会話 100 より奥・HUD より手前）。`SceneFader`（全 UI より手前）とは別。
- 演出の部品（11-13）：`ShakeEventAction`（画面を揺らす。`Camera/CameraShake` が描画の間だけカメラをずらす＝FocusCamera とぶつからない・UI は揺れない）、`FaceDirectionEventAction`（キャラの向き。SpriteAnimator.SetDirection。止まっている間はそのまま）、`MoveObjectEventAction`（ずらした位置から／へ・目的の物へ〔動いていても追う。目的はノア／シグルも選べる＋ずらし＝その横に止まる〕。動かすもの＝物／ノア／シグル〔再生中に探す。待機・別行動のときに使う。一緒だとついてくる動きに戻される〕。Restore で最後の位置〔ノア・シグルは戻さない〕）、`DarknessEventAction`（StageDarkness の暗さと視界の半径を同時に。Restore で最後の値。SetDarkness・SetRadius は続けて呼ぶと前の変化を止めるので自前で補間）。
- **シーンごとのイベント**（Stage1_1 の OP・Stage1_2 の一層の序盤と出口・テストの注意）は `.claude/rules/scenes.md`。シーンにまとめて置くのは `scene-recipe` スキルのレシピで。
- **部品「オートセーブ」**（`Save/AutoSaveEventAction`、11-13）：オートセーブ枠に進み具合と再開位置（今の操作キャラの位置／指定した場所）を書く。**入口に戻れなくなるなど通り道が変わるイベントの最後に必ず置く**（置かないとロードで「シーンに着いた位置」から始まる）。置いた所：Stage1_2 の助けの場面の最後（`PartyModeEventAction` の次）・Stage1_1 `OP_はじめ` の最後・Stage_1_3 `全部調律できたとき（会話）` の最後。レシピ `autosave_points` で足した（`stage1_2_events` で作り直すと消えるので、そのあとに実行し直す）。
- `SceneStartEvent`：シーン開始時に GameCondition（Awake で1回だけ判定）を満たせば、1フレーム待って（プレイヤーの生成後）EventSequence を実行。「最初から暗転しておく」は Awake で幕を真っ暗に。
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

## 会話
- `.claude/rules/dialogue.md`（仕組み）と `dialogue-csv` スキル（セリフを足す・直す）。

## プランナー用ツール（`Assets/Script/Common/`）
- `[SceneName]` / `[SpawnPointId(nameof(sceneField))]`：string をドロップダウンに。`SceneLookup` が Build Settings と閉じたシーンのファイルから出現位置 ID を読む（プレハブ内のものは読めない）。
- `SceneChecker` / `SceneCheckWindow`（Tools > シーンチェック）：設定ミスの一覧（11-13：歩ける範囲・ティンパニの確認を追加）。**チェックを増やすときは `SceneChecker.CheckScene` / `CheckProject` に足す。**
- `DebugWindow`（Tools > デバッグ）：アイテム増減、フラグ、ワープ（`PlayerSwitchManager.Warp`）、シーン移動、回復、無敵（`Health.PlayerInvincible`）、再生開始時の状態（`DebugStartState`、EditorPrefs）、セーブ操作、敵、仕掛け、ティンパニ（TimpaniRoom があるときだけ）。
- `GameCondition`：フラグ・アイテム・操作キャラ・パーティーの体力・範囲・仕掛けの状態・2人の状態。`IsMet` / `AllMet` / `AnyMet`。
