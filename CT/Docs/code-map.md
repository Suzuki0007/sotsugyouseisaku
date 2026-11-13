# コード構成と問題点（開発者・Claude 向け）

最終更新：2026-11-13（性能の計測 PerfMonitor を追加。性能の点検スキル perf-review と Project Auditor で全体を点検し、問題点に追記：ビルドのコンパイル失敗〔PlayerFollower〕・品質レベル・static のリセット漏れの疑い など）。**毎回は自動で読み込まない**（2026-11-12 から。機能ごとの説明は `.claude/rules/`）。コードを触る前に関係する節を読み、コードを変えたら、この表も更新すること。
ファイルの参照は `Assets/Script/` からの相対パス。行番号は目安。

---

## 1. アセットのフォルダ

| フォルダ | 中身 |
|---|---|
| `Assets/Scenes/` | **Build Settings：`Title` → `Stage1_1` ⇄ `Stage1_2`、`SceneGameOver`**。ファイルだけ残っているもの：`Stage1` / `Stage2`（**古い作り**：プレイヤーをシーンに直接置く）、`SampleScene`、`kari/`（仮の Start / Clear）、`Tilemap/` |
| `Assets/Prefab/` | `Players/`（Player_Noah / Player_Sigle）、`Gimmicks/Sample/`（仕掛けのサンプルの部屋）、`Enemies/`（Enemy＝古い作り。新しい作りの Enemy_Base は `Resources/Enemies/`）、`Bullet/`、`UI/`（DialoguePanel、**DialogueSystem**） |
| `Assets/Resources/` | `Items/`（ItemData。**必ずここ**。`Items/Sample/` にサンプル）、`UI/`（ItemMenu・GameMenu のプレハブ。それぞれ Resources.Load で自動生成）、`PlaceNames.asset`（場所の表示名）、`Enemies/Enemy_Base.prefab`（新しい作りの敵の共通プレハブ。仲間を呼ぶときに実行中に読む）、`UI/BattleHud.prefab`（戦闘の画面表示）、`Barks/`（相棒の一言 BarkData。`Sample/` にサンプル 16 個＝使うはオフ）、`BarkSettings.asset`（一言の見た目・出し方）、`PartyHints.asset`・`PuzzleUI.asset`・`Notes/` |
| `Assets/Data/` | `Enemies/`（EnemyData・EnemyPattern・AttackData。`Sample/`〔`Attacks/`・`Patterns/`〕にサンプル）、`Characters/`（CharacterProfile：Noah / Sigle）、`Dialogue/`（シナリオの会話：`Scenario_OP.csv`〔OP〕・`Scenario_F1.csv`〔一層〕・`Scenario_F2.csv`〔二層〕と、取り込んだ DialogueData の `Scenario_OP/`・`Scenario_F1/`・`Scenario_F2/`）、`Flags/`（GameFlag。`Flag_OP_Done`＝OP を見た、`Flag_Sigle_Joined`＝シグルが仲間になった）、`Animation/Sample/`（SpriteAnimData）、`Effect/Sample/`（EffectData） |
| `Assets/Behavior/Enemy/` | Unity Behavior の敵 AI グラフ（`Behavior Graph.asset`）と独自ノード |
| `Assets/Images/`、`Assets/BGM/`、`Assets/SE/`、`Assets/Fonts/` | 素材 |
| `Assets/res_kari/` | 仮素材（マップタイル約2000枚、仮プレイヤー画像など） |
| `Assets/_Recovery/0.unity` | Unity のクラッシュ復旧で作られたシーン（不要なら削除してよい） |
| `Docs/` | プランナー向け手順書（通しの手順書は `プランナー向け作業手順書.md`）、この文書、プログラマー向けの引き継ぎ資料（`引き継ぎ_アイテム画面以降の変更.md`） |
| `.claude/` | Claude の作業環境。`skills/`（`unity-check`：再コンパイルとコンソールの要約・シーンチェック、`unity-playtest`：再生テストの準備と後片付け〔セーブのバックアップと復元〕・キー入力、`finish-task`：作業の締め・`scripts/changes.ps1`〔Fork 同梱の Git で変更の一覧と差分の要約。読むだけ〕）、`rules/`（機能ごとの説明。`paths:` で対象のファイルを触ったときに読み込まれる）、`settings.json`（確認なしで実行してよいコマンド）。スクリプトは PowerShell 5.1 用で ASCII のみ |

---

## 2. 起動とシーン開始時の流れ

0. 起動：`AutoSave.LoadOnStartup`（BeforeSceneLoad）が旧 `save.json` をスロット1へ移す。`GameSession.Init` がプレイ時間のカウンターを作る。
   - **セーブの枠（11-13）**：オートセーブ枠 `save_auto.json`（`Save.AutoSlot` = -1）と手動の枠 `save_1〜3.json`。**自動で書くのはいつもオートセーブ枠**（シーンに着いたとき・CheckPoint・イベントの部品「オートセーブ」）。手動の枠はセーブポイントの手動セーブ（`Save.SaveToSlot`）だけが書く。終了時・タイトルへ戻るとき・シーンを読み込んだときは**書かない**（古い再開位置で進み具合だけ書かれて、ロードで進めなくなる不具合があったため）
   - **Title**：`TitleMenu` でスロットを選ぶ → `GameSession.StartNewGame`（オートセーブ枠を空にして始める）か `Continue`（`Save.LoadSlot` → 記録した場所を `PlayerSpawnPoint.PendingSpawnPointId` / `PendingPosition` に）→ `InventoryManager.ReloadFromSave` → SceneFader でステージへ
   - エディタでステージから直接再生したときだけ、デバッグウィンドウで選んだスロットをここで読む（初期値は使わない）

ステージ（Stage1_1 / Stage1_2）の開始：
1. （Title で選んだスロットのデータが Save に入っている）
2. シーンの Awake：`InventoryManager`（なければ AfterSceneLoad で自動生成。セーブから所持品を復元）、`DialogueManager`、宝箱などがセーブを読む
3. `PlayerPartySpawner.Start`（`Spawner/Player/`）
   - `PlayerSpawnPoint.ConsumePendingPosition()`（つづきから・チェックポイント）→ `ConsumePending()`（出現位置ID）→ 既定の位置 の順で出現位置を決める
   - **着いた場所を「再開する場所」として記録し、`GameSession.AutoSaveOnArrival()`**（Commit ＋ オートセーブ枠へ WriteAsync。セーブを使っていなければファイルには書かない）
   - Noah / Sigle のプレハブを Instantiate
   - `PlayerRuntimeInitializer` で、シーンの参照（`PlayerSceneContext`：Tilemap・弾プール・カメラ・UI・SceneChange・AreaAttack・BulletDeleteArea）を渡す
   - カメラ・ライフ UI・攻撃・死亡時の SceneChange などを配線し、最後に `PlayerSwitchManager.InitializePlayers`
4. `EnemySpawner`（`ObjectSpawner` 派生）が敵を出し、Behavior Graph の `TargetPlayer` と `EnemyShooter` に現在のプレイヤーを渡す（キャラ切り替えにも追従）

操作キー：移動 WASD / 矢印、調べる・会話送り E、キャラ切り替え C、**相棒を待たせる／呼ぶ F**、射撃 マウス（Sigle）、弾消しエリア Q（Noah）、範囲攻撃 Space、タイミングゲージ攻撃 R（判定 Space）、**メニュー Tab**（アイテム・ロード・タイトルへ。Esc で閉じる）。

プレイヤーの操作を止める条件は `Common/InputBlock.cs`（`InputBlock.IsBlocked`）に一本化している。

---

## 3. スクリプト一覧

状態：◎ 今の作りの中心 ／ ○ 使用中 ／ △ 古い・使われていない ／ 🛠 エディタ専用

