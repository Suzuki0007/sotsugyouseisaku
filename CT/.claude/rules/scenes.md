---
paths:
  - "Assets/Scenes/**"
  - "Assets/Images/**"
  - "Assets/res_kari/**"
---

# シーン（シーンを開く・組み立てる・保存するとき）

ゲームの流れ：`Title` → `Stage1_0`（砂浜・OP。夜）→ `Stage1_1`（砂漠。夜）→ `Stage1_2`（一層の序盤）→ `Stage1_3`（書斎。2026-11-19〜）→ `Stage1_4`（T字通路。2026-11-20〜）→（左右の部屋・大なぞ・合流はまだない）→ `Stage_1_Boss`（ティンパニ部屋。名前に `_` が2つ。今はどこからもつながっていない）、`SceneGameOver`。Build Settings もこの7つ。
`Stage1`・`Stage2`・`SampleScene`・`kari_*` はファイルだけ（古い作り。触らない）。

## 約束
- **シーンは勝手に保存しない**。保存が要るときは「何のために」をユーザーに伝える。ユーザーも同じエディタで作業・テストしている。**開いているシーンに保存していない変更があるときは、シーンを切り替えない**（聞く）。
- 新しい部屋（マップの1区画）は **部屋の型**（Hierarchy 右クリック > 部屋 > …、レシピからは `RoomMenu.Build`）から作る。歩ける範囲はマスク画像から（`rules/map-room.md`）。部屋の点検（Room の Inspector・シーンチェック）で ⚠ ✖ をなくす。
- 物をまとめて置く・作り直すのは **`scene-recipe` スキルのレシピ**で行う（手でつつかない。作り直せるように）。レシピで作った物を手で直したら、レシピにも反映するか、`scene-recipe/SKILL.md` の「レシピに入っていない値」に書く。
- イベントで通り道が変わる（入口がふさがる・通路が開く）なら、そのイベントの最後に部品「オートセーブ」。足したらロードを確かめる（`unity-playtest` の `event.ps1 -SaveOn` → `-AutoSave`／イベント → `-Reload`）。
- 置いた物を確かめるときは `unity-check -SceneCheck`（シーンチェック）。シーンチェックの既存のエラー（コピー元の残り）は `Docs/code-map.md` の「4. 問題点」にある。
- 画面：マップは中央 4:3（`MapCameraAspect`）、UI は 16:9 全体。Canvas は Scale With Screen Size 1920×1080・Match 0.5。文字は NotoSansJP（`rules/save-ui.md`）。

## シーンごとのメモ

