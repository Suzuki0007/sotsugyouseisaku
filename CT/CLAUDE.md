# CT（卒業制作） - Claude 向けメモ

2D トップダウンのアクション / 探索ゲーム（Unity 6.5 / 6000.5.4f1、URP 2D）。
主人公は **ノア（Noah）** と **シグル（Sigle）** の2人で、C キーで操作を切り替える。
目標は「**プランナーがコードを書かずに Inspector だけで制作を進められる**」こと。

最終更新：2026-11-08（①〜⑥の実装、既存の問題の修正、タイトル画面とセーブスロット、メニュー画面とセーブポイント、敵の仕組みの段階0〔シグルの射撃・体力の共有〕・段階1〔土台〕・段階2〔行動パターンと攻撃の基本〕・段階3〔ギミック連動・見えない敵・落下／突き出る／設置／弾幕／仲間を呼ぶ〕・段階4〔追跡と逃げる戦闘〕・段階5〔部位・ひるみ・見た目の変化〕・段階6〔倒したとき・倒した後・グループ・撃破の順番・ウェーブ〕・段階7〔スポナー・ツール・サンプル・資料〕まで＝敵の仕組みは完了、謎解き・2人の仕掛けの段階0〔2人の状態・体力・攻撃は操作中のキャラだけ〕まで）

**進行中：謎解き・2人の仕掛け**（プランナーが謎解き・ステージギミック・2人の協力・離れ離れの展開・バディ感を Inspector だけで作れるように）。段階 0 前提の修正（攻撃は操作中のキャラだけ・2人の状態〔一緒／待機／別行動〕・体力〔一緒＝シグルの体力を共有、離れる＝別々。シグル 6・ノア 3〕）→ 1 土台（仕掛けの状態・条件・使えるキャラ・答え合わせ・配線の表示）→ 2 基本の部品 → 3 2人の能力（待て／ついてこい・狭い通路・重い物・壊せる壁・見える物・同時押し）→ 4 離れ離れ（エリアごとのカメラ・敵の狙い・合流・セーブ）→ 5 ホラー・謎解きの演出 → 6 バディ感（相棒の一言・ヒント・絆の段階）→ 7 ツール・サンプル・資料。**今は段階0まで完了**。手順書は `Docs/謎解きと2人の仕掛けの作り方.md`。

**完了：敵の仕組み**（プランナーが敵・攻撃・行動パターン・ギミック連動・追跡などを Inspector だけで作れるように）。段階 0 前提の修正 → 1 土台（DamageInfo・攻撃を受ける部品・GameCondition・EnemyData・2D ナビメッシュ〔標準の NavMesh API + 2D コライダーから自作の収集。NavMeshAgent は使わず経路だけ計算し Rigidbody2D で動かす〕）→ 2 行動パターンと攻撃の基本 → 3 ギミック連動・攻撃の発生源 → 4 追跡 → 5 部位・ひるみ・見た目 → 6 倒した後・グループ → 7 ツールと資料。**1段階ずつ終わったら報告とコミットメッセージを渡す**。**段階7まで完了**。提案にあって入れていないもの：Behavior Graph のノードから行動の部品・攻撃を呼ぶこと、グループの「○秒以内に全員倒した」、スポナー・独立した部位をグループのメンバーにすること（引き継ぎ資料の 9 章）。

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
  - キー入力は `InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Tab))` → 少し待って空の KeyboardState で離す、で再現できる。UI ボタンは `button.onClick.Invoke()`。
    ただし Input System の設定が `PointersAndKeyboardsRespectGameViewFocus` なので、ゲームビューにフォーカスがないとアクション経由の入力（UI の決定・移動など）は届かず、結果が安定しない。UI のキー操作の確認は、結果を断定せずユーザーに手元での確認を頼む。
  - **ユーザーも同じエディタでテストプレイしていることがある。** Title から始めるテストはセーブファイルを書き換えるので、必ず `persistentDataPath/save_*.json` をバックアップしてから行い、終わったら戻す（スロット1〜3はすべてユーザーのデータとして扱う）。
  - コードを変えた直後はエディタがリロード中で `EDITOR_NOT_RUNNING` になる → `unity status --until-ready` で待ってから次へ。
  - PowerShell のスクリプトファイル（.ps1）に日本語を書かない（BOM なしだと文字化けして構文エラーになる）。関数名 `H` は Get-History の別名と衝突する。

