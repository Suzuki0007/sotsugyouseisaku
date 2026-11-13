---
paths:
  - "Assets/Script/Map/**"
  - "Assets/Script/Gimmick/WalkArea*.cs"
  - "Assets/Script/Camera/**"
  - "Assets/Script/Enemy/Navigation/**"
  - "Assets/Script/Party/PartyRoute.cs"
---

# マップの範囲・部屋づくり（提案 D）

ユーザーと合意した設計（2026-11-13）。**1段階ずつ報告とコミットメッセージ**。

## 決めたこと（ユーザーの選択）
- 歩ける範囲の描き方：**別のマスク画像に白く塗る**（マップの絵と同じ大きさ。白＝歩ける、透明・黒＝歩けない。内側の輪郭は穴）。
- Stage1_1・Stage1_2 のカメラ：**マップの絵（MapBounds）に合わせる**（絵の外が映る問題を直す）。

## 段階
1. **範囲の一本化（完了）**：`Map/MapArea`（窓口）。
   - 歩ける範囲＝`WalkArea`（あれば。シーンに何個でも＝全部を合わせる）→ `MapBounds`（マップの絵）→ Tilemap（`PlayerSceneContext.PlayerLimitTilemap`）。
   - 映す範囲＝`MapBounds` → Tilemap（`CameraArea` がある所はそちらが先）。
   - 使う所：プレイヤー（`PlayerMapLimit`：MapBounds＋`WalkArea.Clamp`。変えていない）、カメラ（`FocusCamera`：Tilemap の前に `MapArea.TryGetViewRect`）、敵の移動（`Enemy.ClampToMap`＝`MapArea.Clamp`、**Paused の間は収めない**＝イベントで上から降らせる・画面外から入れるため）、敵と相棒の道（`NavMesh2D`：床の四角＝`MapArea.TryGetWalkRect`、WalkArea があれば床を 0.2 マスの升目で形どおりに・`WalkArea.Signature` が変わると作り直す）、相棒のまっすぐの道（`PartyRoute.IsClear`：0.25 マスごとに WalkArea の中か）。
   - `WalkArea`：多角形の全部の輪郭で偶奇判定（内側＝穴）、子に `WalkAreaExclude`（歩けない形）。`Clamp` は外・歩けない形の中から一番近い歩ける所へ（ふちの点を 0.01 内側へ押す）。
   - 影響：Stage1_1・Stage1_2 は WalkArea がないので、敵の移動と道の範囲が「Tilemap（とても広い）」→「マップの絵」に変わった（絵の外へ出ない）。カメラも絵の中で止まる。
2. **マスク画像から作る道具（完了）**：WalkArea の Inspector（`Gimmick/Editor/WalkAreaEditor`）。
   - 欄：マスク画像・合わせるマップの絵（空ならシーンの MapBounds の絵）・線の細かさ（0.1）・白とみなす明るさ（白さ×不透明さ 0.5）。
   - 「マスク画像から作る」（`WalkAreaMask.Build`）：マスクを「線の細かさの半分」くらいの升目で読み（RenderTexture に描いて読むので取り込み設定は関係ない）→ マーチングスクエアで輪郭（斜めの2点は別々の角）→ ダグラス・ポイカーで間引き → 小さすぎる輪郭は捨てる → 子「マスクから作った範囲」の PolygonCollider2D（輪郭ごとに path。内側は穴）。押し直すと作り直す（Undo できる）。
   - 「今の形をマスク画像に書き出す」（`WalkAreaMask.Export`）：表示されている子の形を白黒の PNG（横 最大 2048）に。マスク画像が空なら書き出した物を入れる。
   - 形を足すボタン（円・四角・歩けない円・歩けない四角。シーンビューの中心に作る）。
   - 確かめ：ティンパニ部屋で書き出し → 読み込みの往復が元の形と 99.7% 一致（ふち沿いの間引きの差だけ。点 53 個、26ms）。マスクに黒い円を塗ると穴になる。**シーン（Stage_1_3）は今の円・六角形のまま**（作り直していない）。
   - イベントで開け閉めする通路は、マスクとは別の子にする（書き出しも表示されている子だけ）。
