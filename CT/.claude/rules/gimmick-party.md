---
paths:
  - "Assets/Script/Gimmick/**"
  - "Assets/Script/Party/**"
  - "Assets/Script/Camera/**"
  - "Assets/Script/Intaract/**"
  - "Assets/Script/Manager/PlayerSwitchManager.cs"
  - "Assets/Script/Player/**"
  - "Assets/Resources/Barks/**"
  - "Assets/Resources/Notes/**"
  - "Assets/Prefab/Gimmicks/**"
---

# 謎解き・2人の仕掛け（仕掛け・2人・カメラ・調べるを触るとき）

手順書は `Docs/謎解きと2人の仕掛けの作り方.md`（22 章）。ファイルごとの役割は `Docs/code-map.md` の「3. スクリプト一覧」の Gimmick / Party の行を読む。
段階 0〜5 完了、段階6 は相棒の一言だけ（ユーザーの指示：ヒント・待っている相棒の振る舞い・絆の段階・通信・ピンチの助けは作らない）、段階7 は一言のツール・仕掛けの一覧・サンプルの部屋。

## 2人の状態・体力（段階0）
- **相棒の合流**：`PlayerSwitchManager.sigleJoinedFlag`（`Flag_Sigle_Joined`）が立つまでシグルは非表示でノア1人（`PartnerAbsent`。体力はノアの分、C・F・SetMode は効かない、相棒の一言〔`Bark.CanSpeak`〕も出ない）。`GameFlags.OnChanged` でフラグが立つとノアの位置に出て一緒になる。空ならいつも2人。相棒の一言は、自動のもの（`Bark.Fire`）だけ止める（イベントから出す一言はノアが話せる）。Stage1_1・Stage1_2 に入っている（Stage1_2 の追跡の後の助けの場面で立つ）。イベントでフラグを立てた直後に別の場所へ移すときは、先に「待機」にする（一緒のままだと、ついてくる動きでノアの所へ戻る）。
- 攻撃は操作中のキャラだけ。2人の状態 `PartyMode`（一緒／待機／別行動）。体力は一緒＝シグルの体力を共有（`Health.ShareWith`）、離れる＝別々（シグル 6・ノア 3）。`PartyHealth`・`HealthOf`。
- **ノアとシグルの体どうしはすり抜ける**（`PlayerSwitchManager.IgnorePartnerCollision`。当たり判定を有効にし直すと無視が消えることがあるので ApplyFollow のたびにかけ直す）。プレイヤーは物理の速さを使わない（`PlayerMovement.FixedUpdate` が毎回 0 にする。操作できなくなったら向きも消す）。
- **プレイヤーの LateUpdate の順番は固定**：PlayerFollower（0）→ PlayerMapLimit（100）→ PlayerHistory（200）→ CharacterAnimLink（300）。履歴は位置が決まってから記録する（崩れると、ついてくる相棒が震える）。マップの端は動く前に止める（`CharacterMovement.LimitNextPosition` → `PlayerMapLimit.ClampToMap`）。ノア・シグルのプレハブの Rigidbody2D は Interpolate をオン（オフだと 6 フレームに 1 回止まる）。
- `PlayerSwitchManager.ApplyBodyCollision`：シグルの体の当たり判定は、ついてきている間だけプレハブのまま（無効）・操作中／待機／別行動は有効。
- **`Intaractable` を継承したクラスに `Update()` を作らない**（土台の private な `Update` が E キーを見ているので、作ると調べられなくなる。毎フレームの処理は `LateUpdate`。GimmickInteractable の子は `protected override void LateUpdate()` で `base.LateUpdate()` を呼ぶ）。Intaractable は成田さんのコードなので、土台は変えずにこの約束で守る。
- **`Intaractable`（調べる）は操作中のキャラだけに反応**（範囲の中のプレイヤーを覚え、操作中のキャラだけを player にする。`OnPlayerEnter` も操作中のキャラだけ）。
- 使えるキャラ `CharacterRestriction`（両方／ノアだけ／シグルだけ）。`GimmickSwitch` は範囲の中の**操作中のキャラ**だけ（`Intaractable` の player はついてくる相棒のこともあるため、自分で確かめる）。

## 仕掛けの土台（`Assets/Script/Gimmick/`）
- **入力 → 状態 → 条件 → 動く物**：入力（`GimmickSwitch` 調べる・`AttackSwitch` 攻撃・`GimmickEventAction` イベント）が `GimmickState`（ON/OFF か 0〜n、`Gimmick/<シーン>/<ID>` でセーブ、固定は `/Locked`）を変え、`GimmickReaction`（条件で表示切り替え・イベント、一度満たしたらずっと＝`Reaction/<シーン>/<ID>`）や `PuzzleCheck`（全部 ON／順番／組み合わせ、正解で自分の GimmickState を ON・入力を固定）が `GameCondition.gimmicks`（`GimmickCondition`）で見る。`GameCondition` に `party`（一緒／離れている）・`partnerWithin`。
- **基本の部品**（段階2）：`FloorSwitch`（ノア・シグル・押せる箱・敵、必要な数、乗っている間だけ／一度で ON／切り替え）、`PushBlock`（操作中のキャラが押し続けると 1 マス、押せるキャラ、`Block/<シーン>/<ID>`、`ResetToHome`、`PushBlockResetEventAction`、「2人で押す」）、`GimmickHole`（箱が落ちると埋まる）、`ItemSocket`（置き台。Choices で何をはめるか選ぶ）、`GimmickVisual`（状態ごとの絵・色・表示する物・有効にする部品）、`GimmickState` の時間で戻る、`GimmickReaction` の Moves。調べる部品の共通の土台は `GimmickInteractable`（範囲の中の操作中のキャラだけ・断るときの反応）。