## フォルダ・コードの書き方

- スクリプトは `Assets/Script/<機能>/`、エディタ拡張は各機能の `Editor/` フォルダ。asmdef はない（Assembly-CSharp / -Editor）。
- ファイル先頭のヘッダーコメント（`\file` `\brief` `\author`）、`//===` の区切り、`if(x) { ... }` の1行スタイル、日本語コメントに合わせる。
- エディタのドメインリロードは無効。static を持つクラスは `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` で再生ごとにリセットする。
- `FindObjectsByType` は `FindObjectsSortMode` 付きが非推奨（Unity 6.5）。引数なし版を使う。
- シーン：**ゲームで使うのは `Title` → `Stage1_1` ⇄ `Stage1_2`、`SceneGameOver`**（Build Settings もこの4つ）。`Stage1`、`Stage2`、`SampleScene`、`kari_*` はファイルだけ残っている（Build Settings から外した）。
- 再生中のテストで Title から始めると、選んだスロットのファイルに書き込む。スロット1〜3はすべてユーザーのデータとして扱い、テスト前に save_*.json をバックアップして終わったら戻す。

---

## 主な仕組み

### タイトル・セーブ（`Assets/Script/Title/`、`Assets/Script/Save/`）
- ゲームは **`Title.unity`** から始まる（Build Settings：Title → Stage1_1 → Stage1_2 → SceneGameOver）。`TitleMenu`：はじめから（スロット選択、使用中なら上書き確認）／つづきから／ゲーム終了。`TitleSceneBuilder`（Tools > タイトル画面）でシーンを作り直せる。
- **スロット3つ**：`save_1.json`〜`save_3.json`。`Save.CurrentSlot`（0 = 使わない → ファイルに書かない）、`LoadSlot` / `StartNewSlot` / `CloseSlot` / `PeekSlot` / `DeleteSlot`。旧 `save.json` は起動時にスロット1へ移す（`MigrateLegacy`）。
- `Save`：`SetPending` で仮置き → `Commit` で確定（`BeforeCommit` イベントでプレイ時間・保存日時を記録）→ `Write` / `WriteAsync`。
- **確定のタイミング**：シーンに着いたとき（`PlayerPartySpawner.Start` で再開場所を記録して Commit + WriteAsync）、シーンを出るとき（`SceneChange`）、`CheckPoint`（位置を記録）、終了時。
- `GameSession`：`StartNewGame` / `Continue` / `ReturnToTitle`、プレイ時間（`PlayTimeCounter`）、データ切り替え時の状態リセット（`InventoryManager.ReloadFromSave`、一時フラグ）。
- `SaveSlotInfo`：`Meta/Scene`・`Meta/SpawnPoint`・`Meta/HasPos`・`Meta/PosX/Y`・`Meta/PlayTime`・`Meta/SavedAt`。再開は `PlayerSpawnPoint.PendingSpawnPointId` か `PendingPosition`。
- `AutoSave`：起動時は読み込まない（タイトルで選ぶ）。**エディタでステージから直接再生**したときだけ、デバッグウィンドウの「ステージから再生するとき」（EditorPrefs、初期値 0 = 使わない）のスロットを読む。
- `ReturnToTitleButton`：ボタンからタイトルへ（ゲームオーバー画面で使用）。
- **メニュー画面** `GameMenu`（`Assets/Script/UI/GameMenu/`、`Resources/UI/GameMenu.prefab` から自動生成・シーンをまたぐ）：Tab で開閉（ゲーム停止）、アイテム（`ItemMenuUI.OpenFromMenu`、閉じるとメニューへ）／ロード／タイトルに戻る。`OpenSave` でセーブの流れ（確認 → スロット選択 → `GameSession.SaveAt` → メッセージ）。プレハブは `GameMenuBuilder`（Tools > メニュー画面）。
- **セーブポイント** `SavePoint`（Intaractable、`Assets/Script/SavePoint/`）：調べると `GameMenu.OpenSave`。`Resume Offset` の位置から再開。Hierarchy 右クリック > セーブポイント（`SavePointCreator`）。Stage1_1 / Stage1_2 に1つずつ配置。
- **共通 UI**（`Assets/Script/UI/Common/`）：`SaveSlotPanel`（NewGame / Continue / Load / Save）、`ConfirmDialog`（はい／いいえ、Esc = いいえ）、`PlaceNameTable`（`Resources/PlaceNames.asset`）、🛠`MenuUIBuilder`（ボタン・スロット画面・確認ダイアログを作る共通処理）。`Save.SaveToSlot` で別スロットへ全部書き込み。
- **入力停止の判定は `InputBlock.IsBlocked`** に一本化（会話・暗転・アイテム画面・イベント・メニュー）。条件を足すときはここだけ変える。
- **UI の効果音**：`UISound.Play(UISoundType.X)`（または `UISound.Play(個別SE, 種類)`）。音は `Resources/UISounds.asset`（`UISoundSettings`：Select / Decide / Cancel / MenuOpen / MenuClose / Save / Cannot）。仮の電子音は `Assets/SE/UI/`（🛠`UISoundGenerator`、Tools > UI効果音）。選択の音は `UISubmitKeys` がキー操作で選択が変わったときに鳴らす。確認ダイアログは はい=Decide／いいえ=Cancel を自分で鳴らす。ほかのボタンは `UIButtonSound` を付ける。
- **UI の決定キー**：`UISubmitKeys`（起動時に自動生成）が、すべての EventSystem（InputSystemUIInputModule の Submit）に実行中だけ Space を足す（標準は Enter のみ。パッケージの DefaultInputActions は書き換えない）。`ConfirmDialog` は はい／いいえ の左右移動を明示的に設定（自動だと後ろの画面のボタンが選ばれる）。
- 主なキー：`Inventory/Items`、`Flag/<id>`、`Pickup/<scene>/<id か階層パス>`、`Interaction/<scene>/<id か階層パス>`、`<scene>/<宝箱id>`、`Meta/*`。
- 手順書：`Docs/タイトル画面とセーブの仕組み.md`

