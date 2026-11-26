---
paths:
  - "Assets/Script/Presentation/**"
  - "Assets/Resources/Presentation/**"
  - "Assets/Script/StageDarkness/**"
  - "Assets/Script/Map/Look/AreaTone.cs"
  - "Assets/Script/Map/Look/LightFlicker.cs"
---

# 演出の基盤（色調・光・天候・回想・1枚絵）

指示書 `CT_presentation_mood_light_weather_still_claudecode_instruction_2026-11-25`（2026-11-26 実装）。プランナー向けは `Docs/演出の作り方.md`。

## 決めたこと（ユーザーと合意した方針）
- **窓口は1つ**：`PresentationDirector`（再生で自動・DontDestroyOnLoad）が色調・光・天候・回想を決めて反映する。Global Light 2D を書くのは窓口だけ（`StageDarkness` は `ReportDarkness` で暗さを渡す、`AreaTone` は窓口の「範囲」の層）。**新しく全体の光を変える仕組みを作るときも、窓口の層を通す**（直接 Global Light を書かない）。
- **層**：色調・光＝既定(0)＜範囲(1)＜イベント(2)＜回想(3)。天候だけ既定＜イベント＜範囲＜回想（室内に入ると、部品で降らせた雨も止む）。上の層が値を持てば勝つ。値＝プリセット（null は「通常／シーンのまま／なし」）。
- **切り替えは今の値から**：`StartMood`／`StartLight`／`StartWeather` が今の値を from にする（途中で次が来ても飛ばない）。回想の「色が抜けてから差す」は from → 灰色（中間の Desaturated）→ to の 2 区間（4:6）。
- **色調はプリセットの値を直接混ぜる**（Volume Profile を2つ重ねない）。実行中に作る Volume 1つ（priority 100）。加工がないときはカメラの `renderPostProcessing` を切る（シーンのカメラは全部オフのまま保存。再生中に窓口が入れる）。
- **回想の中の部品は回想の層に書く**（`PresentationDirector.EventLayer`）。戻ると回想の層を消すだけで「入る前」に戻る。
- **セーブ**：`Save.BeforeCommit` でイベントの層だけを `Presentation/<シーン>/Mood|Light|Weather` に名前で（""＝なし、"*"＝null を指定）。回想の層は記録しない＝回想中にセーブしても入る前の状態。回想中フラグ `Flag_Flashback_Active` はセーブしないフラグ。シーンの既定は毎回 `ScenePresentation` から。**部品の `Restore` は使わない**（完了したイベントの Restore の順番で値が入れ替わるため）。
- プリセットは **`Assets/Resources/Presentation/`**（指示書の案の `Data/` ではなく。名前で探すため）。`PresentationDirector.Find<T>(名前)`。
- **会話画面の色調**（11-26 ユーザーの依頼）：`MoodPreset` の「会話画面にもかける」（`applyToDialogue`）と強さ（`dialogueStrength`）→ `MoodValues.uiAmount`（混ぜる）。`DialogueMoodLayer`（窓口の物に付く）が立ち絵のマテリアルを `CT/UI Mood` の複製に替え、文字は **CanvasRenderer の色**（`TintText`。子の `TMP_SubMeshUI` にも。会話中は 0.25 秒ごとにかけ直す。**TMP の文字のマテリアルを複製しない**＝動的なフォントのアトラスが増えると `GetFallbackMaterial` が範囲外のエラーになり会話が止まった、11-26）、会話中（`DialogueManager.IsBusy`）だけ並び順 101 に粒子（`uvRect` を 1 秒 12 回）と周辺の暗さ。**粒子は「点の数」で強さを変える**（11-26：濃さを下げると色の段 1/255 を下回り 0.16 あたりで急に消えた）：点の割合＝粒子×会話画面の強さ×0.1（`DotDensityPerGrain`）、1 つの点の濃さは固定（立ち絵 ±0.07・重ねる粒子 0.07）。重ねる粒子の絵は割合が変わったときだけ作り直す（点の順番は固定）。会話が始まった瞬間に拾い直す（シネマの字幕は後から作られるため）。uiAmount が 0 になると元のマテリアルに戻して複製を消す。頂点の色（会話の仕組みのフェード・名前の色）には触らない。最初は「回想」だけオン（0.8）。（並び順 95：暗転の幕 90 ＜ 1枚絵 ＜ シネマの帯 97 ＜ 会話 100）。既定はマップの範囲 4:3（`MapCameraAspect` のカメラの rect）。
- 天候：カメラについてくる箱から出し、粒は World 空間に残る。並び順 キャラの後ろ 6・前 150・かすみ 160。光の影響の既定＝受ける（光の粒だけ受けない）。マテリアルは `Resources/Presentation/Materials/Mat_Weather_Lit|Unlit`（粒の絵は MaterialPropertyBlock で）。
- コマ落ち：`SpriteAnimator.GlobalStepFps`（絵を替えるのを1秒に N 回だけ。時間は進む）。Unity の Animator は対象外。
- S4（時代の切り替え）は `PresentationDirector.SetBase(色調, 光, 天候, 秒)` か、新しい層を足して呼ぶ（既定の層をシーンの既定と取り合うなら、層を1つ足すのがよい）。