### プレイヤー・キャラクター
| ファイル | 役割 | 状態 |
|---|---|---|
| `ObjectBase/ObjectBase.cs` → `CharaBase/CharaBase.cs` | 共通の基底（中身は空の Start / Update） | ○ |
| `CharaBase/CharacterMovement.cs` | Rigidbody2D.MovePosition で移動。`LastDirection`（最後に動いた向き。見ていない間に動く像が使う）。`LimitNextPosition`（動く前に行き先を制限。PlayerMovement がマップの範囲に収める） | ◎ |
| `Player/PlayerMapLimit.cs` | マップの範囲（`MapBounds`）に収める。`ClampToMap`（体の大きさの分だけ内側。当たり判定が無効でも形から大きさを出す）を PlayerMovement が動く前に使い、LateUpdate でも念のため戻す（物理の位置も合わせる）。**LateUpdate の順番**：PlayerFollower（0）→ PlayerMapLimit（100）→ PlayerHistory（200）→ CharacterAnimLink（300）。`DefaultExecutionOrder` で決めている（順番が崩れると、ついてくる相棒が端で震える） | ◎ |
| `Player/PlayerMovement.cs` | 操作できるかのフラグを追加。操作できなくなったら向きと速さを消す。FixedUpdate で毎回速さを 0 にしてから動かす（押されて滑らない） | ◎ |
| `Player/PlayerInput.cs` | キー入力 → 操作中キャラの移動、Q で弾消し、**マウスの左クリックで射撃**（操作中のキャラに SigleShooter があれば。Update で直接読む。Target Camera が空ならメインカメラ。`OnShot` は Input System のイベント用に残しているが未使用） | ◎ |
| `Manager/PlayerSwitchManager.cs` | Noah / Sigle の切り替え（位置の入れ替え、追従、カメラ）、`Warp`。**2人の状態** `PartyMode`（一緒／待機／別行動。`SetMode`、`ModeChanged`・`Switched` イベント）：一緒のときだけ位置を入れ替え・相棒がついてくる（待機・別行動は `PlayerFollower.Hold`、カメラだけ移る）。一緒に戻ると相棒が合流（道があれば歩く）。**待て／ついてこい**（F キー、`ToggleWait`・`TrySetMode(..., requireRoute)`・`CanPartnerReach`・`WaitCommandLocked`）。体の当たり判定（`ApplyBodyCollision`：ついてきている間だけプレハブのまま＝シグルは無効）。体力の共有（一緒＝ノア → シグル、離れる＝別々）、`PartyHealth`（一緒＝シグル、離れる＝操作中のキャラ）、`HealthOf`。**相棒の合流**（11-13）：「シグルが仲間になったフラグ」（`sigleJoinedFlag`、空なら最初から2人）が立つまでシグルを非表示にしてノア1人（`PartnerAbsent` / `IsPartnerAbsent`。体力はノアの分、C・F・`SetMode` は効かない）。`GameFlags.OnChanged` を見て、フラグが立つとノアの位置にシグルを出して一緒にする | ◎ |
| `Party/PartyModeEventAction.cs` | イベントの部品「2人の状態を変える」（状態〔変えないも可〕・操作するキャラ・待て／ついてこいのキーを使えなくする）。まだセーブしない | ◎ |
| `Player/PlayerHistory.cs`（`SeedPolyline`：曲がり角のある道を歩いてきた履歴にする）、`Player/PlayerFollower.cs` | 位置の履歴と、操作していない側の追従（何フレーム前の位置か） | ◎ |
| `Player/PlayerMapLimit.cs` | （古い行。下の PlayerMapLimit の行が新しい）マップの絵（MapBounds）と歩ける範囲の中に収める | ◎ |
| `Player/PlayerRuntimeInitializer.cs`、`Player/PlayerSceneContext.cs` | 生成したプレイヤーにシーンの参照を渡す | ◎ |
| `Player/Player_Noah/NoahMovement.cs`、`NoahAnimation.cs` / `Player_Sigle/SigleMovement.cs`、`SigleAnimation.cs` | 古い向き切り替え（SpriteAnimator があれば何もしない） | △ |
| `Player/Player_Noah/NoahDead.cs` | 死亡 → 死亡アニメ → `SceneGameOver` へ。相棒（シグル）の体力も見て、どちらが倒れてもゲームオーバー（倒れたキャラの死亡アニメ。`SetPartner`） | ◎ |
| `Player/Player_Noah/NoahBulletDeleteAreaController.cs` | Q を押している間、弾消しエリアを出す（SE 付き） | ◎ |
| `Player/Player_Noah/NoahInventory.cs`、`Data/NoahData.cs` | 鍵フラグ（`hasKey`）の古い仕組み | △（KeyItem・Treasure が使う） |
| `Player/Player_Sigle/SigleShooter.cs` | Sigle の射撃（プール・SE・エフェクト） | ◎ |
| `Player/FocusPlayer.cs`、`Player_Noah/NoahMapLimit.cs`、`Player_Noah/NoahHistory.cs` | 古い仕組み（Stage1 / Stage2 で使われている） | △ |
| `CharaBase/CharacterSorting.cs` | Sigle の描画順を Noah との上下で切り替え | ○ |
| `Gimmick/WalkArea.cs`、`WalkAreaExclude.cs` | （11-13）歩ける範囲を円・四角・多角形の組み合わせで決める（子の Collider2D を形として読み、再生中は無効にする）。**シーンに何個置いてもよい（全部を合わせた範囲）**。多角形の輪郭が2つ以上なら内側は穴（偶奇判定）、子に `WalkAreaExclude`（歩けない形）を付けるとその形は抜ける（⋮「歩けない形を追加（円／四角）」）。`Clamp`（外・歩けない形の中なら一番近い歩ける所へ）・`Contains`・`TryGetBounds`・`TryRandomPoint`・`Signature`（形が変わったかの目印）・`ContainsPoint`／`ShapeCount`（エディタでも使える。シーンチェック用）。プレイヤー（`PlayerMapLimit.ClampToMap` の最後）・敵の移動・敵と相棒の道（`MapArea` 経由）が使う。Stage_1_3 のティンパニ部屋で使用 | ◎ |
| 🛠`Gimmick/Editor/WalkAreaEditor.cs`、`WalkAreaMask.cs` | （11-13 D 段階2）WalkArea の Inspector：「マスク画像から作る」（白い所の輪郭をマーチングスクエアでなぞり、ダグラス・ポイカーで間引いて、子「マスクから作った範囲」の PolygonCollider2D に。内側の輪郭は穴）・「今の形をマスク画像に書き出す」（白黒 PNG）・形を足すボタン。WalkArea にマスク画像・合わせるマップの絵・線の細かさ・白とみなす明るさの欄と `BuildTester`（エディタ用の速い判定） | ◎ |
| `Map/Room.cs`、🛠`Map/Editor/RoomMenu.cs`・`RoomChecks.cs`・`RoomEditor.cs` | （11-13 D 段階3）**部屋**：`Room`（名前・ID・種類・部屋の絵・メモ。目印と点検）。Hierarchy 右クリック > 部屋 > 普通／戦闘／謎解きの部屋（選んだ絵に合わせて、歩ける範囲・カメラの範囲・出現位置・着いたとき〔非表示〕、戦闘エリア＋勝ったときのオートセーブ、スイッチ→答え合わせ→扉＋オートセーブを作る。`RoomMenu.Build`）。Inspector に点検の一覧（`RoomChecks`。シーンチェックの `CheckRooms` も同じ）と「カメラの範囲を部屋の絵に合わせる」 | ◎ |
| `Map/MapDebugView.cs`、`Map/MapReach.cs` | （11-13 D 段階4）デバッグ用：ゲーム画面に範囲の線（緑＝歩ける範囲・赤＝歩けない形・水色＝マップの範囲。`MapDebugView.Show`）／今いる場所から調べる物・出口・出現位置へ歩いて行けるか（NavMesh2D の小さい体。`MapReach.Check`・`Report`）。デバッグウィンドウ「マップの範囲」と `event.ps1 -ShowArea` / `-Reach` から使う | ◎ |
| `Map/MapArea.cs` | （11-13 D 段階1）**マップの範囲の窓口**。歩ける範囲＝WalkArea → マップの絵（`MapBounds`）→ Tilemap（`PlayerSceneContext`）、映す範囲＝マップの絵 → Tilemap。`TryGetViewRect`（カメラ）・`TryGetWalkRect`・`HasWalkShape`・`Contains`・`Clamp`。FocusCamera（CameraArea がない所）・Enemy の `ClampToMap`（止められている間は収めない）・NavMesh2D（床：WalkArea があればその形どおりに 0.2 マスの升目で作る）・PartyRoute（まっすぐの線が範囲の外を通らないか）が使う | ◎ |
| `Party/GuardGauge.cs` | （11-13）守り（弾消し）のゲージ。置いたシーンだけ、弾消しを出せる時間とリチャージを決める（空になると決めた量までたまるまで出せない）。画面の HP の下に「シグルの守り（Q）」のゲージ。`Use`（`NoahBulletDeleteAreaController` が呼ぶ）・`Refill`・`BoostRecharge`・`ResetGauge`・`Emptied`・`Ready`。Stage_1_3 で使用 | ◎ |
| `Item/InstantPickup.cs` | （11-13）触れるとすぐ効くアイテム（回復／守りのゲージ）。持ち物に入らない・セーブしない。決めた時間で点滅して消える。`Spawn`・`Picked`・`Appeared`。プレハブ `Prefab/Gimmicks/Timpani/Item_Heal`・`Item_GuardBoost` | ◎ |
| `Timpani/TimpaniUnit.cs` | （11-13）ティンパニ1台。ペグが締まるたびに周りの弾を消し敵を止める、全部締まると敵をおとなしくする（`Kill`・絵の差し替え・♪・回復アイテム）。`Calmed`・`AttacksCleared` | ◎ |
| `Timpani/TimpaniRoom.cs` | （11-13）ティンパニの部屋全体。調律できた数で敵の段階を進めるフラグを立てる、全部でクリアのフラグ・`AllCalmed`・イベント | ◎ |
| `Timpani/TimpaniBarks.cs` | （11-13）ティンパニ戦の一言。戦闘エリアが動いている間、できごと（ペグ・調律・守り・アイテム・ダメージ・体力・進まない・始まった）ごとに候補の一言を確率と間隔で出す（`Bark.PlayLines`） | ◎ |
| `Timpani/TimpaniPeg.cs` | （11-13）ティンパニのペグ。調べるとリズムゲーム（縮む輪が中心の輪に重なるときに E）、決めた回数の成功で締まり GimmickState が ON。ゲームは止まらない・離れると中断・使えるキャラ（ノアだけ）。色と印（◆◇・♪）で進み具合。`Succeeded`・`Missed`・`Tightened`（static イベント）。プレハブ `Prefab/Gimmicks/Timpani/Peg.prefab` | ◎ |
| `Scene/SceneChangeEventAction.cs` | （11-13）イベントの部品「シーンを移る」（シーンの `SceneChange.ChangeScene` を呼ぶ。イベントの最後に置く）。Stage1_2 の正面の扉 → Stage_1_3 | ◎ |
| `Camera/CameraShake.cs`、`ShakeEventAction.cs` | （11-13）画面を揺らす。描画の間だけカメラの位置をずらす（`RenderPipelineManager.begin/endCameraRendering`）ので、FocusCamera の追従とぶつからない。イベントの部品「画面を揺らす」 | ◎ |
| `StageDarkness/DarknessEventAction.cs` | （11-13）イベントの部品「暗さ・視界を変える」（暗さとプレイヤーの明かりの半径を同時に、時間をかけて。セーブから戻るときは最後の値） | ◎ |
| `Event/Actions/FaceDirectionEventAction.cs`、`MoveObjectEventAction.cs` | （11-13）イベントの部品「キャラの向きを変える」「物を動かす」（ずらした位置から／へ・目的の物へ〔動いていても追う〕。敵が降ってくる・岩がどく・弾が飛ぶ。動かすもの・向かう先にノア／シグルも選べる〔再生中に探す。待機・別行動のときに使う〕） | ◎ |
| `Party/Bark/Bark.cs`（11-13 の変更） | シグルが仲間になる前（`PlayerSwitchManager.IsPartnerAbsent`）は、自動の一言（`Fire`）だけ出さない。イベントから出す一言（`Play`・`PlayLines`）は出せる（Stage1_2 のノアの「逃げなきゃ！」） | ◎ |
| `Camera/MapCameraAspect.cs` | （11-13）マップを写すカメラを画面の中央の 4:3 にし（`Camera.rect`）、左右（上下）に帯（並び順 -1000 の Overlay Canvas）を出す。FocusCamera の付いたカメラにシーン読み込み時に自動で付く。比率・帯の色は Inspector。UI は画面全体（16:9）を使う | ◎ |
| `Camera/FocusCamera.cs` | プレイヤーを追い、マップの範囲で止める（11-13 から `MapArea.TryGetViewRect`＝マップの絵 → Tilemap。前は Tilemap で、絵の外が映っていた）。**カメラの範囲**（`CameraArea`）の中ならその範囲で止める。`BeginBlend`（なめらかに移る。範囲が変わったとき・`SetTarget(t, true)`）、`LookAt`（決めた場所を映す）（謎解き段階4） | ◎ |
| `Camera/CameraArea.cs` | カメラの範囲（Is Trigger の BoxCollider2D。`Find` で位置を含む範囲〔優先度〕） | ◎ |
| `Camera/CameraLookEventAction.cs` | イベントの部品「カメラを向ける」（見せる場所・動く時間・見せる時間・戻す） | ◎ |

