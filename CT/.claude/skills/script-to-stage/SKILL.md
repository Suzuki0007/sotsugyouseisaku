---
name: script-to-stage
description: CT プロジェクトで、台本（会話の CSV の会話 ID と # の演出メモ）から、まだない場面・部屋・ステージを作るときの通しの手順。台本を場面に分けて「作る物の一覧」を作り、ユーザーと合意してから、会話（dialogue-csv）→ 部屋（room-build）→ イベント・敵・仕掛け（scene-recipe・event-part）→ 遷移とオートセーブ → テストシナリオ → ロードの確認、の順に1場面ずつ進める。F1_08〜F1_13（扉・広い部屋・階段）や二層（F2）の場面を作る、「台本の続きを作って」と言われたときに使う。
---

# script-to-stage：台本から場面を作る

今あるスキルをつなぐ**順番**を決めたもの。それぞれの細かい手順は各スキルにある。
大きな作業なので、**1場面（会話 ID のまとまり）ずつ終わったら報告とコミットメッセージを渡す**（CLAUDE.md）。

## 0. 台本を読む → 作る物の一覧（ここで合意する）
1. まだ置いていない会話：
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/dialogue-csv/scripts/import.ps1 -Lint Scenario_F1   # UNUSED の行
   ```
2. その会話 ID の範囲を CSV（`Assets/Data/Dialogue/Scenario_F*.csv`）で読む。`#` の行が演出の指示（場所・動き・音・暗さ・戦闘）。
3. 関係するルールを読む：`rules/scenes.md`（シーンごとのメモ・今の流れ）、`rules/map-room.md`、`rules/gimmick-party.md`、`rules/enemy.md`、ボス・特殊な部屋なら `rules/timpani.md`（作り方の手本）。
4. 場面ごとに一覧を作って**ユーザーに見せる**：

| 会話 ID | どこで（シーン・部屋） | きっかけ | 起きること（部品） | 新しく要る物 | 素材 |
|---|---|---|---|---|---|
| F1_08_AutoDoor | Stage1_2 の扉の前 | エリアに入る | 会話 → 扉が開く → シーンを移る | なし | 扉の開く音（空欄） |

   - **新しく要る物**＝今の部品・仕掛けでできないこと（新しい部品は `event-part`、新しい仕組みは方針を先に見せる）。
   - 新しいシーンが要るなら：名前（今の並び `Stage1_1`→`Stage1_2`→`Stage_1_3` に合わせる）・Build Settings に足すか・出現位置 ID。
   - 素材が無いもの（絵・音）は仮で作り、`asset-audit` で素材チェックリストに行を足す。
   - 台本で決まっていないこと（部屋の形・敵の数・謎解きの答え）は、**既定の案を書いて**確認する（質問は2つまで）。

## 1. 会話
- `dialogue-csv` スキル：CSV を直す（短く・テンポよく）→ `import.ps1 -Csv … -Lint …`。

## 2. 部屋・シーン
- `room-build` スキル：部屋の型（`RoomMenu`）→ マスク画像から歩ける範囲 → 点検。新しいシーンは `rules/scenes.md` の保存の約束に従う（**勝手に保存しない**。保存が要るときは理由を伝える）。
- 前のシーンからの遷移：部品「シーンを移る」＋出現位置（`[SpawnPointId]`）。

## 3. イベント・敵・仕掛け
- 置くのは **`scene-recipe` のレシピ**で（`recipes/<シーン>_<番号>_<内容>.cs`。作り直せるように）。今あるレシピの順番は SKILL.md。
- 足りない部品は `event-part`。敵は `Docs/敵の作り方.md`・`rules/enemy.md`、2人の仕掛けは `Docs/謎解きと2人の仕掛けの作り方.md`、出来事に反応させるなら合図（`Docs/合図の使い方.md`）。
- **通り道が変わるイベント（戻れなくなる・合流する・倒した）の最後に部品「オートセーブ」**。ゲームオーバーのリトライ位置（`BattleArea` のやり直す位置）も決める。

## 4. 確かめる
1. `unity-check -SceneCheck`（新しいシーンでも）。
2. `unity-playtest` の `event.ps1`：`-Advance`・`-Run`・`-CheckInteract`・`-Prompt`・`-Reach`・`-ShowArea` → `-Capture` で見る。
3. **テストシナリオを1本**：`Tests/Scenarios/<シーン>_<場面>.txt`（`rules/tests.md`。`key E` で調べる・最後に `reload` と `expect`）→ `scenario.ps1 -Name …`。前の場面のシナリオも流す（つながりが壊れていないか）。
4. ロード：イベントの前と後で `event.ps1 -Reload`（出た位置から進めるか・イベントが繰り返さないか）。

## 5. 締め
- `rules/scenes.md` にそのシーンの流れのメモ、`Docs/code-map.md`、CLAUDE.md の進み具合（問題点の要約の「台本の F1_08〜F1_13 は部屋がまだない」を更新）、`Docs/変更履歴.md`。
- `finish-task`。次の場面に進む前に、ユーザーの確認（遊んでみた感想）をもらう。
