# CT（卒業制作） - Claude 向けメモ

2D トップダウンのアクション / 探索ゲーム（Unity 6.5 / 6000.5.4f1、URP 2D）。
主人公は **ノア（Noah）** と **シグル（Sigle）** の2人で、C キーで操作を切り替える。
目標は「**プランナーがコードを書かずに Inspector だけで制作を進められる**」こと。

最終更新：2026-11-07（①〜⑥の実装、既存の問題 1・2・3・10、攻撃・弾・出現位置まわりの不具合の修正まで）

---

## 作業のルール（ユーザーとの約束）

- **1つ作り終えるたびに、コミットメッセージ（日本語）を作って渡す。** 複数まとめて頼まれたら、最後にまとめて1つ。
- **コミットはユーザーが自分で行う。** こちらからは commit / push しない。
- プランナー向けの機能には、**Inspector の Tooltip / InspectorName を日本語で付け**、`Docs/` に手順書を書く（または追記する）。
- 実装したら、**Unity CLI で実際のエディタを操作して動作確認する**（下記）。確認できなかった項目ははっきり伝える。
- 既存のコードの問題を見つけたら、勝手に直さずに報告する（頼まれたら直す）。そして `Docs/code-map.md` の問題点に追記する。
- 作業が終わったら、CLAUDE.md と `Docs/code-map.md` を最新の状態に更新する（ファイルの追加・役割の変更・問題の解決／発見）。

## 環境

- git は PATH にない（コミット状況・差分は確認できない）。
- **Unity CLI**：`C:\Users\uhiro\AppData\Local\Unity\bin\unity.exe`（PowerShell では `$env:Path = "C:\Users\uhiro\AppData\Local\Unity\bin;" + $env:Path` を先に）。
  - `com.unity.pipeline`（0.10.0-exp.1）を Packages に追加済み。エディタが開いていれば `unity command ...` で操作できる。
  - よく使うもの：`unity status --until-ready`、`unity recompile`（コンパイルエラー・警告の確認）、`unity command script eval --file <cs>`（C# 実行）、`unity command console get`、`unity command editor.playmode start/stop`、`unity command capture editor_window --window <タイトル>`（エディタは日本語 UI。例：インスペクター）。
  - **再生中のテストは `editor.playmode set_run_in_background --enabled true` を先に**（エディタが裏にあるとフレームが進まない）。終わったら `--enabled false` で戻す。
  - 再生中、何もしないと敵にやられてゲームオーバーになる → `Health.PlayerInvincible = true` を先に。
  - テストで作るアセットは `Assets/__ClaudeTest/` に置き、最後に削除する。シーンは勝手に保存しない（必要なときは理由を伝える）。

## フォルダ・コードの書き方

- スクリプトは `Assets/Script/<機能>/`、エディタ拡張は各機能の `Editor/` フォルダ。asmdef はない（Assembly-CSharp / -Editor）。
- ファイル先頭のヘッダーコメント（`\file` `\brief` `\author`）、`//===` の区切り、`if(x) { ... }` の1行スタイル、日本語コメントに合わせる。
- エディタのドメインリロードは無効。static を持つクラスは `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` で再生ごとにリセットする。
- `FindObjectsByType` は `FindObjectsSortMode` 付きが非推奨（Unity 6.5）。引数なし版を使う。
- シーン：`Stage1_1` ⇄ `Stage1_2`（メインの確認用）、`Stage1`、`Stage2`、`SampleScene`、`SceneGameOver`、`kari_*`（仮）。

---

## 主な仕組み

### セーブ（`Assets/Script/Save/`）
- `Save`（static）：key/value。`SetPending` で仮置き → チェックポイント（`CheckPoint`）とシーン遷移（`SceneChange`）で `Commit`。ファイルは `persistentDataPath/save.json`。
- `AutoSave`：**起動時（BeforeSceneLoad）に `Save.Read()`**、シーン読み込み時に `WriteAsync`、終了時に `Write`。
  エディタではデバッグウィンドウの「再生するときにセーブを読み込む」（EditorPrefs）で読み込みをオフにできる。
- セーブには現在のシーン・位置は入っていない（起動すると最初のシーンから）。
- 主なキー：`Inventory/Items`、`Flag/<id>`、`Pickup/<scene>/<id か階層パス>`、`Interaction/<scene>/<id か階層パス>`、`<scene>/<宝箱id>`。

### イベント（`Assets/Script/Event/`）
- `EventSequence`：`Play Mode` = **順番に実行（初期値）**／すべて同時に実行。`Lock Player` で実行中はプレイヤーの入力を止める（`EventSequence.IsBusy`）。Actions が空なら同じオブジェクトの EventAction を上から順に使う。実行中の再生要求は無視。
- `EventAction`：`Execute` / `Run`（コルーチン。待つ部品がオーバーライド）/ `OnExit` / `Restore`（セーブからの復元。見た目だけ）。
- 部品：Dialogue / Se / SetActive / SpriteChange / Anim（終わるまで待てる）/ Effect / Bgm系 / ItemGive / ItemTake / SetFlag / **Wait**。
- 注意：順番に実行中に、自分（か親）を非表示にすると途中で止まる（シーンチェックで検出）。
- 入力停止の判定は `DialogueManager.IsBusy || SceneFader.IsBusy || ItemMenuUI.IsBusy || EventSequence.IsBusy` を各所（PlayerInput / AttackController / PlayerSwitchManager / Intaractable / ItemMenuUI）で見ている。

