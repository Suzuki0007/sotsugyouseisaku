# PreToolUse (PowerShell|Bash): Git commands that change the repository need the user's OK.
# Project rule: the user commits and pushes with Fork. Read-only git (status/diff/log/show) passes.
# Keep this file ASCII only.
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$raw = [Console]::In.ReadToEnd()
try { $in = $raw | ConvertFrom-Json } catch { exit 0 }
$cmd = [string]$in.tool_input.command
if (-not $cmd) { exit 0 }
$verbs = 'commit|push|pull|fetch|checkout|switch|reset|stash|merge|rebase|cherry-pick|revert|restore|clean|tag|am|apply|branch\s+-[dDmM]|worktree|gc|update-ref|filter-branch'
$pattern = '(?i)git(\.exe)?["'']?\s+(-C\s+\S+\s+|-c\s+\S+\s+)*(' + $verbs + ')\b'
if ($cmd -notmatch $pattern) { exit 0 }
$reason = 'Git command that changes the repository (' + $Matches[3] + '). Project rule: the user commits/pushes with Fork; run it only if the user asked for it in chat, and only on ueeyama_home or Uh_School.'
$out = @{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; permissionDecision = 'ask'; permissionDecisionReason = $reason } }
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$out | ConvertTo-Json -Depth 5 -Compress
exit 0
