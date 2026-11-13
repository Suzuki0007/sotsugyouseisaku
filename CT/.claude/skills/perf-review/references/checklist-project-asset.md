# レビューチェックリスト: CT 独自（アセット・設定）

> rules-project.md から手で作る（直したら両方そろえる）。`SPECIAL:` は scan.ps1 の中の専用の確認。

| ID | レベル | 適用条件 | grep_pattern | ルール名 | 詳細 |
|----|--------|----------|--------------|----------|------|
| CT08 | Warning | - | `SPECIAL:audio-large-decompress` | 長い音（BGM など）を「読み込み時に展開」している | rules-project.md |