### Stage1_1（OP）
- ルート `OP`（レシピ `stage1_1_op`）：`OP_はじめ`（SceneStartEvent、条件 `Flag_OP_Done` OFF → 待つ → F1_00 → OP_01 → 明転 → OP_02 → フラグ ON → オートセーブ）、`OP_途中`・`OP_塔の入口`（ItemInteraction のエリアに入る・1回だけ、saveId `OP_Bell` / `OP_TowerEntrance`）。会話用の Dialogue は子の「会話」に置く（調べる範囲なし。シーンチェックは DialogueEventAction から使われていれば対象外）。
- 最初はノア1人（`Flag_Sigle_Joined` が立つまで）。
- **上陸（2026-11-20、レシピ `landing_2_stage1_1`、ルート `上陸（Stage1_1）`）**：
  - 着いたとき（SceneStartEvent・`Flag_Land_TowerStill` OFF）：暗くする → 塔の全景スチル（`演出の画像（手紙・スチル）` の Canvas 並び順 95、仮画像 `Images/UI/Still/tower_still_placeholder_lowangle.png`）→ OP_02h → スチルを閉じて明転 → フラグ → オートセーブ（砂漠に入ったときのオートセーブ）。
  - 鐘の言い伝え（`OP/OP_途中`、y 21.5）は今のまま。**塔の入口の会話 `OP/OP_塔の入口（F1_01）` はオフ**（扉の爪痕に置き換え）。
  - `扉の爪痕（…）`（横いっぱいの帯 y 23.3・1回だけ `Land_DoorMark`）：ルールが2つ＝`Flag_Land_WreckAny` ON なら OP_02j、OFF なら OP_02j_Monster_NoWreck（子の流れが2つ。中身は会話だけ違う）。OP_02i → `Flag_Land_DoorMark` → 地響き長・揺れ（大 0.32・3 秒）→ ノアが下を向く → 怪物を表示・止める・下から 4 マス入ってくる → カメラを怪物へ（戻さない）→ うなり声 → OP_02j → カメラをノアへ戻す → 戦闘エリアを始める → 一言「逃げなきゃ！」。
  - 追跡（BattleArea：ゴール＝塔の階段の上 (-115.55, 27.6)・捕まる 0.9・やり直す位置 (-115.2, 22.4)・目的「塔の入口へ逃げろ」・1回だけ `Land_DesertChase`）→ 勝ったとき＝Stage1_2 の `FromStage1_1`（正面の扉と同じ）。怪物は `EnemyData_Stage1_1_DesertChaser`（攻撃 0 倍・速さ 2.6・見た目は Stage1_2 の化け物と同じ仮）。
  - テストの注意：Stage1_1 を直接再生すると古い OP（`OP_はじめ`）が流れる。`Flag_OP_Done` を立てて `PendingSpawnPointId = "FromStage1_0"` で読み直す（`landing_desert.txt` の頭）。追跡が始まったらすぐワープ（動かないと捕まる）。

### Stage1_2（一層の序盤）
- ルート `Stage1_2_イベント`（レシピ `stage1_2_events` → `stage1_2_door` → `autosave_points`）。
- **上陸（2026-11-20、レシピ `landing_3_stage1_2`）**：着いたときの流れの頭（SceneStartEvent のすぐ後）に「待つ → 扉が閉まる音 `Se_Door_Close` → 入口の扉の絵を閉じた方へ → 小さく揺れる → 待つ → OP_02k」を差し込んだ（部品の並べ替えは `ComponentUtility.MoveComponentUp`）。入口の扉はルート `上陸（Stage1_2 入口）`（仮の絵＝書斎の `door_A_open/closed`、(-107.57, 8.7)。FlagSwitch：`Flag_1_2_Intro_Done` ON で閉じた絵）。「さらに大きな怪物」は今のランタンのあとに降ってくる化け物（順序は今のまま）。`stage1_2_events` で作り直すと消えるので、最後にこのレシピを流す。
- 流れ：着いたとき（SceneStartEvent・`Flag_1_2_Intro_Done`）→ 真っ暗（Darkness 0.97・半径 1）で見えない壁の道をランタンへ → ランタン（ItemInteraction 調べる・saveId `Stage1_2_Lantern`：壁を消す・右の壁の明かり2つだけ点ける〔左の奥は暗いまま〕・`Item_Lantern` を渡す・Darkness 0.88・半径 3・揺れ・右を向く・敵とガレキ〔入口をふさぐ・当たり判定は落ちてから〕が降る・左を映す〔左端は映さない〕・戦闘エリアを始める・一言「逃げなきゃ！」）→ 追跡（BattleArea：ゴール＝左の行き止まり・捕まる・やり直す〔やり直す位置＝敵が出たところ〕・メッセージなし・目的だけ表示・1回だけ `Stage1_2_Chase`）→ 勝ったとき＝助けの場面。
- **助けの場面（2026-11-19 から。書斎の仕様で合流をボスの前へ移した）**：F1_TP_ChaseDoor → シグルの攻撃（仮の弾）が飛んで当たる・バン・白いフラッシュ・敵を倒す → **シグル（NPC。`Stage1_2_イベント/シグル（NPC・助けの場面）`、絵は Player_Sigle と同じ）**が画面の外からノアの右 2.6 マスへ → F1_TP_Rescue・Grab・GoHome（帰れ）→ 左の行き止まりの奥の壁の **扉A**（`扉A（廊下の奥。…）`、仮の絵は書斎の `door_A_closed/open`）へ歩く → 扉を壊す音・揺れ → `Flag_Shosai_DoorABroken` → F1_TP_BreakDoor → シグルは扉の奥へ消える → オートセーブ。**合流フラグ `Flag_Sigle_Joined` は立てない**（シグルは操作キャラとしては出ない）。扉A の子「入口（壊れたあと、入ると書斎へ）」（ItemInteraction エリアに入る・条件 DoorABroken）→ Stage1_3 の `FromStage1_2`。
- 最初の「右を向く」は 0.3 秒待ってから（走り込んだ勢いで向きが上書きされる）。
- 古い出口：`正面の扉（合流後に調べると Stage_1_Boss へ）`（条件 `Flag_Sigle_Joined` ON）は残してあるが、今の流れでは合流しないので使われない（前の流れのセーブから続けたときだけ）。F1_04〜F1_08b の会話も今は使っていない。
- 音は SeEventAction を空欄で置いてある（キーーーン・ドスン・バン）。追跡の敵・ランタンの見た目は仮。
- テストの注意：ノアを動かさずに追跡を始めると、捕まる → 読み直しを繰り返す。追跡が始まったらすぐワープ（`event.ps1 -Warp "-128.6,13.0"`）。
- ランタンの調べる範囲は半径 1 と小さい（ぴったり近づかないとキーの案内が出ない）。