### 攻撃・弾・敵
| ファイル | 役割 | 状態 |
|---|---|---|
| `Attack/AttackController.cs` | キーで攻撃を始める基底。操作しているキャラの攻撃だけを出す | ◎ |
| `Attack/Player/AreaAttackController.cs`、`Attack/AreaAttack.cs` | Space：Noah の周りに範囲攻撃（数フレーム判定）。攻撃の種類（範囲／ゲージ＋判定）つきで `DamageUtility.Apply`。同じ攻撃で同じ Health には1回だけ（本体と部位の二重ヒットを防ぐ）。ターゲットレイヤー（敵）に加え、レイヤーに関係なく `DamageReceiver` の付いた物（スイッチ・爆弾など）にも当たる | ◎ |
| `Attack/Player/TimingGaugeAttackController.cs`、`UI/AttackArcGauge/TimingGaugeUI.cs` | R：ゲージ → Space で判定 → 倍率つきで範囲攻撃（判定待ちの間は `TimingGaugeUI.IsJudging` が true で、通常の Space 攻撃は出ない。会話・イベント中はゲージが止まる） | ◎ |
| `Bullet/BulletBase.cs`、`Bullet/Player/PlayerBullet.cs`、`Bullet/Enemy/EnemyBullet.cs` | 弾（寿命・命中・エフェクト）。味方（敵の弾→Enemies、プレイヤーの弾→Player）と、イベントなどのトリガー範囲は通り抜ける。壁・相手・弾どうし・弾消しエリア・`DamageReceiver` の付いた物には当たる。`BulletBase.Configure`（1発だけ速さ・ダメージを変える）、`EnemyBullet.SetRules`（すり抜けるキャラ・弾消しで消せるか。取り出すたびに戻す） | ◎ |
| `Bullet/BulletDeleteArea.cs` | 敵の弾を消すエリア | ◎ |
| `Pool/BulletPoolBase.cs`、`Pool/Bullet/*` | UnityEngine.Pool による弾プール（初期300、最大1000） | ◎ |
| `Enemy/EnemyShooter.cs`、`Enemy/EnemyDead.cs`、`Enemy/EnemyMovement.cs`（空） | 古い作りの敵の射撃・死亡。EnemyShooter は狙う相手（操作中のキャラ）と弾のプールを自分で探す（スポナーなしでも撃てる）。`EnemyBlock` の間は撃たない | ○ |
| `Enemy/Enemy.cs`、`Enemy/EnemyData.cs` | **新しい作りの敵**（段階1〜2）。EnemyData（体力・速さ・大きさ・体当たり・見た目・倒されたとき・**行動パターン**・**受けるダメージの効き方** `DamageRule`〔種類 `DamageKindFilter`・キャラ `CharacterFilter`・ゲージ判定 × 倍率〕）を反映、Rigidbody2D（Dynamic）で移動、`MoveTo` で NavMesh2D の経路に沿って進む、`SetOverrideVelocity`（突進）、`Facing`、`PlayAction`、マップの範囲内、体当たり、プレイヤーに触れている向きへは進まない（押し出さない）、`Paused`、`Enemy.All`。**ひるみ**（段階5）：`AddStagger`（本体の OnDamagedInfo と部位から。EnemyData の staggerRules で倍率）→ たまると `Stagger`（行動をやめ、`Staggered` の間止まる）。攻撃中でキャンセルできないときは `staggerPending` で待ち、終わったらひるむ。`Stopped`＝Paused か Staggered。部位：`NotifyPartBroken` / `IsPartBroken` / `DisableAttack` / `IsAttackDisabled`。効き方は `Health.DamageFilter` に設定。**倒したとき**（段階6）：EnemyData の「倒されたあと」（`DefeatStyle`：消える／倒れた姿で残る〔死亡アニメの最後・色・当たり判定なし〕／おとなしくなる〔Idle・体の当たり判定は残す〕）と `drops`（`EnemyDrop`：確率・個数・すぐ持ち物へ）、置いた敵ごとの「倒したときのイベント」「フラグ」「倒した後に調べたとき」（`DefeatedTalk`）「倒したことをセーブする」（`Enemy/<シーン>/<ID か階層パス>`、Awake の最後で `RestoreDefeat`）「戻ったときの姿」（`DefeatReturn`）。`Kill()` は効き方と無敵を無視して倒す。共通プレハブは `Resources/Enemies/Enemy_Base.prefab`（Enemy + EnemyBrain） | ◎ |
| `Enemy/Pattern/EnemyPattern.cs`、`EnemyActions.cs`、`PatternRunner.cs`、`EnemyBrain.cs` | **行動パターン**（段階2〜3）。EnemyPattern（SO）＝段階（選び方・次の段階へ進む条件〔体力・時間・GameCondition〕・入ったときの演出）× 行動の部品（`[SerializeReference]`：待つ／近づく／離れる／巡回／ランダムに歩く／**追いかける（見失ったら最後に見た場所を探す・距離で速さが変わる）**／攻撃する／**仲間を呼ぶ**。どの行動にも距離と**見え方**（`CanSeeTarget`：壁＝動かない当たり判定で視線が切れる）の条件。`[EnemyActionMenu]` で追加メニューに出る。移動は敵の体があるときだけ）。**PatternRunner**（共通の実行部分）：行動の選択、`Wait`／`MoveUntil`（止められている間は数えない）、`Interrupt`、`SetPhase`、`RunAttack`、呼んだ仲間の管理、`IgnoreEnemyBlock`。EnemyBrain（共通プレハブに付く）＝ PatternRunner ＋ 巡回ポイント・動き出す距離・パターンの差し替え・選んだときの Gizmo（行動の距離＝オレンジ、見える距離＝水色、ラベルつき）。`PatternRunner.ForceAttack`（段階7）：今の行動をやめて攻撃を割り込ませる（パターンが動いていなければその場で1回）。デバッグウィンドウの「攻撃」から使う | ◎ |
| `Enemy/Battle/BattleArea.cs`、`BattleControlEventAction.cs` | **戦闘エリア**（段階4）。始まり方（エリア・イベントの部品・シーン開始）×勝ち方（全滅・生き残る・ゴール・フラグ）×負け方（捕まる・時間切れ）×負けたとき（**やり直す**〔始まったときに `Save.Commit` とセーブしないフラグの写し（`GameFlags.CaptureTemporary`）→ 負けたら「負けたとき」のイベントを待って `Save.DiscardPending`・`GameFlags.RestoreTemporary`・`InventoryManager.ReloadFromSave` → 同じシーンを SceneFader で読み込み直し（`PlayerSpawnPoint.PendingPosition` ＝やり直す位置）→ 読み込み後、同じ戦闘エリアがプレイヤーの出現を待って始め直し、敵を少し止める〕・ダメージ・ゲームオーバー・イベントだけ）。始まるまで参加者を止める、戦闘中フラグ、勝ったあとの敵、1回だけセーブ（`Battle/<シーン>/<ID>`）。`BattleArea.AnyActive` の間は SavePoint でセーブ不可。時間は EnemyBlock の間は進まない。**ウェーブ**（段階6）：`BattleWave`（置いた敵〔始まるまで隠す〕・新しく出す敵 `WaveSpawn`・出てくるまでの秒数・メッセージ）。前のウェーブを全部倒すと次へ、「敵を全部倒す」は全ウェーブ後。参加者は `participants`（参加する敵＋ウェーブの敵）。イベントの部品「戦闘エリアを操作」（始める・勝ち・やめる） | ◎ |
| `Enemy/Parts/EnemyPart.cs` | **部位**（段階5）。敵の子（Collider2D トリガー＋DamageReceiver）。`DamageReceiver.Handler` で受ける：部位の効き方 → 部位の体力（壊れる）→ 本体へ倍率をかけて渡す（`noStagger`）→ ひるみは部位で数える。種類（弱点・装甲・ふつう）で `Priority`（範囲攻撃で同時に当たったら部位を先に＝`DamageUtility.SortByPriority`）。壊れたとき：消える／絵・段階を変える・攻撃を使えなくする・独立（`Enemy.Spawn`）・イベント。見える条件（GameCondition）と目印・見えている間だけ効く | ◎ |
| `Enemy/Defeat/DroppedItem.cs`、`Enemy/Defeat/DefeatedTalk.cs` | **倒したとき・倒した後**（段階6）。DroppedItem：落ちたアイテム（`Drop(item, 数, 位置)`。アイコンの絵・ふわふわ動く・0.3 秒後からプレイヤーが触れると拾う。セーブしない＝シーンを出ると消える）。DefeatedTalk：倒れた／おとなしくなった敵の子に付く Intaractable（`Attach`。調べると Enemy の「倒した後に調べたとき」を実行） | ◎ |
| `Enemy/Battle/EnemyGroup.cs` | **敵のグループ**（段階6）。メンバー全部／○体倒したらイベント・フラグ・1回だけ（セーブ `Group/<シーン>/<ID か階層パス>`、戻ったら Restore）。**撃破の順番**：順番でない敵は 無敵（バリア＝TelegraphView の円）／体力 1 で止めて○秒で全回復／攻撃したら全員回復（メンバーの `DamageFilter` を包んで体力 1 未満にしない）。次に倒す敵に目印（下向きの扇形）。Hierarchy 右クリック > 敵 > グループ（選んだ順にメンバー） | ◎ |
| `Enemy/EnemyLook.cs`、🛠`Enemy/Editor/EnemyLookRuleDrawer.cs` | **見た目の変化**（段階5）。Enemy が自動で付ける。EnemyData の `lookRules`（きっかけ：体力・攻撃を受けた〔秒〕・部位が壊れた・段階・ひるみ・止められている・条件 → アニメ差し替え・色・点滅・エフェクト・SE。下の行ほど優先）。命令で止めたときの色（`Enemy.PausedTint`）もここでかける | ◎ |
| `Enemy/Battle/BattleHud.cs`、🛠`Enemy/Editor/BattleHudBuilder.cs` | 戦闘の画面表示（上の帯：目的・残り時間・残りの敵、残りわずかで赤／真ん中：始まり・勝ち・負けのメッセージ。unscaled で消える）。`Resources/UI/BattleHud.prefab` から必要なときに作り、シーンをまたぐ。帯は出した戦闘エリアだけが消せる（owner）。Tools > 敵 > 戦闘の画面表示のプレハブを作る | ◎ |
| `Enemy/AttackEmitter.cs` | **攻撃の発生源（見えない敵）**（段階3）。PatternRunner の、体のない版。動く条件（GameCondition。範囲など）・最初から動く・`SetStopped`／`StopFor`。ダメージエリアの一時的な発生源にも使う | ◎ |
| `Enemy/EnemyEgg.cs` | 「仲間を呼ぶ」の卵（時間でかえる。体力があり壊せる。Enemies のタグ・レイヤー） | ◎ |
| `Enemy/Gimmick/`（`EnemyCommandEventAction`、`EnemyDamageEventAction`、`DamageAreaEventAction`、`AttackSwitch`。AttackSwitch は作動したときに仕掛けの状態も変えられる〔謎解き段階1〕） | **ギミック連動**（段階3）。イベントの部品「敵に命令」（止める・○秒止める・動かす・行動をやめさせる・段階を変える・倒す・消す。Restore はずっと続く命令だけ）、「敵にダメージ」、「ダメージエリアを出す」（一時的な AttackEmitter で AttackData を出す。イベント中も止まらない）。AttackSwitch：DamageReceiver の OnHit を条件（DamageRule）で絞って EventSequence を実行、1回だけならセーブ（`Switch/<シーン>/<ID か階層パス>`） | ◎ |
| `Enemy/Attack/AttackData.cs`、`AttackShape.cs`、`AttackExecution.cs`、`TelegraphView.cs` | **攻撃**（段階2）。（11-13）「上から落ちる」の**ばらまき**：`dropCount` が 2 以上なら `AttackExecution.Rain` が `dropInterval` おきに `DropScatter`（歩ける範囲全体／敵の周り／プレイヤーの周り）へ落とす。`dropAimChance` の割合だけプレイヤーの近く（`dropAimJitter`）、影どうし `dropMinSpacing` 以上（今出ている影を static で全部の敵と共有）。1つずつの影・落下・当たる・揺れ・出す物は `OneDrop`（runner のコルーチン。`Cleanup` で止める）。AttackData（SO）＝予告（時間・表示・色・満ちる・点滅・順番・Unlit）→ 起点（敵／プレイヤー〔予告時・追う〕）・向き・形（円／長方形／扇形／リング、`AttackShape` が判定と表示のメッシュを両方作る）・壁で止める → 発動（一瞬／出し続ける〔回転〕／弾〔狙い・全方位・うず巻き・ばらまき。速さ・ダメージ・すり抜け・弾消し〕／**上から落ちる**／**下から突き出る**〔残す・当たり判定があれば壁〕／**設置して起爆**〔`PlacedBomb`〕）・突進（プレイヤーをすり抜け、壁で気絶）・当たる相手・効かないキャラ → 後隙。当てる処理は `AttackHits`（タグのない DamageReceiver はギミックとしていつも当てる。自分の子には当てない）。TelegraphView は使い回し（DontDestroyOnLoad のプール。`ShowShape` / `ShowCircle` で AttackData なしでも出せる）。`AttackExecution` は PatternRunner を相手に動く（体がなければ突進・アニメはしない） | ◎ |
| `Enemy/EnemyBlock.cs` | 敵を止める判定（`InputBlock.IsBlocked` + デバッグの「敵を止める」）。条件を足すときはここだけ | ◎ |
| `Enemy/Navigation/NavMesh2D.cs`、`EnemyNavArea.cs` | 2D ナビメッシュ（自動生成・シーンごと）。標準の NavMesh API を -90° 回して XY に重ね、動かない 2D コライダーを `CreateMesh` で取り出して壁にする。大きさ（小中大）ごとに別のナビメッシュ。壁の変化を 0.5 秒ごとに見て作り直す。シーンを出るときにデータと設定を消す。デバッグ表示（線のメッシュ）。EnemyNavArea：敵だけ入れない／通りにくい | ◎ |
| 🛠`Enemy/Editor/EnemyBaseBuilder.cs`、`EnemySampleBuilder.cs`、`EnemyPatternEditor.cs`、`AttackDataEditor.cs`、`AttackPreviewAnchor.cs`、`EnemyListWindow.cs` | Tools > 敵 > 共通プレハブとサンプルを作る／サンプルを作る（攻撃13・行動パターン11・敵のデータ11・部位ボスのプレハブ `Prefab/Enemies/Enemy_Sample_部位ボス`、段階6・7：倒れて残る・おとなしくなる、`Sample_順番に倒すグループ`・`Sample_スポナー` のプレハブ）、Hierarchy 右クリック > 敵 > 敵を置く／敵の通行設定エリア／攻撃の発生源（見えない敵）／攻撃で作動するスイッチ／戦闘エリア／部位を追加／グループ（全滅・撃破の順番）。共通プレハブは **`Resources/Enemies/Enemy_Base`**（`Enemy.Spawn` が実行中に読む）。行動パターンの Inspector（段階の折りたたみ・「＋」で行動の種類を選ぶ ReorderableList）、攻撃の Inspector（設定で使わない項目を隠す。**形のプレビュー**：Inspector の下に図、Scene ビューには最後に選んだ敵〔`AttackPreviewAnchor`〕の位置に形と弾の方向）。**敵の一覧**（`EnemyListWindow`、Tools > 敵 > 敵の一覧：全 EnemyData を表で編集・並べ替え・検索・新しく作る） | ◎ |
| `Damage/DamageInfo.cs`、`Damage/DamageReceiver.cs` | 攻撃の情報（量・種類 `DamageKind`・攻撃者・キャラ・位置・ゲージ判定）、`PlayerCharacters`（ノア／シグルの判定・パーティーの体力）。DamageReceiver（攻撃を受ける部品：部位・ギミック用、倍率、OnHit）と `DamageUtility.Apply / HitKey`（範囲攻撃・弾・体当たりはここを通す） | ◎ |
| `Behavior/Enemy/EnemyShotAction.cs` | Behavior Graph の「一定時間撃つ」ノード | ◎ |
| `Behavior/Enemy/AbcdefuckAction.cs`、`SampleAction.cs` | 練習用ノード（どのグラフからも使われていない） | △ **削除推奨**（下記） |
| `Spawner/ObjectSpawner.cs`、`Spawner/SpawnedObject.cs`、`Spawner/Enemy/EnemySpawner.cs` | 一定間隔で再出現（プレハブを出す古い作り。今もそのまま使える） | ◎ |
| `Spawner/Enemy/EnemyDataSpawner.cs` | **敵のスポナー（新しい作り）**（段階7）。敵のデータを何種類でも（順番／重みつきランダム）、同時にいる数・合計・1回に出す数・最初と次までの秒数・出る場所（複数・ばらつき・プレイヤーから離す距離）・条件（GameCondition）・条件を外れたら消す・予告の丸・全部倒したらイベントとフラグ。`Enemy.Spawn` で出す。EnemyBlock の間は時間が進まない。Hierarchy 右クリック > 敵 > スポナー | ◎ |
| `Spawner/Player/PlayerPartySpawner.cs`、`PlayerSpawnPoint.cs` | プレイヤーの生成と出現位置。出現位置に Sigle 用の位置があれば、Sigle はそこに出てノアの後ろへ歩いて付いていく（`PlayerHistory.SeedPath`） | ◎ |
| `Health/Health.cs` | 体力、`SetDeadSilently`（イベントを出さずに 0 にする。倒した記録から戻すとき）、`OnDied` / `OnDamaged` / `OnHealed` / **`OnDamagedInfo`**（攻撃の情報つき）/ **`OnBlocked`**（効かなかった）、`DamageFilter`（受ける前に量を変える。0 以下なら OnBlocked だけ出して終わり）、`LastDamage`、`Damage(int)` と `Damage(DamageInfo)`、`SetMaxHealth`、`OwnHealth` / `SetHealthSilently`（体力のセーブ用）、`Invincible` / `PlayerInvincible`、**体力の共有**（`ShareWith`：受けたダメージ・回復を共有先へ回し、自分のイベントも出す。2人の状態が「一緒」のときノア → シグルを `PlayerSwitchManager` が設定、離れたら共有しない） | ◎ |

