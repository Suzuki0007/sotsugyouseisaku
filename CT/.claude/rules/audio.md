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
- **足音**：`FootstepSe` はノア・シグルのプレハブに付いている。鳴らし方は「一歩ずつ」（決めた距離ごとに1歩の短い音）と「歩いている間ループ」（長い録音をループ）。**2026-11-14 に1歩の素材が入ったので「一歩ずつ」**（`Se_Footstep_Noah`／`_Sigle`／`_Follow`。塔の中の音）。ノアとシグルで同じ SeData を使わない（Min Interval で片方が鳴らない。操作中と「ついてくる」を別の SeData にしていれば同じ音を2人で使ってよい）。
- **場所ごとの足音** `SceneFootstep`（Hierarchy 右クリック > 音 > 足音の設定（このシーン全体／範囲の中だけ）、メニューは `Se/Editor/SceneFootstepMenu`）：鳴らし方と、ノア・シグル × 操作中・ついてくるの音を上書き（空ならプレハブの音）。Is Trigger の Collider2D があれば範囲の中だけ（範囲つきが先）。`FootstepSe` が歩いている間に毎フレーム `SceneFootstep.At(位置)` を引き、鳴らし方がループ→一歩ずつに替わったらループを止める。**外のマップ（Stage1_1 の砂漠）は `足音の設定（外・砂）`＝`Se_Walk_sabaku` の録音を「歩いている間ループ」**（ユーザーの指示 2026-11-14「外にいるマップでは足音を変える」）。レシピ `audio_4_footstep_outdoor`。外のマップを足したら置く。
- **レシピの落とし穴**：シーンを `OpenScene` する前に読んだアセットは、開くときの片付けで消え、空の参照が保存される。アセットはシーンを開いてから読む。
- **BGM の読み込み設定**：長い曲・環境音は **Streaming・Vorbis・品質 70%**（2026-11-14 に `BGM_Stage1`・`PMH_Atmos_HospitalHall_01` を変更。再生中 1 曲 約 0.2MB。前は読み込み時に展開で 42MB・16MB）。新しい BGM を入れたら同じにする。短い SE は今のまま（Decompress On Load）でよい。
- **BGM**：`BgmManager`（クロスフェード・`PlayAfterFadeOut`・続きから再生・エフェクト）。シーンごとの曲は `SceneBgm`（条件つきのルール：`Conditions/` の Area / Flag / Health）。フラグで変えるのは `BgmFlagEventAction`、進行度で音を変えるのは `Effects/`（`BgmProgress`）。
- 2つとも `instance` を持つ static の窓口。**static は `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` でリセットする**（`SeManager.ResetStatics` が手本。ドメインリロードが無効なので、しないと前回の再生の値が残る）。
- フェードは `Time.unscaledDeltaTime`（ポーズ中も進む）。

