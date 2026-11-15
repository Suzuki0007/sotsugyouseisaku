---
name: asset-intake
description: CT プロジェクトに、渡された素材（音声・画像のフォルダ）を取り込んで、対応する場所に実装するときの手順。一時置き場にコピーして点検（長さ・音量・無音・大きさ・透過・同じ中身・mp3 のループ、素材チェックリストの行と確認キー）→ 割り当て表（Docs/素材の割り当て/*.tsv）の案を作ってユーザーと合意 → 置き場所へ移す（取り込みの決まりが自動でかかる）→ 表を試す → 流す → 点検（asset-audit・シナリオ・再生）→ 報告（耳・目で確かめてほしい所）。BGM・SE・立ち絵・アイコン・エフェクト・敵やキャラの絵を渡されたとき、仮素材を本物に差し替えるときに使う。
---

# asset-intake：素材を受け取って実装する

素材チェックリスト（`asset-audit`）・取り込みの決まり（`Assets/Data/AssetTools/取り込み設定.tsv`）・割り当て表（`Docs/素材の割り当て/`）・差し替えツールを順に使う。仕組みの説明は `.claude/rules/asset-tools.md`。

## 流れ

```powershell
$s = ".claude/skills/asset-intake/scripts"
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/asset-intake/scripts/inspect.ps1 -Source "C:\Users\uhiro\Downloads\<届いたフォルダ>" -Name 2026-11-20_<内容>
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/asset-intake/scripts/place.ps1 -Name 2026-11-20_<内容> -Skip "要らない.wav,同じ.png" -Update
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/asset-intake/scripts/apply.ps1 -Table "Docs/素材の割り当て/2026-11-20_<内容>.tsv" -Changes
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/asset-intake/scripts/apply.ps1 -Table "Docs/素材の割り当て/2026-11-20_<内容>.tsv" -Apply
```

1. **点検**（`inspect.ps1`）：届いたフォルダを `Assets/__Intake/<Name>/` にコピー（フォルダの形はそのまま）して Unity で調べる。
   - 出る物：同じ中身のファイル（`[SAME]`）、フォルダごとの素材チェックリストの行（素材名・入れる場所・形式の目安）と確認キー、ファイルごとの長さ・音量（RMS・最大）・頭の無音・画像の大きさ・透過、気になる所（⚠：大きい／かなり小さい／割れているかも／頭に無音／mp3 のループ／長いループの曲／透過がない／2048 より大きい／立ち絵が 1920×1080 でない／連番）。
   - 割り当て表の案 `Docs/素材の割り当て/<Name>_draft.tsv`（フォルダごとにチェックリストの行・キーをメモ）。**名前の決まり（`Docs/素材の渡し方.md`）から推せる物は入れる先を埋める**：立ち絵の表情（`UI_Noah_Surprise.png` → `chara:Noah:Surprise`）、アイコン（`Item_Key.png` → `item:Item_Key`、同じ名前のアイテムのデータがあるとき）、フォルダに1ファイルで確認キーが `slot:` 1つの物。推した行はメモに「名前の決まりから推した」。ほかは「?」。
   - **直した版**（同じファイル名がもうプロジェクトにある）：⚠ で出し、表の案では `-`。`place.ps1 -Update` で中身だけ上書き（GUID・取り込みの設定・参照はそのまま。表は要らない）。
   - フォルダ名が ID（`SE-16_Peg`・`POR-01_〇〇`）でないと、チェックリストと結びつかない（`[ID なし]`）。そのときはファイル名と中身から考える。
2. **割り当て表を作る**：案の「?」を埋めて `<Name>.tsv` にする（`_draft` は消す）。書き方は下。決めにくい所（どの場面の音か・表情の名前・使わない物）は**ユーザーに一覧で見せて合意してから**。手で選んだこと（推測）は表の「メモ」に書く。
3. **置き場所へ移す**（`place.ps1`）：フォルダ名の頭で置き場所を決めて `AssetDatabase.MoveAsset`（`data/destinations.tsv`：BGM→`Assets/BGM/`、SE→`Assets/SE/`、POR・UI→`Assets/Images/UI/`、CHR→`Player`、ENM→`Enemy`、FX→`Effect`、MAP→`Map`、EVT→`Event`、ETC→`Other`。フォルダ名はそのまま）。移すと**取り込みの決まり**がかかる。`_image_`（通しの試聴用）・`_old` のフォルダ・`-Skip` の名前は移さず、一時置き場ごと消す（元のファイルはダウンロードに残る）。同じ名前がもうあれば移さない（差し替えなら下の replace）。
4. **試す**（`apply.ps1 -Changes`）：何がどう変わるか（「→」の行）と NG を見る。NG を直す。
5. **流す**（`apply.ps1 -Apply`）：プレハブ・データ・ゲームのシーンを保存する。**シーンを保存することを報告に書く**。
6. **確かめる**：音は **Tools > 素材 > 音の一覧（試聴・音量）**（`SoundListWindow`）で「聞こえ方」の赤・青と「使っていない」を見る（ユーザーにはここで聞き比べてもらう）。`unity-check -SceneCheck`、`asset-audit` の `audit.ps1 -Apply`（素材チェックリストの「確認待ち」へ）、`unity-playtest` のシナリオ（場面の BGM は `expect eval BgmManager.Instance.CurrentClip.name == ...` を足すとよい）、絵は `event.ps1` の撮影。音は鳴っているか（`AudioSource.isPlaying` と clip 名）まで。**音の聞こえ方・絵の見え方は確かめられないので、ユーザーに頼む所を一覧にする**。
7. 文書：`Docs/変更履歴.md`、必要なら `rules/audio.md`・手順書。表（`Docs/素材の割り当て/*.tsv`）はコミットに入れる（何をどこに入れたかの記録）。

## 割り当て表の書き方（`AssetMapApplier`）
UTF-8 のタブ区切り。1行目は見出し（ファイル／入れる先／設定／メモ）、`#` で始まる行は飛ばす。手本：`Docs/素材の割り当て/2026-11-14_BGM_SE.tsv`（78 行。試すと全部「同じ」になる）。

| 列 | 書き方 |
|---|---|
| ファイル | ファイル名（同じ名前が1つだけなら）か `Assets/...` のパス。`a.wav+b.wav` でまとめて1つの SeData（ランダムで鳴る）。`x.png:絵の名前` で画像の中の絵。`-` は入れない |
| 入れる先 | `;` で複数。下の表 |
| 設定 | SeData を作るとき `vol=auto|0〜1;loop=on;pitch=0.95-1.05;interval=秒;max=数;name=Se_名前`（`name=Assets/...asset` で今ある SeData の音を差し替え）。`bgm`・`bgm-rule` は `vol=` |

| 入れる先 | 意味 |
|---|---|
| `slot:部品.欄@場所#n` | その部品の欄に入れる。場所＝シーン名・プレハブ／データのパス・階層の一部（素材チェックリストの確認キーと同じ）。`#n` は見つかった n 番目だけ。`欄[]` でリストに足す、`欄[2]` で 3 つ目。欄の型に合わせて入れる（SeData なら音から作る、Sprite なら画像の中の絵） |
| `bgm:シーン` | SceneBgm の基本の曲 |
| `bgm-rule:シーン/名前=フラグ` | 条件付きの曲（フラグが ON の間。戦闘中フラグと組み合わせる） |
| `event-se:シーン/イベントの階層@番号` | EventSequence の流れの「番号」番目に効果音の部品を足す（同じ音があれば足さない） |
| `chara:キャラ:表情` | 立ち絵の表情（Normal・Smile・Angry・Sad・Surprise・Troubled） |
| `item:アイテム` | アイテムのアイコン |
| `replace:古い素材のパス` | 古い素材を使っている所を全部付け替える（仮素材の差し替え。画像は中の絵を名前 → 順番で対応） |

- 欄の名前（英語）は、その部品の .cs の SerializeField 名。わからなければ `Docs/早見表（自動）.md` か `asset-audit` の調査結果（`%TEMP%\ct_asset_survey.txt` の SLOT 行）で探す。
- イベントの中の効果音の部品が同じオブジェクトに2つ以上あるときは `#n`（流れの順ではなく、部品が付いている順）。試して「（同じ）／→」を見て確かめる。

## 作る人への決まり
`Docs/素材の渡し方.md`（フォルダ名＝チェックリストの ID・ファイル名の決まり・形式）。素材チェックリストの「使い方」シートにも要点がある（`asset-audit/data/howto.tsv` → `asset-audit/scripts/howto.ps1` で使い方シートだけ書き直す）。決まりを変えたら3か所（この文書・howto.tsv・inspect.cs の推し方）をそろえる。

## 落とし穴
- `apply` も差し替えも、ゲームのシーン（Build Settings）を順に開く。**保存していない変更のあるシーンがあると止まる**（勝手に保存しない。ユーザーに聞く）。再生中も止まる。
- シーンを開くと、それまでに読んだアセットが片付けられて消えることがある（空の参照が保存される）。自分でスクリプトを書くときは、アセットはシーンを開いてから読む（道具の中は GUID で比べて読み直している）。
- 表の「同じ」行は何も変えない。何度流しても同じ結果。
- 音量の「自動」（`vol=auto`）は RMS を -20dB くらいにそろえる（大きい音だけ下げる。小さい音は 1 のまま → 素材を上げてもらう）。BGM（Streaming）は測れない。
- 立ち絵などで新しい種類の絵が来たら、取り込みの決まり（`取り込み設定.tsv`）に行があるか確かめる。今ある素材にかけ直すのは Project で右クリック > 素材 > 取り込み設定をかけ直す。