## 2人の能力（段階3、`Assets/Script/Party/`）
- **待て／ついてこい**（`PlayerSwitchManager` の F キー、`ToggleWait`。呼び戻しは `PartyRoute`〔まっすぐ→ナビメッシュの回り道〕で道があるときだけ。`PlayerHistory.SeedPolyline`。`WaitCommandLocked` はイベントの部品で）。
- `CharacterBarrier`（キャラだけ通れる壁＝`Physics2D.IgnoreCollision`、入ったら相棒を待たせる）、`BreakableWall`（攻撃したキャラ・種類で壊れる、GimmickState でセーブ）、`ConditionalVisible`（GameCondition で見える・当たり判定）、`LiftPoint`（2人そろって調べると移る）。
- **相棒の目印** `PartyHint` / `PartyHintView` / `PartyHintSettings`（`Resources/PartyHints.asset`）。

## 離れ離れ（段階4）
- `MapCameraAspect`（11-13）：マップのカメラは画面の中央 4:3（`Camera.rect`）。`cam.aspect` も 4:3 になるので、カメラの範囲の計算・ビューポートの判定はそのままでよい。画面の端を `Screen.width` で計算する処理を作るときは、カメラの `pixelRect` を使う。
- `CameraArea`（Is Trigger の四角。`FocusCamera` はその中ならその範囲で、なければ Tilemap で止める。範囲が変わるとなめらかに移る）、`FocusCamera.BeginBlend` / `LookAt`、`CameraLookEventAction`。
- `PlayerSwitchManager` の「離れているとき」設定（切り替えの演出：すぐ／スライド／暗転〔`SceneFader.FadeThrough`〕、敵が狙う相手：操作中だけ／近い方＝`EnemyTargetFrom`）。
- `PartyRejoinArea`（2人がそろったら一緒に）、`PartyWarpEventAction`（キャラを移す）、`PartyModeEventAction`。
- `PartyPersistence`（`Save.BeforeCommit` で `Party/*` を記録し、`PlayerPartySpawner` が同じシーンなら復元。違うシーンでは一緒に戻る。**体力**〔`Health.OwnHealth`〕も記録し、どのシーンでも戻す。記録が 0 以下・記録なしなら 2人とも満タン）。

## ホラー・謎解きの演出（段階5）
- 謎解きの画面 `PuzzleUI`（実行中に作る uGUI。番号錠・リストから選ぶ・文章〔ページ〕。`InputBlock` に `PuzzleUI.IsBusy`。見た目は `Resources/PuzzleUI.asset`）、`CodeLock`。
- メモ `NoteData`（**`Resources/Notes/` に置く**）・`NoteBook`（`Note/<ID>`）・`NotePickup`・`NoteEventAction`、メニュー画面の「メモ」（`GameMenu.noteButton`）。
- `ExamineSequence`（調べるたびに変わる。`Examine/<シーン>/<ID>`）。
- 即死 `InstantDeath`・`DeathTrap`・`DeathEventAction`・`RetryPoint`（入ると Save.Commit と一時フラグの写し → 即死で仮置きを捨てて同じシーンを読み直す）。
- 敵の行動「見ていない間に近づく」（`UnseenApproachAction`。`CharacterMovement.LastDirection`）。サンプル `EnemyData_Sample_像`。

## 相棒の一言（段階6、`Assets/Script/Party/Bark/`）
- `BarkData`（**`Resources/Barks/` に置くだけ**で自動。きっかけ `BarkTrigger` 18 種〔番号は末尾に足す〕・条件・優先度・確率・回数〔`Bark/<名前>` でセーブ〕・次に出せるまで・候補〔行が2つ以上で掛け合い〕・使う）。
- `Bark.Fire(trigger, BarkContext)` をきっかけの側から呼ぶ（GimmickInteractable の `Refuse`／`RefuseCharacter`・PushBlock・PartyHintView・PlayerSwitchManager の来られない・PuzzleCheck／CodeLock）。`BarkWatcher`（シーンごとに自動）が 2人の状態・切り替え・ダメージ／体力・`Enemy.Defeated`・待たせている時間・止まっている時間・シーンに着いたを見る。
- `BarkView`：ゲームを止めない吹き出し（画面の外は画面の下に名前つき、会話・メニューでやめる）。設定 `Resources/BarkSettings.asset`。イベントの部品 `BarkEventAction`、`BarkArea`。
- ツール：一言の一覧（`BarkListWindow`）・CSV（`BarkCsv`。`DialogueCsv` の Parse などを internal にして使い回し）。サンプル `Resources/Barks/Sample/`（16 個、使うはオフ）。

## ツール（段階7・エディタ）
- 仕掛けの一覧（`GimmickListWindow`、Tools > 仕掛け > 仕掛けの一覧。つながっていない状態をオレンジで）。
- サンプルの部屋（`GimmickSampleBuilder`、Tools > 仕掛け > サンプルの部屋を作る → `Assets/Prefab/Gimmicks/Sample/` に 6 つ。部品は `GimmickMenu.Build*`。説明は `GimmickSampleMemo`）。
- Hierarchy 右クリック > 仕掛け > …（選んだスイッチが条件・入力に入る。複数選んでも 1 回だけ作る `GimmickMenu.FirstCallThisClick`。仮の見た目は子の「見た目」に Unity 付属の絵を拡大〔付属の絵は Sliced にすると警告が出るため〕）。
- 配線の表示 `GimmickWiring`（シーンの全コンポーネントから GimmickState への参照を探して矢印。Tools > 仕掛け > 配線をいつも全部表示）。シーンチェック・デバッグウィンドウに「仕掛け」。
