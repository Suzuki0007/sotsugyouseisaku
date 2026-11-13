---
name: dialogue-csv
description: CT プロジェクトで会話（シナリオのセリフ）を足す・直すときの手順。CSV（Assets/Data/Dialogue/Scenario_*.csv）を書き換えて DialogueData に取り込み、長すぎる文・長い会話・話し手のない文・まだどこにも置いていない会話を点検する。台本からセリフを起こす・会話を直す・会話の量を見直すときに使う。
---

# dialogue-csv：会話を CSV で足す・直す

会話の元は **CSV**（`Assets/Data/Dialogue/Scenario_OP.csv`・`Scenario_F1.csv`・`Scenario_F2.csv`）。
**DialogueData（.asset）を直接直さない**。CSV を直して取り込み直す（取り込むと同じ名前の DialogueData は中身だけ上書き＝GUID は変わらないので、シーンの参照は切れない）。

## 使い方

```powershell
$d = ".claude/skills/dialogue-csv/scripts/import.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Csv Assets/Data/Dialogue/Scenario_F1.csv   # 取り込み（注意の行も出る）
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Lint Scenario_F1                           # 点検
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Csv Assets/Data/Dialogue/Scenario_F1.csv -Lint Scenario_F1 -MaxChars 40 -MaxLines 10
```

点検で出るもの：
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
