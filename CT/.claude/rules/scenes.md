---
paths:
  - "Assets/Scenes/**"
  - "Assets/Images/**"
  - "Assets/res_kari/**"
---

# シーン（シーンを開く・組み立てる・保存するとき）

ゲームの流れ：`Title` → `Stage1_1`（砂漠・OP）→ `Stage1_2`（一層の序盤）→ `Stage_1_3`（ティンパニ部屋。名前に `_` が2つ）、`SceneGameOver`。Build Settings もこの5つ。
`Stage1`・`Stage2`・`SampleScene`・`kari_*` はファイルだけ（古い作り。触らない）。

## 約束
- **シーンは勝手に保存しない**。保存が要るときは「何のために」をユーザーに伝える。ユーザーも同じエディタで作業・テストしている。**開いているシーンに保存していない変更があるときは、シーンを切り替えない**（聞く）。
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
- 出口：`正面の扉（合流後に調べると Stage_1_3 へ）`（ItemInteraction 調べる・条件 `Flag_Sigle_Joined` ON・完了にしない → F1_08・F1_08b → 揺れ → SceneChangeEventAction〔Stage_1_3・`FromStage1_2`〕）。合流前は調べても何も起きない（キーの案内も出ない）。
- 音は SeEventAction を空欄で置いてある（キーーーン・ドスン・バン）。追跡の敵・ランタンの見た目は仮。
- テストの注意：ノアを動かさずに追跡を始めると、捕まる → 読み直しを繰り返す。追跡が始まったらすぐワープ（`event.ps1 -Warp "-128.6,13.0"`）。
- ランタンの調べる範囲は半径 1 と小さい（ぴったり近づかないとキーの案内が出ない）。

### Stage_1_3（ティンパニ部屋）
- `.claude/rules/timpani.md`（このシーンを開くと自動で読む）。ルート `ティンパニ部屋`（レシピ `stage1_3_*`）。Stage1_1 のコピーなので、使っていない `Event`・宝箱 `OB_TRbox` が残っている（シーンチェックでエラー。害はない）。

### Title・SceneGameOver
- Title：`TitleSceneBuilder`（Tools > タイトル画面）で作り直せる。背景 `Images/UI/UI_TitleImage.PNG`（AspectRatioFitter で画面いっぱい）。
- SceneGameOver：Canvas に `MenuKeyboardNav`（レシピ `gameover_keyboard`）。

## 仮の素材
- 仮の絵は名前に `kari`（`Images/Enemy/*_kari.png`、`res_kari/`）か、レシピで作った丸・四角。差し替えるときは同じ名前のファイルを上書きすると参照が切れない（取り込み設定〔Sprite・Pixels Per Unit〕は確かめる）。
- 立ち絵：`Images/UI/UI_Noah.png`・`UI_siguru.png`（`Data/Characters/*.asset` の「通常」）。
