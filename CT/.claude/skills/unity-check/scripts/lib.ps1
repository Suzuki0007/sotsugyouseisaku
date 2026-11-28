# Shared helpers for the CT project's Unity CLI skills.
# ASCII only (Windows PowerShell 5.1 reads BOM-less files as ANSI).

# Unity CLI: the installer puts it in %LOCALAPPDATA%\Unity\bin on every PC (home: uhiro, school: amg).
# UNITY_CLI_HOME (installer option) wins; otherwise fall back to whatever "unity" is already on PATH.
$Script:UnityBin = $null
foreach ($d in @($(if ($env:UNITY_CLI_HOME) { Join-Path $env:UNITY_CLI_HOME "bin" }), (Join-Path $env:LOCALAPPDATA "Unity\bin"))) {
    if ($d -and (Test-Path (Join-Path $d "unity.exe"))) { $Script:UnityBin = $d; break }
}
if ($Script:UnityBin) { $env:Path = $Script:UnityBin + ";" + $env:Path }
elseif (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    Write-Output "[ERROR] Unity CLI not found. Install it with: irm https://unity.com/install.ps1 | iex"
}
$env:UNITY_NO_BANNER = "1"
$env:UNITY_NO_PAGER = "1"

# Project root = three levels above .claude/skills/<skill>/scripts
$Script:CtRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
Set-Location $Script:CtRoot

# True when an Editor (not the CLI, not an import worker) has this project open.
function Test-UnityEditorRunning {
    $want = ($Script:CtRoot -replace "/", "\").TrimEnd("\").ToLowerInvariant()
    foreach ($p in @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue)) {
        $c = "$($p.CommandLine)"
        if ($c -match "-batchMode" -or $c -match "AssetImportWorker") { continue }
        if ("$($p.ExecutablePath)" -match "\\Unity\\bin\\") { continue }
        if (($c -replace "/", "\").ToLowerInvariant().Contains($want)) { return $true }
    }
    return $false
}

function Wait-UnityReady {
    param([int]$Timeout = 300)
    # An Editor that is closed never becomes ready: say so at once instead of waiting the whole timeout (2026-11-28).
    if (-not (Test-UnityEditorRunning)) {
        Write-Host "[ERROR] Unity Editor is not running for this project. Ask the user, then: unity open ."
        return $false
    }
    # Right after a recompile (domain reload) "status --until-ready" can fail at once instead of waiting: try again
    # until the timeout while the Editor process is alive. (Hidden until 2026-11-28: the old error path returned an
    # array that was always true, so nothing ever stopped here.)
    $deadline = (Get-Date).AddSeconds($Timeout)
    while ($true) {
        $left = [int][math]::Max(5, ($deadline - (Get-Date)).TotalSeconds)
        $out = unity status --until-ready --timeout $left 2>$null
        if ($LASTEXITCODE -eq 0) { return $true }
        if ((Get-Date) -ge $deadline) { break }
        if (-not (Test-UnityEditorRunning)) { Write-Host "[ERROR] Unity Editor closed while waiting."; return $false }
        Start-Sleep -Seconds 2
    }
    Write-Host "[ERROR] Unity Editor is not ready after $Timeout s (a dialog open? still importing?)."
    return $false
}

# Prints console entries as one short line each (no stack traces).
# Level: log | warn | error. Returns nothing; prints counts first.
function Show-UnityConsole {
    param([string]$Level = "warn", [int]$Tail = 30, [int]$Width = 240)
    $raw = unity command console get --level $Level --tail $Tail --result-only 2>$null
    try { $json = ($raw -join "`n") | ConvertFrom-Json } catch {
        Write-Output "[ERROR] console get failed"; Write-Output ($raw -join "`n"); return
    }
    $c = $json.counts
    if ($null -ne $c) { Write-Output ("counts: error {0} / warn {1} / log {2}" -f $c.error, $c.warn, $c.log) }
    foreach ($e in $json.entries) {
        $msg = ($e.message -split "`r?`n")[0]
        if ($msg.Length -gt $Width) { $msg = $msg.Substring(0, $Width) + "..." }
        Write-Output ("[{0}] #{1} {2}" -f $e.level, $e.seq, $msg)
    }
}

# TMP font assets (dynamic atlas) in Assets/Fonts are rewritten by the Editor when new characters are drawn
# (play mode, or a recipe that places Japanese text and saves). Back them up before and put them back after.
function Backup-FontAssets {
    param([string]$Dir)
    New-Item -ItemType Directory -Force $Dir | Out-Null
    $fonts = @(Get-ChildItem -Path (Join-Path $Script:CtRoot "Assets\Fonts") -Filter "*.asset" -ErrorAction SilentlyContinue)
    foreach ($f in $fonts) { Copy-Item $f.FullName $Dir -Force }
    return $fonts.Count
}

# Returns one line: "unchanged" or the restored names. Also reloads fonts that are only changed in memory (dirty).
function Restore-FontAssets {
    param([string]$Dir)
    if (-not $Dir -or -not (Test-Path $Dir)) { return "no backup" }
    $fontDir = Join-Path $Script:CtRoot "Assets\Fonts"
    $put = @()
    foreach ($b in @(Get-ChildItem -Path $Dir -Filter "*.asset")) {
        $cur = Join-Path $fontDir $b.Name
        if (-not (Test-Path $cur)) { continue }
        if ((Get-FileHash $cur).Hash -ne (Get-FileHash $b.FullName).Hash) { Copy-Item $b.FullName $cur -Force; $put += ("Assets/Fonts/" + $b.Name) }
    }
    $list = ($put | ForEach-Object { '"' + $_ + '"' }) -join ","
    $code = @"
var put = new System.Collections.Generic.List<string>(new string[]{ $list });
var sb = new System.Text.StringBuilder();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset", new[]{ "Assets/Fonts" })) {
  var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
  var a = UnityEditor.AssetDatabase.LoadMainAssetAtPath(p);
  bool dirty = a != null && UnityEditor.EditorUtility.IsDirty(a);
  if (!dirty && !put.Contains(p)) continue;
  if (dirty) UnityEditor.EditorUtility.ClearDirty(a);
  UnityEditor.AssetDatabase.ImportAsset(p, UnityEditor.ImportAssetOptions.ForceUpdate);
  sb.Append(System.IO.Path.GetFileName(p)).Append(dirty ? " (was dirty)" : " (file restored)").Append("; ");
}
// TMP keeps fallback materials / lookups made for the old atlas; without this the next play threw
// IndexOutOfRange in TMP_MaterialManager.GetFallbackMaterial ~1900 times (2026-11-28).
if (sb.Length > 0) {
  TMPro.TMP_MaterialManager.CleanupFallbackMaterials();
  foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset", new[]{ "Assets/Fonts" })) {
    var fa = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    if (fa != null) { fa.ReadFontAssetDefinition(); TMPro.TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, fa); }
  }
}
return sb.Length == 0 ? "unchanged" : sb.ToString();
"@
    return (Invoke-UnityEval -Code $code -Timeout 60)
}

