# コード構成と問題点（開発者・Claude 向け）

最終更新：2026-11-08。CLAUDE.md から読み込まれる。コードを変えたら、この表も更新すること。
ファイルの参照は `Assets/Script/` からの相対パス。行番号は目安。

---

## 1. アセットのフォルダ

| フォルダ | 中身 |
|---|---|
| `Assets/Scenes/` | **Build Settings：`Title` → `Stage1_1` ⇄ `Stage1_2`、`SceneGameOver`**。ファイルだけ残っているもの：`Stage1` / `Stage2`（**古い作り**：プレイヤーをシーンに直接置く）、`SampleScene`、`kari/`（仮の Start / Clear）、`Tilemap/` |
| `Assets/Prefab/` | `Players/`（Player_Noah / Player_Sigle）、`Enemies/`（**Enemy_Base**＝新しい作りの共通プレハブ、Enemy＝古い作り）、`Bullet/`、`UI/`（DialoguePanel、**DialogueSystem**） |
| `Assets/Resources/` | `Items/`（ItemData。**必ずここ**。`Items/Sample/` にサンプル）、`UI/`（ItemMenu・GameMenu のプレハブ。それぞれ Resources.Load で自動生成）、`PlaceNames.asset`（場所の表示名） |
| `Assets/Data/` | `Enemies/`（EnemyData。`Sample/` にサンプル）、`Characters/`（CharacterProfile：Noah / Sigle）、`Dialogue/`、`Flags/`（GameFlag）、`Animation/Sample/`（SpriteAnimData）、`Effect/Sample/`（EffectData） |
| `Assets/Behavior/Enemy/` | Unity Behavior の敵 AI グラフ（`Behavior Graph.asset`）と独自ノード |
| `Assets/Images/`、`Assets/BGM/`、`Assets/SE/`、`Assets/Fonts/` | 素材 |
| `Assets/res_kari/` | 仮素材（マップタイル約2000枚、仮プレイヤー画像など） |
| `Assets/_Recovery/0.unity` | Unity のクラッシュ復旧で作られたシーン（不要なら削除してよい） |
| `Docs/` | プランナー向け手順書（通しの手順書は `プランナー向け作業手順書.md`）、この文書、プログラマー向けの引き継ぎ資料（`引き継ぎ_アイテム画面以降の変更.md`） |

---

## 2. 起動とシーン開始時の流れ

0. 起動：`AutoSave.LoadOnStartup`（BeforeSceneLoad）が旧 `save.json` をスロット1へ移す。`GameSession.Init` がプレイ時間のカウンターを作る。
   - **Title**：`TitleMenu` でスロットを選ぶ → `GameSession.StartNewGame`（スロットを空に）か `Continue`（`Save.LoadSlot` → 記録した場所を `PlayerSpawnPoint.PendingSpawnPointId` / `PendingPosition` に）→ `InventoryManager.ReloadFromSave` → SceneFader でステージへ
   - エディタでステージから直接再生したときだけ、デバッグウィンドウで選んだスロットをここで読む（初期値は使わない）

ステージ（Stage1_1 / Stage1_2）の開始：
1. （Title で選んだスロットのデータが Save に入っている）
2. シーンの Awake：`InventoryManager`（なければ AfterSceneLoad で自動生成。セーブから所持品を復元）、`DialogueManager`、宝箱などがセーブを読む
3. `PlayerPartySpawner.Start`（`Spawner/Player/`）
   - `PlayerSpawnPoint.ConsumePendingPosition()`（つづきから・チェックポイント）→ `ConsumePending()`（出現位置ID）→ 既定の位置 の順で出現位置を決める
   - **着いた場所を「再開する場所」として記録し、`Save.Commit()` + `WriteAsync()`**（スロットを使っていなければファイルには書かない）
   - Noah / Sigle のプレハブを Instantiate
   - `PlayerRuntimeInitializer` で、シーンの参照（`PlayerSceneContext`：Tilemap・弾プール・カメラ・UI・SceneChange・AreaAttack・BulletDeleteArea）を渡す
   - カメラ・ライフ UI・攻撃・死亡時の SceneChange などを配線し、最後に `PlayerSwitchManager.InitializePlayers`
