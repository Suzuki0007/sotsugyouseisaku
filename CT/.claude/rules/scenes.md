---
paths:
  - "Assets/Scenes/**"
  - "Assets/Images/**"
  - "Assets/res_kari/**"
---

# シーン（シーンを開く・組み立てる・保存するとき）

ゲームの流れ：`Title` → `Stage1_0`（砂浜・OP。夜）→ `Stage1_1`（砂漠。夜）→ `Stage1_2`（一層の序盤）→ `Stage_1_Boss`（ティンパニ部屋。名前に `_` が2つ）、`SceneGameOver`。Build Settings もこの6つ。
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

### Stage1_2（一層の序盤）
- ルート `Stage1_2_イベント`（レシピ `stage1_2_events` → `stage1_2_door` → `autosave_points`）。
- 流れ：着いたとき（SceneStartEvent・`Flag_1_2_Intro_Done`）→ 真っ暗（Darkness 0.97・半径 1）で見えない壁の道をランタンへ → ランタン（ItemInteraction 調べる・saveId `Stage1_2_Lantern`：壁を消す・右の壁の明かり2つだけ点ける〔左の奥は暗いまま〕・`Item_Lantern` を渡す・Darkness 0.88・半径 3・揺れ・右を向く・敵とガレキ〔入口をふさぐ・当たり判定は落ちてから〕が降る・左を映す〔左端は映さない〕・戦闘エリアを始める・一言「逃げなきゃ！」）→ 追跡（BattleArea：ゴール＝左の行き止まり・捕まる・やり直す〔やり直す位置＝敵が出たところ〕・メッセージなし・目的だけ表示・1回だけ `Stage1_2_Chase`）→ 勝ったとき＝助けの場面（シグルは合流フラグ → 待機 → 画面の外へ移す／シグルの攻撃〔仮の弾〕が飛んで当たる・エフェクト・白いフラッシュ・敵を倒す／シグルが画面の外からノアの右 2.6 マスへ歩いてくる → ノア右・シグル左を向く → F1_05〜F1_07 → 一緒 → オートセーブ）。
- 最初の「右を向く」は 0.3 秒待ってから（走り込んだ勢いで向きが上書きされる）。会話中は待機のまま（一緒にするとついてくる動きで近づく）。
- 出口：`正面の扉（合流後に調べると Stage_1_Boss へ）`（ItemInteraction 調べる・条件 `Flag_Sigle_Joined` ON・完了にしない → F1_08・F1_08b → 揺れ → SceneChangeEventAction〔Stage_1_Boss・`FromStage1_2`〕）。合流前は調べても何も起きない（キーの案内も出ない）。
- 音は SeEventAction を空欄で置いてある（キーーーン・ドスン・バン）。追跡の敵・ランタンの見た目は仮。
- テストの注意：ノアを動かさずに追跡を始めると、捕まる → 読み直しを繰り返す。追跡が始まったらすぐワープ（`event.ps1 -Warp "-128.6,13.0"`）。
- ランタンの調べる範囲は半径 1 と小さい（ぴったり近づかないとキーの案内が出ない）。

### Stage_1_Boss（ティンパニ部屋）
- `.claude/rules/timpani.md`（このシーンを開くと自動で読む）。ルート `ティンパニ部屋`（レシピ `stage1_3_*`）。Stage1_1 のコピーなので、使っていない `Event`・宝箱 `OB_TRbox` が残っている（シーンチェックでエラー。害はない）。

