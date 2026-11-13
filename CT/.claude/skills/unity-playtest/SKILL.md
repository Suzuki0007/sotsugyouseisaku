---
name: unity-playtest
description: CT プロジェクトを Unity エディタで実際に再生して動作確認するときの準備と後片付け。セーブ（save_*.json＝ユーザーのスロット1〜3）のバックアップと復元、run_in_background、無敵、シーンを開く、キー入力、C# での状態の確認・操作。再生して確かめるときは必ずこれを使う。
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
```

### ロードの確かめ・調べるの確かめ（11-13 に追加）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -SaveOn          # 直接再生のとき：セーブを有効に（書くのは save_auto.json。start.ps1 がバックアップ済み）
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -AutoSave        # 今の位置でオートセーブ
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Reload          # タイトルへ → つづきから（オートセーブ）→ 状態を表示
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -CheckInteract   # 範囲の中の調べる物に、E キーの経路（Intaractable.Update）が生きているか
powershell -NoProfile -ExecutionPolicy Bypass -File $ev -Prompt          # キーの案内（[E] 〜）が今何を出しているか
```

- **ロードの確かめ（ユーザーの指示：ときどき行う）**：イベントの前・途中・後で `-Reload`。見ること：出た場所から先へ進めるか（入口側に戻されていないか）・イベントが繰り返さないか・拾った物やフラグが残っているか。
- `-Run` で `Interact` を直接呼ぶと、キー入力の経路が壊れていても動いてしまう（ペグの不具合を見逃した）。調べる物を足した・継承した物を変えたら `-CheckInteract` も。
- **eval からのキー入力（`InputSystem.QueueStateEvent` も）はゲームビューに届かない**。キーの押し心地は「確かめられなかった」と報告する。

- **キー入力（press.ps1）は届かないことが多い**（ゲームビューのフォーカス）。歩く・調べるは `-Warp`・`-Run` で代わりにする。キーで確かめられなかったことは報告に書く。
- `-Run` は**表示されている**オブジェクトだけ（`GameObject.Find`）。ItemInteraction は操作中のキャラが調べた扱い、EventSequence はそのまま再生。
- **eval は 1 回 1.5 秒ほどかかる**。それより短い演出（フラッシュ・弾・吹き出しの出始め）は撮れない・状態で見逃す。撮れなかったら「確かめられなかった」と書く。撮りたいときは、進める操作の直後に間を入れずに `-Capture`。
- **追跡（BattleArea の「捕まる → やり直す」）の落とし穴**：ノアを動かさないと、捕まる → 暗転 → 読み直し を繰り返す（その間に出した吹き出しも消える・ワープも戻される）。確かめるときは、追跡が始まったら**すぐ** `-Warp` でゴールへ。止めたいときは `BattleArea.Cancel()` でも、やり直しの予約が残っていることがあるので `end.ps1` で再生ごと止める。
- 向きを確かめるときは `-State` の `noah=… Right` などを見る（CharacterAnimLink は、止まっている間は向きを変えない。走り込んだ直後は勢いで向きが上書きされる）。
- 長い組み立て（シーンにイベントを置くなど）は C# をスクラッチパッドに書いて `Invoke-UnityEval -File`。作り直すときは、ルートのオブジェクトを消してから作る形にしておくと、何度でもやり直せる。

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