4. `EnemySpawner`（`ObjectSpawner` 派生）が敵を出し、Behavior Graph の `TargetPlayer` と `EnemyShooter` に現在のプレイヤーを渡す（キャラ切り替えにも追従）

操作キー：移動 WASD / 矢印、調べる・会話送り E、キャラ切り替え C、射撃 マウス（Sigle）、弾消しエリア Q（Noah）、範囲攻撃 Space、タイミングゲージ攻撃 R（判定 Space）、**メニュー Tab**（アイテム・ロード・タイトルへ。Esc で閉じる）。

プレイヤーの操作を止める条件は `Common/InputBlock.cs`（`InputBlock.IsBlocked`）に一本化している。

---

## 3. スクリプト一覧

状態：◎ 今の作りの中心 ／ ○ 使用中 ／ △ 古い・使われていない ／ 🛠 エディタ専用

### プレイヤー・キャラクター
| ファイル | 役割 | 状態 |
|---|---|---|
| `ObjectBase/ObjectBase.cs` → `CharaBase/CharaBase.cs` | 共通の基底（中身は空の Start / Update） | ○ |
| `CharaBase/CharacterMovement.cs` | Rigidbody2D.MovePosition で移動 | ◎ |
| `Player/PlayerMovement.cs` | 操作できるかのフラグを追加 | ◎ |
| `Player/PlayerInput.cs` | キー入力 → 操作中キャラの移動、Q で弾消し、**マウスの左クリックで射撃**（操作中のキャラに SigleShooter があれば。Update で直接読む。Target Camera が空ならメインカメラ。`OnShot` は Input System のイベント用に残しているが未使用） | ◎ |
| `Manager/PlayerSwitchManager.cs` | Noah / Sigle の切り替え（位置の入れ替え、追従、カメラ）、`Warp` | ◎ |
| `Player/PlayerHistory.cs`、`Player/PlayerFollower.cs` | 位置の履歴と、操作していない側の追従（何フレーム前の位置か） | ◎ |
| `Player/PlayerMapLimit.cs` | Tilemap の範囲内に収める | ◎ |
| `Player/PlayerRuntimeInitializer.cs`、`Player/PlayerSceneContext.cs` | 生成したプレイヤーにシーンの参照を渡す | ◎ |
| `Player/Player_Noah/NoahMovement.cs`、`NoahAnimation.cs` / `Player_Sigle/SigleMovement.cs`、`SigleAnimation.cs` | 古い向き切り替え（SpriteAnimator があれば何もしない） | △ |
| `Player/Player_Noah/NoahDead.cs` | 死亡 → 死亡アニメ → `SceneGameOver` へ | ◎ |
| `Player/Player_Noah/NoahBulletDeleteAreaController.cs` | Q を押している間、弾消しエリアを出す（SE 付き） | ◎ |
| `Player/Player_Noah/NoahInventory.cs`、`Data/NoahData.cs` | 鍵フラグ（`hasKey`）の古い仕組み | △（KeyItem・Treasure が使う） |
| `Player/Player_Sigle/SigleShooter.cs` | Sigle の射撃（プール・SE・エフェクト） | ◎ |
| `Player/FocusPlayer.cs`、`Player_Noah/NoahMapLimit.cs`、`Player_Noah/NoahHistory.cs` | 古い仕組み（Stage1 / Stage2 で使われている） | △ |
| `CharaBase/CharacterSorting.cs` | Sigle の描画順を Noah との上下で切り替え | ○ |
| `Camera/FocusCamera.cs` | プレイヤーを追い、Tilemap の範囲で止める | ◎ |

