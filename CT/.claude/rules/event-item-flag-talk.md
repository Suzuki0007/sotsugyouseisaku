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
- `ScreenFadeEventAction`（画面の暗転）：`ScreenCurtain`（シーンごとの Canvas、並び順 90 = 会話 100 より奥・HUD より手前）。`SceneFader`（全 UI より手前）とは別。
- 演出の部品（11-13）：`ShakeEventAction`（画面を揺らす。`Camera/CameraShake` が描画の間だけカメラをずらす＝FocusCamera とぶつからない・UI は揺れない）、`FaceDirectionEventAction`（キャラの向き。SpriteAnimator.SetDirection。止まっている間はそのまま）、`MoveObjectEventAction`（ずらした位置から／へ・目的の物へ〔動いていても追う。目的はノア／シグルも選べる＋ずらし＝その横に止まる〕。動かすもの＝物／ノア／シグル〔再生中に探す。待機・別行動のときに使う。一緒だとついてくる動きに戻される〕。Restore で最後の位置〔ノア・シグルは戻さない〕）、`DarknessEventAction`（StageDarkness の暗さと視界の半径を同時に。Restore で最後の値。SetDarkness・SetRadius は続けて呼ぶと前の変化を止めるので自前で補間）。
- **Stage1_2 の一層の序盤**（`Stage1_2_イベント`）：着いたとき（SceneStartEvent・`Flag_1_2_Intro_Done`）→ 真っ暗（Darkness 0.97・半径 1）で見えない壁の道をランタンへ → ランタン（ItemInteraction 調べる・saveId `Stage1_2_Lantern`：壁を消す・右の壁の明かり2つだけ点ける〔左の奥は暗いまま〕・`Item_Lantern` を渡す・Darkness 0.88・半径 3・揺れ・右を向く・敵とガレキ〔入口をふさぐ・当たり判定は落ちてから〕が降る・左を映す〔左端は映さない〕・戦闘エリアを始める・一言「逃げなきゃ！」）→ 追跡（BattleArea：ゴール＝左の行き止まり・捕まる・やり直す〔やり直す位置＝敵が出たところ〕・メッセージなし・目的だけ表示・1回だけ `Stage1_2_Chase`）→ 勝ったとき＝助けの場面（シグルは合流フラグ → 待機 → 画面の外へ移す／シグルの攻撃〔仮の弾〕が飛んで当たる・エフェクト・白いフラッシュ〔画面の暗転の白〕・敵を倒す／シグルが画面の外からノアの右 2.6 マスへ歩いてくる〔物を動かす：シグル → 目的ノア＋ずらし〕→ ノア右・シグル左を向く → F1_05〜F1_07 → 一緒）。最初の「右を向く」は 0.3 秒待ってから（走り込んだ勢いで向きが上書きされるため）。会話中は待機のまま（一緒にするとついてくる動きで近づく）。音は SeEventAction を空欄で置いてある（キーーーン・ドスン・バン）。セットアップは eval の C#（`setup_12`）で作った。
- Stage1_2 の出口：`Stage1_2_イベント/正面の扉（合流後に調べると Stage_1_3 へ）`（ItemInteraction 調べる・条件 `Flag_Sigle_Joined` ON・完了にしない → F1_08・F1_08b → 揺れ → `SceneChangeEventAction`〔Stage_1_3・`FromStage1_2`〕）。合流前は調べても何も起きない。`setup_12` で作り直すと消えるので、作り直したら `setup_12_door` も実行する。
- 注意：テストでノアを動かさずに追跡を始めると、捕まる → 読み直しを繰り返す（一言の吹き出しも読み直しで消える）。確かめるときは追跡が始まったらすぐワープする。
- `SceneStartEvent`：シーン開始時に GameCondition（Awake で1回だけ判定）を満たせば、1フレーム待って（プレイヤーの生成後）EventSequence を実行。「最初から暗転しておく」は Awake で幕を真っ暗に。
- **OP**（Stage1_1 の `OP`）：`OP_はじめ`（SceneStartEvent、条件 `Flag_OP_Done` OFF → 待つ → F1_00 → OP_01 → 明転 → OP_02 → フラグ ON）、`OP_途中`・`OP_塔の入口`（ItemInteraction のエリアに入る・1回だけ、saveId `OP_Bell` / `OP_TowerEntrance`）。会話用の Dialogue は子の「会話」に置く（調べる範囲なし。シーンチェックは DialogueEventAction から使われていれば対象外）。
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
- シナリオの会話：`Assets/Data/Dialogue/Scenario_OP.csv`（OP）・`Scenario_F1.csv`（一層）・`Scenario_F2.csv`（二層）が元。表情は台本から推測して入れてある（立ち絵は今「通常」だけなので、ほかの表情も通常の絵になる）（台本 `セリフ.xlsx` から変換。演出は「#」のメモ行）。会話ID は `F<層>_<番号>_<場面>`。**直すときは CSV を直して取り込み直す**（DialogueData を直接直さない）。ノア・シグル以外は CharacterProfile がなく「名前」の列だけ（ティンパニ・ヴェルツィー・カイロス・？？？）。正体を隠すシグルは キャラ=Sigle・名前=？？？。

## プランナー用ツール（`Assets/Script/Common/`）
- `[SceneName]` / `[SpawnPointId(nameof(sceneField))]`：string をドロップダウンに。`SceneLookup` が Build Settings と閉じたシーンのファイルから出現位置 ID を読む（プレハブ内のものは読めない）。
- `SceneChecker` / `SceneCheckWindow`（Tools > シーンチェック）：設定ミスの一覧（11-13：歩ける範囲・ティンパニの確認を追加）。**チェックを増やすときは `SceneChecker.CheckScene` / `CheckProject` に足す。**
- `DebugWindow`（Tools > デバッグ）：アイテム増減、フラグ、ワープ（`PlayerSwitchManager.Warp`）、シーン移動、回復、無敵（`Health.PlayerInvincible`）、再生開始時の状態（`DebugStartState`、EditorPrefs）、セーブ操作、敵、仕掛け、ティンパニ（TimpaniRoom があるときだけ）。
- `GameCondition`：フラグ・アイテム・操作キャラ・パーティーの体力・範囲・仕掛けの状態・2人の状態。`IsMet` / `AllMet` / `AnyMet`。
