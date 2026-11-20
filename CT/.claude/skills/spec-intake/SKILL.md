---
name: spec-intake
description: CT プロジェクトで、MUSE.AI など外部で書かれた指示書（新しいシーン・部屋・イベントの仕様、会話 CSV、仮素材のフォルダ）を受け取って実装するときの入口の手順。指示書の名前（フラグ・シーン・会話 ID・出現位置 ID）を今のプロジェクトの決まりに読み替えた対応表と、すでにある物とのぶつかりを scan.ps1 で出し、座標を左上基準から中心基準・Unity の座標に直し、作る物の一覧で合意してから実装し、最後に「指示書との違いの報告」を返す。「指示書に従って作って」「MUSE の指示書」「仕様書を渡すので実装して」と言われたときに使う。
---

# spec-intake：指示書を受け取って実装する

指示書を書く側（MUSE.AI）への決まりは `Docs/MUSE向け/1_指示書を書くときの決まり.md`。この手順はその受け手側。
実装の中身は今のスキル（`script-to-stage`・`room-build`・`scene-recipe`・`dialogue-csv`・`asset-intake`）で行う。ここで決めるのは **入口（読み替え・合意）と出口（報告）**。

## 1. 読む・読み替える（ここまでで 1 回だけ合意する）
1. 指示書・会話 CSV・仮素材の README を全部読む。添付の zip はスクラッチパッドに展開する（プロジェクトに直接置かない）。
2. 名前の対応表とぶつかりを出す：
   ```powershell
   $s = ".claude/skills/spec-intake/scripts/scan.ps1"
   powershell -NoProfile -ExecutionPolicy Bypass -File $s -File <指示書.md> -Out <スクラッチパッド>/names.md
   ```
   - `flag`：`FLAG_TC_DOOR_EVENT_DONE` → `Flag_TC_DoorEventDone`。`EXISTS` は同じ意味なら使い回す・違えば新しい方の名前を変える。`place part ... is not used yet` は場所の略が抜けている（`FLAG_WEST_PIPE_ON` → `Flag_TC_WestPipeOn`）。
   - `scene`：今ないシーン名は `Stage<層>_<番号>` に（`Stage1_TCorridor` → `Stage1_4`）。
   - `dialogue`：`EXISTS` の会話 ID を書き換える指示なら、`dialogue-csv` の「まず案を見せる」に従う。
   - `[CHECK] both origins appear`：表ごとに基準（左上か中心か）を確かめる。
3. 座標を直す（表の数だけ繰り返す）：
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File $s -Rect "990,0,180,180" -Map 2160x1440   # 左上の x,y,w,h → 中心・Unity の座標・レシピの TL()/P()
   powershell -NoProfile -ExecutionPolicy Bypass -File $s -Point "0,585" -Map 2160x1440 -Center # 中心基準の点 → 左上・Unity
   ```
   - 出現位置と、入ると動く範囲（出口・開始ライン）は **1.2 マス以上**離す。近ければ直す案を一覧に書く。
4. 作る物の一覧（`script-to-stage` の 0 の表）に、**対応表・変える座標・指示書と違えるところ・決まっていない所の既定の案**を添えて見せ、1 回で合意する（質問は 2 つまで）。指示書が「実装は任せる」なら、一覧は報告に回して先に進めてよい。

## 2. 実装する
- 作業の前に `unity-check/scripts/preflight.ps1 -Recipe <レシピの頭>`（保存していないシーン・再生中・ブランチ。[ASK] があれば最初に聞く）。
- 会話 → 部屋 → イベント → シナリオ → ロード の順（`script-to-stage`）。会話は `text.ps1 -Snapshot` してから CSV を直す。
- 仮素材が足りない・別の物で代用したら `Substitute(...)`（`asset-audit` の代用の記録）。

## 3. 確かめる
- `rules/tests.md` の「新しいシーン」の行（通しのシナリオ＋直接再生のシナリオ＋シーンチェック）。
- **配置の撮影**：`room-build/scripts/mapshot.ps1 -Scene <シーン>` → PNG を報告に添付（SendUserFile）。範囲の重なり・物の寄りすぎ・出口と出現位置の近さをここで見る。
- **仕掛けの流れ図**：`FlowMap.Build()`（Tools > 手順書 > 仕掛けの流れ図を作り直す）。注意が出たら直すか、受け口なら `Docs/仕掛けの流れ/_例外.tsv` に書く。

## 4. 報告を返す
- `templates/report.md` を写して `Docs/MUSE向け/報告/<日付>_<場所>.md` に書く（名前の対応表は names.md から変えた物だけ、会話の違いは `text.ps1 -Diff -Head` の結果）。
- `Docs/MUSE向け/2_今のゲームの状態.md`（シーン・フラグ・未定）と、会話を変えたら 3 の「4. 今の会話の全文」（`text.ps1 -Export`）を更新する。
- ユーザーへの報告には、報告ファイルへのリンクと、MUSE.AI に渡す物（報告・2・3・CSV・撮影の PNG）を書く。
