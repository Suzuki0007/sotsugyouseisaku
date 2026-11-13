---
paths:
  - "Assets/Script/**"
---

# コードの書き方（Assets/Script を触るとき）

## 置き場所
- スクリプトは `Assets/Script/<機能>/`、エディタ拡張は各機能の `Editor/` フォルダ。asmdef はない（Assembly-CSharp / -Editor）。
- 新しいファイル・役割の変更は `Docs/code-map.md` の「3. スクリプト一覧」に足す。その機能の表を読んでから書き始める。

## 書き方
- ファイル先頭のヘッダーコメントをそろえる：
  ```
  /*********************************************************************/
  // * \file   ファイル名.cs
  // * \brief  何をするか（日本語）
  // *
  // * \author
  /*********************************************************************/
  ```
- `//===` の区切り、`if(x) { ... }` の1行スタイル、日本語のコメント・`/// <summary>` に合わせる。
- エディタのドメインリロードは無効。**static を持つクラスは `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` で再生ごとにリセット**する。
- `FindObjectsByType` は `FindObjectsSortMode` 付きが非推奨（Unity 6.5）。引数なし版を使う。
- `NavMeshPath` などフィールド初期化で作れない物は Awake で作る。
- **土台のクラスが private な `Update`／`Awake` などを持っているとき、子で同じ名前のメソッドを作らない**（Unity は子のものだけを呼び、土台の処理が止まる）。特に `Intaractable` の子に `Update` を作ると E キーで調べられなくなる（11-13 のペグの不具合。シーンチェックのプロジェクトの確認で検出）。毎フレームの処理は `LateUpdate`、土台が virtual にしていればそれを override して `base.〜()` を呼ぶ。
- **他の人のコード**（ファイル先頭の `\author` が uh 以外：`Intaractable`・`Dialogue`〔成田さん〕、`Save`・`AutoSave`・`CheckPoint`〔鈴木さん〕など）は、なるべく変えずに外側で対応する。変えたらコミットメッセージにその人への説明を書く（CLAUDE.md）。
- **確かめるときに `Interact` などを eval で直接呼ぶだけで済ませない**：キー入力の経路（土台の Update）が動いているかは `unity-playtest` の `event.ps1 -CheckInteract` で見る（eval からのキー入力はゲームビューに届かない）。

## Inspector（プランナーが触る項目）
- 項目名は **`[Label("日本語")]`**。最小値・スライダーは `[Label("名前", min)]` / `[Label("名前", min, max)]`。Range / Min と併用しない。リストには付けない。
- `InspectorName` は enum の値にしか効かない（enum の値の表示名に使う）。
- `[Tooltip("日本語")]` を付ける。
- `[SerializeReference]` の部品（敵の行動など）はクラス名を変えるとデータが消える。変えるときは `[MovedFrom]`。
- **セーブや番号で保存される enum は末尾にだけ足す**（`Expression`、`BarkTrigger`、`DamageKind`〔`DamageKindFilter` は同じ番号＋Any=-1〕など）。

## プランナー向けの機能を足すときのチェックリスト
1. Inspector の日本語（Label・Tooltip・InspectorName）。
2. Hierarchy 右クリックのメニュー（各機能の `Editor/*Menu.cs` に合わせる）や Tools メニュー。
3. 設定ミスが起きやすければ `SceneChecker.CheckScene` / `CheckProject` にチェックを足す。
4. 再生中に確かめたい状態があれば `DebugWindow` に足す。
5. 仕掛けの状態（GimmickState）を参照するなら、配線の表示（`GimmickWiring`）に出るか確かめる。
6. `Docs/` の手順書に追記し、`Docs/プランナー向け作業手順書.md`（付録の早見表を含む）も更新する。
7. 入力を止める条件は `InputBlock.IsBlocked`、敵を止める条件は `EnemyBlock.IsBlocked` にだけ足す。
8. 攻撃を当てる処理は `DamageUtility.Apply(collider, info)` を通す。

## 既存の問題
- 見つけても勝手に直さない。報告して `Docs/code-map.md` の「4. 問題点」に追記する（頼まれたら直す）。
