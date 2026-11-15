# asset-intake: copy a delivery folder into Assets/__Intake/<Name>/, then inspect it in Unity
#   inspect.ps1 -Source "C:\Users\...\Downloads\Delivery" -Name 2026-11-20_chara
#   - copies every file (keeps sub folders). Same-content files are reported (hash).
#   - runs inspect.cs: length / loudness / silence / size / alpha / checklist row and keys per folder
#   - writes a draft assignment table: Docs/<assign folder>/<Name>_draft.tsv (targets are "?")
# Text in Japanese lives in inspect.cs (this file must stay ASCII).
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Name
)
$ErrorActionPreference = "Continue"
. "$PSScriptRoot/../../unity-check/scripts/lib.ps1"

if (-not (Test-Path $Source)) { Write-Output "[ERROR] source not found: $Source"; exit 1 }
$dst = Join-Path (Get-Location) "Assets/__Intake/$Name"
New-Item -ItemType Directory -Force $dst | Out-Null

# copy (keep relative folders), find duplicates by hash
$hashes = @{}
$n = 0
Get-ChildItem $Source -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring((Resolve-Path $Source).Path.Length).TrimStart('\')
    $to = Join-Path $dst $rel
    New-Item -ItemType Directory -Force (Split-Path $to) | Out-Null
    Copy-Item $_.FullName $to -Force
    $n++
    $h = (Get-FileHash $_.FullName -Algorithm MD5).Hash
    if ($hashes.ContainsKey($h)) { Write-Output ("[SAME] " + $rel + "  ==  " + $hashes[$h]) } else { $hashes[$h] = $rel }
}
Write-Output "copied $n file(s) to Assets/__Intake/$Name"

# the assign folder name comes from AssetMapApplier.Folder (Japanese), so ask Unity for it
$code = [IO.File]::ReadAllText("$PSScriptRoot/inspect.cs", [Text.Encoding]::UTF8)
$draft = "__FOLDER__/" + $Name + "_draft.tsv"
$code = $code.Replace("__NAME__", $Name).Replace("__DRAFT__", $draft).Replace("`"__FOLDER__", "AssetMapApplier.Folder + `"")
$tmp = Join-Path $env:TEMP "ct_intake_inspect.cs"
[IO.File]::WriteAllText($tmp, $code, (New-Object Text.UTF8Encoding $false))
Invoke-UnityEval -File $tmp -Timeout 600
