---
name: unity-playtest
description: CT プロジェクトを Unity エディタで実際に再生して動作確認するときの準備と後片付け。セーブ（save_*.json＝ユーザーのスロット1〜3）のバックアップと復元、run_in_background、無敵、シーンを開く、キー入力、C# での状態の確認・操作、性能の計測（perf.ps1：fps・処理時間・GC の確保・前回との比較・メモリの漏れ）。再生して確かめる・重さを測るときは必ずこれを使う。
---

# unity-playtest：再生テストの準備と後片付け

**ユーザーも同じエディタでテストプレイしていることがある。スロット1〜3はすべてユーザーのデータ。**
必ず `start.ps1` で始め、成功しても失敗しても `end.ps1` で終える。

## 流れ

```powershell
# 1. 準備：セーブのバックアップ → （シーンを開く）→ run_in_background → 再生 → 無敵
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/start.ps1 -Scene Stage1_1

# 2. 確かめる（下の「よく使う C#」・キー入力）

# 3. 後片付け：再生停止 → run_in_background を戻す → セーブを元に戻す → Assets/__ClaudeTest を消す → 元のシーンを開き直す
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/end.ps1
```

- `start.ps1 -Scene <名前>`：シーンを開いてから再生（省略すると今開いているシーン）。**保存していない変更があるシーンが開いていると止まる**（勝手に保存・破棄しない。ユーザーに聞く）。
- `start.ps1 -NoInvincible`：無敵にしない（ダメージ・ゲームオーバーを確かめるとき）。
- 再生中なら start は止まる（ユーザーがテスト中かもしれない）。前のテストの片付けが残っていても止まる → `end.ps1` を先に。
- バックアップは `%TEMP%\ct_playtest_backup\<日時>\` に残る（end のあとも消さない）。
- `end.ps1 -KeepTestAssets`：`Assets/__ClaudeTest/` を残す。
- **フォント（TMP の動的なアトラス）は自動で戻る**（2026-11-28）：start が `Assets/Fonts/*.asset` を写し、end が中身の変わった物を書き戻して読み直す（メモリの上だけで変わった〔dirty〕物も読み直す）。`NotoSansJP-Regular SDF.asset` を手で戻す必要はない。end の出力の `font assets:` の行で確かめる。2026-11-28 に試したときは、再生だけではファイルは変わらなかった（unchanged）。前に変わっていたのは、レシピでシーンに日本語の文字を置いて保存したときに、エディタが字を足して書いた分らしい（再生ではなく保存のとき）。
- 再コンパイルの直後の `PIPELINE_UNREACHABLE` は `Invoke-UnityEval` が 2 秒おきに 10 回までやり直す（コードは動いていないので、やり直しても安全）。
- エディタのドメインリロードは無効なので、再生開始はすぐ。static は `SubsystemRegistration` でリセットされる（無敵は再生のたびに start が付け直す）。

## キー入力

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/press.ps1 -Key Tab
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/press.ps1 -Key "W" -HoldMs 800
```

キー名は `UnityEngine.InputSystem.Key`（`E`・`C`・`F`・`Space`・`Tab`・`Escape`・`UpArrow` など）。
**注意**：Input System の設定が `PointersAndKeyboardsRespectGameViewFocus` なので、ゲームビューにフォーカスがないとアクション経由の入力（UI の決定・移動など）は届かず、結果が安定しない。UI のキー操作は結果を断定せず、ユーザーに手元での確認を頼む。UI のボタンは `button.onClick.Invoke()` で押す。

## イベントを確かめる（`event.ps1`）

会話・演出・追跡などのイベントを再生で確かめるときの道具。1回に1つの操作。

```powershell
$ev = ".claude/skills/unity-playtest/scripts/event.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -State              # 1行で状態（会話の文・入力停止・2人・暗さ・敵・戦闘エリア）
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Advance 3          # 会話を3文進める（表示途中なら全文→次へ）
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -UntilIdle          # 会話・イベントが終わるまで進め続ける
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Warp "-128.6,13.0" # ノアとシグルをワープ
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Run "Stage1_2_イベント/ランタン（調べると拾う）"  # 調べる・エリアに入る の代わり
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Flag Flag_Sigle_Joined -Value 1   # フラグを ON/OFF
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Capture s1         # ゲームビューを撮る（%TEMP%\ct_playtest_capture\s1.png を Read で見る）
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -CaptureAB frame -Before "PresentationDirector.SetFrameForEvent(false, 0f);" -After "PresentationDirector.SetFrameForEvent(true, 0f);" -Labels "なし,あり"
                                                                            # 前後を撮って横に並べる（frame.png。報告に添える比較画像）
```

重さを前後で比べる（同じ場所で切り替えるだけの比較。CSV には書かない）：
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/perf.ps1 -AB -Off "MapDebugView.Show = false;" -On "MapDebugView.Show = true;" -Seconds 10
```

### テストシナリオ（11-14 に追加。**通しの確認・壊れていないかの確認はまずこれ**）

```powershell
$sc = ".claude/skills/unity-playtest/scripts/scenario.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $sc -Name stage1_2_rescue   # Tests/Scenarios/stage1_2_rescue.txt
powershell -NoProfile -ExecutionPolicy Bypass -File $sc -Name all               # 全部（1本 1〜2 分）
```

- 書き方は `Docs/テストシナリオの書き方.md`（プランナー向け）と、`scenario_legacy.ps1` の先頭のコメント（命令の一覧。新旧で同じ）。
- **エディタの中でまとめて動かす**（2026-11-28〜）：最初の `scene` の行だけ `start.ps1`、残りは 1 回の eval で `Assets/Script/Debug/Editor/ScenarioRunner.cs` に渡す（`expect eval`・`expect within`・`wait until`・`eval` の C# はラムダにして一緒に翻訳）。実行役が `EditorApplication.update` で 1 行ずつ進め、結果を `%TEMP%\ct_scenario_<名前>.txt` に書き、scenario.ps1 はそれを表示するだけ。1 本 約 2 分 → 約 1 分（残りはシナリオの中の wait）。
  - **C# の式が 1 つでも翻訳できないと、シナリオ全体が始まらない**（`runner did not start` と、翻訳のエラーの行番号＝生成したコードの中の行）。前は、その行だけ不合格だった。
  - `key X 1200` の数字は**本当に押している時間**。前の実行役は呼び出しの往復（約 1 秒）が足されていたので、11-28 に全シナリオの数字を +1000 した。
  - `expect no-error` は `Application.logMessageReceivedThreaded` で数える。前の実行役はコンソールのウィンドウの表示（`LogEntries`）を読んでいたので、**表示の絞り込みによってはエラーを見逃していた**（11-28：TMP の例外 1900 件を見逃した）。
  - 前の実行役は `-Legacy` で使える（`scenario_legacy.ps1`。新しい方の不具合を疑うときに比べる）。ただし今のシナリオは新しい方に合わせてある（`key` の数字 +1000 など）ので、前の方では押す時間が 1 秒ほど長くなる。
  - **前の実行役の遅さに頼っていた所**（11-28 に全部を流して見つけ、直した）：`advance 数` を会話が出る前に呼ぶ（→ 実行役が会話が出るまで最大 5 秒待つ）・会話を続けてすぐ送ると飛ばされる（→ 1 文ごとに次の文に変わるのを待つ）・やり直しの直後のワープが戻される（`stage1_2_chase_retry` に `wait 1.5`）・イベントのあとの入力の止まりの間に E を押す（`corridor` に `wait until 5 !InputBlock.IsBlocked`）・回想が抜けきる前に確かめる（`presentation_gesture` に `wait until`）。**新しいシナリオでも同じことに気をつける**。
- **変えたファイルから選ぶ**：`scenario.ps1 -Affected`（`-WhatIf` で一覧だけ）。対応表は `data/affected.tsv`（上から最初に当たった行。`@self`・`@scene`＝そのシーンを使うシナリオ・`@all`・`-`）。表にないファイルは `NOMAP` と出る → 手で決めて、表に行を足す。シナリオを足したら関係する行に名前を足す。
- **見た目の比較** `expect looks <名前> [許す割合%]`：ゲームビューを撮って 192×108 の升目に縮め、`Tests/Baselines/<名前>.png` と比べる。色の差が 0.12 を超える升目の割合が許す割合（既定 5%）以下なら合格。不合格なら違う所を赤くした `%TEMP%\ct_playtest_capture\looks_<名前>_diff.png` を見る。**見た目を変えて、それが正しいとユーザーが言ったら `-UpdateBaselines`** で基準を作り直す（基準は Git に入れる）。光漏れ・雲など動く物があるので、許す割合はシナリオごとに決める。
- `scene` の最初の行で `start.ps1` を呼び、最後に必ず `end.ps1`（セーブを戻す）。`-KeepPlaying` で再生を止めない。`-StopOnFail` で最初の不合格で止まる。`-Name a,b,c` で続けて流す。
- **本当のキー入力**：実行中だけ `InputSystem.settings.editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`・`backgroundBehavior = IgnoreFocus` にして `QueueStateEvent` で押す（エディタが手前になくても届く。E で調べる・Enter で UI の決定まで確かめた）。終わったら元の設定（`PointersAndKeyboardsRespectGameViewFocus`・`ResetAndDisableNonBackgroundDevices`）に戻す。press.ps1 を単体で使うときは設定を変えないので届かない。設定を変えたあと `InputSystem.EnableDevice(Keyboard.current)` も呼ぶ（2026-11-25、学校の PC：エディタが裏のまま再生を始めると、設定を変えてもキーボードが止まったままで、どのキーも届かなかった）。
- `key` の 1 行は呼び出し 1 回（押す → エディタの `EditorApplication.update` が決まった時間のあとに離す）。2026-11-28 までは押す・離すで 2 回呼び、1 行に約 3 秒かかっていた。
- **時間で消える物・時間がかかる物は、`wait 秒` の当て推量ではなく `expect within 秒 式`（その秒数のうちに真になれば合格）と `wait until 秒 式`（真になるまで待つ）**で書く。字幕・フェード・空の切り替えなど（2026-11-27、決まった秒数で書いて字幕の確認が 2 回落ちた）。
- 追跡は `advance battle`（戦闘エリアが始まった瞬間に止まる）のすぐ後に `warp`。遅いと捕まって最初からやり直しになる。
- 戦闘中に `invincible off` で会話を送ると倒れる。倒す確認は `kill`（自分で無敵を切る）。
- `expect no-error` は、始めてからのコンソールのエラー（Pipeline・Account API の通信のエラーは除く）。
- イベント・部屋・セーブを変えたら、関係するシナリオを流す（finish-task）。新しいイベントには1本足す。

### ロードの確かめ・調べるの確かめ（11-13 に追加）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -SaveOn          # 直接再生のとき：セーブを有効に（書くのは save_auto.json。start.ps1 がバックアップ済み）
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -AutoSave        # 今の位置でオートセーブ
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Reload          # タイトルへ → つづきから（オートセーブ）→ 状態を表示
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -CheckInteract   # 範囲の中の調べる物に、E キーの経路（Intaractable.Update）が生きているか
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Prompt          # キーの案内（[E] 〜）が今何を出しているか
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Reach           # 今いる場所から、調べる物・出口・出現位置へ歩いて行けるか（MapReach）
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -ShowArea -Value 1  # 歩ける範囲の線をゲーム画面に出す（0 で消す）→ -Capture で見る
```

- **ロードの確かめ（ユーザーの指示）**：ふだんはシナリオの `reload` 行で済ませる（合格なら手で試し直さない。11-20 合意）。シナリオがない通り道・不具合を調べるときだけ、イベントの前・途中・後で `-Reload`。見ること：出た場所から先へ進めるか（入口側に戻されていないか）・イベントが繰り返さないか・拾った物やフラグが残っているか。
- `-Run` で `Interact` を直接呼ぶと、キー入力の経路が壊れていても動いてしまう（ペグの不具合を見逃した）。調べる物を足した・継承した物を変えたら `-CheckInteract` も。
- **eval からのキー入力（`InputSystem.QueueStateEvent` も）はゲームビューに届かない**。キーの押し心地は「確かめられなかった」と報告する。

- **キー入力（press.ps1）は届かないことが多い**（ゲームビューのフォーカス）。歩く・調べるは `-Warp`・`-Run` で代わりにする。キーで確かめられなかったことは報告に書く。
- `-Run` は**表示されている**オブジェクトだけ（`GameObject.Find`）。ItemInteraction は操作中のキャラが調べた扱い、EventSequence はそのまま再生。
- **eval は 1 回 1.5 秒ほどかかる**。それより短い演出（フラッシュ・弾・吹き出しの出始め）は撮れない・状態で見逃す。撮れなかったら「確かめられなかった」と書く。撮りたいときは、進める操作の直後に間を入れずに `-Capture`。
- **追跡（BattleArea の「捕まる → やり直す」）の落とし穴**：ノアを動かさないと、捕まる → 暗転 → 読み直し を繰り返す（その間に出した吹き出しも消える・ワープも戻される）。確かめるときは、追跡が始まったら**すぐ** `-Warp` でゴールへ。止めたいときは `BattleArea.Cancel()` でも、やり直しの予約が残っていることがあるので `end.ps1` で再生ごと止める。
- 向きを確かめるときは `-State` の `noah=… Right` などを見る（CharacterAnimLink は、止まっている間は向きを変えない。走り込んだ直後は勢いで向きが上書きされる）。
- 長い組み立て（シーンにイベントを置くなど）は C# をスクラッチパッドに書いて `Invoke-UnityEval -File`。作り直すときは、ルートのオブジェクトを消してから作る形にしておくと、何度でもやり直せる。

## 性能を測る（`perf.ps1`、11-13 に追加）

```powershell
$pf = ".claude/skills/unity-playtest/scripts/perf.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File $pf -Measure -Label timpani_battle -Seconds 20   # 記録 → PerfLog/perf.csv → 前回と比べる → 3秒プロファイラ
powershell -NoProfile -ExecutionPolicy Bypass -File $pf -Begin -Label op_event   # ここから記録（event.ps1 で進める）
powershell -NoProfile -ExecutionPolicy Bypass -File $pf -End                     # 記録を止めて同じように出す
powershell -NoProfile -ExecutionPolicy Bypass -File $pf -Profile                 # 3秒だけプロファイラ（ゲームの確保・確保の多いスクリプト・時間の多い処理）
powershell -NoProfile -ExecutionPolicy Bypass -File $pf -LeakCheck -Times 3      # タイトル → つづきから を繰り返し、メモリ・物の数が増え続けないか（先に event.ps1 -SaveOn・-AutoSave）
powershell -NoProfile -ExecutionPolicy Bypass -File $pf -History -Label timpani_battle
```

- **同じラベル＝同じ場面**。前回の同じラベルと比べて、悪くなった値に `!!`、良くなった値に `++`。`!!` が出たら、もう一度測ってから判断する（エディタの揺れ）。
- 基準の場面：`timpani_battle`（Stage_1_Boss を start → `event.ps1 -Flag Flag_Sigle_Joined -Value 1` → `-UntilIdle` で戦闘が始まる → `-Measure`）。
- **エディタの落とし穴**（GC の確保にエディタの分が入る・メモリは回ごとに動く など）と基準の値は `rules/performance.md`。
- `PerfLog/` はコミットしてよい（前回との比較の元になる）。テストで作った行は消してよい。

## よく使う C#（`Invoke-UnityEval`）

```powershell
. .claude/skills/unity-check/scripts/lib.ps1
Invoke-UnityEval -Code 'var m = UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>(); m.CurrentPlayer.name + " " + m.CurrentPlayer.transform.position'
```

- 長いものは `Assets/` の外（例：スクラッチパッド）に .cs を書いて `Invoke-UnityEval -File <パス>`。**Assets の中に置くとインポート・再コンパイルが走る。**テストで作るアセットだけ `Assets/__ClaudeTest/` に置く。
- 最後の式が結果として返る（`return 式;` と書いてもよい）。
- 時間を進めるには eval を分けて間に `Start-Sleep` を入れる（1回の eval の中ではフレームは進まない）。

| 確かめたいこと | 式 |
|---|---|
| 操作中のキャラ | `UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>().CurrentPlayer` |
| ワープ | `PlayerSwitchManager.Warp`（引数はコードを見て確かめる） |
| フラグ | `GameFlags` の static メソッド |
| 仕掛けの状態 | `GimmickState.All` |
| 敵 | `Enemy.All` |
| 入力が止まっているか | `InputBlock.IsBlocked` |
| セーブの仮置き・確定 | `Save` の static メソッド（**Title から始めるとスロットに書き込む**。end で戻る） |

## 確認できなかったこと
キー入力が届かなかった・見た目（描画・音）など、eval で確かめられなかった項目は**はっきりユーザーに伝える**。