### 攻撃・弾・敵
| ファイル | 役割 | 状態 |
|---|---|---|
| `Attack/AttackController.cs` | キーで攻撃を始める基底 | ◎ |
| `Attack/Player/AreaAttackController.cs`、`Attack/AreaAttack.cs` | Space：Noah の周りに範囲攻撃（数フレーム判定）。攻撃の種類（範囲／ゲージ＋判定）つきで `DamageUtility.Apply`。同じ攻撃で同じ Health には1回だけ（本体と部位の二重ヒットを防ぐ） | ◎ |
| `Attack/Player/TimingGaugeAttackController.cs`、`UI/AttackArcGauge/TimingGaugeUI.cs` | R：ゲージ → Space で判定 → 倍率つきで範囲攻撃（判定待ちの間は `TimingGaugeUI.IsJudging` が true で、通常の Space 攻撃は出ない。会話・イベント中はゲージが止まる） | ◎ |
| `Bullet/BulletBase.cs`、`Bullet/Player/PlayerBullet.cs`、`Bullet/Enemy/EnemyBullet.cs` | 弾（寿命・命中・エフェクト）。味方（敵の弾→Enemies、プレイヤーの弾→Player）と、イベントなどのトリガー範囲は通り抜ける。壁・相手・弾どうし・弾消しエリアには当たる | ◎ |
| `Bullet/BulletDeleteArea.cs` | 敵の弾を消すエリア | ◎ |
| `Pool/BulletPoolBase.cs`、`Pool/Bullet/*` | UnityEngine.Pool による弾プール（初期300、最大1000） | ◎ |
| `Enemy/EnemyShooter.cs`、`Enemy/EnemyDead.cs`、`Enemy/EnemyMovement.cs`（空） | 古い作りの敵の射撃・死亡。EnemyShooter は狙う相手（操作中のキャラ）と弾のプールを自分で探す（スポナーなしでも撃てる）。`EnemyBlock` の間は撃たない | ○ |
| `Enemy/Enemy.cs`、`Enemy/EnemyData.cs` | **新しい作りの敵**（段階1）。EnemyData（体力・速さ・大きさ・体当たり・見た目・倒されたとき）を反映、Rigidbody2D（Dynamic）で移動、`MoveTo` で NavMesh2D の経路に沿って進む、マップの範囲内、体当たり、`Paused`、`Enemy.All`。共通プレハブは `Prefab/Enemies/Enemy_Base.prefab` | ◎ |
| `Enemy/EnemyBlock.cs` | 敵を止める判定（`InputBlock.IsBlocked` + デバッグの「敵を止める」）。条件を足すときはここだけ | ◎ |
| `Enemy/Navigation/NavMesh2D.cs`、`EnemyNavArea.cs` | 2D ナビメッシュ（自動生成・シーンごと）。標準の NavMesh API を -90° 回して XY に重ね、動かない 2D コライダーを `CreateMesh` で取り出して壁にする。大きさ（小中大）ごとに別のナビメッシュ。壁の変化を 0.5 秒ごとに見て作り直す。シーンを出るときにデータと設定を消す。デバッグ表示（線のメッシュ）。EnemyNavArea：敵だけ入れない／通りにくい | ◎ |
| 🛠`Enemy/Editor/EnemyBaseBuilder.cs` | Tools > 敵 > 共通プレハブとサンプルを作る、Hierarchy 右クリック > 敵 > 敵を置く／敵の通行設定エリア | ◎ |
| `Damage/DamageInfo.cs`、`Damage/DamageReceiver.cs` | 攻撃の情報（量・種類 `DamageKind`・攻撃者・キャラ・位置・ゲージ判定）、`PlayerCharacters`（ノア／シグルの判定・パーティーの体力）。DamageReceiver（攻撃を受ける部品：部位・ギミック用、倍率、OnHit）と `DamageUtility.Apply / HitKey`（範囲攻撃・弾・体当たりはここを通す） | ◎ |
| `Behavior/Enemy/EnemyShotAction.cs` | Behavior Graph の「一定時間撃つ」ノード | ◎ |
| `Behavior/Enemy/AbcdefuckAction.cs`、`SampleAction.cs` | 練習用ノード（どのグラフからも使われていない） | △ **削除推奨**（下記） |
| `Spawner/ObjectSpawner.cs`、`Spawner/SpawnedObject.cs`、`Spawner/Enemy/EnemySpawner.cs` | 一定間隔で再出現 | ◎ |
| `Spawner/Player/PlayerPartySpawner.cs`、`PlayerSpawnPoint.cs` | プレイヤーの生成と出現位置。出現位置に Sigle 用の位置があれば、Sigle はそこに出てノアの後ろへ歩いて付いていく（`PlayerHistory.SeedPath`） | ◎ |
| `Health/Health.cs` | 体力、`OnDied` / `OnDamaged` / `OnHealed` / **`OnDamagedInfo`**（攻撃の情報つき）、`LastDamage`、`Damage(int)` と `Damage(DamageInfo)`、`SetMaxHealth`、`Invincible` / `PlayerInvincible`、**体力の共有**（`ShareWith`：受けたダメージ・回復を共有先へ回し、自分のイベントも出す。シグル → ノアは `PlayerPartySpawner` が設定） | ◎ |