### イベント・アイテム・フラグ・会話・セーブ（プランナー向けの中心）
詳しくは CLAUDE.md の「主な仕組み」と各 Docs。

| フォルダ | 主なファイル |
|---|---|
| `Event/` | `EventProcess/EventSequence.cs`、`EventAction.cs`、`EventZone.cs`、`Actions/`（SetActive / SpriteChange / Wait / **ScreenFade**＝画面の暗転。会話の画面〔並び順 100〕より奥の黒い幕 `ScreenCurtain`〔90、シーンごと〕なので、暗いまま会話できる）、**`SceneStartEvent.cs`**（シーンが始まったときに条件〔GameCondition〕を満たせば EventSequence を実行。「最初から暗転しておく」。OP で使用） |
| `Item/` | `ItemData.cs`、`ItemPickup.cs`、`Interaction/`（ItemInteraction / ItemCondition / ItemPossessionSwitch / ItemGive / ItemTake） |
| `Flag/` | `GameFlag.cs`、`GameFlags.cs`、`FlagCondition.cs`、`SetFlagEventAction.cs`、`FlagSwitch.cs` |
| `Talk/` | `DialogueManager.cs`（11-13：送りは E・Space・Enter、枠の右下の下にキーの説明 `showKeyHint`）、`Dialogue.cs`、`DialogueData.cs`、`DialogueEventAction.cs`（11-13：「会話データ」に DialogueData を直接入れられる。空なら前の作り方＝Dialogue）、`CharacterProfile.cs`、`Expression.cs`、🛠`Editor/DialogueCsv.cs` |
| `Save/` | `Save.cs`（スロット・仮置き・確定・書き込み。`AutoSlot`＝オートセーブ枠、`Write` はいつもオートセーブ枠へ、手動は `SaveToSlot`）、`AutoSave.cs`（旧ファイルの引き継ぎ・エディタでの直接再生。**終了時・シーン読み込み時の書き込みはやめた**）、`GameSession.cs`（はじめから・つづきから・タイトルへ・プレイ時間・再開場所の記録・`AutoSaveAt`／`AutoSaveOnArrival`・`AutoSaved` イベント）、`AutoSaveEventAction.cs`（イベントの部品「オートセーブ」：今の位置か指定した場所を再開位置にしてオートセーブ枠へ書く）、`AutoSaveIndicator.cs`（オートセーブしたとき右上に「オートセーブしました」。自動生成）、`SaveSlotInfo.cs`（Meta キーとスロットの概要）、`PlayTimeCounter.cs` |
| `Title/` | `TitleMenu.cs`（タイトル画面）、`SaveSlotButton.cs`（スロット1つ分の表示。オートセーブ枠は「オートセーブ」）、`ReturnToTitleButton.cs`（ボタンからタイトルへ）、🛠`Editor/TitleSceneBuilder.cs`（Title.unity を作る） |
| `UI/GameMenu/`（メニューに「メモ」：`noteButton`、`NoteBook.OpenList`）  | `GameMenu.cs`（Tab のメニュー：アイテム・ロード・タイトルへ、セーブポイントからのセーブの流れ、メッセージ）、🛠`Editor/GameMenuBuilder.cs`（`Resources/UI/GameMenu.prefab` を作る） |
| `UI/Common/` | `SaveSlotPanel.cs`（セーブデータ選択画面。NewGame / Continue / Load / Save。一番上にオートセーブ枠の行を実行中に複製して足す〔セーブのときは出さない・はじめからはこの行だけ〕。`AnySaveExists`）、`MenuKeyboardNav.cs`（ボタンの並びをキーボードで選ぶ：最初のボタンを選ぶ・↑↓で回る・外れたらキーで戻す・選ばれた色。SceneGameOver の Canvas に付けた）、`ConfirmDialog.cs`（はい／いいえ。左右移動は2つの間だけ）、`UISubmitKeys.cs`（UI の決定に Space を足す・キー操作で選択が動いたら選択の SE。起動時に自動生成）、`UISound.cs` / `UISoundSettings.cs`（UI の効果音。`Resources/UISounds.asset`）、`UIButtonSound.cs`（ボタンに付けると押したときに鳴る）、🛠`Editor/UISoundGenerator.cs`（仮の電子音を `Assets/SE/UI/` に作る）、`PlaceNameTable.cs`（場所の表示名、`Resources/PlaceNames.asset`）、🛠`Editor/MenuUIBuilder.cs`（メニュー系 UI を作る共通処理） |
| `SavePoint/` | `SavePoint.cs`（調べるとセーブ。Intaractable。戦闘エリアの戦闘中はセーブできない）、🛠`Editor/SavePointCreator.cs`（Hierarchy 右クリック > セーブポイント） |
| `Common/InputBlock.cs` | プレイヤーの操作を止める判定（会話・暗転・アイテム画面・イベント・メニュー・謎解きの画面） |
| `Intaract/Intaractable.cs` | 「調べる」の基底（範囲に入って E）。範囲の中のプレイヤーを覚え、**操作中のキャラだけ**を player にする（ついてくる相棒・待たせている相棒には反応しない。キャラを切り替えると入れ直す）。`OnPlayerEnter` / `OnPlayerExit` も操作中のキャラだけ（謎解き段階3） |
| `Intaract/InteractPrompt.cs` | **キーの案内**（11-13）：`IInteractPrompt`（`PromptVisible`・`PromptLabel`）と `InteractPromptView`（自動生成・シーンをまたぐ。操作中のキャラが範囲にいる調べる物のうち一番近い物の上に「[E] 調べる」など。入力停止中は出さない）。IInteractPrompt を持つ物（ItemInteraction〔調べたとき・実行できるルールがあるときだけ・文は Inspector〕、GimmickInteractable〔使えるキャラだけ・`CanPrompt`〕、NotePickup「読む」、TimpaniPeg「締める」）はその判断、持たない物は種類ごとの文（ItemPickup「拾う」・SavePoint「セーブ」・Dialogue／DefeatedTalk「話す」・Treasure「開ける」・KeyItem）で、Intaractable の player をリフレクションで見る |
| `Manager/InventoryManager.cs` | 所持品（シングルトン、シーンをまたぐ、セーブ） |
| `Key/KeyItem.cs`、`Treasure/Treasure.cs` | 古い鍵・宝箱（NoahData.hasKey を使う）。ItemInteraction で同じことができる | △（シーンで使用中） |
| `CheckPoint/CheckPoint.cs` | 触れると再開する位置を記録して Commit + Write（Write はオートセーブ枠へ） | ○（**どのシーンにも置かれていない**） |
| `Scene/SceneChange.cs`、`Scene/SceneFader.cs` | 暗転つきシーン遷移（SceneFader は自動生成・シーンをまたぐ。`FadeThrough`：シーンを変えずに暗転→処理→明転） | ◎ |
| `UI/ItemMenu/ItemMenuUI.cs`、`ItemSlotUI.cs` | アイテム画面（Resources から自動生成）。メニュー画面から `OpenFromMenu` で開く（GameMenu がないときだけ Tab で直接開く） | ◎ |
| `UI/Life/LifeUIController.cs` | ハートの表示（Stage1_1・Stage1_2 の `Canvas/LifeUI` は画面の左上に固定。11-13）。`PlayerCharacters.PartyHealth`（一緒＝シグルの体力、離れる＝操作中のキャラ）を見る。最大の体力の数だけハートを出す（足りなければ複製）。色で区別（一緒・ノア・シグル） | ◎ |

