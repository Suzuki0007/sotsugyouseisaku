---
name: scene-recipe
description: CT プロジェクトで、シーンにイベント・仕掛け・敵などをまとめて組み立てる（または作り直す）ときの手順。組み立ての C#（レシピ）を .claude/skills/scene-recipe/recipes/ に残し、scripts/run.ps1 で開いている Unity エディタに実行させる。シーンに物を置く・作り直す・既存のレシピの順番を知りたいときに使う。
---

# scene-recipe：シーンの組み立てをレシピで行う

シーンに物をまとめて置く作業（Stage1_2 の一層の序盤、ティンパニ部屋、OP など）は、手でつつかずに **C# のレシピ**で組み立てる。
レシピはリポジトリに残すので、別の会話でも同じ物を作り直せる（前は使い捨ての場所に置いていて、作り直しの順番がわからなくなっていた）。

## 使い方

```powershell
$r = ".claude/skills/scene-recipe/scripts/run.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $r -List                  # レシピの一覧（1行目のコメント）
powershell -NoProfile -ExecutionPolicy Bypass -File $r -Recipe stage1_2_door  # 実行
```

- 再生中・**開いているシーンに保存していない変更があるとき**は止まる（ユーザーの作業かもしれない。聞いてから）。
- レシピの中で `OpenScene` → 組み立て → `SaveScene` まで行う。**シーンを保存するので、実行する前にユーザーに「何のために保存するか」を伝える**（CLAUDE.md の約束）。
- 実行したら：参照（フラグ・会話・プレハブ）が入っているかを eval で確かめ、`unity-check -SceneCheck`、必要なら `unity-playtest`。

## 新しいレシピを書く

1. `recipes/<シーン>_<内容>.cs` を作る。**1行目は「// 何を作るか」のコメント**（一覧に出る）。
2. `// @helpers` の行を入れると、`recipes/_helpers.cs`（SetProp・SetList・NewGO・Flag・Dlg・MakeSeq・AddTalk・AddWait・OpenSceneSafe・Log）が前に付く。
3. **作り直せる形にする**：ルートのオブジェクトを探して消してから作る（何度実行しても同じ結果）。部品を足すだけのレシピは「もうあれば足さない」。
4. 最後に `return Log.ToString();` で、作った物・入れた参照を返す（確かめやすい）。
5. フラグ・会話などのアセットは、シーンを開いたあとで読み直す（シーンを開くと前に読んだ参照が外れることがあった：ティンパニの TimpaniRoom のクリアのフラグ）。

## 今あるレシピと順番（作り直すとき）

| シーン | 順番 | 注意 |
|---|---|---|
| Stage1_1 | `stage1_1_op` → `autosave_points` | ルート「OP」を消して作る |
| Stage1_2 | `stage1_2_events` → `stage1_2_door` → `autosave_points` → `audio_3_scenes` | events は「Stage1_2_イベント」を消して作るので、扉・オートセーブ・音も消える。11-19 から助けの場面は合流せず扉A → 書斎（先に `shosai_0_assets` と会話の取り込みが要る） |
| Stage1_3（書斎） | `shosai_0_assets` → `shosai_1_scene` → `shosai_2_contents` | 0＝フラグ・アイテム・メモ・一言（何度でも）。1＝シーンがなければ Stage_1_Boss をコピーし、ルート「書斎（Stage1_3）」を消して作り直す。**1 のあとは必ず 2**（2 だけを2回は流せない）。座標は仮素材の README の中心ピクセル |
| 音（全シーン・データ・プレハブ） | `audio_1_sedata` → `audio_2_assets` → `audio_3_scenes` → `audio_4_footstep_outdoor`（Stage1_1 の外の足音）。**11-15 から同じことを割り当て表 `Docs/素材の割り当て/2026-11-14_BGM_SE.tsv` で流せる**（`asset-intake` の `apply.ps1`。素材の入れ直しはこちらを使う） | 2026-11-14 の BGM・SE の素材。1＝SeData を作る（音量をそろえる）、2＝プレハブ・データの欄、3＝5 シーンの欄・BGM の条件（保存する）。何度実行しても同じ。**Stage1_2・Stage_1_Boss のイベントのレシピで作り直したら `audio_3_scenes` をもう一度**（演出の音・足した鐘やうなり声の部品が消えるため） |
| Stage_1_Boss | `stage1_3_1_room` → `2_pegs` → `3b_enemies` → `3c_bullet` → `4_link` → `4b_calmsprite` → `5_flow` → `autosave_points` | 1_room は「ティンパニ部屋」を消して作るので、**後の全部をやり直す**。`3a_enemydata` はアセット（EnemyData・行動・攻撃・フラグ）を作る/上書きするので、ふだんは実行しない。`6_barks`（11-14）は TimpaniBarks を一言データに移した一度きりのレシピ（スクリプトは削除済みなので、もう実行できない。`5_flow` は戦闘エリアの戦闘中フラグ `Flag_Timpani_InBattle` を入れる形に直した） |
| SceneGameOver | `gameover_keyboard` | |
| （アセット） | `portrait_and_title` | Sigle.asset・UI_TitleImage の取り込み設定・Title の背景 |

**レシピに入っていない、後から変えた値**（作り直すと戻る → 作り直したら入れ直す）：
- `3a_enemydata` のあとで調整したもの：`Attack_Timpani_Ring_Hard`（12 方向×2 回・0.8 秒あけ）・`Attack_Timpani_Spiral`（3 本×14 回）・`Attack_Timpani_Rock` の落とす数 5／0.3 秒／歩ける範囲全体／狙う割合 0.15／ずれ 1.5／間 1.8／アイテム 8%、行動の落石の回数（通常 1・激しい 2・もっと激しい 2、間隔 0.7・0.8・0.8）。値の一覧は `.claude/rules/timpani.md`。
- 手で直したシーンの物は、レシピに入っていない。手で変えたと聞いたら、作り直す前にユーザーに確かめる。

## よくある落とし穴
- `GameObject.Find` は表示されている物だけ。非表示の物は親から `transform.Find`。
- `[SerializeReference]` の行動（敵の行動パターン）は SerializedObject の `managedReferenceValue` で入れる。
- 会話は `DialogueCsv` で作った DialogueData を使う（`Assets/Data/Dialogue/<Scenario_xx>/<ID>.asset`）。会話を直すなら CSV を直して取り込み直す（`dialogue-csv` スキル）。
