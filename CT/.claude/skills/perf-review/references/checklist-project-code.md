# レビューチェックリスト: CT 独自（コード）

> rules-project.md から手で作る（直したら両方そろえる）。形式は上流のチェックリストと同じ。

| ID | レベル | 適用条件 | grep_pattern | ルール名 | 詳細 |
|----|--------|----------|--------------|----------|------|
| CT01 | Info | - | `GetComponent(InChildren\|InParent)?<[^>]+>\(\)\s*(!=\|==\|\?\.\|\?\?)\|GetComponent(InChildren\|InParent)?<[^>]+>\(\)\s*is\b` | 有るかわからないコンポーネントを GetComponent で調べている | rules-project.md |
| CT02 | Warning | - | `\.text\s*=[^=]` | 毎フレームの処理で TextMeshPro の text に代入している | rules-project.md |
| CT03 | Warning | - | `\bnew\s+(List\|Dictionary\|HashSet\|Queue\|Stack)<\|\bnew\s+[A-Za-z_][\w.]*\s*\[\s*[\w.]*\s*\]` | 毎フレームの処理で List・配列・Dictionary を new している | rules-project.md |
| CT04 | Warning | - | `\.name\b\|GetType\(\)\.Name` | 毎フレームの処理で name・GetType().Name を読んでいる | rules-project.md |
| CT05 | Critical | - | `Physics2D\.(OverlapCircleAll\|OverlapBoxAll\|OverlapAreaAll\|OverlapPointAll\|OverlapCapsuleAll\|RaycastAll\|CircleCastAll\|BoxCastAll\|CapsuleCastAll\|LinecastAll)\s*\(` | 2D の物理の判定で、配列を作る版を使っている | rules-project.md |
| CT06 | Info | - | `Physics2D\.(Raycast\|OverlapCircle\|OverlapBox\|OverlapPoint\|OverlapArea\|CircleCast\|BoxCast\|Linecast)\s*\(` | 2D の物理の判定にレイヤーの指定がない | rules-project.md |
| CT07 | Critical | - | `Resources\.Load(All)?\s*[<(]` | 毎フレームの処理で Resources.Load している | rules-project.md |
