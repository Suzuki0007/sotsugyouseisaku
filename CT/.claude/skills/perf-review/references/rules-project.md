# CT（このプロジェクト）独自のルール

> 上流のルール集（`upstream/`）にない、2D・PC のこの作品で効くもの。書き方は上流と同じ。
> 一次点検の表は `checklist-project-code.md`・`checklist-project-asset.md`（**ここを直したら表も手で直す**。上流の gen-checklists.py は Python が要るので使わない）。
> 「毎フレーム」の判定は scan.ps1 がする（Update・LateUpdate・FixedUpdate・OnGUI・OnTriggerStay* などの中の行を hot とする）。

## ルール一覧

### CT01 有るかわからないコンポーネントを GetComponent で調べている
- **レベル**: Info
- **対象**: コードレビュー
- **検出パターン**: `GetComponent<T>()` の結果を null と比べている・`?.` で使っている（見つからないことがある使い方）
- **grep_pattern**: `GetComponent(InChildren|InParent)?<[^>]+>\(\)\s*(!=|==|\?\.|\?\?)|GetComponent(InChildren|InParent)?<[^>]+>\(\)\s*is\b`
- **理由**: 見つからないとき、エディタでは毎回エラーメッセージの文字列を作る（11-13 のティンパニ戦の計測で `GetComponentNullErrorMessage` が 0.16〜0.52KB/フレーム、ゲームの確保でいちばん多かった）。ビルドでは作らないが、エディタの計測が乱れる
- **対処**: `TryGetComponent(out T c)` にする。毎フレーム呼ぶなら Awake でキャッシュ（US02）

### CT02 毎フレームの処理で TextMeshPro の text に代入している
- **レベル**: Warning
- **対象**: コードレビュー（hot のときだけ指摘）
- **検出パターン**: Update 系の中の `.text = `（値が変わったときだけ代入しているなら非該当）
- **grep_pattern**: `\.text\s*=[^=]`
- **理由**: 文字列を毎回作り、TMP のメッシュも作り直す（バイブル 8.6）
- **対処**: 変わったときだけ代入する。数字は `SetText("{0}", n)` か StringBuilder を `SetText(sb)`

### CT03 毎フレームの処理で List・配列・Dictionary を new している
- **レベル**: Warning
- **対象**: コードレビュー（hot のときだけ指摘）
- **検出パターン**: Update 系の中の `new List<`・`new T[n]`・`new Dictionary<` など
- **grep_pattern**: `\bnew\s+(List|Dictionary|HashSet|Queue|Stack)<|\bnew\s+[A-Za-z_][\w.]*\s*\[\s*[\w.]*\s*\]`
- **理由**: 毎フレームの確保は GC の止まりの元（11-13 の計測で PartyHintView.LateUpdate の `new List` が該当）
- **対処**: フィールドに持って `Clear()` で使い回す

### CT04 毎フレームの処理で name・GetType().Name を読んでいる
- **レベル**: Warning
- **対象**: コードレビュー（hot のときだけ指摘）
- **検出パターン**: Update 系の中の `.name`・`GetType().Name`（比べる・辞書を引くなど）
- **grep_pattern**: `\.name\b|GetType\(\)\.Name`
- **理由**: どちらも呼ぶたびに文字列を作る（バイブル 9.2）
- **対処**: Awake で読んでおく。型で分けるなら `is` / switch の型パターン

### CT05 2D の物理の判定で、配列を作る版を使っている
- **レベル**: Critical（hot）／ Info（それ以外）
- **対象**: コードレビュー
- **検出パターン**: `Physics2D.OverlapCircleAll` など `...All(` の版（上流の PH05 は 3D の Physics だけ）
- **grep_pattern**: `Physics2D\.(OverlapCircleAll|OverlapBoxAll|OverlapAreaAll|OverlapPointAll|OverlapCapsuleAll|RaycastAll|CircleCastAll|BoxCastAll|CapsuleCastAll|LinecastAll)\s*\(`
- **理由**: 呼ぶたびに結果の配列を作る
- **対処**: 結果の List（または配列）を渡す版（`Physics2D.OverlapCircle(pos, r, filter, results)`）でフィールドを使い回す

### CT06 2D の物理の判定にレイヤーの指定がない
- **レベル**: Info
- **対象**: コードレビュー
- **検出パターン**: `Physics2D.Raycast(` などで、レイヤーマスク・ContactFilter2D の引数がない
- **grep_pattern**: `Physics2D\.(Raycast|OverlapCircle|OverlapBox|OverlapPoint|OverlapArea|CircleCast|BoxCast|Linecast)\s*\(`
- **理由**: すべてのコライダーと判定するので、物が増えると重くなり、余計な物にも当たる（上流の PH06 の 2D 版）
- **対処**: 当たるレイヤーだけのマスクか ContactFilter2D を渡す

### CT07 毎フレームの処理で Resources.Load している
- **レベル**: Critical
- **対象**: コードレビュー（hot のときだけ指摘）
- **grep_pattern**: `Resources\.Load(All)?\s*[<(]`
- **理由**: ファイルを探して読む重い処理
- **対処**: Awake・初期化で読んでおく

### CT08 長い音（BGM など）を「読み込み時に展開」している
- **レベル**: Warning
- **対象**: アセット設定（音の .meta）
- **検出パターン**: 1MB を超える音のファイルで、Load Type が Decompress On Load（`loadType: 0`）
- **grep_pattern**: SPECIAL:audio-large-decompress（scan.ps1 が音のファイルの大きさと .meta を突き合わせる）
- **理由**: 展開した PCM がまるごとメモリに乗る（3 分のステレオ 44.1kHz で約 30MB）。上流の AS07 は全部の音にヒットするので、長い音だけに絞った
- **対処**: BGM は Streaming、長めの環境音は Compressed In Memory。短い効果音は Decompress On Load のままでよい
