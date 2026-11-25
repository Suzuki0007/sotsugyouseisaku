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

function Wait-UnityReady {
    param([int]$Timeout = 300)
    $out = unity status --until-ready --timeout $Timeout 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Output "[ERROR] Unity Editor is not ready (not running?). Start it with: unity open ."
        return $false
    }
    return $true
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
    $raw = & unity @args2 2>$null
    if ($tmp) { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
    $text = ($raw -join "`n")
    try { $json = $text | ConvertFrom-Json } catch {
        if (-not $Quiet) { Write-Output "[ERROR] eval output was not JSON:"; Write-Output $text }
        return $null
    }
    if (-not $json.success) {
        if (-not $Quiet) { Write-Output "[ERROR] eval failed:"; Write-Output $text }
        return $null
    }
    return $json.result
}