### イベント・アイテム・フラグ・会話・セーブ（プランナー向けの中心）
詳しくは CLAUDE.md の「主な仕組み」と各 Docs。

| フォルダ | 主なファイル |
|---|---|
| `Event/` | `EventProcess/EventSequence.cs`、`EventAction.cs`、`EventZone.cs`、`Actions/`（SetActive / SpriteChange / Wait） |
| `Item/` | `ItemData.cs`、`ItemPickup.cs`、`Interaction/`（ItemInteraction / ItemCondition / ItemPossessionSwitch / ItemGive / ItemTake） |
| `Flag/` | `GameFlag.cs`、`GameFlags.cs`、`FlagCondition.cs`、`SetFlagEventAction.cs`、`FlagSwitch.cs` |
| `Talk/` | `DialogueManager.cs`、`Dialogue.cs`、`DialogueData.cs`、`DialogueEventAction.cs`、`CharacterProfile.cs`、`Expression.cs`、🛠`Editor/DialogueCsv.cs` |
| `Save/` | `Save.cs`（スロット・仮置き・確定・書き込み）、`AutoSave.cs`（旧ファイルの引き継ぎ・エディタでの直接再生・シーン読み込み時と終了時の書き込み）、`GameSession.cs`（はじめから・つづきから・タイトルへ・プレイ時間・再開場所の記録）、`SaveSlotInfo.cs`（Meta キーとスロットの概要）、`PlayTimeCounter.cs` |
| `Title/` | `TitleMenu.cs`（タイトル画面）、`SaveSlotButton.cs`（スロット1つ分の表示）、`ReturnToTitleButton.cs`（ボタンからタイトルへ）、🛠`Editor/TitleSceneBuilder.cs`（Title.unity を作る） |
| `UI/GameMenu/` | `GameMenu.cs`（Tab のメニュー：アイテム・ロード・タイトルへ、セーブポイントからのセーブの流れ、メッセージ）、🛠`Editor/GameMenuBuilder.cs`（`Resources/UI/GameMenu.prefab` を作る） |
| `UI/Common/` | `SaveSlotPanel.cs`（セーブデータ選択画面。NewGame / Continue / Load / Save）、`ConfirmDialog.cs`（はい／いいえ。左右移動は2つの間だけ）、`UISubmitKeys.cs`（UI の決定に Space を足す・キー操作で選択が動いたら選択の SE。起動時に自動生成）、`UISound.cs` / `UISoundSettings.cs`（UI の効果音。`Resources/UISounds.asset`）、`UIButtonSound.cs`（ボタンに付けると押したときに鳴る）、🛠`Editor/UISoundGenerator.cs`（仮の電子音を `Assets/SE/UI/` に作る）、`PlaceNameTable.cs`（場所の表示名、`Resources/PlaceNames.asset`）、🛠`Editor/MenuUIBuilder.cs`（メニュー系 UI を作る共通処理） |
| `SavePoint/` | `SavePoint.cs`（調べるとセーブ。Intaractable）、🛠`Editor/SavePointCreator.cs`（Hierarchy 右クリック > セーブポイント） |
| `Common/InputBlock.cs` | プレイヤーの操作を止める判定（会話・暗転・アイテム画面・イベント・メニュー） |
| `Intaract/Intaractable.cs` | 「調べる」の基底（範囲に入って E） |
| `Manager/InventoryManager.cs` | 所持品（シングルトン、シーンをまたぐ、セーブ） |
| `Key/KeyItem.cs`、`Treasure/Treasure.cs` | 古い鍵・宝箱（NoahData.hasKey を使う）。ItemInteraction で同じことができる | △（シーンで使用中） |
| `CheckPoint/CheckPoint.cs` | 触れると再開する位置を記録して Commit + Write | ○（**どのシーンにも置かれていない**） |
| `Scene/SceneChange.cs`、`Scene/SceneFader.cs` | 暗転つきシーン遷移（SceneFader は自動生成・シーンをまたぐ） | ◎ |
| `UI/ItemMenu/ItemMenuUI.cs`、`ItemSlotUI.cs` | アイテム画面（Resources から自動生成）。メニュー画面から `OpenFromMenu` で開く（GameMenu がないときだけ Tab で直接開く） | ◎ |
| `UI/Life/LifeUIController.cs` | ハートの表示 | ◎ |

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
| 🛠`Common/Editor/`（SceneChecker / SceneCheckWindow / DebugWindow / SceneLookup / PickerDrawers）、`Common/Attributes/PickerAttributes.cs` | ③④⑤（デバッグウィンドウに「敵」：止める・経路の表示・倒す。シーンチェックに敵・敵の通行設定エリア） |
| `Common/Attributes/LabelAttribute.cs`、🛠`Common/Editor/LabelDrawer.cs` | `[Label("日本語")]`：Inspector の項目名（InspectorName は enum の値にしか効かないため）。`[Label("名前", min)]` / `[Label("名前", min, max)]` で最小値・スライダー。リストには付けない |
| `Common/GameCondition.cs` | 条件のまとめ（フラグ・アイテム〔既存の FlagCondition / ItemCondition をそのまま中に持つ〕・操作中のキャラ・パーティーの体力・プレイヤーがいる範囲）。`IsMet` / `AllMet` / `AnyMet` |
| 🛠`Flag/Editor/`、`Item/Interaction/Editor/`、`UI/ItemMenu/Editor/`、`Event/EventProcess/Editor/` | ひな形・一覧・Inspector |
| `Singleton/Singleton.cs` | `Singleton<T>`（`persistAcrossScenes` でシーンをまたぐ） |
| `Fps/Fps.cs` | 60fps 固定（各シーンに配置） |
| `Debug/CircleDebugRenderer.cs` | 円のデバッグ表示 |
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
| **高** | `Prefab/Players/Player_Noah.prefab`、`Player_Sigle.prefab` | **FootstepSe が付いていない**ため足音が鳴らない。FootstepSe は Stage1_2 のルートの `Player`（Transform と FootstepSe だけの動かないオブジェクト）に付いている。11-06 の SE 手順書は「プレハブの FootstepSe に設定」と書いていた |
| 中 | `Bgm/Effects/BgmProgress.cs` | 進行度（static）が `SubsystemRegistration` でリセットされない（ドメインリロード無効のエディタで前回の値が残る）。セーブもされず、はじめから・つづきからでも戻らない |
| 中 | `StageDarkness/StageDarkness.cs` FindTarget | 明かりの追従先を `FindWithTag("Player")` の最初の1人で決めるため、キャラ切り替えで操作キャラに付いていかないことがある |
| 中 | **古い作りの敵**の移動（`Behavior/Enemy/Behavior Graph.asset` の SetPositionToTarget、`Prefab/Enemies/Enemy.prefab`） | 「6秒撃つ → 3秒かけてプレイヤーの位置へ移動」の繰り返し。移動は transform を直接動かし、Rigidbody2D は Static なので**壁をすり抜け、プレイヤーと重なる**。会話・イベント中も移動は続く（撃つのは止まるようにした）。新しい作りの敵（Enemy）に置き換える予定（段階2） |
| 低 | 敵のスポナー（`Spawner/Enemy/EnemySpawner.cs`） | 1つのスポナーで同時に出せるのは1体だけ。Stage1_1 のスポナーは非アクティブ（敵が出ない）。（スポナーなしでも敵は動くようになった） |
| 低 | 🆕 `Enemy/Enemy.cs` の体当たり | プレイヤーに無敵時間がないため、体当たりのダメージは敵ごとの間隔（既定 1 秒）だけで決まる。敵が多いと連続で受ける。プレイヤー側の無敵時間を足すかは未定 |
| 低 | 🆕 `Attack/AttackController.cs` | 操作中のキャラかを見ていないため、シグル操作中も Space / R でノアの位置から攻撃が出る（Q の弾消しは操作中だけ）。意図した作りかは未確認 |
| 低 | 🆕 Physics 2D の衝突設定 | Bullet と Bullet の衝突が無効になっているので、弾どうしはぶつからない（`BulletBase` などのコメント「弾どうしは今まで通り当たる」と食い違う） |
| 低 | 敵を倒したとき（`Enemy/EnemyDead.cs`、`Enemy/Enemy.cs`） | 消えるだけで、イベント・フラグ・アイテムのドロップにつなげられない。倒した記録もセーブされない（シーンに戻ると再出現。段階6で対応予定） |
| 低 | 🆕 `Prefab/Enemies/Enemy.prefab` | 見た目が仮（シグルの画像 `Sigle-front_0`）。SpriteAnimator がないので死亡アニメ（Dead）は流れない。`Behavior Graph.asset` の EnemyShot ノードに、コードから消えた `shootInterval` の値が残っている（害はない） |
| 低 | `CheckPoint/CheckPoint.cs` | どのシーンにも置かれていない（自動セーブ用。手動セーブはセーブポイント） |
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
| 低 | `Player/PlayerMapLimit.cs`、`PlayerFollower.cs` | Rigidbody2D を使っているのに transform.position を直接書き換えている（物理と位置がずれることがある） |
| 低 | `StateMachine/StateMachine.cs` Update | 状態が変わったフレームしか `Current.Update` を呼ばない（括弧の位置の誤り）。今は未使用 |
| 低 | `Data/NoahData.cs` / `NoahInventory.GetKey(null)` | `hasKey` はセーブされない。ItemData なしで鍵を拾うと、シーン遷移後に鍵が消える |
| 低 | 🆕 `Title.unity` の `System`（SceneBgm） | Default Clip が空なので、タイトルで BGM が鳴らない（ステージの曲はタイトルで止まる）。曲が決まったら入れる |

