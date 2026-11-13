---
name: room-build
description: CT プロジェクトで新しい部屋（マップの1区画）や、当たり判定・ギミックの多いマップ（ティンパニ部屋のような）を作るときの手順。部屋の型（RoomMenu）→ マスク画像から歩ける範囲（WalkAreaMask）→ 中身（イベント・戦闘・仕掛け）→ 点検（RoomChecks・シーンチェック）→ 再生で範囲の表示・届くかの確認・ロードの確認、の順に進める。部屋・ステージ・マップを作る／作り直すときに使う。
---

# room-build：部屋（マップ）を作る

設計とプランは `.claude/rules/map-room.md`（提案 D）。シーンの組み立ては `scene-recipe` のレシピで行い、レシピは残す。
**シーンは勝手に保存しない**：保存するときは「何のために」をユーザーに伝える（`rules/scenes.md`）。

## 手順

### 1. 部屋の型を作る（レシピ）
`scene-recipe` のレシピに `// @helpers` を書き、`RoomMenu.Build(種類, 部屋の絵の SpriteRenderer, "部屋ID")` で作る。
- 種類：`RoomKind.Normal`（普通）／`Battle`（戦闘エリア＋勝ったときのオートセーブ）／`Puzzle`（スイッチ2つ→答え合わせ→扉＋オートセーブ）。
- 部屋の絵がマップ全体の絵（MapBounds）と別なら、その SpriteRenderer を渡す（カメラの範囲・歩ける範囲の大きさがそれに合う）。
- 作り直せる形にする：ルート（`部屋_…（ID）`）を探して消してから作る。中身を足すのも同じレシピの中で。

### 2. 歩ける範囲を絵に合わせる
- **ユーザーやプランナーがマスク画像を用意した**：`WalkArea` の `maskTexture`（と `maskTarget`）を入れて `WalkAreaMask.Build(walkArea)` → 仮の四角（`床（仮の四角…）`）を消す。
- **マスクがない**：円・四角・多角形の子で形を作る（`rules/timpani.md` の座標の取り方。絵の上の形の中心と半径を読む）。柱や穴は `WalkAreaExclude`。作ったら `WalkAreaMask.Export` で白黒の画像を書き出しておくと、あとでプランナーが塗り直せる。
- イベントで開け閉めする通路は**マスクとは別の子**（四角）にして、SetActive で切り替える（非表示の子は範囲に入らない。表示すると敵と相棒の道も作り直される）。

### 3. 中身を置く
- 会話：`dialogue-csv` で DialogueData を作り、`AddTalk`（会話の部品に会話データを直接入れる）。
- 着いたときのイベント：型の `着いたとき（使うなら表示する）` を表示し、SceneStartEvent の条件（1回だけならフラグ）と中身を決める。
- 戦闘：敵を置いて戦闘エリアの Enemies に入れる。勝ち方・目的の文を決める。
- **通り道が変わるイベントの最後にはオートセーブ**（型の戦闘・謎解きには入っている）。

### 4. 点検
- `RoomChecks.Check(room)` を eval で出して、⚠ ✖ をなくす（Inspector の点検と同じ）。
- `unity-check -SceneCheck`（部屋の点検はシーンチェックにも出る）。

### 5. 再生で確かめる（`unity-playtest`）
```powershell
$ev = ".claude/skills/unity-playtest/scripts/event.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -ShowArea -Value 1   # 歩ける範囲の線（緑・赤・水色）を出す
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Capture room1       # 線が絵に合っているか見る
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Reach               # 今いる場所から、調べる物・出口・出現位置へ歩いて行けるか
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -CheckInteract       # 調べる物の範囲に入って、E キーの経路が生きているか
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -SaveOn              # （直接再生のとき）セーブを有効に
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Reload              # イベントの前後で、ロードしても困らないか
```
- `-Reach` の NG は、閉じた扉・まだ開いていない通路の先なら正しい（開けてからもう一度）。それ以外は、歩ける範囲の形・壁・調べる範囲の位置を直す。
- 敵と相棒の道を見るなら `NavMesh2D.ShowDebug = true`（デバッグウィンドウの「敵と相棒の道を表示」）。

### 6. 締め
- レシピを `scene-recipe/recipes/` に残し、`scene-recipe/SKILL.md` の表（作り直す順番）に足す。
- `rules/scenes.md` の「シーンごとのメモ」に部屋の流れを書く。`finish-task`。

## 落とし穴
- WalkArea は Awake で子の形を読む。**再生中に部屋を作っても形を読まない**（部屋はエディタで作る）。
- 部屋の絵の SpriteRenderer をシーンを開き直したあとに渡すときは、`GameObject.Find` で取り直す（開く前の参照は外れる）。
- 歩ける範囲が部屋の絵より大きいと、絵のない所を歩ける（点検で ⚠）。
- マスク画像は絵と同じ縦横比にする（違うと引き伸ばして重ねる。点検の結果に ⚠）。
