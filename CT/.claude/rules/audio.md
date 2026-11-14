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
- **SE**：`SeManager`（static の `PlaySe(SeData)`・位置つき・追従つき・`StopSe`・ループ。同時に鳴らせる数を超えたら一番古いものを止める）。音の設定は `SeData`（アセット、🛠`SeDataCreator` で作る）。イベントからは部品 `SeEventAction`。体力の音は `HealthSe`。
- **足音**：`FootstepSe` はノア・シグルのプレハブに付いている（2026-11-14）。音は `SE/Se_Walk_sabaku`（操作中）・`Se_Walk_sabaku_follow`（ついてくるとき・音量 0.5）。鳴らし方は「一歩ずつ」（決めた距離ごとに1歩の短い音）と「歩いている間ループ」（長い録音をループ・止まったら `StopSe`・操作中／ついてくるが変わったら音を替える）。**今の素材は 9 秒の録音なので「歩いている間ループ」**（SeData の「ループ」オン）。1歩の音ができたら「一歩ずつ」にし、SeData のループを切る。ノアとシグルで同じ SeData を使わない（Min Interval で片方が鳴らない）。
- **BGM の読み込み設定**：長い曲・環境音は **Streaming・Vorbis・品質 70%**（2026-11-14 に `BGM_Stage1`・`PMH_Atmos_HospitalHall_01` を変更。再生中 1 曲 約 0.2MB。前は読み込み時に展開で 42MB・16MB）。新しい BGM を入れたら同じにする。短い SE は今のまま（Decompress On Load）でよい。
- **BGM**：`BgmManager`（クロスフェード・`PlayAfterFadeOut`・続きから再生・エフェクト）。シーンごとの曲は `SceneBgm`（条件つきのルール：`Conditions/` の Area / Flag / Health）。フラグで変えるのは `BgmFlagEventAction`、進行度で音を変えるのは `Effects/`（`BgmProgress`）。
- 2つとも `instance` を持つ static の窓口。**static は `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` でリセットする**（`SeManager.ResetStatics` が手本。ドメインリロードが無効なので、しないと前回の再生の値が残る）。
- フェードは `Time.unscaledDeltaTime`（ポーズ中も進む）。

## 既知の問題（code-map の「4. 問題点」）
- 足音の素材が 9 秒の録音（砂漠）1つだけ。歩き始めるたびに録音の頭から鳴る。塔の中・部屋も砂漠の足音。
- `BgmProgress`（進行度）が再生ごとにリセットされず、セーブもされない。
- タイトルの `SceneBgm` の Default Clip が空（タイトルで BGM が鳴らない）。
- Stage1_2 の演出の音（キーーーン・ドスン・バン）が空欄。
- 未使用：`SePlayer`・`HealthSe`・`BgmProgressDebug`（シーン・プレハブに置いていない）。

## 書くときの約束
- 音を鳴らすのは `SeManager.PlaySe` を通す（`AudioSource` を物ごとに増やさない）。毎フレーム鳴らし直さない。
- 新しい音の項目は、Inspector の項目名・Tooltip を日本語で（`rules/coding-style.md`）。
- 音のファイルを入れたら `asset-audit` スキルで素材チェックリストを点検する。
- 再生での確認は、音そのものは聞けない。`event.ps1 -State` や C# で「鳴らした・今の曲」を確かめ、**耳での確認はユーザーに頼む**と伝える。
