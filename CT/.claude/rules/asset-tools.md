---
paths:
  - "Assets/Script/AssetTools/**"
  - "Assets/Data/AssetTools/**"
  - "Docs/素材の割り当て/**"
  - ".claude/skills/asset-intake/**"
---

# 素材の取り込みの道具（2026-11-14〜15。ユーザーが選んだ提案 ①〜④）

手順は `asset-intake` スキル。プランナー向けは `Docs/素材の取り込み.md`。

## 部品（すべてエディタ用 `Assets/Script/AssetTools/Editor/`）
- `AssetImportRules`（AssetPostprocessor）：`Assets/Data/AssetTools/取り込み設定.tsv`（フォルダ／種類〔画像・音〕／設定 key=value;／メモ。一番長く一致したフォルダの行）を **File で読む**（取り込み中に AssetDatabase を使わない）。**初めての取り込み（`importSettingsMissing`）と `Assets/__Intake/` から移したファイルにだけ**かける（今ある素材は変えない）。かけ直しは Project 右クリック > 素材 > 取り込み設定をかけ直す。
  - 決めた値（2026-11-15 に今の素材を集計して合わせた）：画像は Sprite・PPU 100・Bilinear・圧縮・ミップなし・Tight、マップは FullRect、エフェクトは圧縮なし、Max Size は絵の大きさ以上の 2 のべき乗（2048〜8192。絵が縮まない）。音は BGM＝Streaming・Vorbis 70%・裏で読み込む、SE＝読み込み時に展開。**Sprite の分け方は Single**（2026-11-19、ユーザーの指示「大きい画像が勝手に分割される」）。名前に `sheet=` の言葉（`_sheet`・`_animetion`・`_animation`・`_anim`）が入っていれば Multiple（連番。Sprite Editor で切る）。指定がない行は Unity の既定（Multiple で自動に切られ、1枚の絵が「名前_0」とかけらに分かれる）。
  - **マップチップ**（2026-11-25）：`Assets/Images/Mapchip/` の行＝PPU 64・Point・圧縮なし・ミップなし・Full Rect・Single（Clamp は Sprite の既定のまま。表に wrap の欄はない）。継ぎ目の対策は `MapchipSeamFix`（Tools > Mapchip > Fix Seams）：取り込み設定をそろえ直し、アトラス `Assets/Images/Atlas/Mapchip.spriteatlasv2` を作り直してパックする。**チップを足したら押す**（表は初めての取り込みにしかかからない・アトラスもフォルダごとなので押せば入る）。「取り込み設定をかけ直す」を前の表（`Assets/Images/` の行＝PPU 100・Bilinear・圧縮あり）でかけると壊れるので、この行を消さない。
  - `Kabe.2.png` は 11-25 にユーザーの指示で**絵を直した**（上 43 行のまっ白〔255,255,255〕を透明に・32bit ARGB で保存・Alpha Is Transparency）。元の絵は `Docs/素材の元/Mapchip/Kabe.2.png`（Assets の外＝取り込まれない）。追加のタイル（向き違い・ランダム反転・影）は画像を増やさずタイルのアセットだけ（レシピ `mapchip_tiles`、`Docs/マップチップの置き方.md`）。
- `AssetWalker`：プレハブ（Assets/Prefab・Resources）→ データ（Assets/Data・Resources の ScriptableObject）→ Build Settings のシーンの部品を見て回る土台。変えた所だけ保存し、最後に元のシーンを開き直す。`AssetKey`（GUID＋ファイル内の番号）で比べ・読み直す（**シーンを開くと先に読んだアセットが消える**ため）。`DirtySceneName()`。
- `AssetReplacer` ＋ `AssetReplaceWindow`（Tools > 素材 > 仮素材を差し替える）：古い → 新しいの対応（画像は Texture2D と中の Sprite を、名前 → 並びの順で）を作り、ObjectReference を全部付け替える。探すだけもできる。
- `AssetMapApplier`（Tools > 素材 > 割り当て表を試す／流す、スキルの `apply.ps1`）：`Docs/素材の割り当て/*.tsv` を読む。入れる先 `slot`（確認キーと同じ書き方＋`#n`・`欄[]`・`欄[i]`）・`item`・`bgm`・`bgm-rule`・`event-se`・`chara`・`replace`。欄の型で入れる物を決める（`PPtr<$SeData>` は音から SeData を作る／`PPtr<Sprite>` は画像の中の絵。**Unity の型は `$` が付かない**ので正規化している）。順番：部品の欄（1回の見回り）→ シーンごとの操作 → 表情 → 差し替え（最後）。dryRun は何も変えない。
- `SoundListWindow`（11-15 提案 ⑤、Tools > 素材 > 音の一覧（試聴・音量））：SeData と `Assets/BGM` の曲。▶ は見えない AudioSource（HideAndDontSave）で SeData の音量・ピッチ・ループのまま鳴らす（エディタでも鳴る。鳴らなければ AudioUtil の試聴）。音量をその場で直す（Undo）・「自動」＝`AssetMapApplier.AutoVolume`。聞こえ方（RMS。DecompressOnLoad で 30 秒未満だけ）赤 -14dB より上・青 -28dB より下。使っている所＝ビルドのシーン・Prefab・Resources・Data の直接の依存（`GetDependencies(path, false)`。シーンを開かない）。確かめ用に `Summary()`・`PlayByName()`・`IsPlaying`。
- 素材の渡し方（提案 ⑥）：`Docs/素材の渡し方.md`、チェックリストの使い方シート（`asset-audit/data/howto.tsv` → `howto.ps1`）。`inspect.cs` が名前の決まりから入れる先を推す（表情・Item_・1ファイル1キー）、同じ名前＝直した版（`place.ps1 -Update` で中身だけ上書き）。
- 表の手本・記録：`Docs/素材の割り当て/2026-11-14_BGM_SE.tsv`（11-14 の音の納品。試すと 78 行すべて「同じ」＝今の状態と一致）。

## 約束
- 新しい「素材を入れる欄」を作ったら、割り当て表で入れられるか（`slot:部品.欄`）を確かめる。シーンの中の決まった物に入れる特別な操作が要るなら `AssetMapApplier` に入れる先を足す（例：bgm-rule）。
- 取り込みの決まりの表を変えたら、今ある素材にはかからない（かけ直しはメニュー）。
- テストは `Assets/__ClaudeTest/` と `Assets/Prefab/__ClaudeTest_*.prefab`（見回りの対象にするため）で行い、消す。