### Stage1_3（書斎。2026-11-19 仮組み）
- 仕様：`CT_timpani4_shosai_spec_2026-11-19.md`（部屋・前後のイベント）と `CT_timpani4_detail_spec_2026-11-19.md`（ティンパニ4台のギミック）。配置は仮素材の README の中心ピクセル座標が正。
- Stage_1_Boss のコピー。ルート `書斎（Stage1_3）`（Room、ID `Stage1_3`）。レシピ `shosai_0_assets`（フラグ・アイテム・メモ・一言）→ `shosai_1_scene`（シーン・仮素材の配置・歩ける範囲・出現位置）→ `shosai_2_contents`（仕掛けとイベント）。**1 を流したら 2 も続けて流す**（1 が物を作り直す）。
- 座標：絵（1440×1080、100px＝1 マス）の中心がワールドの (0,0)。`ワールド = ((x−720)/100, (540−y)/100)`。1440×1080 でカメラの1画面ちょうど。
- 仮素材：`Images/Map/Stage1_3/Karisozai/`。本番の絵は同じ名前で上書きすれば差し替わる（位置が変わるならレシピの座標を直す）。シグル（NPC）の絵だけはプレイヤーのシグル（ユーザーの指示 11-20）。
- **README と違う位置**（11-20 ユーザーの指示）：東の本棚は (1300,300) → 右下 (1300,860)（ティンパニに寄りすぎ）。西の譜面箱 (240,830) を足した（仮素材なし。四角と丸の仮の絵）。座標はレシピ `shosai_1_scene` の `shelfX/Y`・`boxX/Y`。
- 歩ける範囲：壁の内側の四角（`床（壁の内側。仮の四角）`）から、ティンパニ4台（円）・机・マレット台・東の本棚・西の譜面箱（壁まで）を抜く。北西の出口の通路は開くと表示。背景の本番の絵ができたらマスク画像から作り直す。
- ノアの当たり判定はキャラの位置より 0.6 マス下（足元）。調べる範囲は物の手前（南側）に置き、ワープで確かめるときは範囲の 0.3〜0.5 マス上に立たせる。
- 流れ：着いたとき（`Flag_Shosai_DoorsLocked` OFF：シグルが北西の扉を殴る → F1_TP_Enter → 扉A が閉まる → F1_TP_Enter2 → ティンパニ列を映す → シグルが (270,400) へ → オートセーブ）→ マレット → 第1問（T1→T3→T4）→ **2か所の鍵が外れる**（東の本棚の格子・西の譜面箱の帯と錠。エフェクト・揺れ・カメラで本棚 → 譜面箱。`Flag_Shosai_Q1Clear`・オートセーブ）→ 2か所が光る → 東の本棚＝**チューニングキー**（`Flag_Shosai_GotKey`）／西の譜面箱＝**暗号のメモ**（`Note_Shosai_Cipher`「はじまりは一番大きな太鼓。→2 ←1 →1 →1」＝第2問の答え。`Flag_Shosai_ClueCipher`）（11-20 ユーザーの指示「一気に獲得しすぎ」。楽譜の切れ端・棚のメモはなくした）→ T2 の調律（ペグのプレハブ。**初めて調べると F1_TP_TuneStart〔よし直すぞ・「操作」の遊び方〕→ そのままリズムゲーム**＝ペグの「初めて始めるときのイベント」・`Flag_Shosai_TuneExplained`。輪の下に「輪が重なった瞬間に [E]（あと n 回）」。輪＋E 3回・`Flag_Shosai_T2Tuned`）→ 第2問（T1→T3→T2→T3→T4。3回失敗で暗号のヒント、5回で答えのメモ）→ 出口が開く → F1_TP_Q2Clear・F1_TP_Intro（名前を聞く → 以降「シグル」）→ シグルが出口から出ていく（`Flag_Shosai_SigleLeft`）→ オートセーブ。
- 出口の先の範囲に入ると T字通路（Stage1_4）の `FromStage1_3` へ（11-20）。戻ってくると `FromStage1_4`（北西の出口の内側 (270,180)）。合流（`Flag_Sigle_Joined`）はまだどこにもない。
- 止まっているときの一言（25 秒）は進み具合ごとに6つ（`Resources/Barks/Shosai/Bark_Shosai_Idle*`。マレットの前／第1問の前／キーを取る前／調律の前／暗号のメモを取る前／第2問の前。条件はどれか1つだけ合う）。
- T2 は同じ場所に3つ（濁った音／調律／叩く）を重ね、条件で1つだけ表示。**表示されていない物の状態（GimmickState）は Awake が走らずセーブから読まれない**ので、条件に使う状態は表示したままの物に置く（調律済みはフラグにした）。

