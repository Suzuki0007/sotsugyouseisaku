---
name: asset-audit
description: CT プロジェクトの素材チェックリスト（Docs/素材チェックリスト.xlsx）を、Unity の今の状態と照らし合わせて更新する。シーン・データ・プレハブを調べ直して、空いていた欄に素材が入ったか・仮の絵が使われなくなったかを行ごとに判定し、「自動の点検」の列に書く（解決した行は進み具合を「確認待ち」に）。新しくできた空き欄（どの行にも入っていない物）も出す。素材を入れた後・素材の進み具合を聞かれたとき・チェックリストを作り直す／行を足すときに使う。
---

# asset-audit：素材チェックリストの点検

チェックリスト：`Docs/素材チェックリスト.xlsx`（シート「使い方」「素材リスト」「集計」「仕様の目安」）。
チームは Google スプレッドシートで直していることがある。**Google 側が正本になっているときは、ユーザーに最新の xlsx を書き出してもらってから点検する**（xlsx だけ直すと食い違う）。

## 使い方

```powershell
$a = ".claude/skills/asset-audit/scripts/audit.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $a              # 調べ直して報告だけ（ファイルは変えない）
powershell -NoProfile -ExecutionPolicy Bypass -File $a -Apply       # 「自動の点検」の列に書き、解決した行を「確認待ち」にして保存
powershell -NoProfile -ExecutionPolicy Bypass -File $a -NewSlots    # どの行にも入っていない空き欄も出す（行を足す候補）
powershell -NoProfile -ExecutionPolicy Bypass -File $a -NoSurvey    # 前回の調査結果を使う（速い）
powershell -NoProfile -ExecutionPolicy Bypass -File $a -SetKeys     # data/keys.tsv から空の「確認キー」を埋める（-ForceKeys で上書き）
```

- 調査（`scripts/survey.cs`）は **Build Settings の全シーンを順に開いて、元のシーンを開き直す**（30 秒ほど）。再生中・開いているシーンに保存していない変更があると止まる（ユーザーに聞く）。
- **xlsx を Excel で開いていると書けない**（読み取り専用と出る）。閉じてもらってから `-Apply`。
- 結果の文字の色：緑＝解決／黄土＝まだ／赤＝欄が見つからない（キーが古い：シーン・部品の名前が変わった）。
- 「確認待ち」になった行は、人が Unity で見て（鳴る・見える）から「完了」にする。**自動で「完了」にはしない**。

## 見た目（`scripts/format.ps1`）
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/asset-audit/scripts/format.ps1
```
- ユーザーの指示（2026-11-13）：**既定の並び＝区分（プルダウンの順：効果音→BGM→立ち絵→キャラの絵・モーション→敵→エフェクト→UI→背景・マップ→一枚絵・演出→その他）→ 優先度（A→C）→ ID**。**ID と区分のセルは区分ごとの色（同じ色）**。**優先度の列は優先度ごとの色**（A＝薄い赤・太字、B＝薄い黄、C＝色なし。縞はかけない）。**その右（素材名）から右の列は、白と薄いグレーの縞模様**（絞り込んでも縞がそろう）。**一番左に「済」の列**（素材の収集・作成が終わったら ✓。プルダウン。✓ の行は文字が灰色・✓ のセルは緑）。
- 何度かけても同じ結果（「済」の列は1回だけ足す）。値は変えない。行を足した・並びが崩れたときにかける。`build.ps1` の最後にも自動でかかる。
- 色は `data/config.json` の `format`（区分の色・縞の色）。区分を増やしたら色も足す。
- `audit.ps1 -Apply` は「自動の点検」の**文字の色だけ**変える（緑＝解決・黄土＝まだ・赤＝欄が見つからない）。セルの色は変えない（縞・区分の色を守るため）。
- 「済」（素材ができた）と「進み具合」（Unity に入れて確かめた）は別。✓ が付いても自動の点検が「まだ」なら、Unity に入れていない。

## 確認キー（「素材リスト」の「確認キー（自動）」列）
1つの行に `;` 区切りで複数書ける。`@場所` は「シーン名・アセットのパス・オブジェクトのパス」の一部（その場所だけを見る）。

| キー | 解決とみなす条件 | 例 |
|---|---|---|
| `slot:部品.欄@場所` | その部品の欄が全部空でなくなった | `slot:TimpaniPeg.successSe` |
| `nouse:ファイル名の一部@場所` | その絵・データが、絵・欄・アニメのどこにも使われなくなった（仮素材の差し替え） | `nouse:res_kari/kari/hp.png` |
| `has:プレハブかシーン:部品` | その部品が付いた | `has:Player_Noah.prefab:FootstepSe` |
| `chara:キャラ:表情` | `Data/Characters/キャラ.asset` にその表情の絵がある（Normal・Smile・Angry・Sad・Surprise・Troubled） | `chara:Noah:Surprise` |
| `anim:名前の一部:動き` | その名前を含むアニメのデータに、その動きの絵がある | `anim:Sigle:Walk` |
| `item:アイテム` | `Resources/Items/…/アイテム.asset` にアイコンがある（Unity 付属の絵は除く） | `item:Item_Lantern` |

- キーの元は `data/keys.tsv`（ID → キー）。キーがない行（今後の素材・質の改善など）は手で確認する。
- 行を足すとき・キーを直すときは、`keys.tsv` も直して `-SetKeys`（チームがキーを直していないなら `-ForceKeys`）。
- `-NewSlots` に出したくない欄（空でよい欄：共通の音で代わりに鳴る・使うときだけ入れる・古い仕組み）は `data/ignore.tsv` に理由と一緒に書く。

## 行を足す・作り直す
- 新しい素材が要るとわかったら（台本の追加・新しい部屋・`-NewSlots` で出た欄）、「素材リスト」の一番下に行を足す（ID は区分の頭文字＋番号）。列の書き方は既存の行と「使い方」シートに合わせる：概要・どんなものが良いか／使う場所／Unity の入れる欄／形式の目安／根拠（台本 ID・実装）。
- **最初から作り直す**（`scripts/build.ps1`）のは、チームがまだ何も書いていないときだけ（担当・期限・進み具合・備考が消える）。データは `data/list.tsv`・`howto.tsv`・`spec.tsv`・`config.json`。作り直したら `audit.ps1 -SetKeys -Apply`。
- 台本からの予想は `dialogue-csv` の CSV（表情の使用回数・話す人物・演出のメモ）から拾う。

## しくみ
- `scripts/survey.cs`（`Invoke-UnityEval` で実行）：データ（Assets/Data・Resources）、プレハブ（Resources・Prefab）、ビルドの全シーンの MonoBehaviour について、SE・音・絵・エフェクト・アニメ・画像・フォントの欄（ObjectReference）を全部書き出す（空なら EMPTY）。SpriteRenderer・UI の Image・AudioSource の絵と音、付いている部品の種類、SeData・EffectData・アニメのデータ・アイテム・キャラの中身も。結果は `%TEMP%\ct_asset_survey.txt`。
- `scripts/audit.ps1`：調査結果を読んでキーを判定し、Excel（COM）で xlsx を開いて書く。日本語の文字は `data/config.json` から読む（.ps1 は英数字だけ）。
- `scripts/build.ps1`：xlsx を一から作る（Excel が要る。Python はこの PC にない）。
