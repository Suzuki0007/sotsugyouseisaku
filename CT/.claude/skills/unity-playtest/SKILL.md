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

## よく使う C#（`Invoke-UnityEval`）

```powershell
. .claude/skills/unity-check/scripts/lib.ps1
Invoke-UnityEval -Code 'var m = UnityEngine.Object.FindAnyObjectByType<PlayerSwitchManager>(); m.CurrentPlayer.name + " " + m.CurrentPlayer.transform.position'
```

- 長いものは `Assets/` の外（例：スクラッチパッド）に .cs を書いて `Invoke-UnityEval -File <パス>`。**Assets の中に置くとインポート・再コンパイルが走る。**テストで作るアセットだけ `Assets/__ClaudeTest/` に置く。
- 最後の式が結果として返る（`return` は書かない）。
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