### Stage1_4（T字通路。2026-11-20 仮組み）
- 指示書 `CT_T_corridor_claudecode_instruction_2026-11-20.md`（ユーザーの指示：実装と会話の中身はこちらで判断してよい・ノアがこの場所の物と喋れる内容はいらない）。Stage1_3 のコピー。ルート `T字通路（Stage1_4）`（Room、ID `Stage1_4`、場所の名前「T字通路」）。レシピ `corridor_0_assets`（フラグ）→ `corridor_1_scene`（配置・歩ける範囲・出現位置・Build Settings）→ `corridor_2_contents`（イベント・調べる・出口）。**1 を流したら 2 も続けて流す**。
- 座標：絵 2160×1440（100px＝1 マス）の中心がワールドの (0,0)。`ワールド = ((x−1080)/100, (720−y)/100)`。指示書の素材の配置は**左上**の座標（レシピで中心に直している）。カメラは MapBounds（背景）の端で止まる。
- 仮素材：`Images/Map/Stage1_4/Karisozai/`（README あり）。シグル（NPC）は書斎と同じくプレイヤーのシグルの絵。**後ろ向きの絵がない**ので、北向き（E3 の前）も正面の絵（`絵_北向き（E3 の前）` の Sprite を差し替える）。
- 歩ける範囲：横の通路（x180〜1980・y180〜540）＋縦の通路（x810〜1350・y520〜1440）の四角2つ。
- 流れ：書斎の北西の出口の先（`Flag_Shosai_DoorOpen` で出る範囲）→ `FromStage1_3`（1080,1305）→ E1（SceneStartEvent、`Flag_TC_EnterSeen` OFF：北を向く → F1_TC_Enter → オートセーブ）→ E2（y=700 の帯〔指示書は 760。扉が画面の端に入るように上げた〕：小さな揺れ・音符の光を一瞬。`Flag_TC_Rumble`）→ E3（開始ライン y=650、`Flag_TC_DoorEventDone` OFF：ノアが扉の前 (1080,330) へ歩く → シグルが振り向く → F1_TC_DoorFirst → 触れる〔音・`Flag_TC_NoteGlowed`・エフェクト・揺れ〕→ ランプ2つを2回明滅 → 消灯 → F1_TC_DoorFirst2 → `Flag_TC_DoorEventDone` → オートセーブ）。会話を飛ばしても部品は全部実行される（会話を2つに分けたのはそのため）。
- 調べる／話しかける（E3 のあと）：扉・シグル＝ランプ0つ／1つ（西か東）で会話を切り替え。**ランプ2つは未定義**（大なぞの仕様で決める。今は何も出ない）。管（西・東）＝`Flag_TC_WestPipeOn`／`EastPipeOn` の前後。扉を調べる範囲は扉の幅（シグルの範囲と重ならないように）。
- 見た目：ランプ・管の震え・音符の光・シグルの向きは GimmickReaction（フラグ。シーンに入るたびに計算し直す。latch なし）。
- **出口の範囲と出現位置を近くに置くときの注意**（11-20 の不具合）：ノアの当たり判定は足元（中心がキャラの 0.61 下・半径 0.40＝下へ約 1.0 マス）。出現位置からこの範囲が出口に重なると、**直接再生したときだけ**着いた瞬間に出口が反応する（シーン移動で着いたときは暗転中で反応しないので気づきにくい）。南の出口は y1415〜1440 にした。シナリオ `corridor_direct` で確かめる。
- 出口：南（一番下の帯）＝書斎の `FromStage1_4`（北西の出口の内側）へ。西・東＝**部屋がまだない**ので一言だけ（E3 のあと）。部屋ができたら流れの一言を消して部品「シーンを移る」を足す。戻りの出現位置 `FromWestRoom`・`FromEastRoom` は置いてある。`Flag_TC_WestPipeOn`／`EastPipeOn` はどこからも立てていない（部屋の仕様で立てる）。扉を開ける処理もまだない。

