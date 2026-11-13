---
name: perf-review
description: CT プロジェクトのコード・アセット・設定を性能の観点で点検する（『Unity パフォーマンスチューニング バイブル』『ティップス 2026』由来の 163 ルール＋この作品独自のルール）。scripts/scan.ps1 が全ルールの検索を機械的に行い、毎フレームの処理（Update など）の中かどうか・この作品の例外（rules-overrides.md）まで決めてから、ヒットした所だけルールの詳細を読んで誤検出を除き、ルール ID 付きで報告する。C# を書いた・直したあとの性能の確認、重い処理・GC の確保・アセットの設定（テクスチャ・音）の点検、性能のレビューを頼まれたときに使う。実際の重さを測るのは unity-playtest の perf.ps1。
---

# perf-review：性能の点検（静的）

元のルール集は CyberAgent の公開スキル `unity-perf-review`（MIT、`references/upstream/`、取り込んだ版と出典は `references/upstream/UPSTREAM_README.md`・下の「取り込み元」）。
**この作品は PC・2D・URP・Mono**（`references/tuning-env.md`）。上流の Python のスクリプトはこの PC で動かないので、一次の点検は `scripts/scan.ps1`（PowerShell に移したもの）で行う。

## 流れ

1. **対象を決める**：頼まれた範囲。なければ変更したファイル（scan.ps1 の既定）。
2. **一次の点検（機械）**：
   ```powershell
   $scan = ".claude/skills/perf-review/scripts/scan.ps1"
   powershell -NoProfile -ExecutionPolicy Bypass -File $scan                       # 変更したファイル（git diff HEAD＋未追跡、Assets/ と ProjectSettings/）
   powershell -NoProfile -ExecutionPolicy Bypass -File $scan -Path "Assets/Script/Party,Assets/Script/Enemy"
   powershell -NoProfile -ExecutionPolicy Bypass -File $scan -All                  # 全体（約 15 秒）
   powershell -NoProfile -ExecutionPolicy Bypass -File $scan -All -Only "AS02,CT08" -MaxFiles 999   # ルールを絞って全部の場所
   ```
   - 出力は 1. 毎フレームの処理（**hot**。元の行も出る）→ 2. それ以外のコード（code）→ 3. アセット・設定 → 4. エディタ（数だけ。`-ShowTooling`）→ ABSENT・MANUAL → Coverage。
   - レベルは `rules-overrides.md` を当てたあと（`Critical->Info` のように元のレベルも出る）。Skip になった数は Coverage に出る。
   - 決まった2つ目の条件があるルール（AS04 スプライトのミップマップ・GR10 反射プローブ・UI06 透明な Image）は scan.ps1 が絞り込み済み。
3. **二次の精査（自分で読む）**：ヒットしたルールだけ、表の「詳細」のファイル（`references/upstream/rules-*.md` か `references/rules-project.md`）の**検出パターン・理由・対処**を読み、該当のコードを開いて誤検出を除く。よくある誤検出：
   - 値が変わったときだけ代入している（`if(changed) text.text = ...`）、エラーのときだけ動く行（`if(x == null) { Debug.LogError(...); return; }`）。
   - `PH11`（FixedUpdate）・`US01` の行は**メソッドの宣言の行**に当たる。中身を読んで判断する。
   - `code` の中でも、Update から毎フレーム呼ばれるメソッドは実質 hot（scan.ps1 は呼び出し先を追わない）。気になる処理は呼び出し元を確かめる。
4. **報告**：下の形。各指摘にルール ID。根拠はルールの理由欄（実測値）を引用。**直すのは頼まれたときだけ**。ほかの人のコード（`\author` が uh 以外）は報告だけにして `Docs/code-map.md` の問題点へ。
5. **直したら計測で確かめる**：`unity-playtest` の `perf.ps1 -Measure -Label <同じ場面>` で前後を比べる（静的な点検は「疑い」。効いたかは測って決める。`rules/performance.md`）。

## 報告の形

```markdown
## 性能の点検（perf-review）
対象：…（変更したファイル N 個／フォルダ…）

### 直したほうがよい（Critical・Warning、毎フレームの処理）
- `Assets/Script/Party/PartyHintView.cs:50` LateUpdate で毎フレーム List を new している [CT03]
  - 根拠：毎フレームの確保は GC の止まりの元（11-13 の計測で該当を確認）
  - 直し方：フィールドに持って Clear() で使い回す

### 余裕があれば（Info・初期化のコード・アセット）
（同じ形）

### 誤検出として外したもの
- `InteractPrompt.cs:150` [CT02] 対象が変わったときだけ代入している

### Coverage
（scan.ps1 の Coverage をそのまま）＋ 精査で採用 N 件・誤検出 N 件
```

## 合わせ方（この作品の事情）

- **例外・レベルの付け替え** → `references/rules-overrides.md`（scan.ps1 が読む。コンテキスト `hot`／`code`／`tooling`／`asset`／`all`、ID の `*`）。意図して外している所は理由を備考に書く。
- **環境**（Unity の版・URP・Mono・Windows・Addressables なし）→ `references/tuning-env.md`。変わったら直す（合わないルール＝IL2CPP・iOS・Android・VAT などは自動で外れる）。
- **この作品独自のルール**（CT01〜CT08：TryGetComponent・毎フレームの text 代入／new／name・2D 物理の配列版とレイヤー・Resources.Load・長い音の Load Type）→ `references/rules-project.md` と `checklist-project-code.md`／`checklist-project-asset.md`（**両方を手でそろえる**）。
- **上流のファイル（`references/upstream/`）は直さない**（更新を取り込めなくなる）。合わない所は overrides か独自のルールで。

## 取り込み元と更新

- 取り込み元：https://github.com/CyberAgentGameEntertainment/UnityPerformanceTuningTips2026 の `skills/unity-perf-review`（コミット a981514、2026-11-11）。スキルのフォルダに MIT と書かれている（リポジトリ全体にはライセンスなし）。取り込んだのはルール・チェックリスト・診断集・README・SKILL.md（`UPSTREAM_SKILL.md`）。Python のスクリプトは入れていない。
- 更新するとき：同じ場所を取り直して `references/upstream/` を置き換え → `scan.ps1 -All -Summary` で数が大きく変わっていないか見る。上流のチェックリストの形（6 列の表・`MANUAL`／`ABSENT`・`\|`）が変わったら scan.ps1 の `Read-Checklist` を直す。
- プロファイラの数字の読み方（症状 → 原因 → 対処）は `references/upstream/diagnosis-knowledge.md`。`perf.ps1 -Profile` の結果を読むときに使う。

## 2026-11-13 に全体へかけた結果（基準）

`-All`：コード 304・アセット 769 ファイル、hot 33 行・code 487 行・アセット 165 行。主な本物の指摘（直していない。`Docs/code-map.md` の問題点）：
- `PartyHintView.LateUpdate` の毎フレームの `new List`［CT03］。
- 基底クラス `ObjectBase`・`CharaBase`（鈴木さん）と `Fps` の空の `Update`・`Start`［US01］：自分で Update を持たない子（CharacterSorting・NoahInventory など 20 以上のクラス）でも、毎フレーム空の Update が呼ばれる。
- `BGM/PMH_Atmos_HospitalHall_01.wav`（47MB）・`BGM_Stage1.mp3` が Decompress On Load［CT08］。`UI_TitleImage`・`playermotion_idle(192)` の機種別の設定で最大サイズ 4096／8192［AS01］。Physics 2D の衝突の組み合わせがほぼ全部オン［PH10］。
- エフェクトのサンプル画像 47 枚が非圧縮［AS02］（小さいので影響は小さい）。
