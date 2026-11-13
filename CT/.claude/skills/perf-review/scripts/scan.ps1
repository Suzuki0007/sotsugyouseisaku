# First-pass scanner of the perf-review skill (PowerShell port of the upstream scan.py, extended for CT).
# Runs every grep_pattern of the checklists against the targets, decides a context per hit line
# (hot = inside Update/LateUpdate/FixedUpdate/OnGUI/On*Stay*, code = other runtime code, tooling = Editor, asset),
# applies tuning-env.md (env tags) and rules-overrides.md (level / Skip per context) deterministically,
# and prints a Markdown report to be refined by reading the rule details (second pass).
# Usage:
#   scan.ps1                         changed files (git diff HEAD + untracked) under Assets/ and ProjectSettings/
#   scan.ps1 -All                    Assets/Script (code) + Assets (asset settings) + ProjectSettings
#   scan.ps1 -Path Assets/Script/Party,Assets/Script/Enemy
#   options: -Only US02,CT03 (rule ids) / -MaxLines 5 (per file) / -MaxFiles 8 (per rule) / -Summary (counts only) / -ShowTooling
# ASCII only (Windows PowerShell 5.1 reads BOM-less files as ANSI).
param(
    [string[]]$Path,
    [switch]$All,
    [string[]]$Only,
    [int]$MaxLines = 5,
    [int]$MaxFiles = 8,
    [switch]$Summary,
    [switch]$ShowTooling
)