## 書くときの注意
- イベントの部品は **1ファイル1クラス**（MonoBehaviour はファイル名とクラス名が合わないとシーンに保存できない）。
- `ScenePresentation` の光の欄は `lightPreset`（`light` は Component のプロパティと名前がぶつかる）。
- 毎フレームの処理（窓口の LateUpdate・WeatherView.Tick）は確保なし。天候の量は値が変わったときだけ ParticleSystem に入れる。
- 性能（11-26、Test_Presentation、品質「標準」）：演出なし 処理 p50 2.07ms・ドローコール 14 → 強い雨＋回想の色調 p50 2.32ms・p95 3.38ms・ドローコール 34。ゲームの確保は増えない（0.11KB/フレーム）。`PerfLog` のラベル `presentation_none`・`presentation_rain_memory`。

## 確かめ方
- 試し用マップ `Test_Presentation`（レシピ `test_presentation`。Stage1_0 のコピー、OP などは切ってある）。**直接再生で見本の広場 (-115.2, 22.0) から**（PlayerPartySpawner の直接再生の位置を広場へ）。台座 `PresentationStation`（色調 (-120.5,33.5)・光 (-117.5,33.5)・天候 (-114.5,33.5)・回想 (-111.5,33.5)・1枚絵 3 つ (-121/-118.5/-116, 29.5)・暗さ (-113,29.5)・全部戻す (-111.5,22.5)）、天候の範囲 (-109.5,36.6)、場所の色味 (-120.5,36.5)・3.2×3.4・夕方の色 (1, 0.62, 0.4)×0.9（11-26：前の「暗く青く」は夜の光とほぼ同じ値で、入っても変わらないように見えた）、右上の表示 `PresentationStatusView`。室内（11-27）：Stage1_2 の通路の絵を砂浜の北につないだ（入口 (-113, 38.4)〜床 y 40.2〜45.7）。台座「室内の明るさ」＝`PresentationStation` の暗さの段 `darknessLevels`（名前・暗さ・明かりの半径。`FadeDark` で暗さと半径をいっしょに変える）。暗さの台座は非表示の StageDarkness（暗さ 0）を表示してから暗くする（Awake が先に走ると、切ってある StageDarkness でもプレイヤーの明かりが点いたままになるため、物ごと非表示で置く）。
- `PresentationDirector.Describe()` で層の中身を1行。`CurrentLightName` は場所の色味（範囲の層）のとき「場所の色味」を返す。シナリオ `presentation_test`（試し用マップ：台座を本当の E キーで調べて回る・天候の範囲・場所の色味・回想・暗さ・1枚絵を Space で閉じる・全部戻す。25 件）・`presentation_save`（Stage1_0：夜の値・回想中にセーブ → ロードで入る前）。
- 夜の移し替えはレシピ `presentation_night_default`（Stage1_0・Stage1_1 に既定＝Light_夜）。