### 演出（音・エフェクト・アニメ・明るさ）
| フォルダ | 内容 | 手順書 |
|---|---|---|
| `Bgm/` | `BgmManager`（クロスフェード、エフェクト）、`SceneBgm`（条件つきルール）、`Conditions/`（Area / Flag / Health）、`Effects/`（進行度で音を変える） | なし |
| `Se/` | `SeManager`（SeData で再生、ループ、追従）、`SePlayer`、`FootstepSe`、`HealthSe`、`SeEventAction`、🛠`SeDataCreator` | なし |
| `Effect/` | `EffectManager`、`EffectData`、`EffectTrigger`（コードなしで出す）、`EffectEventAction`、🛠エディタ一式 | `Effect/README_エフェクトの使い方.md` |
| `SpriteAnimation/` | `SpriteAnimator` + `SpriteAnimData`（新方式）、`CharacterAnimLink`（移動・体力で自動切り替え）、`AnimEventAction`、🛠エディタ一式。`SpriteAnimation.cs`（Animator 版）は古い | `SpriteAnimation/README_アニメーションの使い方.md` |
| `StageDarkness/` | `StageDarkness`（ステージを暗くしてプレイヤーの周りだけ明るく）、`LightSource` | なし |

### プランナー用ツール・その他
| ファイル | 役割 |
|---|---|
| 🛠`Common/Editor/`（SceneChecker / SceneCheckWindow / DebugWindow / SceneLookup / PickerDrawers）、`Common/Attributes/PickerAttributes.cs` | ③④⑤（デバッグウィンドウに「イベント（その場で試す）」〔EventSequence を名前で絞り込んで実行・止める、11-13〕、「敵」：止める・経路の表示・倒す・**攻撃を出させる**・スポナー／グループの状態。シーンチェックに敵・敵の通行設定エリア・倒した記録のキーの重なり・グループ・ウェーブ・スポナー） |
| `Common/Attributes/LabelAttribute.cs`（`[SerializeReference]` の部品はクラス名を変えるとデータが消える。変えるときは `[MovedFrom]`）、🛠`Common/Editor/LabelDrawer.cs` | `[Label("日本語")]`：Inspector の項目名（InspectorName は enum の値にしか効かないため）。`[Label("名前", min)]` / `[Label("名前", min, max)]` で最小値・スライダー。リストには付けない |
| `Common/GameCondition.cs` | 条件のまとめ（フラグ・アイテム〔既存の FlagCondition / ItemCondition をそのまま中に持つ〕・操作中のキャラ・パーティーの体力・プレイヤーがいる範囲・**仕掛けの状態** `gimmicks`・**2人の状態** `party`）。`IsMet` / `AllMet` / `AnyMet` |
| `Gimmick/GimmickState.cs` | **仕掛けの状態**（謎解き段階1）。**時間で戻る**（段階2：戻るまでの秒数・残り時間の音・戻ったときの音、`RemainingTime`。InputBlock の間は進まない、入り直したら最初から）。ON/OFF か 0〜n の段階、最後の次は最初へ、変わったときのイベント、`Changed` イベント、セーブ（`Gimmick/<シーン>/<ID か階層パス>`、固定は `/Locked`）、`Set`（固定中は force のときだけ）/`TurnOn`/`TurnOff`/`Toggle`/`Advance`/`ResetValue`/`SetLocked`、`All` |
| `Gimmick/GimmickCondition.cs` | 仕掛けの状態の条件（ON・OFF・値・以上・以下）。`GameCondition.gimmicks` の要素 |
| `Gimmick/GimmickReaction.cs` | **条件で動く物**（扉・橋）。**Moves**（段階2：置いた位置から offset だけ、動く時間をかけてずらす。Rigidbody2D があれば position も）。条件で Show/Hide When Met を毎フレーム決める、満たした／満たさなくなったときのイベント、一度満たしたらずっと（`Reaction/<シーン>/<ID>`）。条件が空なら満たさない |
| `Gimmick/PuzzleCheck.cs` | **答え合わせ**。入力（GimmickState）を 全部 ON／決めた順番（間違えたら戻す。読み込み時は最初から）／決めた組み合わせ。いつ確かめるか（変わるたび／イベントで）。正解で自分の GimmickState を ON・固定、入力を固定 |
| `Gimmick/GimmickSwitch.cs`、`CharacterRestriction.cs` | **調べるスイッチ**（Intaractable）。切り替え・ON・OFF・次へ、使えるキャラ（両方・ノアだけ・シグルだけ）、使える条件、使えないとき・条件を満たさないとき・固定のときのイベント。範囲の中の操作中のキャラだけが調べられる |
| `Gimmick/FloorSwitch.cs` | **床スイッチ**（謎解き段階2）。乗っている間だけ ON は、読み込み後 2 回目の FixedUpdate で、セーブの値でなく今乗っているかに合わせる（固定中は変えない）。Is Trigger の範囲に乗った物（ノア・シグル・押せる箱・敵。中心が範囲の中）を FixedUpdate で数え、必要な数以上で押される。乗っている間だけ／一度で ON／乗るたびに切り替え |
| `Gimmick/PushBlock.cs`、`GimmickHole.cs`、`PushBlockResetEventAction.cs` | **押せる箱と穴**。箱：Kinematic、操作中のキャラが上下左右に「押し始めるまで」歩き続けると 1 マス（行き先を OverlapBox で確かめ、壁・箱・キャラ・敵があれば動かない。埋まっていない穴なら落ちて埋める）。押せるキャラ、位置と落ちたかをセーブ（`Block/<シーン>/<ID>`）、`ResetToHome`・`All`。穴：当たり判定で通れない、埋まると消える（GimmickState の ON）。イベントの部品「押せる箱を元に戻す」 |
| `Gimmick/ItemSocket.cs` | **置き台**。アイテムをはめる（消費する／見せるだけ）、取り外せる、はめた物の絵（大きさを合わせる）、使えるキャラ |
| `Gimmick/GimmickVisual.cs` | **状態で見た目を変える**。状態の値ごとに絵・色・表示する物・有効にする部品（燭台の火と LightSource など） |
| `Party/PartyRejoinArea.cs` | 合流の範囲（2人がそろうと一緒に戻す・イベント・待て／ついてこいを使えるように・1回だけ `Rejoin/<シーン>/<ID>`） |
| `Party/PartyWarpEventAction.cs` | イベントの部品「キャラを移す」（ノア・シグル・操作中・相棒を移す場所へ。暗転つき） |
| `Party/PartyPersistence.cs` | 2人の状態のセーブ（`Save.BeforeCommit` で `Party/Mode`・`Current`・`Scene`・`PartnerX/Y`・`WaitLocked`・**`NoahHP`・`SigleHP`**）と復元（`PlayerPartySpawner` から。状態は同じシーンのときだけ、体力はどのシーンでも。体力の記録が 0 以下・なしなら満タン） |
| `Party/PartyRoute.cs` | 相棒が合流する道（まっすぐ CircleCast で行けるか。通り抜けられる壁は無視 → ナビメッシュの回り道 → なければ来られない） |
| `Party/CharacterBarrier.cs` | キャラによって通れる壁（通れるキャラの当たり判定と IgnoreCollision。0.5 秒ごとにかけ直す）。一緒のときに通れるキャラが入ると相棒を待機に。通れないキャラが触れると目印 |
| `Party/BreakableWall.cs` | 壊せる壁（DamageReceiver。攻撃したキャラ・種類・ゲージ判定・回数。壊れると当たり判定を切り、絵を消す／差し替える。GimmickState の ON でセーブ） |
| `Party/ConditionalVisible.cs` | 条件で見える物（GameCondition。SpriteRenderer・Tilemap の濃さをふわっと変える、当たり判定を見えている間だけ／見えていない間だけ） |
| `Party/LiftPoint.cs` | 持ち上げる場所（GimmickInteractable。移るキャラ・行き先・相棒の手伝いが要るか。弧を描いて移り、手伝った相棒は待機、移ったキャラを操作） |
| `Party/PartyHint.cs`、`PartyHintView.cs`、`PartyHintSettings.cs` | 相棒の目印（使えないキャラで近づくと「シグル」「ノア」の札。仕掛けが毎フレーム `Request`、`PartyHintView` が札を出し消し。設定は `Resources/PartyHints.asset`） |
| `Party/Bark/`（`BarkData`・`Bark`・`BarkView`・`BarkWatcher`・`BarkSettings`・`BarkEventAction`・`BarkArea`） | **相棒の一言**（謎解き段階6）。BarkData（SO、`Resources/Barks/`）：使う・きっかけ `BarkTrigger`（18 種。番号は末尾に足す）・秒数・体力の割合・倒した敵・条件（GameCondition）・優先度・確率・回数（何度でも／シーンで1回／ゲームで1回＝`Bark/<名前>` でセーブ）・次に出せるまで・出すまでの間・候補（`BarkVariant`＝行 `BarkLine` のリスト。2行以上で掛け合い。話すキャラ `BarkSpeaker`）。`Bark`（static）：`Fire(trigger, BarkContext)`（条件・待ち時間・確率で絞り、優先度の高いものを選ぶ。出している一言より高ければ割り込む）、`Play`・`PlayLines`（イベント・範囲）、`CanPlay`・`WhyNot`・`ResetHistory`・`Muted`・`Log`・`Reload`。`BarkView`：吹き出し（TMP のワールド文字・1つだけ・掛け合いは順番・画面の外は画面の下に名前つき・会話/メニュー/謎解きの画面/暗転でやめる。自動の一言は InputBlock の間は 4 秒まで待つ）。`BarkWatcher`（シーンごとに自動生成）：2人の状態（待たせた・呼び戻した・離れ離れ・合流。着いて 1.5 秒は出さない）・切り替え・ダメージ／体力・`Enemy.Defeated`・待たせている時間・止まっている時間・シーンに着いた。ほかのきっかけは GimmickInteractable（`Refuse`・`RefuseCharacter`）・PushBlock・PartyHintView（目印が出た）・PlayerSwitchManager（来られない）・PuzzleCheck／CodeLock（正解・間違い）から。BarkSettings（`Resources/BarkSettings.asset`）：一言どうしの間・秒数・見た目。BarkEventAction：イベントの部品「一言」（データか文・終わるまで待つ）。BarkArea：一言の範囲 |
| 🛠`Party/Bark/Editor/`（`BarkMenu`・`BarkListWindow`・`BarkCsv`） | Hierarchy 右クリック > 仕掛け > 2人 > 一言の範囲、Tools > 仕掛け > 相棒の一言 > 一言の一覧（表で編集・探す・きっかけで絞る。再生中は出せない理由・▶ で出す・出さない・記録を消す・最近出した一言）／新しい一言を作る／一言の設定を作る／CSVから取り込む（`Resources/Barks/<CSV名>/`、同じ名前は上書き）／CSVのひな形／すべてCSVに書き出す。CSV の読み書きは `DialogueCsv` の internal の関数を使う |
| 🛠`Party/Editor/PartyMenu.cs` | Hierarchy 右クリック > 仕掛け > 2人 > 狭い通路／壊せる壁／条件で見える物／持ち上げる場所、Tools > 仕掛け > 相棒の目印の設定を作る |
| `Gimmick/UI/PuzzleUI.cs`、`PuzzleUISettings.cs` | **謎解きの画面**（段階5。必要なときに作る uGUI〔DontDestroyOnLoad、並び順 550〕）。`OpenCode`（番号錠：←→ 桁・↑↓ 変える）、`OpenChoice`（リストから選ぶ。絵つき）、`OpenText`（文章。TMP のページ）。`IsOpen` / `IsBusy`（InputBlock）。見た目は `Resources/PuzzleUI.asset` |
| `Gimmick/CodeLock.cs` | **番号錠**（使える文字・答え・前の入力を覚える。正解で GimmickState を ON・固定） |
| `Gimmick/Note/`（`NoteData`・`NoteBook`・`NotePickup`・`NoteEventAction`） | **メモ**。NoteData は `Resources/Notes/`。持っているかは `Note/<ID>`。`NoteBook.OpenList`（メニューの「メモ」：一覧 → 読む → 一覧へ戻る） |
| `Gimmick/ExamineSequence.cs` | **調べるたびに変わる**（N 回目のイベント・最後まで行ったら・回数を GimmickState に出す・`Examine/<シーン>/<ID>`） |
| `Gimmick/Death/`（`InstantDeath`・`DeathTrap`・`DeathEventAction`・`RetryPoint`） | **即死とやり直し**。RetryPoint：入ると Save.Commit・やり直す位置・一時フラグの写し（static。シーンをまたがない）。InstantDeath：演出を待って、やり直す＝仮置きを捨てる・フラグと所持品を戻す・同じシーンを読み直してやり直す位置から（記録がなければセーブの再開位置）／ゲームオーバー＝今の体力を 0 に |
| `Gimmick/GimmickInteractable.cs` | 調べる仕掛けの共通の土台（Intaractable を継承。範囲の中の操作中のキャラだけ、相棒が出たときに操作中のキャラを入れ直す、断るときの反応）。GimmickSwitch・ItemSocket が継承 |
| `Gimmick/GimmickEventAction.cs` | イベントの部品「仕掛けを操作」（ON・OFF・切り替え・次へ・値・戻す・固定・固定を外す・答え合わせ・入力を戻す。固定されていても変える） |
| 🛠`Gimmick/Editor/PuzzleMenu.cs` | Hierarchy 右クリック > 仕掛け > 謎解き > 番号錠／メモ／調べるたびに変わる／即死の罠／やり直す位置、Tools > 仕掛け > 謎解きの画面の設定を作る、Tools > メニュー画面 > メニューに「メモ」を足す（プレハブを作り直さずにボタンを足す） |
| 🛠`Gimmick/Editor/GimmickListWindow.cs` | **仕掛けの一覧**（Tools > 仕掛け > 仕掛けの一覧。段階7）：シーンの GimmickState ごとに種類・使えるキャラ（部品の user / passable / breaker）・段階・最初／今の値・つながっている先（`GimmickWiring.Collect`）。つながっていない物はオレンジ（壊せる壁・穴は除く）。2人の仕掛け・演出の部品の一覧。再生中は切り替え・戻す |
| 🛠`Gimmick/Editor/GimmickSampleBuilder.cs`、`Gimmick/GimmickSampleMemo.cs` | **サンプルの部屋**（Tools > 仕掛け > サンプルの部屋を作る。段階7）：`Assets/Prefab/Gimmicks/Sample/` に壁つきの部屋のプレハブ 6 つ（押せる箱と床スイッチ・燭台の順番・番号錠とメモ〔`Resources/Notes/Note_Sample_金庫の番号`〕・同時押し・狭い通路とお互いの扉・像の部屋）。GimmickMenu の `BuildSwitch` / `BuildDoor` / `BuildPuzzle` / `BuildFloor` / `BuildBlock` / `BuildBrazier` を使う。GimmickSampleMemo は部屋の説明（Inspector 用・ゲームでは何もしない） |
| 🛠`Gimmick/Editor/GimmickWiring.cs`、`GimmickMenu.cs` | 配線の表示（シーンの全 MonoBehaviour の GimmickState への参照を矢印で。選んだ物につながる線だけ／Tools > 仕掛け > 配線をいつも全部表示）。Hierarchy 右クリック > 仕掛け > …（選んだスイッチを条件・入力に入れる。`FirstCallThisClick` で複数選択でも 1 回） |
| 🛠`Flag/Editor/`、`Item/Interaction/Editor/`、`UI/ItemMenu/Editor/`、`Event/EventProcess/Editor/` | ひな形・一覧・Inspector。（11-13）`EventSequenceEditor`＝「流れ」（部品を番号・日本語名・一言要約で並べ、▶ でその場で編集・↑↓・×・＋ 部品を追加〔分類メニュー〕）、再生中の「今すぐ実行」「止める」・実行中の部品を緑（`EventSequence.CurrentAction`）。`EventActionCatalog`＝部品の日本語名・分類・要約（新しい EventAction は表に1行足す） |
| `Singleton/Singleton.cs` | `Singleton<T>`（`persistAcrossScenes` でシーンをまたぐ） |
| `Fps/Fps.cs` | 60fps 固定（各シーンに配置） |
| `Debug/CircleDebugRenderer.cs` | 円のデバッグ表示 |
| `Debug/PerfMonitor.cs` | （11-13）性能の計測。エディタと Development Build だけで自動で作られる（シーンをまたぐ）。画面右上に FPS・処理時間（PlayerLoop − 60fps の待ち）・スクリプトの時間・メモリ・1フレームの確保・ドローコール（F3 で切り替え、PlayerPrefs `Debug_PerfOverlay`）。`BeginRecord`／`EndRecord`（毎フレームの値を記録して中央値・p95・スパイクなどを1行に）・`Snapshot`／`CollectNow`（メモリと読み込まれている物の数）。Claude の `unity-playtest/scripts/perf.ps1` が使い、結果は `PerfLog/perf.csv`・`leak.csv`。**エディタでは GC の確保にエディタの分も入る**（ゲームの分だけは perf.ps1 がプロファイラの PlayerLoop で測る） |
| `StateMachine/`（StateMachine / IState / State）、`ComponentBase/ComponentBase.cs` | △ どこからも使われていない |
| `kari/player/`（kari_player / kari_player2 / kari_player_test） | △ 仮プレイヤー。シーンでは未使用（StageDarkness がプレイヤー探しの予備に kari_player_test を参照） |