### Stage_1_Boss（ティンパニ部屋）
- `.claude/rules/timpani.md`（このシーンを開くと自動で読む）。ルート `ティンパニ部屋`（レシピ `stage1_3_*`）。Stage1_1 のコピーなので、使っていない `Event`・宝箱 `OB_TRbox` が残っている（シーンチェックでエラー。害はない）。

### Stage1_0（砂浜。2026-11-19 からゲームの最初のマップ）
- 流れ（ユーザーの指示 2026-11-19、レシピ `stage1_0_flow`）：タイトルの「はじめから」＝ `Stage1_0` の `Start_Boat`（船の上、`PlayerSpawnPoint/出現位置_船の上（はじめから）`）→ `OP/OP_はじめ`（Stage1_1 からのコピー。SceneStartEvent・`Flag_OP_Done` OFF のとき・最後にオートセーブ＝船の上から再開）→ 奥（上の端）の `MapEntity/出口_Stage1_1へ（奥）`（ぶつかると移る SceneChange、横いっぱい）→ `Stage1_1` の `FromStage1_0`（下の端）。Stage1_1 の OP はフラグが立っているので流れない。
- OP の途中（鐘）・塔の入口の範囲は Stage1_0 ではオフ（Stage1_1 の物を使う）。コピー元の塔の扉 `MapEntity/OB_Door`（Stage1_2 行き）もオフ。
- **上陸（2026-11-20、指示書 `CT_landing_to_chase_claudecode_instruction_2026-11-20.md`、レシピ `landing_0_assets` → `landing_1_stage1_0`）**：
  - `OP/OP_はじめ（暗転 → 小舟の上で手紙 → 上陸）`（`Flag_OP_Done` OFF）：F1_00 → OP_01（手紙の行は抜いた）→ 明転 → 手紙の紙（仮。`演出の画像（手紙・スチル）` の Canvas 並び順 95＝暗転の幕 90 より手前・会話 100 より奥）＋ OP_02a → メモ `Note_KairosLetter`（その場では開かない）→ **海の上の小舟とノアを同じずらし・同じ時間で岸へ**（物を動かす ×2）→ 降りる → `Flag_Land_LetterRead` → キーーーン・揺れ（小）→ OP_02b → `Flag_Land_VibeS`・`Flag_OP_Done` → オートセーブ。
  - 小舟は2つ：`上陸（Stage1_0）/小舟（海の上。上陸するまで）` と岸の `sunahama_hune_0`。`歩ける範囲（砂浜だけ）` の FlagSwitch が `Flag_Land_LetterRead` で切り替える（海の小舟・通り道 ⇔ 岸の小舟・小舟の歩けない形）。**ノアは毎フレーム歩ける範囲に戻されるので、海の上にいる間は「小舟の通り道」が要る**。
  - 出現位置 `Start_Boat` と直接再生の位置（`Spawner/PlayerPartySpawner/NoahSpaenPoint`）は海の上の小舟の上 (-115.2, 12.4)。
  - 海の当たり判定（`Grid/soto_yuka` の BoxCollider2D 3つ。海全体と岸の両側）は**オフにした**（消していない）。代わりに歩ける範囲（砂浜の仮の四角 y 20.3〜38.3＋残骸3つの歩けない形）。本番はマスク画像から。
  - 残骸＝もとからある絵 `sunahama_object_1`（西）・`sunahama_object_2_0`（東）・`sunahama_object_3`（奥の歯車）。調べる範囲は手前、何度でも（`Land_Wreck1〜3`）、`Flag_Land_Wreck1〜3` と `Flag_Land_WreckAny`。足跡（床の絵に描いてある）は横いっぱいの帯 y 23.8・1回だけ。奥の帯 y 36.4 で振動（中：揺れ＋地響き短 ×3、間隔 0.65 秒）→ OP_02g → `Flag_Land_VibeM` → Stage1_1 の `FromStage1_0`。奥の出口（ぶつかると移る）は残してある。
  - 海の中の `SavePoint`（-115.4, 10.6。丸い絵）が海の上の場面で小舟のそばに見える（前からある物。触っていない）。
