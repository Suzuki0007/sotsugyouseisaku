---
paths:
  - "Assets/Script/Bgm/**"
  - "Assets/Script/Se/**"
  - "Assets/BGM/**"
  - "Assets/SE/**"
  - "Assets/Data/Se/**"
---

# 音（BGM・SE を触るとき）

ファイルごとの役割は `Docs/code-map.md` の「3. > 演出（音・エフェクト・アニメ・明るさ）」。プランナー向けの手順は `Docs/プランナー向け作業手順書.md` の音の章。

## 仕組み
- **SE**：`SeManager`（static の `PlaySe(SeData)`・位置つき・追従つき・`StopSe`・ループ。同時に鳴らせる数を超えたら一番古いものを止める）。音の設定は `SeData`（アセット、🛠`SeDataCreator` で作る）。イベントからは部品 `SeEventAction`。足音は `FootstepSe`、体力の音は `HealthSe`。
- **BGM**：`BgmManager`（クロスフェード・`PlayAfterFadeOut`・続きから再生・エフェクト）。シーンごとの曲は `SceneBgm`（条件つきのルール：`Conditions/` の Area / Flag / Health）。フラグで変えるのは `BgmFlagEventAction`、進行度で音を変えるのは `Effects/`（`BgmProgress`）。
- 2つとも `instance` を持つ static の窓口。**static は `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` でリセットする**（`SeManager.ResetStatics` が手本。ドメインリロードが無効なので、しないと前回の再生の値が残る）。
- フェードは `Time.unscaledDeltaTime`（ポーズ中も進む）。

## 既知の問題（code-map の「4. 問題点」）
- **ノア・シグルのプレハブに FootstepSe がない**（足音が鳴らない）。今は Stage1_2 のルートの動かない `Player` に付いている。プレハブに移したら、その `Player` を消す。
- `BgmProgress`（進行度）が再生ごとにリセットされず、セーブもされない。
- タイトルの `SceneBgm` の Default Clip が空（タイトルで BGM が鳴らない）。
- Stage1_2 の演出の音（キーーーン・ドスン・バン）が空欄。
- `Assets/BGM/PMH_Atmos_HospitalHall_01.wav` が **47MB**（展開して読むとメモリを食う）。長い曲・環境音は読み込みの設定を Streaming か Compressed In Memory に（`perf-review` の音のルール、`unity:optimize-audio` スキル）。
- 未使用：`SePlayer`・`HealthSe`・`BgmProgressDebug`（シーン・プレハブに置いていない）。

## 書くときの約束
- 音を鳴らすのは `SeManager.PlaySe` を通す（`AudioSource` を物ごとに増やさない）。毎フレーム鳴らし直さない。
- 新しい音の項目は、Inspector の項目名・Tooltip を日本語で（`rules/coding-style.md`）。
- 音のファイルを入れたら `asset-audit` スキルで素材チェックリストを点検する。
- 再生での確認は、音そのものは聞けない。`event.ps1 -State` や C# で「鳴らした・今の曲」を確かめ、**耳での確認はユーザーに頼む**と伝える。
