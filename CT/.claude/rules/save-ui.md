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
- ゲームは **`Title.unity`** から始まる（Build Settings：Title → Stage1_1 → Stage1_2 → Stage_1_3 → SceneGameOver）。`TitleMenu`：はじめから（スロット選択、使用中なら上書き確認）／つづきから／ゲーム終了。`TitleSceneBuilder`（Tools > タイトル画面）でシーンを作り直せる。
- **セーブの枠（11-13 に変更）**：**オートセーブ枠** `save_auto.json`（`Save.AutoSlot` = -1、画面では一番上の「オートセーブ」）＋ **手動の枠** `save_1.json`〜`save_3.json`（`Application.persistentDataPath`）。
  - **自動で書く（`Save.Write` / `WriteAsync`）のはいつもオートセーブ枠**。手動の枠を書くのは `Save.SaveToSlot`（セーブポイントの手動セーブ）だけ。ユーザーの指示：「手動の枠が上書きされるのは手動セーブだけ、オートセーブが上書きするのはオートセーブ枠だけ」。
  - `Save.HasSlot`（セーブを使っているか。直接再生でスロットを選んでいなければ false → ファイルに書かない）、`Save.CurrentSlot`（最後に読んだ・手動でセーブした枠。表示用）、`LoadSlot` / `StartNewGame`（オートセーブ枠を空にして始める）/ `CloseSlot` / `PeekSlot` / `DeleteSlot` / `SlotPath`。旧 `save.json` は起動時にスロット1へ移す（`MigrateLegacy`）。
- `Save`：`SetPending` で仮置き → `Commit` で確定（`BeforeCommit` イベントでプレイ時間・保存日時・2人の状態を記録）→ `Write` / `WriteAsync`（オートセーブ枠）。
- **オートセーブのタイミング**（`GameSession.AutoSaveOnArrival` / `AutoSaveAt`、`AutoSaved` イベントで右上に「オートセーブしました」＝`AutoSaveIndicator`）：シーンに着いたとき（`PlayerPartySpawner.Start`）、`CheckPoint`、イベントの部品「オートセーブ」（`AutoSaveEventAction`：今の位置か指定した場所を再開位置に）。
  - **終了時・タイトルへ戻るとき・シーンを読み込んだときは書かない**（以前は書いていて、「進み具合は新しいのに再開位置はシーンに着いた所」になり、Stage1_2 でガレキの向こう＝入口側から始まって進めなくなった）。
  - **入口に戻れなくなる・通り道が変わるイベントの最後には必ず部品「オートセーブ」を置く**。置いてある所：Stage1_2 の助けの場面の最後（シグル合流）・Stage1_1 の `OP_はじめ` の最後・Stage_1_3 の `全部調律できたとき（会話）` の最後。
  - シーンを出るとき（`SceneChange`）は Commit だけ（行き先に着いたときに書く）。
- **ロードの確かめ（ときどき行う。ユーザーの指示）**：イベントを足したら、イベントの前・途中・後で `GameSession.ReturnToTitle()` → `GameSession.Continue(Save.AutoSlot, "Stage1_1")` を eval で呼び、出た位置から進めるか・イベントが繰り返さないかを見る（`unity-playtest` でバックアップしてから。直接再生のときは先に `Save.StartNewGame()` でセーブを有効にする）。
- `GameSession`：`StartNewGame` / `Continue` / `ReturnToTitle` / `SaveAt`、プレイ時間（`PlayTimeCounter`）、データ切り替え時の状態リセット（`InventoryManager.ReloadFromSave`、一時フラグ）。
- `SaveSlotInfo`：`Meta/Scene`・`Meta/SpawnPoint`・`Meta/HasPos`・`Meta/PosX/Y`・`Meta/PlayTime`・`Meta/SavedAt`。再開は `PlayerSpawnPoint.PendingSpawnPointId` か `PendingPosition`。
- `AutoSave`：起動時は読み込まない（タイトルで選ぶ）。**エディタでステージから直接再生**したときだけ、デバッグウィンドウの「ステージから再生するとき」（EditorPrefs、初期値 0 = 使わない）のスロットを読む。
- `ReturnToTitleButton`：ボタンからタイトルへ（ゲームオーバー画面で使用）。
- ゲームオーバー画面（`SceneGameOver`）：Canvas に `MenuKeyboardNav`（リトライ → タイトルへ、↑↓で回る、最初にリトライを選ぶ、選ばれた色）。ほかのボタンの並びをキーボードで選べるようにするときも、この部品を付ける。

