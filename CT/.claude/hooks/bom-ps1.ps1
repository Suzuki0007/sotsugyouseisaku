# PostToolUse (Write|Edit): give .ps1 files that contain non-ASCII text (e.g. Japanese) a UTF-8 BOM.
# Windows PowerShell 5.1 reads BOM-less .ps1 as ANSI (Japanese breaks the parser), but reads UTF-8 with BOM correctly.
# Replaces the old guard-ps1 (which refused Japanese in .ps1). Keep this file ASCII only.
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$raw = [Console]::In.ReadToEnd()
try { $in = $raw | ConvertFrom-Json } catch { exit 0 }
$path = [string]$in.tool_input.file_path
if (-not $path.ToLower().EndsWith('.ps1')) { exit 0 }
if (-not (Test-Path -LiteralPath $path)) { exit 0 }
$bytes = [System.IO.File]::ReadAllBytes($path)
if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { exit 0 }
$nonAscii = $false
foreach ($b in $bytes) { if ($b -gt 127) { $nonAscii = $true; break } }
if (-not $nonAscii) { exit 0 }
$text = [System.Text.Encoding]::UTF8.GetString($bytes)
[System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($true)))
$out = @{ hookSpecificOutput = @{ hookEventName = 'PostToolUse'; additionalContext = ('Added a UTF-8 BOM to ' + $path + ' (it contains non-ASCII text; PowerShell 5.1 needs the BOM).') } }
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$out | ConvertTo-Json -Depth 5 -Compress
exit 0