### 敵（`Assets/Script/Enemy/`、`Assets/Script/Damage/`）※段階ごとに作っている途中
- **新しい作り**：`EnemyData`（ScriptableObject。`Assets/Data/Enemies/`）＋ 共通プレハブ `Resources/Enemies/Enemy_Base`（`Enemy` コンポーネント）。シーンに置くだけで動く（狙う相手は `PlayerSwitchManager.CurrentPlayer`）。Rigidbody2D Dynamic・回転なし、`MoveTo` で経路に沿って移動、マップの範囲内、体当たり（`DamageKind.Contact`）。行動パターンは段階2から。古い `Enemy.prefab`（Behavior Graph）はそのまま動く。
- **攻撃の情報**：`DamageInfo`（量・`DamageKind`・攻撃者・`PlayerCharacter`・位置・`GaugeJudge`）。`Health.Damage(int)` は残し `Damage(DamageInfo)` を追加、`OnDamagedInfo` / `LastDamage`。**攻撃を当てる処理は `DamageUtility.Apply(collider, info)` を通す**（`DamageReceiver` があればそちら＝部位・ギミック用、なければ Health）。範囲攻撃は `HitKey` で同じ Health に1回だけ。
- **敵を止める判定は `EnemyBlock.IsBlocked`**（`InputBlock` + デバッグ）。1体だけは `Enemy.Paused`。
- **2D ナビメッシュ** `NavMesh2D`（自動生成・シーンごと）：標準の NavMesh API を X 軸 -90° で XY に重ねる。壁＝動かない（Rigidbody2D なし／Static）・トリガーでない 2D コライダー（Bullet・Enemies レイヤーを除く）を `Collider2D.CreateMesh` → 床より 0.5 手前に置いて通れなくする。歩ける範囲は PlayerSceneContext の Tilemap。大きさ（小中大）ごとに `NavMesh.CreateSettings`。0.5 秒ごとに壁の変化を見て非同期で作り直す（作り直し中は材料を変えない）。OnDestroy でデータと設定を消す。NavMeshAgent は使わない。`NavMeshPath` はフィールド初期化で作れない（Awake で）。`EnemyNavArea`：敵だけ入れない／通りにくい（エリア 3、コスト 5）。
- **行動パターン**（`Enemy/Pattern/`）：`EnemyPattern`（SO）→ 段階 `EnemyPhase`（選び方・次へ進む条件・入ったときの演出）→ 行動 `EnemyAction`（`[SerializeReference]`、`[EnemyActionMenu("分類/名前")]` で「＋」メニューに出る。クラス名を変えるとデータが消えるので `[MovedFrom]`）。`EnemyBrain`（共通プレハブ）が実行。待つときは `brain.Wait` / `MoveUntil`（止められている間は数えない）。途中でやめるのは `brain.Interrupt()`（各行動の `Cleanup`、攻撃の `AttackExecution.Cleanup`）。
- **攻撃**（`Enemy/Attack/`）：`AttackData`（SO。予告→発動→後隙）、`AttackShape`（判定と予告のメッシュを同じデータから）、`AttackExecution`（実行。当てるのは `DamageUtility.Apply`）、`TelegraphView`（予告の表示。プール・Unlit 既定・描く順番 3）。突進はプレイヤーとの衝突を切ってすり抜け、離れてから戻す。弾は EnemyBulletPool を使う（`Configure` で速さ・ダメージ、`SetRules` ですり抜け・弾消し）。発動：一瞬／出し続ける／弾（狙い・全方位・うず巻き・ばらまき）／上から落ちる／下から突き出る／設置して起爆（`PlacedBomb`）。当てるのは `AttackHits`（タグのない DamageReceiver はギミックとしていつも当てる）。
- **実行の共通部分 `PatternRunner`**：EnemyBrain（体あり）と `AttackEmitter`（攻撃の発生源＝見えない敵。動く条件・止める命令）が継承。行動の部品・`AttackExecution` は PatternRunner を受け取る（`runner.Enemy` が null なら移動・突進・アニメは飛ばす）。
- **仲間を呼ぶ**：`SummonAction` → `Enemy.Spawn(data, pos)`（`Resources/Enemies/Enemy_Base` を読み、Awake の前にデータを入れる）、卵 `EnemyEgg`。同時に出せる数は PatternRunner が数える。倒されたら呼んだ仲間も倒す（`EnemyData.RemoveSummonedOnDeath`）。
- **ギミック連動**（`Enemy/Gimmick/`）：イベントの部品 `EnemyCommandEventAction`（止める・○秒・動かす・行動をやめさせる・段階・倒す・消す）／`EnemyDamageEventAction`／`DamageAreaEventAction`（一時的な AttackEmitter、`IgnoreEnemyBlock`）。`AttackSwitch`（攻撃で作動するスイッチ。DamageReceiver の OnHit を DamageRule で絞る。セーブキー `Switch/<シーン>/<ID>`）。範囲攻撃・プレイヤーの弾・敵の弾も DamageReceiver の付いた物に当たる。
- 敵の命令：`Enemy.SetPaused(bool, 色)` / `PauseFor` / `Kill`（効き方・無敵を無視）/ `Remove` / `Teleport` / `Stagger(秒)`。
- **倒したとき・倒した後**（段階6、`Enemy/Defeat/`）：EnemyData の `DefeatStyle`（消える／倒れた姿で残る／おとなしくなる＝NPC）・`drops`（`EnemyDrop` → `DroppedItem`、セーブしない）。置いた敵ごとに 倒したときのイベント・フラグ・倒した後に調べたとき（`DefeatedTalk`）・倒したことのセーブ（`Enemy/<シーン>/<ID か階層パス>`、`DefeatReturn`：同じ／いない／復活）。`Health.SetDeadSilently` で戻す。
- **グループ** `EnemyGroup`（`Enemy/Battle/`）：全部／○体倒したらイベント・フラグ・1回だけ（`Group/<シーン>/<ID>`）。撃破の順番（順番でない敵は 無敵バリア／○秒で復活／攻撃したら全員回復。`DamageFilter` を包んで体力 1 未満にしない）、次の敵に目印。**ウェーブ**：`BattleArea.waves`（`BattleWave`・`WaveSpawn`）、前のウェーブを全滅で次へ。
- **部位・ひるみ・見た目**（段階5）：`EnemyPart`（`Enemy/Parts/`。`DamageReceiver.Handler` で受け、本体へ倍率・`DamageInfo.noStagger`、`Priority` で範囲攻撃は部位を先に）。ひるみは `Enemy.AddStagger`（キャンセルできない攻撃中は `staggerPending` で待つ）、`AttackData.cancel`、`AttackExecution.CanCancel`。`Enemy.Stopped`＝Paused か Staggered（行動・戦闘エリアの捕まる判定はこちらを見る）。見た目は `EnemyLook`（Enemy が自動で付ける。色は全部ここでかける）。`EnemyData.ApplyDamageRules` は 0 ダメージを 0 のまま返す。
- **追跡・戦闘**（段階4）：行動 `ChaseAction`（見えている間は追う、見失ったら `LastSeenPosition` を探す、距離で速さ）。どの行動にも「見え方」の条件（`PatternRunner.CanSeeTarget`：Linecast で動かない当たり判定に当たると見えない）。`BattleArea`（`Enemy/Battle/`）：始まり方×勝ち方×負け方×負けたとき（**やり直す＝始まったときに Save.Commit とセーブしないフラグの写し → 負けたら仮置きを捨てて所持品を読み直し、同じシーンを暗転で読み込み直してやり直す位置から始め直す**。拾った物・フラグ・扉・爆弾・敵がすべて始まったときに戻る。体力は満タン）、`BattleArea.AnyActive` の間は SavePoint 不可、時間は EnemyBlock 中は進まない、1回だけならセーブ `Battle/<シーン>/<ID>`。イベントの部品 `BattleControlEventAction`。画面表示 `BattleHud`（`Resources/UI/BattleHud.prefab`、Tools > 敵 > 戦闘の画面表示のプレハブを作る）：上の帯（目的・残り時間・残りの敵）と真ん中のメッセージ。
- **効き方**：`EnemyData.damageRules`（`DamageRule`：`DamageKindFilter`〔DamageKind と同じ番号＋Any=-1〕・`CharacterFilter`・`GaugeJudge`・倍率、全部かけ算）→ `Health.DamageFilter`。0 以下は `OnBlocked`。
- **スポナー** `EnemyDataSpawner`（`Spawner/Enemy/`、段階7）：敵のデータから出す（種類・選び方・同時にいる数・合計・間隔・出る場所・条件・予告・全部倒したらイベント）。古い `EnemySpawner` は残している。
- **ツール**（段階7）：攻撃の形のプレビュー（`AttackDataEditor` の Inspector の図・Scene ビュー〔`AttackPreviewAnchor`＝最後に選んだ敵の位置〕）、敵の一覧（`EnemyListWindow`、Tools > 敵 > 敵の一覧）、EnemyBrain の Gizmo（行動の距離・見える距離）、デバッグの「攻撃」（`PatternRunner.ForceAttack`）。サンプルに 倒れて残る・おとなしくなる・`Sample_順番に倒すグループ`・`Sample_スポナー`。練習問題は `Docs/プランナー向け作業手順書.md` の 7 章 練習 6〜9。
- 敵はプレイヤーに触れている向きへは進まない（重い敵がプレイヤーを押し出してイベント範囲に入れてしまうのを防ぐ）。
- サンプル：Tools > 敵 > サンプルを作る（`Assets/Data/Enemies/Sample/`）。Stage1_2 のスポナーはまだ古い Enemy.prefab（置き換えはシーン変更なので未実施）。
- `GameCondition`（`Common/`）：フラグ・アイテム（既存の Condition を中に持つ）・操作キャラ・パーティーの体力・範囲。
- **Inspector の項目名は `[Label("日本語")]`**（`InspectorName` は enum の値にしか効かない）。最小値・スライダーは `[Label("名前", min)]` / `[Label("名前", min, max)]`。Range / Min と併用しない。リストには付けない。
- エディタ：Tools > 敵 > 共通プレハブとサンプルを作る、Hierarchy 右クリック > 敵 > 敵を置く／敵の通行設定エリア。デバッグウィンドウの「敵」（止める・経路の表示・倒す）。シーンチェックに敵の項目。
- 手順書：`Docs/敵の作り方.md`（段階ごとに追記）