### アイテム連動（`Assets/Script/Item/`）
- `ItemData`（`Assets/Resources/Items/` に置く必要あり）、`InventoryManager`（シングルトン、シーンをまたぐ）、`ItemPickup`。
- `ItemInteraction`：きっかけ（調べる / エリアに入る）× ルール（**アイテム条件 + フラグ条件**、消費、完了）→ EventSequence。Otherwise / After Completed。
- `ItemPossessionSwitch`：所持状況で表示切り替え。
- 手順書：`Docs/アイテム連動イベントの作り方.md`

### フラグ（`Assets/Script/Flag/`）①
- `GameFlag`（ScriptableObject、ID・メモ・セーブするか）、`GameFlags`（static。セーブする→`Save` の `Flag/<id>`、しない→メモリ）。
- `FlagCondition`、`SetFlagEventAction`、`FlagSwitch`（フラグ＋任意のアイテム条件で表示切り替え）。
- `BgmFlags` は GameFlags の「セーブしないフラグ」に統合（同じ ID なら同じフラグ）。`BgmConditionFlag` / `BgmFlagEventAction` は GameFlag も指定できる。
- エディタ：Hierarchy 右クリック > フラグ、Tools > フラグ > フラグ一覧（再生中に切り替え可）。
- 手順書：`Docs/フラグの使い方.md`

### 会話（`Assets/Script/Talk/`）
- `DialogueData`（文の配列）、`CharacterProfile`（名前・色・立ち絵・表情）、`Expression`（enum。末尾にだけ追加）。
- `DialogueManager`（シーンごとのシングルトン）。**`Assets/Prefab/UI/DialogueSystem.prefab`**（専用 Canvas＋UI 一式）を置けば使える。Stage1_1 / SampleScene に配置済み。Stage1_2 はシーンの Canvas に直接組んだもの。
- `Dialogue`：DialogueManager がないシーンでは旧方式（1文・パネル直接）。
- `DialogueCsv`（エディタ）：CSV（会話ID, キャラ, 表情, 名前, 本文）⇄ DialogueData。UTF-8 / Shift-JIS 対応、同名は上書き（GUID 維持）。`DialogueCsv.Import(path, showDialog:false)` でスクリプトからも呼べる。

### プランナー用ツール（`Assets/Script/Common/`）③④⑤
- `[SceneName]` / `[SpawnPointId(nameof(sceneField))]` 属性：string をドロップダウンに（`SceneChange` に適用）。`SceneLookup` が Build Settings と、閉じたシーンのファイルから出現位置 ID を読む（プレハブ内のものは読めない）。
- `SceneChecker` / `SceneCheckWindow`（Tools > シーンチェック）：設定ミスの一覧。チェックを増やすときは `SceneChecker.CheckScene` / `CheckProject` に足す。
- `DebugWindow`（Tools > デバッグ）：アイテム増減、フラグ、ワープ（`PlayerSwitchManager.Warp`）、シーン移動、回復、無敵（`Health.PlayerInvincible`）、再生開始時の状態（`DebugStartState`、EditorPrefs）、セーブ操作。
- 手順書：`Docs/プランナー用ツールの使い方.md`、`Docs/イベントの順番と待ち時間.md`

---

## 提案の進み具合

| # | 内容 | 状態 |
|---|---|---|
| ① | 汎用フラグ | 完了 |
| ② | EventSequence の順番実行・待機 | 完了 |
| ③ | シーン名・出現位置 ID の選択式化 | 完了 |
| ④ | シーンチェック | 完了 |
| ⑤ | デバッグウィンドウ | 完了 |
| ⑥ | 会話 CSV の取り込み | 完了 |
| ⑦ | 古い仕組みの整理（`Treasure`・`NoahData.hasKey` を ItemInteraction に寄せる、`EventZone` の「1回だけ」をセーブする） | **未着手** |

## ファイル構成・全スクリプトの役割・問題点の一覧

詳しくは下の文書（自動で読み込まれる）。**コードやシーンを変えたら、この文書の表と問題点も更新する。**
問題を直したら「4. 問題点」から消し、新しく見つけたら追加する。

@Docs/code-map.md

### 問題点の要約（優先度の高いもの）
- **会話フォント（NotoSansJP-Regular SDF）に文字が足りない**（「ジ」「停」「止」などが空白になる）。
- `StageDarkness` の明かりが操作キャラに付いていかないことがある。
- `CheckPoint` がどのシーンにもない（セーブの確定はシーン遷移のときだけ）。
- `Stage1` / `Stage2` は古い作りのまま Build Settings に入っている。
- **`Behavior/Enemy/AbcdefuckAction.cs` に不適切な語**（未使用。提出前に削除推奨）。

### 解決済み（参考）
- `Save.Read()` 未呼び出し → 起動時に読み込み（AutoSave）。
- `EnemySpawner` のシーン遷移時の MissingReferenceException → ObjectSpawner で防止。
- Stage1_1 / SampleScene に DialogueManager がない → DialogueSystem.prefab を配置。
- デバッグの無敵がシーン遷移で切れる → `Health.PlayerInvincible`。
- Space キーの重複（範囲攻撃とゲージ判定）→ 判定待ちの間は通常攻撃を出さない（`TimingGaugeUI.IsJudging`）。
- タイミングゲージが最初の1回目に出ない／会話・イベント中も進む → 修正。
- `AreaAttack` の倍率の切り捨て → 四捨五入（最低1）。
- 弾がトリガー範囲や味方に当たって消える → 通り抜ける。
- `BulletPoolBase` が上限超えの弾の GameObject を消さない／`PlayerBulletPool` が弾にプールを渡しておらず弾が増え続ける → 修正。
- 出現位置の Sigle 用の位置が効かない → 効くように（そこから歩いて付いてくる）。