### 提出前に整理したいもの
| 場所 | 内容 |
|---|---|
| `Behavior/Enemy/AbcdefuckAction.cs` | **クラス名とノードの文言に不適切な語が含まれている**（卒業制作として提出・公開する前に削除推奨。どのグラフでも未使用） |
| `Behavior/Enemy/SampleAction.cs` | 練習用ノード（未使用。ブラックボードの値を書き換えて戻さない） |
| `Intaract/Intaractable.cs:58` | Debug.Log「調べられるまする」（誤字、範囲に入るたびに出る） |
| 不要な using | `PlayerFollower`（Unity.AppUI.Redux）、`PlayerInput` / `NoahDead`（Unity.VisualScripting）、`AbcdefuckAction`（RuleTile） |
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
| Stage1_1 / Stage1_2 / SampleScene の `MapEntity/OB_TRbox` | 宝箱の Id が空（シーンチェックでエラー。ほかの宝箱と開いた記録が混ざる） |
| 同じ3シーンの `Event`（ルート） | 中身が空の EventZone / EventSequence。Is Trigger の Collider2D もない（シーンチェックでエラー・注意が出るが、動作上の害はない。不要なら消す） |
| Stage1_2 | 会話 UI がシーンの Canvas に直接組まれている（他のシーンは DialogueSystem.prefab）。揃えるなら置き換え |
| 🆕 Stage1_2 のルートの `Player` | Transform と FootstepSe だけの動かないオブジェクト（上の「FootstepSe が付いていない」を参照）。プレハブに移したら消す |
| Stage1_1 / Stage1_2 の `OtherSystem/SceneChange`、SceneGameOver の SceneChange | 遷移先が空（シーンチェックで「参考」）。死亡時・リトライでコードから `ChangeScene` を呼ぶためのもので、問題なし |

### 設計上の注意（不具合ではない）
- ItemPickup / ItemInteraction / KeyItem は、ID が空だと階層パスでセーブする → 名前や親を変えると記録がリセットされる。
- `EventZone` の「1回だけ」はセーブされない（シーンに戻ると再発動）。
- 最後のセーブ以降に手に入れたものは、タイトルに戻る・終了すると失われる（セーブはシーンに着いたとき・チェックポイント）。
- 倒れたときは、その時点までの進行を確定してからゲームオーバーへ移る（倒れても拾ったものは残る）。
- 出口のそばの出現位置へワープすると、そのまま遷移する。