### イベント（`Assets/Script/Event/`）
- `EventSequence`：`Play Mode` = **順番に実行（初期値）**／すべて同時に実行。`Lock Player` で実行中はプレイヤーの入力を止める（`EventSequence.IsBusy`）。Actions が空なら同じオブジェクトの EventAction を上から順に使う。実行中の再生要求は無視。
- `EventAction`：`Execute` / `Run`（コルーチン。待つ部品がオーバーライド）/ `OnExit` / `Restore`（セーブからの復元。見た目だけ）。
- 部品：Dialogue / Se / SetActive / SpriteChange / Anim（終わるまで待てる）/ Effect / Bgm系 / ItemGive / ItemTake / SetFlag / **Wait**。
- 注意：順番に実行中に、自分（か親）を非表示にすると途中で止まる（シーンチェックで検出）。
- 入力停止の判定は `InputBlock.IsBlocked`（`Common/InputBlock.cs`）に一本化済み。PlayerInput / AttackController / PlayerSwitchManager / Intaractable / TimingGaugeUI がこれを見ている。

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

プログラマー向けの引き継ぎ資料（アイテム画面以降の変更の流れ・守る約束・よくある作業の手順・**各設計の理由**）は `Docs/引き継ぎ_アイテム画面以降の変更.md`。仕組みの変更が大きいときはそちらも更新し、新しい設計判断には「理由」「なぜこの形か」を書き足す。
プランナー向けの通しの作業手順書（Unity の基本操作からイベントの組み立て・SE・BGM・明かり・マップ遷移・確認まで、練習問題つき）は `Docs/プランナー向け作業手順書.md`。11-06 に Artifact で作った個別の手順書（SE・暗闇・マップ遷移・BGM）の内容はここに取り込み済みで、食い違うときはこちらが新しい。
共有用（Claude Docs）にも同じ内容を置いている：プランナー向け作業手順書 https://claude.ai/artifact/HawxoKhE7Pz73WdbUbxwt4 、引き継ぎ資料 https://claude.ai/artifact/MZz5A9WgmQNB8owyj3yxeW 。**Markdown と自動で同期はしない**ので、Markdown を大きく直したら共有用にも反映する（頼まれたとき）。**プランナー向けの機能を足したり、メニュー名・Inspector の項目名を変えたりしたら、個別の手順書に加えてこちらも更新する**（付録の早見表を含む）。

