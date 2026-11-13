# ルールの上書き（CT）

> 上流の rules-overrides.example.md と同じ形。**scan.ps1 がこの表を読んで、レベルの付け替えと Skip を機械的に行う**。
> 上流のルール本体（`upstream/`）は変えずに、この作品の事情はここで合わせる。直したら理由を備考に書く。

## コンテキスト（scan.ps1 が行ごとに決める）
- `hot`：Update・LateUpdate・FixedUpdate・OnGUI・OnTriggerStay*・OnCollisionStay* の中（毎フレーム動く）。上流の `in-game` と同じ扱い。
- `code`：それ以外の実行時のコード（Awake・Start・イベントから呼ばれる処理など）。上流の `init` と同じ扱い。
  **注意**：Update から呼ばれる別のメソッドの中は `code` になる（呼び出しの先までは追わない）。指摘が出ない＝安全、ではない。
- `tooling`：`Editor/` フォルダの中、または `#if UNITY_EDITOR` で囲まれたファイル。
- `asset`：.meta・プレハブ・シーン・ProjectSettings。
- `all`：どのコンテキストでも。
- ルール ID の `*` は「すべてのルール」。探す順番：(ID, コンテキスト) → (ID, all) → (*, コンテキスト)。

## 上書きの一覧

| ルールID | base | context | override_level | 備考 |
|----------|------|---------|---------------|------|
| * | - | tooling | Skip | エディタのコードは対象外（下で個別に戻すものを除く） |
| UI09 | Info | tooling | Info | 常に描き直すエディタのウィンドウは対象のまま |
| UI11 | Warning | tooling | Warning | UI Toolkit だけの PropertyDrawer は対象のまま |
| US01 | Critical | all | Warning | 空の Update は確実に直すが、PC で影響は小さい |
| US02 | Critical | code | Skip | 初期化・イベントのときの GetComponent は許容（毎フレームの中だけ指摘） |
| US03 | Critical | code | Info | 初期化での Find は Info（上流の記述どおり）。一定間隔で呼ぶなら備考に理由を |
| US04 | Warning | code | Info | |
| US05 | Critical | code | Info | Debug.Log は PC 向けでは影響が小さい。毎フレーム（hot）だけ Critical のまま |
| US06 | Warning | code | Info | 初期化・イベントでの Instantiate/Destroy は許容 |
| US07 | Warning | code | Info | イベントの演出はコルーチンで作っている（許容） |
| US10 | Warning | code | Info | |
| CS01 | Critical | code | Warning | |
| CS02 | Critical | code | Skip | イベント・UI・セーブの文字列の組み立ては許容。毎フレームだけ指摘 |
| CS03 | Warning | code | Info | |
| CS04 | Info | all | Skip | sealed の効果は IL2CPP の場合。この作品は Mono |
| CS05 | Critical | code | Info | |
| CS06 | Critical | code | Skip | 初期化・エディタのラムダは許容。毎フレームだけ指摘 |
| CS07 | Warning | code | Info | |
| CS08 | Warning | code | Skip | 初期容量はホットパスだけ見る |
| CS09 | Warning | code | Skip | List・配列の foreach は確保しない。毎フレームだけ Info で見る |
| CS09 | Warning | hot | Info | 同上（Dictionary の Values など、インターフェース経由だけ確保する） |
| CS10 | Info | all | Skip | |
| CS11 | Warning | code | Info | |
| CS12 | Warning | code | Info | |
| CS13 | Warning | code | Skip | |
| CS14 | Warning | code | Info | |
| CS15 | Info | all | Skip | |
| AN06 | Warning | all | Skip | 大量のキャラのプロシージャル処理はない（2D・2人＋敵数体） |
| PH08 | Info | all | Skip | 2D。3D の Rigidbody の話 |
| PH12 | Warning | all | Info | プレイヤーは意図して MovePosition で動かしている（rules/gimmick-party.md） |
| PH13 | Info | all | Skip | 2D だけの作品（3D の物理と混ぜていない） |
| UI07 | Warning | code | Info | イベント・メニューの SetActive は許容。毎フレームだけ Warning |
| AS07 | Warning | all | Skip | 全部の音に当たる目印のルール。長い音だけを CT08 で見る |
| RP09 | Warning | code | Info | マテリアルの複製は数が少ない。Destroy しているかだけ見る |
| CT02 | Warning | code | Skip | 毎フレームだけのルール |
| CT03 | Warning | code | Skip | 毎フレームだけのルール |
| CT04 | Warning | code | Skip | 毎フレームだけのルール |
| CT05 | Critical | code | Info | 初期化・単発の判定なら許容 |
| CT07 | Critical | code | Skip | 毎フレームだけのルール |