---

## 4. 問題点（未修正）

優先度：**高** = 遊ぶと気づく不具合 ／ **中** = 条件次第で不具合・提出前に直したい ／ **低** = 整理・品質

**2026-11-08 に全項目を再確認済み**（コードの検索、エディタでのシーンチェック〔Build Settings の4シーン〕・参照の空き・Missing Script・プレハブの構成の確認、再コンパイル）。再生しての確認はしていない。🆕 = この再確認で新しく見つけたもの。
- 再コンパイル：エラー・警告なし。Missing Script：4シーン・全プレハブともなし。

### 不具合・不具合の可能性
| 優先度 | 場所 | 内容 |
|---|---|---|
| 中 | 🆕 `ProjectSettings/QualitySettings.asset` | エディタの今の品質レベルが「Very Low」（テクスチャが半分の解像度〔Texture Quality = Half〕）。Windows のビルドの既定は「Ultra」。エディタで見た目・重さを確かめると、完成品と条件が違う（perf.ps1 の基準の値もこの条件）。使う品質レベルを決めて、使わないレベルを消すとよい（Project Auditor の PAS0018） |
| **高** | `Prefab/Players/Player_Noah.prefab`、`Player_Sigle.prefab` | **FootstepSe が付いていない**ため足音が鳴らない。FootstepSe は Stage1_2 のルートの `Player`（Transform と FootstepSe だけの動かないオブジェクト）に付いている。11-06 の SE 手順書は「プレハブの FootstepSe に設定」と書いていた |
| 中 | `Bgm/Effects/BgmProgress.cs` | 進行度（static）が `SubsystemRegistration` でリセットされない（ドメインリロード無効のエディタで前回の値が残る）。セーブもされず、はじめから・つづきからでも戻らない |
| 中 | `StageDarkness/StageDarkness.cs` FindTarget | 明かりの追従先を `FindWithTag("Player")` の最初の1人で決めるため、キャラ切り替えで操作キャラに付いていかないことがある |
| 中 | **古い作りの敵**の移動（`Behavior/Enemy/Behavior Graph.asset` の SetPositionToTarget、`Prefab/Enemies/Enemy.prefab`） | 「6秒撃つ → 3秒かけてプレイヤーの位置へ移動」の繰り返し。移動は transform を直接動かし、Rigidbody2D は Static なので**壁をすり抜け、プレイヤーと重なる**。会話・イベント中も移動は続く（撃つのは止まるようにした）。同じ動きの新しい敵（サンプル「撃つ」）はできたが、**Stage1_2 のスポナーはまだ古い Enemy.prefab**（置き換えはシーンの変更が要るので未実施） |
| 低 | 🆕 敵の攻撃のノックバック | 攻撃が当たってもプレイヤーは押し戻されない（プレイヤーの移動が MovePosition で毎フレーム位置を決めるため、速度を足しても消える）。必要ならプレイヤー側に「押し戻される」処理を足す |
| 低 | `AttackData` の「弾を撃つ」 | 速さ・ダメージ・すり抜けるキャラ・弾消しで消せるかは攻撃ごとに変えられるようになった（段階3）。**弾の見た目**はシーンの EnemyBulletPool の弾のプレハブのまま（攻撃ごとに変えるには、プールを分けるなどが必要）。「当たる相手：敵」も弾には効かない（敵の弾は Enemies を通り抜ける） |
| 低 | 🆕 装甲の部位（`EnemyPart`） | 装甲の当たり判定が本体の当たり判定を覆っていないと、弾がはみ出た本体に当たって装甲を通らずに効く（範囲攻撃は部位を先に当てるので大丈夫）。手順書に「覆う大きさにする」と書いた |
| 低 | 置いた爆弾（`PlacedBomb`） | 「触れたら爆発」はシグル（操作していない側）が触れても爆発する。ついてくるシグルが踏んで、すぐ爆発することがある（確認中に起きた） |
| 低 | 敵のスポナー（`Spawner/Enemy/EnemySpawner.cs`） | 1つのスポナーで同時に出せるのは1体だけ。Stage1_1 のスポナーは非アクティブ（敵が出ない）。（スポナーなしでも敵は動くようになった） |
| 低 | 🆕 `Enemy/Enemy.cs` の体当たり | プレイヤーに無敵時間がないため、体当たりのダメージは敵ごとの間隔（既定 1 秒）だけで決まる。敵が多いと連続で受ける。プレイヤー側の無敵時間を足すかは未定 |