詳しくは下の文書（自動で読み込まれる）。**コードやシーンを変えたら、この文書の表と問題点も更新する。**
問題を直したら「4. 問題点」から消し、新しく見つけたら追加する。

@Docs/code-map.md

### 問題点の要約（優先度の高いもの）
（2026-11-08 に全項目を再確認。全体は code-map.md の「4. 問題点」と、引き継ぎ資料の「8. 既知の問題（すべて）」）
- **ノア・シグルのプレハブに FootstepSe がない**（足音が鳴らない。FootstepSe は Stage1_2 の動かない `Player` オブジェクトに付いている）。
- `BgmProgress`（BGM の進行度）が再生ごとにリセットされず、セーブもされない。
- `StageDarkness` の明かりが操作キャラに付いていかないことがある。
- **古い作りの敵**（Enemy.prefab / Behavior Graph）：移動が壁をすり抜ける、会話中も移動は続く（撃つのは止まる）。古い作りの敵は倒しても何も起きない・セーブされない（新しい作りは段階6で対応）。落ちたアイテムはシーンを出ると消える。体当たりはプレイヤーに無敵時間がないので間隔だけで決まる。敵の攻撃にノックバックがない。攻撃の「弾を撃つ」は弾の見た目だけプール任せ。置いた爆弾の「触れたら爆発」はついてくるシグルでも爆発する。
- タイトルの SceneBgm に曲が入っていない（タイトルで BGM が鳴らない）。
- `CheckPoint` がどのシーンにもない（再開はシーンの入口から）。
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
- 「続きから」がない・セーブが1つ・再開位置を記録しない → タイトル画面とスロット3つ（再開位置・プレイ時間・保存日時つき）。
- 会話フォントの文字が足りない（アトラスが満杯）→ NotoSansJP-Regular SDF の Multi Atlas Textures をオン。
- シグルが撃てない（SigleShooter なし・OnShot 未接続・Target Camera が空）→ プレハブに SigleShooter、`PlayerInput.Update` で左クリックを直接読む、カメラが空ならメインカメラ。
- シグルに Health がない（ダメージなし・BGM の HP 条件が効かない）→ **2人で体力を共有**（`Health.ShareWith`）。謎解き段階0で「一緒＝シグルの体力を共有、離れる＝別々」に変更。
- シグル操作中も Space / R でノアの攻撃が出る → 操作中のキャラだけが攻撃する（謎解き段階0）。
- `Stage1` / `Stage2` / `SampleScene` が Build Settings に入っていた → 外した。ゲームオーバー画面のリトライ先・SceneChange の SampleScene 参照も修正。
