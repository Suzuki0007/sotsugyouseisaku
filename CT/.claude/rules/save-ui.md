---
paths:
  - "Assets/Script/Save/**"
  - "Assets/Script/Title/**"
  - "Assets/Script/UI/**"
  - "Assets/Script/SavePoint/**"
  - "Assets/Script/CheckPoint/**"
  - "Assets/Script/Scene/**"
  - "Assets/Script/Common/InputBlock.cs"
  - "Assets/Script/Spawner/Player/**"
  - "Assets/Resources/UI/**"
---

# タイトル・セーブ・メニュー・UI（これらを触るとき）

手順書は `Docs/タイトル画面とセーブの仕組み.md`。ファイルごとの役割は `Docs/code-map.md` の「2. 起動とシーン開始時の流れ」と「3. > イベント・アイテム・フラグ・会話・セーブ」を読む。

## タイトル・セーブ（`Assets/Script/Title/`、`Assets/Script/Save/`）
- ゲームは **`Title.unity`** から始まる（Build Settings：Title → Stage1_1 → Stage1_2 → SceneGameOver）。`TitleMenu`：はじめから（スロット選択、使用中なら上書き確認）／つづきから／ゲーム終了。`TitleSceneBuilder`（Tools > タイトル画面）でシーンを作り直せる。
- **スロット3つ**：`save_1.json`〜`save_3.json`（`Application.persistentDataPath`）。`Save.CurrentSlot`（0 = 使わない → ファイルに書かない）、`LoadSlot` / `StartNewSlot` / `CloseSlot` / `PeekSlot` / `DeleteSlot`。旧 `save.json` は起動時にスロット1へ移す（`MigrateLegacy`）。
- `Save`：`SetPending` で仮置き → `Commit` で確定（`BeforeCommit` イベントでプレイ時間・保存日時・2人の状態を記録）→ `Write` / `WriteAsync`。`Save.SaveToSlot` で別スロットへ全部書き込み。
- **確定のタイミング**：シーンに着いたとき（`PlayerPartySpawner.Start` で再開場所を記録して Commit + WriteAsync）、シーンを出るとき（`SceneChange`）、`CheckPoint`、終了時。
- `GameSession`：`StartNewGame` / `Continue` / `ReturnToTitle` / `SaveAt`、プレイ時間（`PlayTimeCounter`）、データ切り替え時の状態リセット（`InventoryManager.ReloadFromSave`、一時フラグ）。
- `SaveSlotInfo`：`Meta/Scene`・`Meta/SpawnPoint`・`Meta/HasPos`・`Meta/PosX/Y`・`Meta/PlayTime`・`Meta/SavedAt`。再開は `PlayerSpawnPoint.PendingSpawnPointId` か `PendingPosition`。
- `AutoSave`：起動時は読み込まない（タイトルで選ぶ）。**エディタでステージから直接再生**したときだけ、デバッグウィンドウの「ステージから再生するとき」（EditorPrefs、初期値 0 = 使わない）のスロットを読む。
- `ReturnToTitleButton`：ボタンからタイトルへ（ゲームオーバー画面で使用）。

## セーブのキー（重ならないように）
`Inventory/Items`、`Flag/<id>`、`Pickup/<scene>/<id か階層パス>`、`Interaction/<scene>/<id か階層パス>`、`<scene>/<宝箱id>`、`Meta/*`、`Party/*`（Mode・Current・Scene・PartnerX/Y・WaitLocked・NoahHP・SigleHP）、`Gimmick/<シーン>/<ID>`（固定は `/Locked`）、`Reaction/…`、`Block/…`、`Rejoin/…`、`Examine/…`、`Note/<ID>`、`Bark/<名前>`、`Switch/…`、`Enemy/…`、`Group/…`、`Battle/…`。
ID が空のときは階層パスでセーブする物が多い（名前や親を変えると記録がリセットされる）。

## メニュー画面・セーブポイント
- **メニュー画面** `GameMenu`（`Assets/Script/UI/GameMenu/`、`Resources/UI/GameMenu.prefab` から自動生成・シーンをまたぐ）：Tab で開閉（ゲーム停止）、アイテム（`ItemMenuUI.OpenFromMenu`、閉じるとメニューへ）／メモ（`NoteBook.OpenList`）／ロード／タイトルに戻る。`OpenSave` でセーブの流れ（確認 → スロット選択 → `GameSession.SaveAt` → メッセージ）。プレハブは `GameMenuBuilder`（Tools > メニュー画面）。
- **セーブポイント** `SavePoint`（Intaractable、`Assets/Script/SavePoint/`）：調べると `GameMenu.OpenSave`。`Resume Offset` の位置から再開。`BattleArea.AnyActive` の間は不可。Hierarchy 右クリック > セーブポイント（`SavePointCreator`）。

## 共通 UI（`Assets/Script/UI/Common/`）
- `SaveSlotPanel`（NewGame / Continue / Load / Save）、`ConfirmDialog`（はい／いいえ、Esc = いいえ。左右移動は明示的に設定＝自動だと後ろの画面のボタンが選ばれる）、`PlaceNameTable`（`Resources/PlaceNames.asset`）、🛠`MenuUIBuilder`（ボタン・スロット画面・確認ダイアログを作る共通処理）。
- **入力停止の判定は `InputBlock.IsBlocked`** に一本化（会話・暗転・アイテム画面・イベント・メニュー・謎解きの画面）。条件を足すときはここだけ変える。
- **UI の効果音**：`UISound.Play(UISoundType.X)`（または `UISound.Play(個別SE, 種類)`）。音は `Resources/UISounds.asset`（`UISoundSettings`：Select / Decide / Cancel / MenuOpen / MenuClose / Save / Cannot）。仮の電子音は `Assets/SE/UI/`（🛠`UISoundGenerator`）。選択の音は `UISubmitKeys` がキー操作で選択が変わったときに鳴らす。確認ダイアログは自分で鳴らす。ほかのボタンは `UIButtonSound` を付ける。
- **UI の決定キー**：`UISubmitKeys`（起動時に自動生成）が、すべての EventSystem に実行中だけ Space を足す（パッケージの DefaultInputActions は書き換えない）。

## テストの注意
- Title から始めるテストは選んだスロットのファイルに書き込む。**スロット1〜3はすべてユーザーのデータ**。`unity-playtest` スキルでバックアップしてから行い、終わったら戻す。
