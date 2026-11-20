---
name: dialogue-csv
description: CT プロジェクトで会話（シナリオのセリフ）を足す・直すときの手順。CSV（Assets/Data/Dialogue/Scenario_*.csv）を書き換えて DialogueData に取り込み、長すぎる文・長い会話・話し手のない文・まだどこにも置いていない会話を点検する。台本からセリフを起こす・会話を直す・会話の量を見直すときに使う。
---

# dialogue-csv：会話を CSV で足す・直す

会話の元は **CSV**（`Assets/Data/Dialogue/Scenario_OP.csv`・`Scenario_F1.csv`・`Scenario_F2.csv`）。
**DialogueData（.asset）を直接直さない**。CSV を直して取り込み直す（取り込むと同じ名前の DialogueData は中身だけ上書き＝GUID は変わらないので、シーンの参照は切れない）。

## まず案を見せる（2026-11-20、ユーザーと合意）
- **新しく書く・書き直す・足すセリフは、CSV に入れる前に、会話の流れの形（話し手「本文」）でユーザーに見せて合意を取る**（入れてから「いらない」と言われると、取り込み・作り直し・確認がむだになる。11-20 に足した4つのうち3つを消した）。
- 見せるときは、変える前と後を並べる。どの場面か（シーン・きっかけ）も書く。足す行・消す行がわかるように。
- 頼まれた文をそのまま入れる・誤字を直すだけ、なら案を見せなくてよい。
- 案は **CSV を書き換える前に `text.ps1 -Snapshot`** → 書き換え → **`text.ps1 -Diff`** で、会話 ID ごとの「- 前 / + 後」を出して見せる（手で並べない）。だめなら CSV を戻すだけ（取り込みの前なら何も壊れない）。
- 合意したら：CSV → `text.ps1 -Check`（列・ID の重複・行がつながっていないか・口調）→ 取り込み＋`-Lint` →（会話を足す・消すときだけ）レシピ。シナリオは `rules/tests.md` の「変更の種類ごとの確認の範囲」に従う（文だけならシナリオは検索で当たった物だけ）。

## 使い方

```powershell
$d = ".claude/skills/dialogue-csv/scripts/import.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Csv Assets/Data/Dialogue/Scenario_F1.csv   # 取り込み（注意の行も出る）
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Lint Scenario_F1                           # 点検
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Csv Assets/Data/Dialogue/Scenario_F1.csv -Lint Scenario_F1 -MaxChars 40 -MaxLines 10
```

文の道具（Unity は要らない。CSV を直接読む）：
```powershell
$t = ".claude/skills/dialogue-csv/scripts/text.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $t -Csv Assets/Data/Dialogue/Scenario_F1.csv -Snapshot          # 書き換える前の写し（%TEMP%）
powershell -NoProfile -ExecutionPolicy Bypass -File $t -Csv Assets/Data/Dialogue/Scenario_F1.csv -Diff [-Head] [-Prefix F1_TC_]   # 写し（-Head なら最後のコミット）との差分
powershell -NoProfile -ExecutionPolicy Bypass -File $t -Csv "Assets/Data/Dialogue/Scenario_F1.csv,Assets/Data/Dialogue/Scenario_Landing.csv" -Check
powershell -NoProfile -ExecutionPolicy Bypass -File $t -Csv Assets/Data/Dialogue/Scenario_F1.csv -Export -Prefix "F1_02,F1_03,F1_TP_,F1_TC_" -Out <ファイル>   # 会話の全文（Markdown）
```
- `-Check` の `TONE` は **`data/tone.tsv`** の決まり（ノアは「ボク」・シグルは「俺」でていねい語なし・お面なので顔色や表情を使わない・間は「、、」）。キャラの設定が増えたら表に1行足す（`rules/dialogue.md` と合わせる）。注意なので、意図した文ならそのままでよい。
- `-Export` は話し手「本文」（表情）の形。ID の直前の `# 【演出】` を1つだけ斜体で付ける。**MUSE 向けの資料3（`Docs/MUSE向け/3_キャラと会話の確定事項.md`）の「4. 今の会話の全文」はこれで作り直す**（4 の見出しの次の行から下を置き換える）。
- 表示名は `data/speakers.tsv`（キャラ → ノア・シグル）。

点検（`import.ps1 -Lint`）で出るもの：
- `LONG-LINE`：1行（改行で区切った1つ）が MaxChars（既定 40 字）より長い → 2つの文に分けるか、削る。
- `LONG-TALK`：1つの会話が MaxLines（既定 10 文）より長い → 途中で区切る（間に演出を挟む・別の会話 ID にする）。
- `NO-SPEAKER`：キャラも名前もない文（地の文なら問題なし）。
- `UNUSED`：どのシーン・プレハブからも使われていない会話（まだ部屋がない・置き忘れ）。

## CSV の書き方
- 列：`会話ID, キャラ, 表情, 名前, 本文`（1行目は見出し）。**UTF-8（BOM 付き）**で保存（Excel なら「CSV UTF-8」）。
- 会話ID は `F<層>_<番号>_<場面>`（例 `F1_07_SigleIntro`）。ID を書いた行から次の ID までが1つの会話。ID が空の行は前の会話の続き。
- `#` で始まる行は演出のメモ（取り込まれない）。台本の演出指示はここに残す。
- キャラは CharacterProfile の名前（`Noah`・`Sigle`）。それ以外の人物（ティンパニ・ヴェルツィー・カイロス・？？？）はキャラを空にして「名前」の列だけ。正体を隠すシグルは キャラ=`Sigle`・名前=`？？？`。
- 表情は `Expression` の名前（Normal・Smile・Angry・Sad・Surprise・Troubled など）。立ち絵は今「通常」だけなので、ほかの表情も通常の絵で出る。

## ユーザーの好み（シナリオ）
- **1回の会話・1つの文を短く、テンポよく**（全部を説明しなくてよい）。点検の LONG-LINE・LONG-TALK を目安に。
- カイロスに触れるときは「あの街一番の調律師」など、どんな人かわかるように。
- 表情は台本・シナリオから推測して入れる。

## 取り込んだあと
1. 点検（`-Lint`）。
2. 新しい会話をシーンに置くなら：イベントの部品 DialogueEventAction ＋ 子の「会話」に Dialogue（`scene-recipe` の `AddTalk`）。
3. 再生して文が収まるか見る（`unity-playtest` の `event.ps1 -Advance` と `-Capture`）。会話の枠は 1600×260、文字が多いと折り返して3行を超える。
