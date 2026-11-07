# コード構成と問題点（開発者・Claude 向け）

最終更新：2026-11-07。CLAUDE.md から読み込まれる。コードを変えたら、この表も更新すること。
ファイルの参照は `Assets/Script/` からの相対パス。行番号は目安。

---

## 1. アセットのフォルダ

| フォルダ | 中身 |
|---|---|
| `Assets/Scenes/` | `Stage1_1` ⇄ `Stage1_2`（今の作りのステージ）、`Stage1` / `Stage2`（**古い作り**：プレイヤーをシーンに直接置く）、`SampleScene`、`SceneGameOver`、`kari/`（仮の Start / Clear）、`Tilemap/` |
| `Assets/Prefab/` | `Players/`（Player_Noah / Player_Sigle）、`Enemies/Enemy`、`Bullet/`、`UI/`（DialoguePanel、**DialogueSystem**） |
| `Assets/Resources/` | `Items/`（ItemData。**必ずここ**。`Items/Sample/` にサンプル）、`UI/`（アイテム画面プレハブ。`ItemMenuUI` が Resources.Load） |
| `Assets/Data/` | `Characters/`（CharacterProfile：Noah / Sigle）、`Dialogue/`、`Flags/`（GameFlag）、`Animation/Sample/`（SpriteAnimData）、`Effect/Sample/`（EffectData） |
| `Assets/Behavior/Enemy/` | Unity Behavior の敵 AI グラフ（`Behavior Graph.asset`）と独自ノード |
| `Assets/Images/`、`Assets/BGM/`、`Assets/SE/`、`Assets/Fonts/` | 素材 |
| `Assets/res_kari/` | 仮素材（マップタイル約2000枚、仮プレイヤー画像など） |
| `Assets/_Recovery/0.unity` | Unity のクラッシュ復旧で作られたシーン（不要なら削除してよい） |
| `Docs/` | プランナー向け手順書と、この文書 |

---

## 2. シーン開始時の流れ（Stage1_1 / Stage1_2 / SampleScene）

1. `AutoSave.LoadOnStartup`（BeforeSceneLoad、起動時1回）→ `Save.Read()`
2. シーンの Awake：`InventoryManager`（なければ AfterSceneLoad で自動生成。セーブから所持品を復元）、`DialogueManager`、宝箱などがセーブを読む
3. `PlayerPartySpawner.Start`（`Spawner/Player/`）
   - `PlayerSpawnPoint.ConsumePending()` で出現位置を決める（なければ既定の位置）
   - Noah / Sigle のプレハブを Instantiate
   - `PlayerRuntimeInitializer` で、シーンの参照（`PlayerSceneContext`：Tilemap・弾プール・カメラ・UI・SceneChange・AreaAttack・BulletDeleteArea）を渡す
   - カメラ・ライフ UI・攻撃・死亡時の SceneChange などを配線し、最後に `PlayerSwitchManager.InitializePlayers`
4. `EnemySpawner`（`ObjectSpawner` 派生）が敵を出し、Behavior Graph の `TargetPlayer` と `EnemyShooter` に現在のプレイヤーを渡す（キャラ切り替えにも追従）

操作キー：移動 WASD / 矢印、調べる・会話送り E、キャラ切り替え C、射撃 マウス（Sigle）、弾消しエリア Q（Noah）、範囲攻撃 Space、タイミングゲージ攻撃 R（判定 Space）、アイテム画面（`ItemMenuUI` で設定）。

---

## 3. スクリプト一覧

状態：◎ 今の作りの中心 ／ ○ 使用中 ／ △ 古い・使われていない ／ 🛠 エディタ専用