# Hidden cameras left in the Editor (HideAndDontSave, enabled, not the Scene view camera).
# A temporary capture camera that was not destroyed (an exception before DestroyImmediate) draws over the Main Camera
# in the Game view: the player seems missing and nothing moves (2026-11-25, Test_Mapchip).
# -Remove destroys the ones whose name starts with "tmp" (made by Claude's eval snippets). Returns one line per camera.
function Find-StrayCameras {
    param([switch]$Remove)
    $rm = if ($Remove) { "true" } else { "false" }
    $code = @"
var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>()) {
  if (UnityEditor.EditorUtility.IsPersistent(c) || c == null) continue;
  if ((c.gameObject.hideFlags & UnityEngine.HideFlags.DontSave) == 0) continue;
  if (c.name == "SceneCamera" || c.name == "Preview Camera" || !c.enabled) continue;
  bool tmp = c.name.StartsWith("tmp");
  sb.Append(c.name).Append(" at ").Append(c.transform.position);
  if ($rm && tmp) { UnityEngine.Object.DestroyImmediate(c.gameObject); sb.Append(" (removed)"); }
  sb.Append("\n");
}
return sb.ToString();
"@
    $r = Invoke-UnityEval -Quiet -Code $code
    if ($r) { return ($r -split "`n" | Where-Object { $_ }) }
    return @()
}

# Runs C# in the Editor and returns the parsed result object (or $null on failure).
function Invoke-UnityEval {
    param(
        [string]$Code,
        [string]$File,
        [int]$Timeout = 30,
        [switch]$Quiet
    )
    # PowerShell 5.1 mangles double quotes inside native arguments, so inline code goes through a temp file.
    $tmp = $null
    if (-not $File) {
        $tmp = Join-Path $env:TEMP ("ct_eval_" + [guid]::NewGuid().ToString("N") + ".cs")
        [System.IO.File]::WriteAllText($tmp, $Code, (New-Object System.Text.UTF8Encoding($false)))
        $File = $tmp
    }
    $args2 = @("command", "script", "eval", "--result-only", "--timeout", "$Timeout", "--file", $File)
    # Right after a recompile / domain reload the pipeline server is not up yet (PIPELINE_UNREACHABLE).
    # The code never ran in that case, so retrying is safe: up to 10 tries, 2 s apart (about 20 s).
    $json = $null; $text = ""
    for ($try = 0; $try -lt 10; $try++) {
        $raw = & unity @args2 2>$null
        $text = ($raw -join "`n")
        $json = $null
        try { $json = $text | ConvertFrom-Json } catch { }
        $unreachable = ($text -match "PIPELINE_UNREACHABLE") -and ($null -eq $json -or -not $json.success)
        if (-not $unreachable) { break }
        Start-Sleep -Seconds 2
    }
    if ($tmp) { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
    if ($null -eq $json) {
        if (-not $Quiet) { Write-Output "[ERROR] eval output was not JSON:"; Write-Output $text }
        return $null
    }
    if (-not $json.success) {
        if (-not $Quiet) { Write-Output "[ERROR] eval failed:"; Write-Output $text }
        return $null
    }
    return $json.result
}