- **夜（Stage1_0・Stage1_1、2026-11-19 ユーザーの指示）**：各シーンの `Global Light 2D` を色 (0.5, 0.6, 0.9)・強さ 0.45（昼は白・1）。レシピ `night_stage1_0_1`。マップ・物・ノア・シグルは Sprite-Lit-Default なので一緒に暗くなる。UI は暗くならない。明かりに反応しない絵（SpriteFrameBlend の波）は「シーンの明るさに合わせる」で同じ色にしている。新しく置く絵も Sprite-Lit-Default にする（Unlit だと夜でも明るいまま）。明るさを試すときは、一時的なカメラで色・強さを何通りか撮って並べる（`Global Light 2D` を変えて撮り、シーンを開き直して捨てる）。
- 波 `sunahama_nami_1_size_0`：SpriteAnimator。**今のデータは `Anim_Sunahama_Nami_Renban`**（連番 `sunahama_nami_animetion.png` を 1440×622 の4コマに切った物・基準は下の真ん中・4fps ループ）。前の `Anim_Sunahama_Nami`（`sunahama_nami_1〜3_size` の3枚）も残してあり、SpriteAnimator の Data を替えれば戻せる。並びは 1,2,3,4,3,2 のループ（ユーザーの指示「1234321」）。**波の SpriteRenderer の Order in Layer は 1**：床 `Grid/soto_yuka` と同じ 0 だと、コマによって床が手前に描かれて波が消えて見えた（2・4コマ目。撮影して確かめた）。マップの絵どうしが重なるときは並び順を変えておく。
- 11-19：波は **SpriteFrameBlend**（コマの間を混ぜる・水面の高さを合わせて上下・ゆらぎ）で動かしている。SpriteAnimator は付いたまま止めてある（戻すときは SpriteFrameBlend を外す・SpriteAnimator を有効・Material を Sprite-Lit-Default に）。データの Fps はユーザーが 0.3 にした（1コマ約 3.3 秒）。確かめ方：一時的なカメラで `EvaluateAt(秒)` ごとに撮る（シーンを保存せずに見られる）。3枚は Single・ピボット下の真ん中（3枚目だけ 3 ピクセル低いので下でそろえる）。前に付いていた Animator（`sunahama_nami_1_size_0.controller`・`NamiAnim.anim`）は外した（アセットは残っている）。
- OP の会話の範囲 `OP/OP_途中（鐘の言い伝え）`・`OP/OP_塔の入口（F1_01）` は Stage1_1 と同じ物がある。

