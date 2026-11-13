---
paths:
  - "Assets/Script/Debug/**"
  - "Assets/Script/Fps/**"
  - "PerfLog/**"
---

# 性能（計測・チューニング）

元にした資料：『Unity パフォーマンスチューニング バイブル』（CyberAgent、2022）・『Unity パフォーマンスチューニング ティップス 2026』。
この作品は PC・2D・URP なので、モバイル・3D・Render Graph・AssetBundle・LTO・iOS の章は使わない。

## 決めたこと（2026-11-13、ユーザーと合意）
- **推測で直さない。計測して原因を決め、直したら前後を比べる**（バイブル 1章）。
- 目標：**60fps**（`Fps.cs` が targetFrameRate 60・VSync なし）。重い場面の基準は **ティンパニ戦**（Stage_1_3、戦闘エリアが動いている間）。
- 4段階で進める：①計測の道具（PerfMonitor・perf.ps1）**完了** → ②点検のスキル `perf-review`（ティップス 4部3章の形。公開されているルール集〔MIT〕のダウンロードはユーザーが許可済み。Python がないので一次の点検は PowerShell で）→ ③ Project Auditor（`com.unity.project-auditor-rules` を入れるのは許可をもらってから）→ ④ 計測で重いとわかった所だけ直す（ほかの人のコードは報告だけ）。

## 道具
- `Debug/PerfMonitor.cs`：エディタと Development Build だけで自動で作られる。画面右上の表示（F3）、`BeginRecord`／`EndRecord`、`Snapshot`／`CollectNow`。
- `unity-playtest/scripts/perf.ps1`：`-Measure -Label x -Seconds 20`（記録 → `PerfLog/perf.csv` に追記 → 同じラベルの前回と比べる → 3 秒プロファイラでゲームの分の確保と上位の処理）、`-Begin`／`-End`、`-Profile`、`-Snapshot`、`-LeakCheck -Times 3`（`event.ps1 -Reload` を繰り返して増え続けないか。先に `-SaveOn`・`-AutoSave`）、`-History`、`-Overlay`。
- `perf_profile.cs`：プロファイラ（`ProfilerDriver`）の記録を読み、PlayerLoop の GC の確保・時間と、確保の多いスクリプト・時間の多い処理を出す。

## 計測の落とし穴（エディタ）
- **エディタでは「GC Allocated In Frame」にエディタ自身の確保（約 200KB/フレーム）が入る**。ゲームの分はプロファイラの PlayerLoop の値（`gameGcKBPerFrame`、11-13 のティンパニ戦で 約 1.3〜1.7KB）。`GC.GetAllocatedBytesForCurrentThread` はこの Mono では 0 を返すので使えない。
- エディタのメモリ（`memEndMB`・`gcUsedMB`）はエディタの分が大きく、回ごとに数百 MB 動く。比べるのは同じ再生の中の増え方（`-LeakCheck`）だけ。
- `maxms`・`spikes` はエディタの GC の止まり（約 65ms、15 秒に 2 回ほど）が入る。比べるしきい値はゆるくしてある。
- 「Batches Count」はエディタでは取れない → エディタでは `UnityEditor.UnityStats.drawCalls`。「Main Thread」は 60fps の待ちが入るので使わず、`PlayerLoop − WaitForTargetFPS` を「処理」とする。
- 計測中に eval を呼ぶと（約 1.5 秒）その間の値が乱れる。`-Measure` は記録中に何も呼ばない。プロファイラは計測のあとに別に動かす（プロファイラ自体が重いため）。
- エディタの数字は完成品より悪く出る。**比べる目安**として使い、絶対値で判断しない。

## 基準の値（11-13、ティンパニ戦、ブランチ ueeyama_home・f6ec1a1＋未コミット）
- fps 59.8、フレーム p95 約 17.7ms、処理 p50 約 1.8〜2.0ms・p95 約 2.7〜3.1ms、スクリプト p95 約 0.36ms、ドローコール 平均 39、ゲームの確保 約 1.3〜1.7KB/フレーム。
- タイトル → つづきから を 3 回：メモリ・テクスチャ・音・マテリアル・オブジェクトの数は増えない。

## 書くときの約束（資料から。点検のスキルができるまでの目安）
- 毎フレームの処理（Update・LateUpdate・FixedUpdate）で `Find*`・`GetComponent`・`Camera.main`・`new List`・文字列の組み立て・LINQ を使わない（Awake でキャッシュする、変わったときだけ作る）。
- 有るかわからないコンポーネントは `TryGetComponent`（`GetComponent` が null を返すと、エディタで確保が出る）。
- 中身のない Update を残さない。Animator・マテリアルの名前指定は `StringToHash`／`PropertyToID` をキャッシュ。`.material` で複製したマテリアル・`new Texture2D`・`Sprite.Create` は自分で `Destroy`。
- TextMeshPro の数字は `SetText("{0}", n)` か StringBuilder（`text = n.ToString()` は毎回文字列を作る）。uGUI をよく出し入れするなら SetActive より CanvasGroup の alpha（資料の実測で約 80 倍速い）。
