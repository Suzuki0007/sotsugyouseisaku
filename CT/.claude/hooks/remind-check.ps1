# PostToolUse (Write|Edit): after editing runtime/editor C# under Assets/, remind to run unity-check.
# Also warns when the file was written by another team member (\author line).
# Keep this file ASCII only (Japanese author names are matched by "not uh").
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$raw = [Console]::In.ReadToEnd()
try { $in = $raw | ConvertFrom-Json } catch { exit 0 }
$path = [string]$in.tool_input.file_path
if (-not $path) { exit 0 }
$norm = $path.Replace('\', '/')
if ($norm -notmatch '/Assets/.+\.cs$') { exit 0 }
$msg = 'C# under Assets/ changed: run the unity-check skill (recompile + console) before reporting.'
try {
  $head = Get-Content -LiteralPath $path -TotalCount 12 -Encoding UTF8 -ErrorAction Stop
  $line = $head | Where-Object { $_ -match '\\author' } | Select-Object -First 1
  # The user's own files leave \author empty; any other name means another member.
  $name = ''
  if ($line) { $name = ($line -replace '^.*\\author', '').Trim() }
  if ($name -and $name -notmatch '^uh\b') {
    $msg += ' This file has an \author line of another team member: do not change it silently; explain the change in the commit message (see rules/team-code.md).'
  }
} catch { }
$out = @{ hookSpecificOutput = @{ hookEventName = 'PostToolUse'; additionalContext = $msg } }
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$out | ConvertTo-Json -Depth 5 -Compress
exit 0