| 低 | 🆕 Physics 2D の衝突設定 | Bullet と Bullet の衝突が無効になっているので、弾どうしはぶつからない（`BulletBase` などのコメント「弾どうしは今まで通り当たる」と食い違う） |
| 低 | **古い作りの敵**を倒したとき（`Enemy/EnemyDead.cs`） | 消えるだけで、イベント・フラグ・ドロップ・セーブにつなげられない（新しい作りの敵は段階6で対応済み） |
| 低 | 🆕 落ちたアイテム（`Enemy/Defeat/DroppedItem.cs`） | セーブしないので、拾わずにシーンを出ると消える（手順書に「大事な物はすぐ持ち物に入れる／イベントで渡す」と書いた） |
| 低 | 🆕 「仲間を呼ぶ」・ウェーブで新しく出した敵（`Enemy.Spawn`） | 階層のパスが毎回同じでないため、倒したことのセーブ・グループのメンバーには使えない（ウェーブの「新しく出す敵」はセーブしない） |
| 低 | 🆕 `Prefab/Enemies/Enemy.prefab` | 見た目が仮（シグルの画像 `Sigle-front_0`）。SpriteAnimator がないので死亡アニメ（Dead）は流れない。`Behavior Graph.asset` の EnemyShot ノードに、コードから消えた `shootInterval` の値が残っている（害はない） |
| 低 | `CheckPoint/CheckPoint.cs` | どのシーンにも置かれていない（自動セーブ用。手動セーブはセーブポイント）。イベントの区切りのオートセーブは部品「オートセーブ」で置ける |
| 中 | 🆕 ゲームオーバーのリトライ（11-13） | リトライは「シーンに着いた位置」から始まるが、進み具合はメモリに残る。入口に戻れなくなるイベント（Stage1_2 のガレキなど）のあとで倒れると、入口側から始まって進めなくなるおそれがある（Stage1_2 は今は倒れる場面がないので起きない）。そうした場面を作るときは、リトライの位置もオートセーブの位置にするなどの対応が要る |
| 低 | 🆕 キーの案内（`InteractPromptView`） | 調べる範囲が小さい物は、範囲の端に触れたときだけ出る（Stage1_2 のランタンは円の半径 1、ノアの体の当たり判定が届く所でだけ出る）。範囲を広げるかはシーンごとに調整 |
| 低 | Stage1_1 / Stage1_2 の `SavePoint` | 位置と見た目は仮（入口の右・青い丸）。Stage1_1 は入口がマップ下端にあり、再開位置を下にずらすとマップ外になるので左にずらしている |
| 低 | `UI/GameMenu/GameMenu.cs` | ゲームオーバー画面・タイトルでは開かない（プレイヤーがいるシーンだけ）。ゲームパッド操作は未対応（キーボード・マウスのみ） |
| 低 | `Scenes/Stage1.unity`、`Stage2.unity`、`SampleScene.unity` | Build Settings から外した古いシーン（ファイルは残っている）。Stage1 / Stage2 は開くと「Creating missing Rigidbody2D / PlayerMovement」が出る。不要なら削除してよい |
| 低 | `Title/TitleMenu.cs` | 再開場所のない旧セーブ（引き継いだスロット1）は「場所不明」と表示され、つづきからは最初のステージ（First Scene）から始まる |
| 低 | 会話フォント `NotoSansJP-Regular SDF` | Dynamic + Multi Atlas にしたので文字は足りるようになったが、エディタで再生すると新しい文字がアセットに追加され、フォントアセットに差分が出ることがある（コミットしてよい） |
| 低 | `Health` を使うクラス（`NoahDead`、`EnemyDead`） | Awake で Health がなければ Log だけ出し、OnEnable で NullReferenceException |
| 低 | `Spawner/Player/PlayerPartySpawner.cs` Awake | 参照が足りなくてもエラーを出すだけで、Start に進んで NullReferenceException |
| 低 | `Behavior/Enemy/EnemyShotAction.cs` | `EnemyShooter` がないと Log のあと OnUpdate で NullReferenceException |
| 低 | `Spawner/Player/PlayerPartySpawner.cs` の `Sigle Spawn Point`（既定の出現位置） | 使われていない（Sigle は既定ではノアと同じ位置に出る）。Stage1_2 では Noah の既定位置から 5.8 離れた場所が入っている。出現位置ごとの Sigle の位置は `PlayerSpawnPoint` で設定できる |
| 低 | `Player/PlayerInput.cs` Update | プレイヤー生成前（最初の数フレーム）に「CurrentPlayerがnullです」を毎フレーム LogError |
| 低 | `Prefab/Players/Player_Sigle.prefab` の SigleShooter | Shot Se が空（射撃の音が鳴らない。撃ったときのエフェクトは仮で Fx_Sample_Muzzle）。音が決まったら入れる |
| 低 | `CharaBase/CharacterSorting.cs` | `SetPlayer` が Start より後に呼ばれると `spriteRenderer` が null のまま |
| 低 | `Player/PlayerFollower.cs`、`PlayerHistory.cs` | 追従の距離が「フレーム数」なので、fps が変わると距離が変わる。`RemoveAt(0)` を毎フレーム（1000件のリスト） |
| 低 | `Player/PlayerFollower.cs` | Rigidbody2D を使っているのに transform.position を直接書き換えている（物理と位置がずれることがある。PlayerMapLimit は 11-13 に物理の位置も合わせるようにした） |
| 低 | `StateMachine/StateMachine.cs` Update | 状態が変わったフレームしか `Current.Update` を呼ばない（括弧の位置の誤り）。今は未使用 |
| 低 | `Data/NoahData.cs` / `NoahInventory.GetKey(null)` | `hasKey` はセーブされない。ItemData なしで鍵を拾うと、シーン遷移後に鍵が消える |
| 低 | 🆕 `Gimmick/Note/NoteData.cs:41` | 警告 CS0414「`memo` は代入されているが使われていない」（2026-11-12 の再コンパイルで確認。メモ欄として Inspector 用なら `#pragma warning disable 0414` か、どこかで読む） |
| 低 | 🆕 毎フレームの小さな確保（11-13 の計測で確認。ティンパニ戦で合計 約 1.3〜1.7KB/フレーム、ほぼ全フレーム） | 多い順に `GetComponent` が null を返す所（エディタだけで確保される `GetComponentNullErrorMessage`。`TryGetComponent` にすると消える）、`Enemy.LateUpdate`、`InteractPromptView.LateUpdate`、`PlayerMapLimit.LateUpdate`、`BattleArea.Update`、`EnemyBrain` のコルーチン。60fps は保てていて（処理 約 2ms）今は困っていないが、GC の引っかかりの元になる。直すかは計測しながら（性能の段階4） |
| 低 | 🆕 `Fps/Fps.cs`、`ObjectBase/ObjectBase.cs`、`CharaBase/CharaBase.cs`（鈴木さん） | 中身のない `Update()`・`Start()` が残っている。ObjectBase・CharaBase は基底クラスなので、自分で Update を持たない子（CharacterSorting・NoahInventory・NoahDead など）でも毎フレーム空の Update が呼ばれる（perf-review の US01。1 回は小さいがキャラの数だけ増える） |
| 中 | 🆕 `BGM/PMH_Atmos_HospitalHall_01.wav`（47MB）・`BGM/BGM_Stage1.mp3`（4.7MB） | Load Type が Decompress On Load（読み込み時に展開してまるごとメモリに乗る）。BGM・長い環境音は Streaming が普通（perf-review の CT08）。音の設定なのでプランナー・サウンド担当と相談して変える |
| 低 | 🆕 `Images/UI/UI_TitleImage.PNG`、`Images/Player/playermotion_idle(192).png` の .meta | 機種別の設定に最大サイズ 4096／8192 がある（既定は 2048。perf-review の AS01）。PC で効いているかを確かめて、要らなければ下げる |
| 低 | 🆕 ドメインリロードの点検（Project Auditor の UDR、11-13） | ゲームのコードも解析できるようになって、static をリセットしていない疑いは 40 件（UDR0005 の 12 件を外したあと。主なもの：`Camera/CameraShake`・`Enemy/Battle/BattleArea` など（UDR0002 11 件）、`Bgm/Effects/BgmProgress`（既知）・`Bgm/BgmManager`・`Party/PartyHintView`・`Scene/SceneFader` など（UDR0001 9 件）、`Flag/FlagSwitch.cs:56` の static なイベントの登録の解除がない（UDR0004）、`Save/GameSession`・`UI/GameMenu` に RuntimeInitializeOnLoadMethod が複数（UDR0005。このプロジェクトの書き方なので auditor-ignore.md で外した））。私（uh）の `InteractPrompt`・`PartyHintView` も入っている。機械の判定なので、OnDestroy で null に戻している物などは誤検出。1 件ずつ確かめる（一覧は `PerfLog/auditor_issues.tsv` の reload） |
| 低 | 🆕 Project Auditor のルールのパッケージ 1.0.3 | ドメインリロードの点検（UDR）がふだんのコンパイルにも入り、コンソールに警告が約 60 件出る（エディタのコードの分も含む）。新しい版（2.0.0 以降）はふだんのコンパイルから外したが、Unity 6.5 のエディタの Project Auditor 本体と合わずコンパイルエラーになる（6.6 以降向け） |
| 低 | 🆕 `ProjectSettings/Physics2DSettings.asset` | 2D の衝突の組み合わせ（Layer Collision Matrix）がほぼ全部オンのまま（perf-review の PH10）。当たる必要のないレイヤーの組を切ると判定が軽くなる。今は 60fps に余裕があるので急がない |
| 低 | 🆕 `Title.unity` の `System`（SceneBgm） | Default Clip が空なので、タイトルで BGM が鳴らない（ステージの曲はタイトルで止まる）。曲が決まったら入れる |