## セーブのキー（重ならないように）
`Inventory/Items`、`Flag/<id>`、`Pickup/<scene>/<id か階層パス>`、`Interaction/<scene>/<id か階層パス>`、`<scene>/<宝箱id>`、`Meta/*`、`Party/*`（Mode・Current・Scene・PartnerX/Y・WaitLocked・NoahHP・SigleHP）、`Gimmick/<シーン>/<ID>`（固定は `/Locked`）、`Reaction/…`、`Block/…`、`Rejoin/…`、`Examine/…`、`Note/<ID>`、`Bark/<名前>`、`Switch/…`、`Enemy/…`、`Group/…`、`Battle/…`。
ID が空のときは階層パスでセーブする物が多い（名前や親を変えると記録がリセットされる）。

## メニュー画面・セーブポイント
- **メニュー画面** `GameMenu`（`Assets/Script/UI/GameMenu/`、`Resources/UI/GameMenu.prefab` から自動生成・シーンをまたぐ）：Tab で開閉（ゲーム停止）、アイテム（`ItemMenuUI.OpenFromMenu`、閉じるとメニューへ）／メモ（`NoteBook.OpenList`）／ロード／タイトルに戻る。`OpenSave` でセーブの流れ（確認 → スロット選択 → `GameSession.SaveAt` → メッセージ）。プレハブは `GameMenuBuilder`（Tools > メニュー画面）。
- **セーブポイント** `SavePoint`（Intaractable、`Assets/Script/SavePoint/`）：調べると `GameMenu.OpenSave`。`Resume Offset` の位置から再開。`BattleArea.AnyActive` の間は不可。Hierarchy 右クリック > セーブポイント（`SavePointCreator`）。

## 画面の決まり（11-13）
- ウィンドウは 16:9（ビルドは 1920×1080 フルスクリーン）。**マップは中央の 4:3**（`Camera/MapCameraAspect`、FocusCamera のカメラに自動で付く）、UI は画面全体を使ってよい（4:3 に収めなくてよい）。
- Screen Space - Overlay の Canvas はすべて **Scale With Screen Size・1920×1080・Match 0.5** にそろえる。HP（`LifeUI`）は左上固定（anchor 0,1）。
- 会話の枠（DialoguePanel）は幅 1600×高さ 260、画面の下の中央（下から 40）。立ち絵の枠は 820×1040、画面の左下・右下の端から（`DialogueSystem.prefab` と Stage1_2 のシーンの会話画面の両方）。
- 文字は **NotoSansJP**（TMP は `NotoSansJP-Regular SDF`＝TMP の既定、古い Text は `NotoSansJP-Regular.ttf`）。新しい UI もこれを使う。
- 並び順：帯 -1000 ＜ 通常の UI ＜ 画面の暗転の幕 90 ＜ 会話 100 ＜ シーン遷移の暗転（最大）。

## 共通 UI（`Assets/Script/UI/Common/`）
- `SaveSlotPanel`（NewGame / Continue / Load / Save）、`ConfirmDialog`（はい／いいえ、Esc = いいえ。左右移動は明示的に設定＝自動だと後ろの画面のボタンが選ばれる）、`PlaceNameTable`（`Resources/PlaceNames.asset`）、🛠`MenuUIBuilder`（ボタン・スロット画面・確認ダイアログを作る共通処理）。
- **入力停止の判定は `InputBlock.IsBlocked`** に一本化（会話・暗転・アイテム画面・イベント・メニュー・謎解きの画面）。条件を足すときはここだけ変える。
- **UI の効果音**：`UISound.Play(UISoundType.X)`（または `UISound.Play(個別SE, 種類)`）。音は `Resources/UISounds.asset`（`UISoundSettings`：Select / Decide / Cancel / MenuOpen / MenuClose / Save / Cannot）。仮の電子音は `Assets/SE/UI/`（🛠`UISoundGenerator`）。選択の音は `UISubmitKeys` がキー操作で選択が変わったときに鳴らす。確認ダイアログは自分で鳴らす。ほかのボタンは `UIButtonSound` を付ける。
- **キーの案内**（`Intaract/InteractPrompt.cs`）：調べられる・拾える物の上に「[E] 調べる」など（自動）。新しい調べる部品は `IInteractPrompt` を実装する（いつ出すか・文）か、`InteractPromptView.labels` に種類名と文を足す。会話の送りは E・Space・Enter で、枠の右下の下にキーの説明（`DialogueManager.showKeyHint`）。
- **UI の決定キー**：`UISubmitKeys`（起動時に自動生成）が、すべての EventSystem に実行中だけ Space を足す（パッケージの DefaultInputActions は書き換えない）。

## テストの注意
- Title から始めるテストはオートセーブ枠（`save_auto.json`）に書き込む。**オートセーブ枠・スロット1〜3はすべてユーザーのデータ**（`save*.json` をバックアップ・復元する）。`unity-playtest` スキルでバックアップしてから行い、終わったら戻す。
