# PreToolUse (Write|Edit): refuse non-ASCII text in .ps1 files.
# Windows PowerShell 5.1 reads BOM-less .ps1 as ANSI, so Japanese text breaks the parser.
# Keep this file ASCII only.
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$raw = [Console]::In.ReadToEnd()
try { $in = $raw | ConvertFrom-Json } catch { exit 0 }
$path = [string]$in.tool_input.file_path
if (-not $path.ToLower().EndsWith('.ps1')) { exit 0 }
$text = ''
if ($in.tool_input.content) { $text += [string]$in.tool_input.content }
if ($in.tool_input.new_string) { $text += [string]$in.tool_input.new_string }
$bad = $false
foreach ($ch in $text.ToCharArray()) { if ([int]$ch -gt 127) { $bad = $true; break } }
if (-not $bad) { exit 0 }
$reason = 'This .ps1 contains non-ASCII characters (e.g. Japanese). Windows PowerShell 5.1 reads BOM-less .ps1 as ANSI and the script breaks. Write comments and strings in ASCII; pass Japanese via a separate UTF-8 file (.cs / .txt / .tsv) instead. See CLAUDE.md (environment).'
$out = @{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; permissionDecision = 'deny'; permissionDecisionReason = $reason } }
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$out | ConvertTo-Json -Depth 5 -Compress
exit 0