### プレイヤー・キャラクター
| ファイル | 役割 | 状態 |
|---|---|---|
| `ObjectBase/ObjectBase.cs` → `CharaBase/CharaBase.cs` | 共通の基底（中身は空の Start / Update） | ○ |
| `CharaBase/CharacterMovement.cs` | Rigidbody2D.MovePosition で移動 | ◎ |
| `Player/PlayerMovement.cs` | 操作できるかのフラグを追加 | ◎ |
| `Player/PlayerInput.cs` | キー入力 → 操作中キャラの移動、Q で弾消し、マウスで射撃（OnShot は PlayerInput コンポーネントのイベントから呼ぶ） | ◎ |
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
| `Attack/Player/AreaAttackController.cs`、`Attack/AreaAttack.cs` | Space：Noah の周りに範囲攻撃（数フレーム判定） | ◎ |
| `Attack/Player/TimingGaugeAttackController.cs`、`UI/AttackArcGauge/TimingGaugeUI.cs` | R：ゲージ → Space で判定 → 倍率つきで範囲攻撃（判定待ちの間は `TimingGaugeUI.IsJudging` が true で、通常の Space 攻撃は出ない。会話・イベント中はゲージが止まる） | ◎ |
| `Bullet/BulletBase.cs`、`Bullet/Player/PlayerBullet.cs`、`Bullet/Enemy/EnemyBullet.cs` | 弾（寿命・命中・エフェクト）。味方（敵の弾→Enemies、プレイヤーの弾→Player）と、イベントなどのトリガー範囲は通り抜ける。壁・相手・弾どうし・弾消しエリアには当たる | ◎ |
| `Bullet/BulletDeleteArea.cs` | 敵の弾を消すエリア | ◎ |
| `Pool/BulletPoolBase.cs`、`Pool/Bullet/*` | UnityEngine.Pool による弾プール（初期300、最大1000） | ◎ |
| `Enemy/EnemyShooter.cs`、`Enemy/EnemyDead.cs`、`Enemy/EnemyMovement.cs`（空） | 敵の射撃・死亡 | ◎ |
| `Behavior/Enemy/EnemyShotAction.cs` | Behavior Graph の「一定時間撃つ」ノード | ◎ |
| `Behavior/Enemy/AbcdefuckAction.cs`、`SampleAction.cs` | 練習用ノード（どのグラフからも使われていない） | △ **削除推奨**（下記） |
| `Spawner/ObjectSpawner.cs`、`Spawner/SpawnedObject.cs`、`Spawner/Enemy/EnemySpawner.cs` | 一定間隔で再出現 | ◎ |
| `Spawner/Player/PlayerPartySpawner.cs`、`PlayerSpawnPoint.cs` | プレイヤーの生成と出現位置。出現位置に Sigle 用の位置があれば、Sigle はそこに出てノアの後ろへ歩いて付いていく（`PlayerHistory.SeedPath`） | ◎ |
| `Health/Health.cs` | 体力、`OnDied` / `OnDamaged` / `OnHealed`、`Invincible` / `PlayerInvincible` | ◎ |

### イベント・アイテム・フラグ・会話・セーブ（プランナー向けの中心）
詳しくは CLAUDE.md の「主な仕組み」と各 Docs。

