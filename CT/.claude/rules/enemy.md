---
paths:
  - "Assets/Script/Enemy/**"
  - "Assets/Script/Damage/**"
  - "Assets/Script/Spawner/**"
  - "Assets/Script/Health/**"
  - "Assets/Script/Attack/**"
  - "Assets/Script/Bullet/**"
  - "Assets/Behavior/**"
  - "Assets/Data/Enemies/**"
---

# 敵の仕組み（敵・攻撃・ダメージ・スポナーを触るとき）

**完了**（段階0〜7）。手順書は `Docs/敵の作り方.md`、練習問題は `Docs/プランナー向け作業手順書.md` の 7 章 練習 6〜9。
ファイルごとの役割は `Docs/code-map.md` の「3. スクリプト一覧 > 攻撃・弾・敵」を読む。
提案にあって入れていないもの：Behavior Graph のノードから行動の部品・攻撃を呼ぶこと、グループの「○秒以内に全員倒した」、スポナー・独立した部位をグループのメンバーにすること（引き継ぎ資料の 9 章）。

- **新しい作り**：`EnemyData`（ScriptableObject。`Assets/Data/Enemies/`）＋ 共通プレハブ `Resources/Enemies/Enemy_Base`（`Enemy` コンポーネント）。シーンに置くだけで動く（狙う相手は `PlayerSwitchManager.CurrentPlayer`）。Rigidbody2D Dynamic・回転なし、`MoveTo` で経路に沿って移動、マップの範囲内、体当たり（`DamageKind.Contact`）。古い `Enemy.prefab`（Behavior Graph）はそのまま動く。
- **攻撃の情報**：`DamageInfo`（量・`DamageKind`・攻撃者・`PlayerCharacter`・位置・`GaugeJudge`）。`Health.Damage(int)` は残し `Damage(DamageInfo)` を追加、`OnDamagedInfo` / `LastDamage`。**攻撃を当てる処理は `DamageUtility.Apply(collider, info)` を通す**（`DamageReceiver` があればそちら＝部位・ギミック用、なければ Health）。範囲攻撃は `HitKey` で同じ Health に1回だけ。
- **敵を止める判定は `EnemyBlock.IsBlocked`**（`InputBlock` + デバッグ）。1体だけは `Enemy.Paused`。
- **2D ナビメッシュ** `NavMesh2D`（自動生成・シーンごと）：標準の NavMesh API を X 軸 -90° で XY に重ねる。壁＝動かない（Rigidbody2D なし／Static）・トリガーでない 2D コライダー（Bullet・Enemies レイヤーを除く）を `Collider2D.CreateMesh` → 床より 0.5 手前に置いて通れなくする。歩ける範囲は PlayerSceneContext の Tilemap。大きさ（小中大）ごとに `NavMesh.CreateSettings`。0.5 秒ごとに壁の変化を見て非同期で作り直す（作り直し中は材料を変えない）。OnDestroy でデータと設定を消す。NavMeshAgent は使わない。`NavMeshPath` はフィールド初期化で作れない（Awake で）。`EnemyNavArea`：敵だけ入れない／通りにくい（エリア 3、コスト 5）。
- **行動パターン**（`Enemy/Pattern/`）：`EnemyPattern`（SO）→ 段階 `EnemyPhase`（選び方・次へ進む条件・入ったときの演出）→ 行動 `EnemyAction`（`[SerializeReference]`、`[EnemyActionMenu("分類/名前")]` で「＋」メニューに出る。クラス名を変えるとデータが消えるので `[MovedFrom]`）。`EnemyBrain`（共通プレハブ）が実行。待つときは `brain.Wait` / `MoveUntil`（止められている間は数えない）。途中でやめるのは `brain.Interrupt()`（各行動の `Cleanup`、攻撃の `AttackExecution.Cleanup`）。
- **攻撃**（`Enemy/Attack/`）：`AttackData`（SO。予告→発動→後隙。発動したときの画面の揺れ `activeShake`＝`CameraShake`、11-13）、`AttackShape`（判定と予告のメッシュを同じデータから）、`AttackExecution`（実行。当てるのは `DamageUtility.Apply`）、`TelegraphView`（予告の表示。プール・Unlit 既定・描く順番 3）。突進はプレイヤーとの衝突を切ってすり抜け、離れてから戻す。弾は EnemyBulletPool を使う（`Configure` で速さ・ダメージ、`SetRules` ですり抜け・弾消し）。発動：一瞬／出し続ける／弾（狙い・全方位・うず巻き・ばらまき）／上から落ちる／下から突き出る／設置して起爆（`PlacedBomb`）。当てるのは `AttackHits`（タグのない DamageReceiver はギミックとしていつも当てる）。
- **実行の共通部分 `PatternRunner`**：EnemyBrain（体あり）と `AttackEmitter`（攻撃の発生源＝見えない敵。動く条件・止める命令）が継承。行動の部品・`AttackExecution` は PatternRunner を受け取る（`runner.Enemy` が null なら移動・突進・アニメは飛ばす）。
- **仲間を呼ぶ**：`SummonAction` → `Enemy.Spawn(data, pos)`（`Resources/Enemies/Enemy_Base` を読み、Awake の前にデータを入れる）、卵 `EnemyEgg`。同時に出せる数は PatternRunner が数える。倒されたら呼んだ仲間も倒す（`EnemyData.RemoveSummonedOnDeath`）。
- **ギミック連動**（`Enemy/Gimmick/`）：イベントの部品 `EnemyCommandEventAction`（止める・○秒・動かす・行動をやめさせる・段階・倒す・消す）／`EnemyDamageEventAction`／`DamageAreaEventAction`（一時的な AttackEmitter、`IgnoreEnemyBlock`）。`AttackSwitch`（攻撃で作動するスイッチ。DamageReceiver の OnHit を DamageRule で絞る。セーブキー `Switch/<シーン>/<ID>`）。範囲攻撃・プレイヤーの弾・敵の弾も DamageReceiver の付いた物に当たる。
- 敵の命令：`Enemy.SetPaused(bool, 色)` / `PauseFor` / `Kill`（効き方・無敵を無視）/ `Remove` / `Teleport` / `Stagger(秒)`。
- **倒したとき・倒した後**（段階6、`Enemy/Defeat/`）：EnemyData の `DefeatStyle`（消える／倒れた姿で残る／おとなしくなる＝NPC）・`drops`（`EnemyDrop` → `DroppedItem`、セーブしない）。置いた敵ごとに 倒したときのイベント・フラグ・倒した後に調べたとき（`DefeatedTalk`）・倒したことのセーブ（`Enemy/<シーン>/<ID か階層パス>`、`DefeatReturn`：同じ／いない／復活）。`Health.SetDeadSilently` で戻す。`Enemy.Defeated`（static イベント。相棒の一言が使う）。
- **グループ** `EnemyGroup`（`Enemy/Battle/`）：全部／○体倒したらイベント・フラグ・1回だけ（`Group/<シーン>/<ID>`）。撃破の順番（順番でない敵は 無敵バリア／○秒で復活／攻撃したら全員回復。`DamageFilter` を包んで体力 1 未満にしない）、次の敵に目印。**ウェーブ**：`BattleArea.waves`（`BattleWave`・`WaveSpawn`）、前のウェーブを全滅で次へ。
- **部位・ひるみ・見た目**（段階5）：`EnemyPart`（`Enemy/Parts/`。`DamageReceiver.Handler` で受け、本体へ倍率・`DamageInfo.noStagger`、`Priority` で範囲攻撃は部位を先に）。ひるみは `Enemy.AddStagger`（キャンセルできない攻撃中は `staggerPending` で待つ）、`AttackData.cancel`、`AttackExecution.CanCancel`。`Enemy.Stopped`＝Paused か Staggered（行動・戦闘エリアの捕まる判定はこちらを見る）。見た目は `EnemyLook`（Enemy が自動で付ける。色は全部ここでかける）。`EnemyData.ApplyDamageRules` は 0 ダメージを 0 のまま返す。
- **追跡・戦闘**（段階4）：行動 `ChaseAction`（見えている間は追う、見失ったら `LastSeenPosition` を探す、距離で速さ）。どの行動にも「見え方」の条件（`PatternRunner.CanSeeTarget`：Linecast で動かない当たり判定に当たると見えない）。`BattleArea`（`Enemy/Battle/`）：始まり方×勝ち方×負け方×負けたとき（**やり直す＝始まったときに Save.Commit とセーブしないフラグの写し → 負けたら仮置きを捨てて所持品を読み直し、同じシーンを暗転で読み込み直してやり直す位置から始め直す**。体力は満タン）、`BattleArea.AnyActive` の間は SavePoint 不可、時間は EnemyBlock 中は進まない、1回だけならセーブ `Battle/<シーン>/<ID>`。イベントの部品 `BattleControlEventAction`。画面表示 `BattleHud`（`Resources/UI/BattleHud.prefab`、Tools > 敵 > 戦闘の画面表示のプレハブを作る）。
- **効き方**：`EnemyData.damageRules`（`DamageRule`：`DamageKindFilter`〔DamageKind と同じ番号＋Any=-1〕・`CharacterFilter`・`GaugeJudge`・倍率、全部かけ算）→ `Health.DamageFilter`。0 以下は `OnBlocked`。
- **離れているとき狙う相手**：`PlayerSwitchManager` の設定（操作中だけ／近い方＝`EnemyTargetFrom`）。Enemy・PatternRunner・EnemyShooter が使う。
- **スポナー** `EnemyDataSpawner`（`Spawner/Enemy/`）：敵のデータから出す（種類・選び方・同時にいる数・合計・間隔・出る場所・条件・予告・全部倒したらイベント）。古い `EnemySpawner` は残している。Stage1_2 のスポナーはまだ古い Enemy.prefab（置き換えはシーン変更なので未実施）。
- **ツール**：攻撃の形のプレビュー（`AttackDataEditor`・`AttackPreviewAnchor`）、敵の一覧（`EnemyListWindow`、Tools > 敵 > 敵の一覧）、EnemyBrain の Gizmo、デバッグの「攻撃」（`PatternRunner.ForceAttack`）。サンプル：Tools > 敵 > サンプルを作る（`Assets/Data/Enemies/Sample/`）。
- 敵はプレイヤーに触れている向きへは進まない（重い敵がプレイヤーを押し出してイベント範囲に入れてしまうのを防ぐ）。
- `GameCondition`（`Common/`）：フラグ・アイテム・操作キャラ・パーティーの体力・範囲・仕掛けの状態・2人の状態。
- エディタ：Tools > 敵 > 共通プレハブとサンプルを作る、Hierarchy 右クリック > 敵 > 敵を置く／敵の通行設定エリア／攻撃の発生源／攻撃で作動するスイッチ／戦闘エリア／部位を追加／グループ／スポナー。デバッグウィンドウの「敵」。シーンチェックに敵の項目。