### Stage1_0（砂浜。2026-11-19 からゲームの最初のマップ）
- 流れ（ユーザーの指示 2026-11-19、レシピ `stage1_0_flow`）：タイトルの「はじめから」＝ `Stage1_0` の `Start_Boat`（船の上、`PlayerSpawnPoint/出現位置_船の上（はじめから）`）→ `OP/OP_はじめ`（Stage1_1 からのコピー。SceneStartEvent・`Flag_OP_Done` OFF のとき・最後にオートセーブ＝船の上から再開）→ 奥（上の端）の `MapEntity/出口_Stage1_1へ（奥）`（ぶつかると移る SceneChange、横いっぱい）→ `Stage1_1` の `FromStage1_0`（下の端）。Stage1_1 の OP はフラグが立っているので流れない。
- OP の途中（鐘）・塔の入口の範囲は Stage1_0 ではオフ（Stage1_1 の物を使う）。コピー元の塔の扉 `MapEntity/OB_Door`（Stage1_2 行き）もオフ。
- まだないもの：歩ける範囲（WalkArea）。今は海の上も歩ける（マップの絵の中ならどこでも）。
- **夜（Stage1_0・Stage1_1、2026-11-19 ユーザーの指示）**：各シーンの `Global Light 2D` を色 (0.5, 0.6, 0.9)・強さ 0.45（昼は白・1）。レシピ `night_stage1_0_1`。マップ・物・ノア・シグルは Sprite-Lit-Default なので一緒に暗くなる。UI は暗くならない。明かりに反応しない絵（SpriteFrameBlend の波）は「シーンの明るさに合わせる」で同じ色にしている。新しく置く絵も Sprite-Lit-Default にする（Unlit だと夜でも明るいまま）。明るさを試すときは、一時的なカメラで色・強さを何通りか撮って並べる（`Global Light 2D` を変えて撮り、シーンを開き直して捨てる）。
- 波 `sunahama_nami_1_size_0`：SpriteAnimator。**今のデータは `Anim_Sunahama_Nami_Renban`**（連番 `sunahama_nami_animetion.png` を 1440×622 の4コマに切った物・基準は下の真ん中・4fps ループ）。前の `Anim_Sunahama_Nami`（`sunahama_nami_1〜3_size` の3枚）も残してあり、SpriteAnimator の Data を替えれば戻せる。並びは 1,2,3,4,3,2 のループ（ユーザーの指示「1234321」）。**波の SpriteRenderer の Order in Layer は 1**：床 `Grid/soto_yuka` と同じ 0 だと、コマによって床が手前に描かれて波が消えて見えた（2・4コマ目。撮影して確かめた）。マップの絵どうしが重なるときは並び順を変えておく。
- 11-19：波は **SpriteFrameBlend**（コマの間を混ぜる・水面の高さを合わせて上下・ゆらぎ）で動かしている。SpriteAnimator は付いたまま止めてある（戻すときは SpriteFrameBlend を外す・SpriteAnimator を有効・Material を Sprite-Lit-Default に）。データの Fps はユーザーが 0.3 にした（1コマ約 3.3 秒）。確かめ方：一時的なカメラで `EvaluateAt(秒)` ごとに撮る（シーンを保存せずに見られる）。3枚は Single・ピボット下の真ん中（3枚目だけ 3 ピクセル低いので下でそろえる）。前に付いていた Animator（`sunahama_nami_1_size_0.controller`・`NamiAnim.anim`）は外した（アセットは残っている）。
- OP の会話の範囲 `OP/OP_途中（鐘の言い伝え）`・`OP/OP_塔の入口（F1_01）` は Stage1_1 と同じ物がある。

### Title・SceneGameOver
- Title：`TitleSceneBuilder`（Tools > タイトル画面）で作り直せる。背景 `Images/UI/UI_TitleImage.PNG`（AspectRatioFitter で画面いっぱい）。
- SceneGameOver：Canvas に `MenuKeyboardNav`（レシピ `gameover_keyboard`）。

## 仮の素材
- 仮の絵は名前に `kari`（`Images/Enemy/*_kari.png`、`res_kari/`）か、レシピで作った丸・四角。差し替えるときは同じ名前のファイルを上書きすると参照が切れない（取り込み設定〔Sprite・Pixels Per Unit〕は確かめる）。
- 立ち絵：`Images/UI/UI_Noah.png`・`UI_siguru.png`（`Data/Characters/*.asset` の「通常」）。