### 提出前に整理したいもの
| 場所 | 内容 |
|---|---|
| `Behavior/Enemy/AbcdefuckAction.cs` | **クラス名とノードの文言に不適切な語が含まれている**（卒業制作として提出・公開する前に削除推奨。どのグラフでも未使用） |
| `Behavior/Enemy/SampleAction.cs` | 練習用ノード（未使用。ブラックボードの値を書き換えて戻さない） |
| `Intaract/Intaractable.cs:58` | Debug.Log「調べられるまする」（誤字、範囲に入るたびに出る） |
| 不要な using | `PlayerFollower` の `using Unity.AppUI.Redux` は 11-13 にコメントアウト済み（エディタだけのパッケージで、ビルドのコンパイルが失敗していた。成田さんに確認のうえ行ごと消してよい）、`PlayerInput` / `NoahDead`（Unity.VisualScripting）、`AbcdefuckAction`（RuleTile） |
| 使われていないクラス | `StateMachine/*`、`ComponentBase`、`FocusPlayer`、`SpriteAnimation`（Animator 版）、`kari/player/*`、`BgmProgressDebug`、`SePlayer`、`HealthSe`（シーン・プレハブに未配置） |
| `Player_Noah/NoahBulletDeleteAreaController.cs` | ヘッダーの説明が「ノアのアニメーションクラス」になっている |
| `Attack/AreaAttack.cs` | 命中のたびに Debug.Log。Health がない相手（壁など）に当たるたびにも Log |
| `Bullet/BulletDeleteArea.cs` | `deleteRadius` は Gizmo の表示にしか使っておらず、実際の範囲は Collider の大きさ |
| `Debug.Log` 全般 | 本番ビルド向けに整理が必要（エディタ拡張を除いて約40か所。LogWarning / LogError は別） |
| `Assets/_Recovery/0.unity` | 復旧シーンの残り |
| Visual Scripting | 再生を始めるたびに「90 node options failed to load」（Regenerate Nodes）が出る（11-08 の段階1の確認で再現）。使っていなければ害はない |

### シーンの設定
| 場所 | 内容 |
|---|---|
| Stage1_1 / Stage1_2 / Stage_1_3 / SampleScene の `MapEntity/OB_TRbox` | 宝箱の Id が空（シーンチェックでエラー。ほかの宝箱と開いた記録が混ざる） |
| 🆕 Stage_1_3 の `Event`（Stage1_1 のコピーの残り） | EventZone の中身が空・Is Trigger の Collider2D がない（シーンチェックでエラー。何も起きないので害はない。不要なら消してよい） |
| 同じ3シーンの `Event`（ルート） | 中身が空の EventZone / EventSequence。Is Trigger の Collider2D もない（シーンチェックでエラー・注意が出るが、動作上の害はない。不要なら消す） |
| 🆕 Stage1_1 の `Event/DialogueEventZone` | 入ると古い会話（非表示の `OB_Text` の Dialogue）を出す EventZone が、マップの右側（-106.8, 17.3）に残っている。OP とは別。要らなければ消す |
| 🆕 Stage_1_3 の音楽生物・弾・アイテム | 絵は仮（紫の丸い生き物・調律後の水色の絵・丸い弾・ハート・青い菱形）。音は空欄。左の出口の先（台本 F1_13 の階段・二層）のシーンはまだない |
| 🆕 Stage_1_3（ティンパニ部屋。作りかけ） | Stage1_1 のコピーが元なので、使わない物（`soto_*`・`OB_*`・`Event`・古い `EnemySpawner` など）が非表示で残っている。`SavePoint` は部屋の外にあったので非表示にした。`Grid/room_timpani_yuka_0`（床だけの絵）が `soto_yuka`（部屋全体の絵）の下に少しずれて重なっている。左の出入口は歩ける範囲の外 |
| 🆕 Stage1_2 の追跡の敵（`Stage1_2_イベント/塔の化け物`） | 見た目が仮（敵の共通プレハブの絵＝シグルに似た絵）。敵の画像ができたら `EnemyData_Stage1_2_Chaser` の Sprite / Animation に入れる。ランタンも仮の丸（`見た目（仮）`） |
| 🆕 シグルのアニメのデータ（`Anim_Sample_Sigle`） | 「Walk」の動きがないので、歩いても立ち姿のまま動く（ついてくるとき・Stage1_2 の助けの場面で歩いてくるとき）。ノアのデータには Walk がある |
| 🆕 Stage1_2 のガレキ・シグルの攻撃 | 仮の絵（四角を並べたガレキ、プレイヤーの弾の絵）。絵ができたら差し替える |
| 🆕 Stage1_2 の演出の音 | 「キーーーン」「ドスン」「バン！」の SeEventAction は空欄（音のファイルがない）。音ができたら入れる |
| 🆕 Stage1_2 で非表示にした物 | アイテムのサンプル一式（`ItemInteractionSample`）・`Itm_Key`・`OB_TRbox`・古い `EnemySpawner`・`Event/DialogueEventZone` は話の邪魔なので非表示（消してはいない）。壁の明かり `Light`〜`Light (2)` は最初非表示でランタンを拾うと点く。StageDarkness の値（0.813・半径 2 → 0.97・半径 1）とカメラの背景色（灰 → 黒）も変えた |
| Stage1_2 | 会話 UI がシーンの Canvas に直接組まれている（他のシーンは DialogueSystem.prefab）。揃えるなら置き換え |
| 🆕 Stage1_2 のルートの `Player` | Transform と FootstepSe だけの動かないオブジェクト（上の「FootstepSe が付いていない」を参照）。プレハブに移したら消す |
| Stage1_1 / Stage1_2 の `OtherSystem/SceneChange`、SceneGameOver の SceneChange | 遷移先が空（シーンチェックで「参考」）。死亡時・リトライでコードから `ChangeScene` を呼ぶためのもので、問題なし |

### 設計上の注意（不具合ではない）
- ItemPickup / ItemInteraction / KeyItem は、ID が空だと階層パスでセーブする → 名前や親を変えると記録がリセットされる。
- `EventZone` の「1回だけ」はセーブされない（シーンに戻ると再発動）。
- 最後のセーブ以降に手に入れたものは、タイトルに戻る・終了すると失われる（セーブはシーンに着いたとき・チェックポイント）。
- 倒れたときは、その時点までの進行を確定してからゲームオーバーへ移る（倒れても拾ったものは残る）。
- 出口のそばの出現位置へワープすると、そのまま遷移する。
- **ノアとシグルの体どうしは当たらない**（すり抜ける。`PlayerSwitchManager.IgnorePartnerCollision` が `Physics2D.IgnoreCollision` を ApplyFollow のたびにかけ直す。2026-11-12）。プレイヤーは物理の速さを使わない（`PlayerMovement.FixedUpdate` が毎回 0 にしてから `MovePosition`。操作できなくなったら向きも消す）。プレイヤーに速さで動きを付ける処理（ノックバックなど）を作るときは、ここを変える必要がある。
- シグルの体の当たり判定は、プレハブ（Player_Sigle.prefab）では無効。ついてきている間だけプレハブのまま、操作中・待機中・別行動のときは `PlayerSwitchManager.ApplyBodyCollision` が有効にする（謎解き段階3）。プレハブの設定を変えるときはここも見る。

---

## 5. 解決済み（参考。2026-11-12 に CLAUDE.md から移した）
- （2026-11-13）ついてくる相棒の絵が震える（シグルが先頭のとき目立つ）→ ①マップの端：動く前に範囲内に収める（`CharacterMovement.LimitNextPosition` → `PlayerMapLimit.ClampToMap`）、LateUpdate の順番を `DefaultExecutionOrder` で固定（相棒が写す履歴が「外」と「戻した位置」を交互に記録していた）。②シグルのプレハブの Interpolate をオン（物理 50 回/秒・描画 60 回/秒で 6 フレームに 1 回止まっていた）。
- （2026-11-12）離れているときに切り替えると前のキャラが歩き続ける → `PlayerMovement.SetControlEnabled(false)` で向きと速さを消す。
- （2026-11-12）2人が押し合い、押された方が滑り続ける・一緒でシグルを操作するとついてくるノアとぶつかって押し出される → 2人の体はすり抜ける（`PlayerSwitchManager.IgnorePartnerCollision`）、プレイヤーは毎回速さを 0 に（`PlayerMovement.FixedUpdate`）。
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