## 素材（2026-11-14 BGM_SE_Assets を入れた）
- 置き場所：**フォルダ名＝素材チェックリストの ID**。BGM は `Assets/BGM/BGM-0x_〇〇/`（Streaming・Vorbis 70%）、SE は `Assets/SE/SE-xx_〇〇/`、SeData は同じフォルダの `Se_<ファイル名から SE-xx_ を外した名前>`。2人の操作の音（SE-14）だけ `Se_party_〇〇`（SE-22 の `Se_switch` とぶつかるため）。
- 入れていない物：`*_image_*.wav`（イメージを確かめるための通しの音）、`SE-01_Footsteps/_old`、`SE-26_save.wav`（`SE-15_ui_save` と中身が同じ）。
- **新しい音を入れるときは `asset-intake` スキル**（割り当て表）。11-14 の分は `Docs/素材の割り当て/2026-11-14_BGM_SE.tsv` に書いてある（流しても同じ結果）。
- 最初に入れたときのレシピ（`scene-recipe`）：`audio_1_sedata`（SeData。音量は RMS を -20dB くらいにそろえる・大きい音だけ下げる）→ `audio_2_assets`（プレハブ・データ）→ `audio_3_scenes`（5 シーン。保存する）。
- 入れた先（主な物）：
  - BGM：Title＝BGM-01／Stage1_2・Stage_1_3 の基本＝BGM-03（塔の探索）／Stage1_2 の追跡中＝BGM-04（`Flag_1_2_Chase`＝追跡の戦闘エリアの戦闘中フラグ）／Stage_1_3 の戦闘中＝BGM-02（`Flag_Timpani_InBattle`）／SceneGameOver＝BGM-05 のループ（`BGMset` を足した）。BGM-05 のジングル・BGM-07〜09 は入れただけ（二層がない・ジングルはゲームオーバーの着いた音 SE-13 と重なるので使っていない）。Stage1_1 は今の BGM_Stage1 のまま。
  - 演出：Stage1_2 着いたとき＝キーーーン、ランタン＝火がつく（ItemGive）・2回目の揺れ＝地響き短・降ってくる＝ドスン・**うなり声（部品を足した、F1_03 の前）**、助け＝バン、正面の扉＝こじ開ける。Stage_1_3 着いたとき＝地響き長。Stage1_1 OP の `OP_途中（鐘の言い伝え）` の最初に鐘（部品を足した）。
  - プレイヤー：足音（一歩ずつ）・ダメージ（HealthSe）・守り（始まり・ループ・終わり）・範囲攻撃・シグルの射撃・ゲームオーバー（倒れる＝fall_glass、着いた＝gameover_arrive）。2人の操作（3 シーンの PlayerSwitchManager）。
  - ティンパニ：ペグ（プレハブ。成功は3段階）・調律・守りのゲージ・攻撃4つ（予告・発動）・すぐ効くアイテム。音楽生物の EnemyData には入れない（攻撃が効かない・おとなしくなる音は TimpaniUnit）。
  - 敵：Stage1_2 の化け物＝ひるみ・倒れる・**動いている間の足音（ループ）**。見本の敵のデータ全部にひるみ・倒れる。
  - 仕掛け：見本の部屋（`Prefab/Gimmicks/Sample/`）の全部・PuzzleUI（番号を回す・ページ）。
  - UI：`Se_UI_〇〇` の音を SE-15 に差し替え。会話の文字送り：全体＝A（DialogueSystem.prefab・Stage1_2 の DialogueManajer）、シグル＝B（CharacterProfile）。出口の SceneChange（OtherSystem・ゲームオーバーの Scene）の Leave＝scene_leave。
- 足した仕組み（2026-11-14）：`TimpaniPeg.successSeSteps`（成功するたびの音）・`successSeOnTighten`、`HealthSe` の共有の重なり防止（同じフレームで共有先が先に鳴らしたら止めて当たった方に替える）、`DialogueManager.typeSe`・`typeSeEvery`・`CharacterProfile.typeSe`、`EnemyData.moveLoopSe`（`Enemy.UpdateMoveSound`：速さ 0.2 以上で鳴らし、0.25 秒止まったら止める）、`PuzzleUISettings.dialSe`・`pageSe`。

## 既知の問題（code-map の「4. 問題点」）
- `BgmProgress`（進行度）が再生ごとにリセットされず、セーブもされない。
- ノアのタイミングゲージ攻撃の成功・失敗の音（SE-09 success / fail）を鳴らす欄がない（`TimingGaugeUI`・`TimingGaugeAttackController` は成田さんのコード。今の Attack Se はゲージを出したときに鳴る＝空のまま）。
- 未使用：`SePlayer`・`BgmProgressDebug`（シーン・プレハブに置いていない）。

## 書くときの約束
- 音を鳴らすのは `SeManager.PlaySe` を通す（`AudioSource` を物ごとに増やさない）。毎フレーム鳴らし直さない。
- 新しい音の項目は、Inspector の項目名・Tooltip を日本語で（`rules/coding-style.md`）。
- 音のファイルを入れたら `asset-audit` スキルで素材チェックリストを点検する。
- 再生での確認は、音そのものは聞けない。`event.ps1 -State` や C# で「鳴らした・今の曲」を確かめ、**耳での確認はユーザーに頼む**と伝える。
