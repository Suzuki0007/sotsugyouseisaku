---
name: unity-check
description: CT プロジェクトで C# を書き換えたあとの確認。開いている Unity エディタを再コンパイルし、コンソールの警告・エラーを1行ずつ要約し、必要ならシーンチェック（SceneChecker）も実行する。スクリプトを変えたら毎回・人に確認を頼む前に使う。
---

# unity-check：再コンパイルとコンソールの確認

コードを変えたら、**人に確認を頼む前に**これを実行する。

## 使い方

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/unity-check/scripts/check.ps1
```

- `-SceneCheck`：エディタで今開いているシーンとプロジェクト全体に `SceneChecker` をかける（シーンは開かない・保存しない）。仕掛け・敵・イベントなどシーンに置く物を変えたときに付ける。
- `-Strict`：警告もコンパイル失敗として扱う。
- `-Tail 50`：コンソールを何件まで見るか（初期値 30）。

出力の最後が `[RESULT] compile OK` / `compile FAILED`。終了コード 0 = OK、1 = コンパイル失敗、2 = エディタがない。

## エディタが起動していないとき
`[ERROR] Unity Editor is not ready` が出たら、ユーザーに断ってから起動する（数分かかるので裏で待つ）：

```powershell
$env:Path = "$env:LOCALAPPDATA\Unity\bin;" + $env:Path; unity open . ; unity status --until-ready --timeout 600
```

## Unity CLI の場所（PC が2台）
- 家の PC（ユーザー uhiro）と学校の PC（ユーザー amg）の両方で作業する。CLI はどちらも `%LOCALAPPDATA%\Unity\bin\unity.exe`（公式の入れ方の既定の場所）。`scripts/lib.ps1` はユーザー名を書かずにここを探す（`UNITY_CLI_HOME` があればそちらが先、なければ PATH の `unity`）。
- CLI がない PC では `[ERROR] Unity CLI not found` が出る → `irm https://unity.com/install.ps1 | iex` で入れる（ユーザーに断ってから。2026-11-25 に学校の PC へ入れた、1.0.1）。
- スクリプトに **ユーザー名の入ったパスを書かない**（`$env:LOCALAPPDATA`・`$env:USERPROFILE` を使う）。Fork の Git も `%LOCALAPPDATA%\Fork\gitInstance` から探している。

## 結果の読み方
- コンパイルのエラー・警告は `recompile` の表（severity / file / line / message）に出る。**自分が書いたファイルのものだけ直す。**既存のものは報告して `Docs/code-map.md` の「4. 問題点」へ。
- コンソールには前からある警告も混ざる。今のところ毎回出るもの（無視してよい）：
  - `Account API did not become accessible within 30 seconds`（AI Assistant パッケージ。サインインしていないため）
  - `90 node options failed to load`（Visual Scripting。code-map の問題点に記載済み）
- 単発の C# を実行したいときは `scripts/lib.ps1` を読み込んで `Invoke-UnityEval -Code '...'` を使う（PowerShell 5.1 は引数の `"` を壊すので、一時ファイル経由で渡している）：

```powershell
. .claude/skills/unity-check/scripts/lib.ps1
Invoke-UnityEval -Code 'UnityEngine.Application.persistentDataPath'
```

## 注意
- コードを変えた直後はリロード中で `EDITOR_NOT_RUNNING` になることがある → スクリプトが `status --until-ready` で待つ。
- .ps1 に日本語を書くなら UTF-8 の BOM 付き（BOM なしだと文字化けして構文エラー。Write／Edit のあとフック `bom-ps1` が自動で付ける）。関数名 `H` は使わない（Get-History の別名）。