| フォルダ | 主なファイル |
|---|---|
| `Event/` | `EventProcess/EventSequence.cs`、`EventAction.cs`、`EventZone.cs`、`Actions/`（SetActive / SpriteChange / Wait） |
| `Item/` | `ItemData.cs`、`ItemPickup.cs`、`Interaction/`（ItemInteraction / ItemCondition / ItemPossessionSwitch / ItemGive / ItemTake） |
| `Flag/` | `GameFlag.cs`、`GameFlags.cs`、`FlagCondition.cs`、`SetFlagEventAction.cs`、`FlagSwitch.cs` |
| `Talk/` | `DialogueManager.cs`、`Dialogue.cs`、`DialogueData.cs`、`DialogueEventAction.cs`、`CharacterProfile.cs`、`Expression.cs`、🛠`Editor/DialogueCsv.cs` |
| `Save/` | `Save.cs`、`AutoSave.cs` |
| `Intaract/Intaractable.cs` | 「調べる」の基底（範囲に入って E） |
| `Manager/InventoryManager.cs` | 所持品（シングルトン、シーンをまたぐ、セーブ） |
| `Key/KeyItem.cs`、`Treasure/Treasure.cs` | 古い鍵・宝箱（NoahData.hasKey を使う）。ItemInteraction で同じことができる | △（シーンで使用中） |
| `CheckPoint/CheckPoint.cs` | 触れると Commit + Write | ○（**どのシーンにも置かれていない**） |
| `Scene/SceneChange.cs`、`Scene/SceneFader.cs` | 暗転つきシーン遷移（SceneFader は自動生成・シーンをまたぐ） | ◎ |
| `UI/ItemMenu/ItemMenuUI.cs`、`ItemSlotUI.cs` | アイテム画面（Resources から自動生成） | ◎ |
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
| 🛠`Common/Editor/`（SceneChecker / SceneCheckWindow / DebugWindow / SceneLookup / PickerDrawers）、`Common/Attributes/PickerAttributes.cs` | ③④⑤ |
| 🛠`Flag/Editor/`、`Item/Interaction/Editor/`、`UI/ItemMenu/Editor/`、`Event/EventProcess/Editor/` | ひな形・一覧・Inspector |
| `Singleton/Singleton.cs` | `Singleton<T>`（`persistAcrossScenes` でシーンをまたぐ） |
| `Fps/Fps.cs` | 60fps 固定（各シーンに配置） |
| `Debug/CircleDebugRenderer.cs` | 円のデバッグ表示 |
| `StateMachine/`（StateMachine / IState / State）、`ComponentBase/ComponentBase.cs` | △ どこからも使われていない |
| `kari/player/`（kari_player / kari_player2 / kari_player_test） | △ 仮プレイヤー。シーンでは未使用（StageDarkness がプレイヤー探しの予備に kari_player_test を参照） |

---

## 4. 問題点（未修正）

優先度：**高** = 遊ぶと気づく不具合 ／ **中** = 条件次第で不具合・提出前に直したい ／ **低** = 整理・品質

### 不具合・不具合の可能性
| 優先度 | 場所 | 内容 |
|---|---|---|
| **高** | 会話用フォント `NotoSansJP-Regular SDF`（TextMeshPro のフォントアセット） | **文字が足りない。**「ジ」「停」「止」などが空白になる（Console に "The character … was not found"）。CSV で台本を増やすほど起きやすい。フォントアセットを Dynamic にするか、使う文字を含めて作り直す |
| 中 | `StageDarkness/StageDarkness.cs` FindTarget | 明かりの追従先を `FindWithTag("Player")` の最初の1人で決めるため、キャラ切り替えで操作キャラに付いていかないことがある |
| 中 | `CheckPoint/CheckPoint.cs` | どのシーンにも置かれていない。セーブが確定するのはシーン遷移のときだけ（手順書の「チェックポイントで確定」は今は起きない） |
| 中 | `Scenes/Stage1.unity`、`Stage2.unity`（Build Settings に入っている） | 古い作り（プレイヤーをシーンに直接置く）。開くと「Creating missing Rigidbody2D / PlayerMovement」が出て、今の仕組み（出現位置・会話・ライフ UI 配線）と合っていない。作り直すか Build Settings から外す |
| 低 | `Health` を使うクラス（`NoahDead`、`EnemyDead`） | Awake で Health がなければ Log だけ出し、OnEnable で NullReferenceException |
| 低 | `Spawner/Player/PlayerPartySpawner.cs` Awake | 参照が足りなくてもエラーを出すだけで、Start に進んで NullReferenceException |
| 低 | `Behavior/Enemy/EnemyShotAction.cs` | `EnemyShooter` がないと Log のあと OnUpdate で NullReferenceException |
| 低 | `Spawner/Player/PlayerPartySpawner.cs` の `Sigle Spawn Point`（既定の出現位置） | 使われていない（Sigle は既定ではノアと同じ位置に出る）。Stage1_2 では Noah の既定位置から 5.8 離れた場所が入っている。出現位置ごとの Sigle の位置は `PlayerSpawnPoint` で設定できる |
| 低 | `Player/PlayerInput.cs` Update | プレイヤー生成前（最初の数フレーム）に「CurrentPlayerがnullです」を毎フレーム LogError。OnShot は `Mouse.current` の null を見ていない |
| 低 | `CharaBase/CharacterSorting.cs` | `SetPlayer` が Start より後に呼ばれると `spriteRenderer` が null のまま |
| 低 | `Player/PlayerFollower.cs`、`PlayerHistory.cs` | 追従の距離が「フレーム数」なので、fps が変わると距離が変わる。`RemoveAt(0)` を毎フレーム（1000件のリスト） |
| 低 | `Player/PlayerMapLimit.cs`、`PlayerFollower.cs` | Rigidbody2D を使っているのに transform.position を直接書き換えている（物理と位置がずれることがある） |
| 低 | `StateMachine/StateMachine.cs` Update | 状態が変わったフレームしか `Current.Update` を呼ばない（括弧の位置の誤り）。今は未使用 |
| 低 | `Data/NoahData.cs` / `NoahInventory.GetKey(null)` | `hasKey` はセーブされない。ItemData なしで鍵を拾うと、シーン遷移後に鍵が消える |