### Test_Mapchip（マップチップの見え方を試すシーン。2026-11-25）
- **Build Settings に入れない**（試すとき専用）。レシピ `mapchip_test`（なければ Stage1_4 をコピーし、T字通路・古い Grid・MapEntity を外して作る。何度流しても同じ）。シナリオ `mapchip_test.txt`。
- マップ 24×18 マス（1 マス＝64px＝PPU 64、左下のマスが (0,0)）。下から海（Umi.1/Umi.2 の市松、y0〜2）／床（左 x1〜11 砂・右 x12〜22 Yuka、Yuka の中に砂の 3×3〔x16〜18・y6〜8〕）／上の壁 Kabe.1（y16〜17）／左右の壁 Kabe.1＋部屋の側の縁（x0・x23。11-25 から。前は Kabe.2）。歯車は飾りの層。Tilemap は「床」0・「壁」1・「飾り」2、当たり判定なし（歩ける範囲で決める）。Grid のセルの大きさ 1・すき間 0（変えない）。
- 部屋の型（Room `Test_Mapchip`）：部屋の絵＝**マップの範囲（見えない絵。Suna.1 を 24×18 に引き伸ばして透明・MapBounds）**。プレイヤーの初期化が MapBounds を必ず使う（ないとエラーで一時停止）。歩ける範囲＝壁の内側（x1〜23・y3〜16）、カメラの範囲＝マップ全体。出現位置 (12, 5.5)。
- カメラ：**Pixel Perfect Camera**（URP。Assets PPU 64・基準 920×690・Crop Frame＝Stretch Fill・Grid Snapping＝Pixel Snapping、Upscale Render Texture は使わない）＋ `MapchipViewTester`（P＝オン・オフ、Z＝ズーム）。
  - 920×690 にした理由：ステージは縦 10.8 マス（Orthographic Size 5.4）・中央 4:3。64px のチップで同じ高さ＝691px → 4:3 で 920×690（Size 5.39）。指示書の候補（320×180 など）は 16px くらいの絵向けで、寄りすぎる。
  - 1920×1080 で確かめた（11-25、描画先を 1920×1080 にして撮影）：Stretch Fill は 4:3 の `Camera.rect`（MapCameraAspect）を保ち、Size 5.39。Crop Frame＝None は画面全体（1920×1080）に広がって Size 8.44（縦 16.9 マス）になる＝使えない。1080p では 1.565 倍の引き伸ばし（整数倍ではない）なので、継ぎ目の対策の中心はアトラスの余白・Point・Clamp・圧縮なし。
  - ゲームビューが 920×690 より小さいと、Pixel Perfect Camera が「Target resolution is smaller than the reference resolution」を出して効かない（学校の PC のゲームビューは 640×480）。見比べるときはゲームビューを 1920×1080 などに。
  - **今のステージ（Stage1_0〜Stage_1_Boss）には付けていない**。付けるかはユーザーがこのシーンで見てから決める。