[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
# powershell -File passes "A,B" as one string
if ($Only) { $Only = @($Only | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
if ($Path) { $Path = @($Path | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
Set-Location $root
$refDir = Join-Path $PSScriptRoot "..\references"
$utf8 = New-Object System.Text.UTF8Encoding($false)

$maxBytes = 2MB
$codeExt = @(".cs", ".shader", ".hlsl", ".cginc", ".compute")
$assetExt = @(".meta", ".prefab", ".unity", ".asset", ".mat", ".controller", ".overrideController", ".anim", ".spriteatlas", ".spriteatlasv2")
$audioExt = @(".wav", ".mp3", ".ogg", ".aif", ".aiff", ".flac")
$skipDirs = @("\Library\", "\Temp\", "\Logs\", "\obj\", "\.git\", "\Assets\TextMesh Pro\", "\Assets\__ClaudeTest\", "\Assets\_Recovery\","\Assets\Plugins\", "\Assets\Samples\", "\.claude\", "\PerfLog\", "\Packages\")
$hotMethods = "Update|LateUpdate|FixedUpdate|OnGUI|OnTriggerStay2D|OnTriggerStay|OnCollisionStay2D|OnCollisionStay|OnWillRenderObject|OnRenderObject"
$hotHeader = New-Object System.Text.RegularExpressions.Regex ("^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|override|virtual|new|static|sealed|unsafe)\s+)*void\s+(" + $hotMethods + ")\s*\(\s*\)")
$levelRank = @{ "Critical" = 0; "Warning" = 1; "Info" = 2 }

# Refinements: some upstream patterns are only "locators" whose detail says a second condition must hold.
# These checks are deterministic, so they are done here instead of by hand. (fileText, lineText) -> keep?
$refine = @{
    # Sprite textures with mipmaps on (textureType: 8 alone hits every sprite)
    "AS04" = { param($t, $l) $t -match '(?m)^\s*enableMipMap:\s*1\s*$' }
    # Reflection probe realtime AND every frame (m_Mode: 1 alone hits many unrelated components)
    "GR10" = { param($t, $l) ($t -match '(?m)^ReflectionProbe:') -and ($t -match '(?m)^\s*m_RefreshMode:\s*1\s*$') }
    # Transparent Image: only color fields (other "a: 0" values are unrelated)
    "UI06" = { param($t, $l) $l -match '^\s*m_Color:' }
}

#======================================================================
# checklists / env / overrides
#======================================================================

function Read-Checklist([string]$file, [string]$type) {
    $rows = @()
    foreach ($line in [System.IO.File]::ReadAllLines($file, $utf8)) {
        if (-not $line.StartsWith("| ")) { continue }
        $cells = [regex]::Split($line, '(?<!\\)\|') | ForEach-Object { $_.Trim() }
        $cells = $cells[1..($cells.Count - 2)]
        if ($cells.Count -ne 6 -or $cells[0] -eq "ID" -or $cells[0] -eq "" -or $cells[0].StartsWith("-")) { continue }
        $pat = $cells[3].Trim('`')
        $kind = "grep"
        if ($pat.StartsWith("MANUAL") -or $pat.StartsWith("grep ")) { $kind = "manual"; $pat = "" }
        elseif ($pat.StartsWith("ABSENT")) { $kind = "absent"; $pat = ($pat -replace '^ABSENT:?\s*', '').Trim('`') }
        elseif ($pat.StartsWith("SPECIAL:")) { $kind = "special"; $pat = $pat.Substring(8) }
        $pat = $pat.Replace('\|', '|')
        $level = $cells[1]
        if ($level -like "Warning+*") { $level = "Warning" }
        $rows += [pscustomobject]@{ id = $cells[0]; level = $level; cond = $cells[2].Replace('\|', '|'); pattern = $pat; name = $cells[4]; detail = $cells[5]; kind = $kind; type = $type; rx = $null }
    }
    return $rows
}

function Read-Env([string]$file) {
    $env = @{}
    if (-not (Test-Path $file)) { return $env }
    foreach ($line in [System.IO.File]::ReadAllLines($file, $utf8)) {
        $m = [regex]::Match($line, '^\|\s*([a-z_]+)\s*\|\s*([^|]+?)\s*\|')
        if ($m.Success) { $env[$m.Groups[1].Value] = $m.Groups[2].Value.Trim() }
    }
    return $env
}

# True = satisfied, False = clearly not, $null = unknown (keep the rule)
function Test-Tag([string]$tag, $envp) {
    $tag = $tag.Trim()
    if ($tag -eq "" -or $tag -eq "-") { return $true }
    $m = [regex]::Match($tag, '^unity>=([\d.]+)')
    if ($m.Success) {
        $v = $envp["unity_version"]
        if (-not $v -or $v -notmatch '\d') { return $null }
        $a = @([regex]::Matches($v, '\d+') | Select-Object -First 3 | ForEach-Object { [int]$_.Value })
        $b = @([regex]::Matches($m.Groups[1].Value, '\d+') | Select-Object -First 3 | ForEach-Object { [int]$_.Value })
        for ($i = 0; $i -lt 3; $i++) {
            $x = if ($i -lt $a.Count) { $a[$i] } else { 0 }
            $y = if ($i -lt $b.Count) { $b[$i] } else { 0 }
            if ($x -ne $y) { return ($x -gt $y) }
        }
        return $true
    }
    $known = { param($k) $v = $envp[$k]; if (-not $v -or $v -eq "unknown") { $null } else { $v } }
    if ($tag -in @("urp", "hdrp", "builtin")) { $v = & $known "render_pipeline"; if ($null -eq $v) { return $null }; return ($v -eq $tag) }
    if ($tag -eq "srp") { $v = & $known "render_pipeline"; if ($null -eq $v) { return $null }; return ($v -in @("urp", "hdrp")) }
    if ($tag -in @("il2cpp", "mono")) { $v = & $known "scripting_backend"; if ($null -eq $v) { return $null }; return ($v -eq $tag) }
    if ($tag -in @("ios", "android")) { $v = & $known "platforms"; if ($null -eq $v) { return $null }; return ($tag -in ($v.ToLower() -split '[,\s/]+')) }
    if ($tag -in @("addressables", "vat")) { $v = & $known $tag; if ($null -eq $v) { return $null }; return ($v.ToLower() -notin @("false", "no", "none", "0")) }
    return $null
}

function Test-Applicable([string]$cond, $envp) {
    foreach ($group in $cond.Split(",")) {
        $res = @($group.Split("|") | ForEach-Object { Test-Tag $_ $envp })
        $allFalse = $true
        foreach ($r in $res) { if ($r -ne $false) { $allFalse = $false } }
        if ($allFalse) { return $false }
    }
    return $true
}

function Read-Overrides([string]$file) {
    $map = @{}
    if (-not (Test-Path $file)) { return $map }
    foreach ($line in [System.IO.File]::ReadAllLines($file, $utf8)) {
        if (-not $line.StartsWith("| ")) { continue }
        $c = $line.Split("|") | ForEach-Object { $_.Trim() }
        if ($c.Count -lt 6 -or $c[1] -eq "" -or $c[1] -like "*ID*" -or $c[1].StartsWith("-")) { continue }
        $ctx = switch ($c[3]) { "in-game" { "hot" } "init" { "code" } default { $c[3] } }
        $map[$c[1] + "/" + $ctx] = $c[4]
    }
    return $map
}

# Level after overrides: (id, ctx) -> (id, all) -> (*, ctx) -> base
function Resolve-Level([string]$id, [string]$ctx, [string]$base) {
    foreach ($k in @("$id/$ctx", "$id/all", "*/$ctx")) { if ($overrides.ContainsKey($k)) { return $overrides[$k] } }
    return $base
}

#======================================================================
# targets
#======================================================================

function Test-Skipped([string]$full) {
    $p = "\" + $full.Substring($root.Length).TrimStart("\") + "\"
    foreach ($d in $skipDirs) { if ($p.IndexOf($d, [StringComparison]::OrdinalIgnoreCase) -ge 0) { return $true } }
    return $false
}

function Get-Git {
    Get-ChildItem (Join-Path $env:LOCALAPPDATA "Fork\gitInstance") -Directory -ErrorAction SilentlyContinue |
        Sort-Object { try { [version]$_.Name } catch { [version]"0.0" } } -Descending |
        ForEach-Object { Join-Path $_.FullName "cmd\git.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1
}

$mode = "changed"
$paths = @()
if ($All) {
    $mode = "all"
    $paths = @("Assets\Script")
    $paths += @(Get-ChildItem Assets -Recurse -File -Include *.meta, *.prefab, *.unity, *.asset, *.mat, *.spriteatlas, *.spriteatlasv2 -ErrorAction SilentlyContinue | Where-Object {
        $_.Extension -ne ".meta" -or $_.Name -match '\.(png|psd|psb|jpg|jpeg|tga|wav|mp3|ogg|aif|aiff|fbx|prefab)\.meta$' } | ForEach-Object { $_.FullName })
    $paths += @(Get-ChildItem ProjectSettings -File -Filter *.asset | ForEach-Object { $_.FullName })
} elseif ($Path) {
    $mode = "path"
    $paths = $Path
} else {
    $git = Get-Git
    if (-not $git) { Write-Output "[ERROR] git not found: use -All or -Path"; exit 2 }
    $paths = @(& $git -c core.quotepath=false diff --name-only --relative HEAD -- Assets ProjectSettings 2>$null)
    $paths += @(& $git -c core.quotepath=false ls-files --others --exclude-standard -- Assets ProjectSettings 2>$null)
    if ($paths.Count -eq 0) { Write-Output "no changed files under Assets/ or ProjectSettings/ (use -All or -Path)"; exit 0 }
}

$files = New-Object System.Collections.Generic.List[string]
$seenFiles = @{}
foreach ($p in $paths) {
    if (-not $p) { continue }
    $full = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $root $p }
    if (Test-Path $full -PathType Container) {
        Get-ChildItem $full -Recurse -File | ForEach-Object { if (-not $seenFiles.ContainsKey($_.FullName)) { $seenFiles[$_.FullName] = 1; $files.Add($_.FullName) } }
    } elseif (Test-Path $full -PathType Leaf) {
        $fp = (Resolve-Path $full).Path
        if (-not $seenFiles.ContainsKey($fp)) { $seenFiles[$fp] = 1; $files.Add($fp) }
    }
}

$codeFiles = @{}; $assetFiles = @{}; $audioMetas = @()
foreach ($f in $files) {
    if (Test-Skipped $f) { continue }
    $ext = [System.IO.Path]::GetExtension($f).ToLower()
    $isCode = $ext -in $codeExt
    $isAsset = ($ext -in $assetExt) -or ($f -like "*\ProjectSettings\*.asset")
    if (-not $isCode -and -not $isAsset) { continue }
    $fi = Get-Item $f
    if ($fi.Length -gt $maxBytes) { continue }
    $text = [System.IO.File]::ReadAllText($f, $utf8)
    if ($text.IndexOf([char]0) -ge 0) { continue }
    if ($ext -eq ".meta") {
        $baseExt = [System.IO.Path]::GetExtension($f.Substring(0, $f.Length - 5)).ToLower()
        if ($baseExt -in $audioExt) { $audioMetas += $f }
    }
    if ($isCode) { $codeFiles[$f] = $text } else { $assetFiles[$f] = $text }
}

#======================================================================
# per-line context of code files
#======================================================================

function Get-LineStarts([string]$text) {
    $starts = New-Object System.Collections.Generic.List[int]
    $starts.Add(0)
    for ($i = 0; $i -lt $text.Length; $i++) { if ($text[$i] -eq "`n") { $starts.Add($i + 1) } }
    return , $starts.ToArray()
}

function Get-LineOf([int[]]$starts, [int]$index) {
    $lo = 0; $hi = $starts.Length - 1
    while ($lo -lt $hi) { $mid = [int](($lo + $hi + 1) / 2); if ($starts[$mid] -le $index) { $lo = $mid } else { $hi = $mid - 1 } }
    return $lo
}

# Returns array: per line index -> "hot:<Method>" / "code" / "tooling"
function Get-Contexts([string]$file, [string]$text) {
    $lines = $text -split "`n"
    $ctx = New-Object string[] $lines.Count
    $tooling = $file -match '\\Editor\\'
    if (-not $tooling) {
        foreach ($l in $lines) {
            $t = $l.Trim()
            if ($t -eq "" -or $t.StartsWith("//") -or $t.StartsWith("/*") -or $t.StartsWith("*") -or $t.StartsWith("using ")) { continue }
            if ($t -match '^#if\s+UNITY_EDITOR\b') { $tooling = $true }
            break
        }
    }
    if ($tooling) { for ($i = 0; $i -lt $lines.Count; $i++) { $ctx[$i] = "tooling" }; return , $ctx }
    $inHot = $false; $depth = 0; $started = $false; $name = ""
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $l = $lines[$i]
        if (-not $inHot) {
            $m = $hotHeader.Match($l)
            if ($m.Success) { $inHot = $true; $depth = 0; $started = $false; $name = $m.Groups[1].Value }
        }
        if ($inHot) {
            $ctx[$i] = "hot:" + $name
            $s = [regex]::Replace($l, '//.*$', '')
            $s = [regex]::Replace($s, '@?"(?:[^"\\]|\\.)*"', '""')
            $s = [regex]::Replace($s, "'(?:[^'\\]|\\.)'", "''")
            foreach ($ch in $s.ToCharArray()) {
                if ($ch -eq '{') { $depth++; $started = $true }
                elseif ($ch -eq '}') { $depth-- }
            }
            if ($started -and $depth -le 0) { $inHot = $false }
            # expression-bodied: void Update() => Foo();
            if (-not $started -and $s -match '=>' -and $s.TrimEnd().EndsWith(";")) { $inHot = $false }
        } else {
            $ctx[$i] = "code"
        }
    }
    return , $ctx
}

#======================================================================
# scan
#======================================================================

$envp = Read-Env (Join-Path $refDir "tuning-env.md")
$overrides = Read-Overrides (Join-Path $refDir "rules-overrides.md")
$rules = @()
$rules += Read-Checklist (Join-Path $refDir "upstream\checklist-code.md") "code"
$rules += Read-Checklist (Join-Path $refDir "checklist-project-code.md") "code"
$rules += Read-Checklist (Join-Path $refDir "upstream\checklist-asset.md") "asset"
$rules += Read-Checklist (Join-Path $refDir "checklist-project-asset.md") "asset"
if ($Only) { $rules = @($rules | Where-Object { $_.id -in $Only }) }

$contexts = @{}; $lineStarts = @{}; $lineTexts = @{}
foreach ($f in $codeFiles.Keys) {
    $contexts[$f] = Get-Contexts $f $codeFiles[$f]
    $lineStarts[$f] = Get-LineStarts $codeFiles[$f]
    $lineTexts[$f] = $codeFiles[$f] -split "`n"
}
foreach ($f in $assetFiles.Keys) { $lineStarts[$f] = Get-LineStarts $assetFiles[$f] }

$hits = New-Object System.Collections.Generic.List[object]   # {rule, file, line, ctx, method, level}
$envSkipped = @(); $manuals = @(); $absents = @(); $rxErrors = @()
$scanned = 0; $overrideSkipped = 0; $refinedOut = 0

foreach ($r in $rules) {
    if (-not (Test-Applicable $r.cond $envp)) { $envSkipped += $r; continue }
    if ($r.kind -eq "manual") { $manuals += $r; continue }
    $targets = if ($r.type -eq "code") { $codeFiles } else { $assetFiles }
    $scanned++

    if ($r.kind -eq "special") {
        if ($r.pattern -eq "audio-large-decompress") {
            foreach ($meta in $audioMetas) {
                $clip = $meta.Substring(0, $meta.Length - 5)
                if (-not (Test-Path $clip)) { continue }
                $size = (Get-Item $clip).Length
                $mt = $assetFiles[$meta]; if (-not $mt) { $mt = [System.IO.File]::ReadAllText($meta, $utf8) }
                $lm = [regex]::Match($mt, '(?m)^\s*loadType:\s*0\s*$')
                if ($size -gt 1MB -and $lm.Success) {
                    $ln = [regex]::Matches($mt.Substring(0, $lm.Index), "`n").Count
                    $lvl = Resolve-Level $r.id "asset" $r.level
                    if ($lvl -eq "Skip") { $overrideSkipped++; continue }
                    $hits.Add([pscustomobject]@{ rule = $r; file = $meta; line = $ln + 1; ctx = "asset"; method = ("{0:N1}MB" -f ($size / 1MB)); level = $lvl })
                }
            }
        }
        continue
    }

    try { $r.rx = New-Object System.Text.RegularExpressions.Regex ($r.pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline) }
    catch { $rxErrors += $r; continue }

    $anyFile = $false
    foreach ($f in $targets.Keys) {
        $ms = $r.rx.Matches($targets[$f])
        if ($ms.Count -eq 0) { continue }
        $anyFile = $true
        if ($r.kind -eq "absent") { break }
        $seenLines = @{}
        foreach ($m in $ms) {
            $ln = Get-LineOf $lineStarts[$f] $m.Index
            if ($seenLines.ContainsKey($ln)) { continue }
            $seenLines[$ln] = 1
            if ($refine.ContainsKey($r.id)) {
                $st = $lineStarts[$f][$ln]
                $en = if ($ln + 1 -lt $lineStarts[$f].Length) { $lineStarts[$f][$ln + 1] } else { $targets[$f].Length }
                $lt = $targets[$f].Substring($st, $en - $st)
                if (-not (& $refine[$r.id] $targets[$f] $lt)) { $refinedOut++; continue }
            }
            $ctx = "asset"; $method = ""
            if ($r.type -eq "code") {
                $c = $contexts[$f][$ln]
                if ($c -like "hot:*") { $ctx = "hot"; $method = $c.Substring(4) } else { $ctx = $c }
            }
            $lvl = Resolve-Level $r.id $ctx $r.level
            if ($lvl -eq "Skip") { $overrideSkipped++; continue }
            $hits.Add([pscustomobject]@{ rule = $r; file = $f; line = $ln + 1; ctx = $ctx; method = $method; level = $lvl })
        }
    }
    if ($r.kind -eq "absent" -and -not $anyFile) { $absents += $r }
}

#======================================================================
# report
#======================================================================

function Rel([string]$f) { return $f.Substring($root.Length).TrimStart("\").Replace("\", "/") }

function Write-Section([string]$title, [string]$ctx, [bool]$showText) {
    $sel = @($hits | Where-Object { $_.ctx -eq $ctx })
    Write-Output ""
    Write-Output "## $title ($($sel.Count) lines)"
    if ($sel.Count -eq 0) { Write-Output "none"; return }
    $groups = $sel | Group-Object { $_.rule.id + "|" + $_.level } | Sort-Object { $levelRank[$_.Group[0].level] }, Name
    foreach ($g in $groups) {
        $r = $g.Group[0].rule; $lvl = $g.Group[0].level
        $tag = if ($lvl -ne $r.level) { "$($r.level)->$lvl" } else { $lvl }
        $fileCount = @($g.Group | Select-Object -ExpandProperty file -Unique).Count
        Write-Output ("### {0} [{1}] {2} ({3}) - {4} lines / {5} files" -f $r.id, $tag, $r.name, $r.detail, $g.Count, $fileCount)
        if ($Summary) { continue }
        $fgroups = @($g.Group | Group-Object file | Sort-Object Name)
        $shownFiles = 0
        foreach ($fg in $fgroups) {
            if ($shownFiles -ge $MaxFiles) { Write-Output ("- ... {0} more files (use -Only {1} -MaxFiles 999)" -f ($fgroups.Count - $shownFiles), $r.id); break }
            $shownFiles++
            $items = @($fg.Group | Sort-Object line)
            if ($showText) {
                $n = 0
                foreach ($h in $items) {
                    if ($n -ge $MaxLines) { Write-Output ("  - ... {0} more in {1}" -f ($items.Count - $n), (Rel $fg.Name)); break }
                    $src = ""
                    if ($lineTexts.ContainsKey($h.file)) { $src = $lineTexts[$h.file][$h.line - 1].Trim(); if ($src.Length -gt 110) { $src = $src.Substring(0, 110) + "..." } }
                    $where = if ($h.method) { " [" + $h.method + "]" } else { "" }
                    Write-Output ("- {0}:{1}{2}  ``{3}``" -f (Rel $h.file), $h.line, $where, $src)
                    $n++
                }
            } else {
                $ls = ($items | Select-Object -First $MaxLines | ForEach-Object { if ($_.method -and $_.ctx -eq "asset") { "L$($_.line) ($($_.method))" } else { "L$($_.line)" } }) -join ", "
                $more = if ($items.Count -gt $MaxLines) { " (+$($items.Count - $MaxLines))" } else { "" }
                Write-Output ("- {0}: {1}{2}" -f (Rel $fg.Name), $ls, $more)
            }
        }
    }
}

Write-Output "# perf-review first-pass scan (scan.ps1)"
Write-Output ""
Write-Output ("mode: {0} | targets: {1} code files, {2} asset files | env: unity {3}, {4}, {5}, {6}" -f $mode, $codeFiles.Count, $assetFiles.Count, $envp["unity_version"], $envp["render_pipeline"], $envp["scripting_backend"], $envp["platforms"])
Write-Output "Second pass: read the rule detail file for each hit and drop false positives (see SKILL.md)."

Write-Section "1. per-frame code (hot: Update / LateUpdate / FixedUpdate / OnGUI / On*Stay*)" "hot" $true
Write-Section "2. other runtime code (code: Awake / Start / events / helpers; levels after rules-overrides.md)" "code" $false
Write-Section "3. assets and settings" "asset" $false
if ($ShowTooling) { Write-Section "4. tooling (Editor)" "tooling" $false }
else { $tc = @($hits | Where-Object { $_.ctx -eq "tooling" }).Count; Write-Output ""; Write-Output "## 4. tooling (Editor): $tc lines (use -ShowTooling)" }

Write-Output ""
Write-Output "## ABSENT (pattern not found anywhere in the targets)"
if ($mode -ne "all") { Write-Output "not evaluated: only meaningful with -All (a partial scan always looks absent)" }
elseif ($absents.Count -eq 0) { Write-Output "none" }
else { $absents | ForEach-Object { Write-Output ("- {0} [{1}] {2} ({3})" -f $_.id, $_.level, $_.name, $_.detail) } }

Write-Output ""
Write-Output "## MANUAL (not greppable: read the detail when the target kind applies)"
if ($manuals.Count -eq 0) { Write-Output "none" } else { $manuals | ForEach-Object { Write-Output ("- {0} {1} ({2})" -f $_.id, $_.name, $_.detail) } }

Write-Output ""
Write-Output "## Coverage (machine generated)"
$hitRules = @($hits | ForEach-Object { $_.rule.id } | Select-Object -Unique).Count
Write-Output ("- checklist rows: {0} (code {1} / asset {2})" -f $rules.Count, @($rules | Where-Object { $_.type -eq "code" }).Count, @($rules | Where-Object { $_.type -eq "asset" }).Count)
Write-Output ("- skipped by tuning-env.md: {0} ({1})" -f $envSkipped.Count, (($envSkipped | ForEach-Object { $_.id } | Select-Object -Unique) -join ", "))
Write-Output ("- grep run: {0} / rules with hits: {1} / hit lines: {2} (hot {3}, code {4}, asset {5}, tooling {6}) / dropped by overrides (Skip): {7}" -f $scanned, $hitRules, $hits.Count,
    @($hits | Where-Object { $_.ctx -eq "hot" }).Count, @($hits | Where-Object { $_.ctx -eq "code" }).Count, @($hits | Where-Object { $_.ctx -eq "asset" }).Count, @($hits | Where-Object { $_.ctx -eq "tooling" }).Count, $overrideSkipped)
Write-Output ("- dropped by built-in refinements (AS04 / GR10 / UI06 second condition): {0}" -f $refinedOut)
Write-Output ("- ABSENT suspected: {0} / MANUAL: {1}" -f $absents.Count, $manuals.Count)
if ($rxErrors.Count -gt 0) { Write-Output ("- regex errors: " + (($rxErrors | ForEach-Object { $_.id }) -join ", ")) }