### 提出前に整理したいもの
| 場所 | 内容 |
|---|---|
| `Behavior/Enemy/AbcdefuckAction.cs` | **クラス名とノードの文言に不適切な語が含まれている**（卒業制作として提出・公開する前に削除推奨。どのグラフでも未使用） |
| `Behavior/Enemy/SampleAction.cs` | 練習用ノード（未使用。ブラックボードの値を書き換えて戻さない） |
| `Intaract/Intaractable.cs:63` | Debug.Log「調べられるまする」（誤字、範囲に入るたびに出る） |
| 不要な using | `PlayerFollower`（Unity.AppUI.Redux）、`PlayerInput` / `NoahDead`（Unity.VisualScripting）、`AbcdefuckAction`（RuleTile） |
| 使われていないクラス | `StateMachine/*`、`ComponentBase`、`FocusPlayer`、`SpriteAnimation`（Animator 版）、`kari/player/*`、`BgmProgressDebug`、`SePlayer`、`HealthSe`（シーン・プレハブに未配置） |
| `Player_Noah/NoahBulletDeleteAreaController.cs` | ヘッダーの説明が「ノアのアニメーションクラス」になっている |
| `Attack/AreaAttack.cs` | 命中のたびに Debug.Log。Health がない相手（壁など）に当たるたびにも Log |
| `Bullet/BulletDeleteArea.cs` | `deleteRadius` は Gizmo の表示にしか使っておらず、実際の範囲は Collider の大きさ |
| `Debug.Log` 全般 | 本番ビルド向けに整理が必要（約60か所） |
| `Assets/_Recovery/0.unity` | 復旧シーンの残り |
| Visual Scripting | 「90 node options failed to load」（Regenerate Nodes） |

### シーンの設定
| 場所 | 内容 |
|---|---|
| Stage1_1 / Stage1_2 / SampleScene の `MapEntity/OB_TRbox` | 宝箱の Id が空 |
| 同じ3シーンの `Event`（ルート） | 中身が空の EventZone / EventSequence（害なし） |
| Stage1_2 | 会話 UI がシーンの Canvas に直接組まれている（他のシーンは DialogueSystem.prefab）。揃えるなら置き換え |

### 設計上の注意（不具合ではない）
- ItemPickup / ItemInteraction / KeyItem は、ID が空だと階層パスでセーブする → 名前や親を変えると記録がリセットされる。
- `EventZone` の「1回だけ」はセーブされない（シーンに戻ると再発動）。
- セーブに現在のシーン・位置は入っていない（起動すると最初のシーンから）。
- 出口のそばの出現位置へワープすると、そのまま遷移する。