3. **部屋の型と点検（完了）**：
   - `Map/Room`（部屋の名前・ID・種類 `RoomKind`・部屋の絵〔空なら MapBounds の絵〕・メモ。動きはなし＝目印と点検）。
   - `Map/Editor/RoomMenu`：Hierarchy 右クリック > 部屋 > 普通の部屋／戦闘の部屋／謎解きの部屋。**部屋の絵（SpriteRenderer）を選んでから作るとその絵に合わせる**（なければシーンビューの真ん中に 16×12）。`RoomMenu.Build(kind, image, id)` はレシピからも使える。
     - 共通：Room・歩ける範囲（WalkArea。合わせる絵＝部屋の絵、仮の四角＝絵の 8 割。マスクから作り直す）・カメラの範囲（絵と同じ大きさ）・出現位置（`<ID>_Entrance`、絵の下のほう）・着いたとき（SceneStartEvent＋流れ〔待つ 0.3〕。**非表示**＝使うなら表示）。
     - 戦闘：戦闘エリア（絵の 6 割の四角。入ったら始まる・敵を全部倒すと勝ち・負け方なし・1回だけ `<ID>_Battle`・目的「敵を全部倒せ」）・やり直す位置（出現位置と同じ）・始まったとき／勝ったとき（待つ 0.5 → オートセーブ）／負けたとき。敵は「敵 > 敵を置く」で足して戦闘エリアの Enemies に入れる。
     - 謎解き：スイッチ2つ（`GimmickMenu.BuildSwitch`）→ 答え合わせ（`BuildPuzzle`、正解したとき＝待つ 0.3）→ 扉（`BuildDoor`、条件＝答え合わせの状態。開いたときの最後にオートセーブ）。
   - `Map/Editor/RoomChecks`（Inspector とシーンチェックで共通）：部屋の ID・歩ける範囲がある・範囲が部屋の絵の中・出現位置が範囲の中・カメラの範囲がある／画面（4:3）より小さい／歩ける範囲を覆っている・調べる物と出口（トリガーの範囲の 5×5 の点）が範囲から届く（**非表示の通路の先なら案内**）・戦闘エリアのやり直す位置・勝ったときの最後のオートセーブ・着いたときのイベントが空。
   - `Map/Editor/RoomEditor`：点検の一覧（✓ ℹ ⚠ ✖、「選ぶ」ボタン）・「カメラの範囲を部屋の絵に合わせる」・「歩ける範囲を選ぶ」。
   - シーンチェック `CheckRooms`：部屋の点検（出現位置の指摘は「歩ける範囲」の点検と重なるので出さない）と、部屋の外のカメラの範囲が画面より小さい。
   - 確かめ：新しいシーンで3種類を作って点検がすべて OK、出現位置を外へずらすとエラー、ティンパニ部屋のルートに Room を付けると案内2つ（左の出口＝非表示の通路の先、戦闘の勝ったときにオートセーブなし＝クリアは TimpaniRoom のイベント側にある）。**Stage_1_3 には Room を付けていない**（付けるなら保存が要る）。
   - 注意：再生中に部屋を作っても WalkArea は形を読まない（Awake が子より先）。部屋はエディタで作る。
4. **ゲーム中の表示・届くかの確認・スキル・手順書（完了）**：
   - `Map/MapDebugView`：ゲーム画面に線（緑＝歩ける範囲・赤＝歩けない形・水色＝マップの範囲）。LineRenderer（URP 2D の Unlit のスプライトのシェーダー、並び順 400）。`WalkArea.Signature` かマップの範囲が変わると描き直す。`MapDebugView.Show`。形は `WalkArea.CollectOutlines`。
   - `Map/MapReach`：今いる場所から、調べる物（トリガーの範囲がある Intaractable）・出口（SceneChange）・出現位置へ、NavMesh2D（小さい体）で行けるか。対象の範囲のそばの床（`SamplePosition`、範囲の大きさ＋0.6）→ `IsReachable`。行けない理由（そばに床がない／今いる所が床でない／道がつながっていない）。`Check` / `Report`。
   - デバッグウィンドウ「マップの範囲（歩ける範囲・届くか）」：歩ける範囲を表示・敵と相棒の道を表示・歩ける範囲の決め方・「届くかの確認」（行けない物が上、選ぶ・ワープ）。
   - `event.ps1 -Reach` / `-ShowArea -Value 1`。
   - スキル `room-build`（部屋の型 → マスク → 中身 → 点検 → 再生で表示・届く・ロード）。
   - 確かめ：ティンパニ部屋で線が円と中央の形に重なる。届くか 11/12（左の出口だけ NG＝閉じた通路の先）→ 通路を表示すると 12/12（敵の道も作り直された）。
**D は全段階完了**。

## 注意
- `FocusCamera`・`PlayerMapLimit`・`MapBounds`・`PlayerSceneContext` は成田さんのコード。範囲を聞く先を変える数行だけにして、コミットメッセージで説明する。
- WalkArea は Awake で子の形を読む。再生中に子を足しても読まない（表示・非表示の切り替えは効く）。テストで歩けない形を足すときは、親を非表示で作って子を足してから WalkArea を付けて表示する。
- 確かめ方：`NavMesh2D.Instance.SamplePosition(点, 0.05, Small)` で床があるか、`IsReachable`、`CalculatePath` の曲がり角。`MapArea.TryGetViewRect` と Tilemap の範囲を比べる。
