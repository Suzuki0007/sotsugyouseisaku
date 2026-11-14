---
name: release-check
description: CT プロジェクトの提出・発表・人に遊んでもらう前の通しの確認。ビルド用のコンパイル（Project Auditor）、テストシナリオ全部、シーンチェック、Build Settings のシーン、提出前に消すもの（不適切な語のファイル・未使用のクラス・テスト用アセット・デバッグの表示）、既知の高い問題、実際のビルドまでを1つの一覧で確かめて報告する。「提出前の確認」「ビルドして」「遊べる版を作りたい」「段階の区切りで全体を確かめて」と言われたときに使う。
---

# release-check：提出・ビルドの前の確認

**直すのは頼まれたときだけ**。このスキルは確かめて一覧にする。消す・直す候補はユーザーが決める（ほかの人のファイルもあるため）。
時間がかかる（シナリオ全部で 10 分ほど・ビルドで数分）。**始める前に、ユーザーがエディタでテストプレイしていないか確かめる**（再生・ビルドの間はエディタを使う）。

## 1. コンパイルとコンソール
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-check/scripts/check.ps1 -SceneCheck
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/perf-review/scripts/auditor.ps1
```
- `!! PLAYER BUILD COMPILE ERRORS` が出たら最優先（エディタでは通るがビルドで落ちる。`#if UNITY_EDITOR` の外の `using UnityEditor` など）。
- シーンチェック：Build Settings の5シーンそれぞれで（`check.ps1 -SceneCheck` は開いているシーン。ほかは `unity-check` の `scene_check.cs` の手順で開いて）。

## 2. テストシナリオ全部
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-playtest/scripts/scenario.ps1 -Name all   # run_in_background で
```
- 不合格は `bug-fix` スキルの手順で原因を調べる（座標のずれ・部屋の作り直しが先に疑わしい）。

## 3. 設定と中身
- [ ] Build Settings が `Title`・`Stage1_1`・`Stage1_2`・`Stage_1_3`・`SceneGameOver` の5つで、`Title` が先頭（`EditorBuildSettings.scenes` を eval で読む）。`Stage1`・`Stage2`・`SampleScene`・`kari_*` が入っていない。
- [ ] **`Assets/Behavior/Enemy/AbcdefuckAction.cs`（不適切な語・未使用）が残っていないか** → 残っていれば削除を提案（Behavior Graph の参照を確かめてから）。
- [ ] `Assets/__ClaudeTest/` がない。`kari_*`・`res_kari/`（仮の素材）がビルドに入るシーンから参照されていないか（`asset-audit` で仮素材の行を見る）。
- [ ] デバッグ用の表示：`PerfMonitor`（F3）・`DebugWindow` の無敵や `DebugStartState` がビルドで効かないか（`Health.PlayerInvincible` が既定で false）。Debug.Log が毎フレーム・命中ごとに出ていないか（code-map の「提出前に整理したいもの」）。
- [ ] 品質の設定：エディタの品質レベルが Very Low になっていた件（11-13）。PC の既定のレベルを確かめる。
- [ ] 音：47MB の WAV の読み込み設定、タイトルの BGM が空（`rules/audio.md`）。
- [ ] `Docs/code-map.md` の「4. 問題点」の **高** の行と、CLAUDE.md の「問題点の要約」を一覧に転記（足音が鳴らない・リトライの位置など）。
- [ ] セーブ：はじめから → OP → オートセーブ → タイトル → つづきから（シナリオ `title_newgame_continue` が合格していれば済み）。

## 4. ビルド（頼まれたとき・1〜3 で止まる問題がないとき）
エディタが開いているので `unity build`（バッチモードで別のエディタを起動する）は使えない。開いているエディタで BuildPipeline を呼ぶ：
```powershell
. .claude/skills/unity-check/scripts/lib.ps1
Invoke-UnityEval -Code @'
var scenes = System.Array.ConvertAll(UnityEditor.EditorBuildSettings.scenes, s => s.path);
var opt = new UnityEditor.BuildPlayerOptions { scenes = scenes, target = UnityEditor.BuildTarget.StandaloneWindows64, locationPathName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ct_build", "CT.exe") };
var r = UnityEditor.BuildPipeline.BuildPlayer(opt).summary;
return r.result + " " + r.totalErrors + " errors, " + (r.totalSize / 1048576) + " MB, " + r.totalTime;
'@
```
- 出力は `%TEMP%\ct_build\`（リポジトリに入れない）。数分エディタが止まる。
- 成功したら、起動して少し動かす確認は**ユーザーに頼む**（ビルドの exe はこちらから操作できない）。

## 5. 報告
```markdown
## 提出前の確認（release-check）
- コンパイル（ビルド用）：OK／NG（…）
- テストシナリオ：5／5 合格（不合格：…）
- シーンチェック：エラー N・警告 N（シーンごと）
- 設定と中身：[x] … [ ] …（残っている物と、消す・直す提案）
- 既知の高い問題：…
- ビルド：成功 NN MB／未実行
- 確かめられなかったこと：ビルドの実際のプレイ・音・見た目
```