- シグルは出ない（`Flag_Sigle_Joined` が立っていない。2人で試すならデバッグウィンドウでフラグを立てる）。
- **東の「浜辺と遺跡」（マップチップの見本。11-25）**：レシピは `mapchip_tiles`（追加タイル・パレット）→ `mapchip_test`（試しの場所。右の壁 x23 の y8〜9 に通り道、MapBounds はシーン全体 66×28）→ `mapchip_beach`（x26〜66・y0〜28＝40×28 マス。ルート「浜辺と遺跡（Test_Mapchip_Beach）」・「Grid（浜辺と遺跡）」）。シナリオ `mapchip_beach.txt`。
  - 中身：段々の海岸線と入り江（Umi.2 の4方向）・沖ほど暗い海（影のタイル）・砂浜（ランダム反転）・途切れた石畳の道・遺跡の部屋 A（lx8〜16・床 y14〜20）と B（lx20〜35・床 y15〜22、下の辺 lx26〜28 は崩れて砂が入り込む）を廊下（lx17〜19・y16〜17）でつなぐ・崩れた壁のかけら 5 つ・見せ場の大きな歯車（部屋 B）・散らばった歯車（影つき）。
  - 層の並び順：海と砂 0・床 1・床の上の砂 2・影 3（物の影 4）・壁の面 4・平らな小物 5・キャラと立っている物 100・壁の縁 110／111。
  - 歩ける範囲：海岸線の段ごとの四角（波打ち際は砂の半分だけ）＋通り道。壁は細い帯だけ `WalkAreaExclude`（部屋の中は陸の四角に入っている）。**上下の壁の帯は縁から足の分（`FEET` 0.7）離す**（11-25 の不具合：壁の上側を歩くと足が壁の面にはみ出た。ノアの絵は位置から 0.61 下・シグル 0.64）。シナリオ `mapchip_beach` が上の壁・下の壁・廊下・かけらで足の位置を確かめる。立っている歯車は円で抜く。カメラの範囲は x23.8〜66（試しの場所の 0〜24 と少し重ねる）。
  - 歯車はドット絵なので 90 度ごとの回転＋反転だけ（斜めに回すと画素がゆがんだ）。影は `DropShadow2D`、立っている歯車は 8 角形の Shadow Caster 2D。**立っている歯車は「奥行きのある物」（DepthProp）**（11-25）：見せ場の大と中はまとめて 1 つ（当たり判定 x52.65〜57.55・y18.1〜20.2、前後の境目 y19.95）、小（x58.95〜59.86・y16.75〜17.4、境目 y17.15）、入口の脇（x31.64〜32.76・y11.8〜12.6、境目 y12.35）。シナリオで後ろを通れる・前から入れない・押し続けても止まったまま・**後ろでは上下左右どの向きでも後ろ・手前ではどの向きでも前**を確かめる。
  - 見た目（11-25 提案 A・B・C・E）：子「見た目（明かり・場所の色味）」＝たいまつの明かり 4（ゆらぐ・影あり）・水面のきらめき 3（影なし）・場所の色味（夕方の浜辺 0.85／遺跡の中 0.42・優先度 1）。「隅の影（自動・AutoShade）」の層（並び順 2）。壁の面の Tilemap の子に「壁の影（Shadow Caster 2D・自動）」。
  - 歩ける範囲が 2 つ（試しの場所・浜辺と遺跡）あるシーン。シーンチェックは全部の範囲を合わせて判定する（11-25 に直した。前は最初の 1 つだけを見て、浜辺の出現位置をエラーにしていた）。
  - コツとタイルの使い方は `Docs/マップチップの置き方.md`。

### Title・SceneGameOver
- Title：`TitleSceneBuilder`（Tools > タイトル画面）で作り直せる。背景 `Images/UI/UI_TitleImage.PNG`（AspectRatioFitter で画面いっぱい）。
- SceneGameOver：Canvas に `MenuKeyboardNav`（レシピ `gameover_keyboard`）。

## 仮の素材
- 仮の絵は名前に `kari`（`Images/Enemy/*_kari.png`、`res_kari/`）か、レシピで作った丸・四角。差し替えるときは同じ名前のファイルを上書きすると参照が切れない（取り込み設定〔Sprite・Pixels Per Unit〕は確かめる）。
- 立ち絵：`Images/UI/UI_Noah.png`・`UI_siguru.png`（`Data/Characters/*.asset` の「通常」）。
