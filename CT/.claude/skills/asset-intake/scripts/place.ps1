# asset-intake: move files from Assets/__Intake/<Name>/ to their real folders (import rules apply on move)
#   place.ps1 -Name 2026-11-20_chara [-Skip "a.wav,b.png"]
#   - destination by folder prefix (data/destinations.tsv), sub folders kept
#   - "_image_" files, "_old" folders and -Skip names are not moved; the staging folder is deleted
#   - a file whose name already exists in the project is a revised version: with -Update its content
#     overwrites the existing file (same GUID, import settings and references kept); without -Update it is reported
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Skip = "",
    [switch]$Update
)
$ErrorActionPreference = "Continue"
. "$PSScriptRoot/../../unity-check/scripts/lib.ps1"
$code = [IO.File]::ReadAllText("$PSScriptRoot/place.cs", [Text.Encoding]::UTF8)
$upd = if ($Update) { "true" } else { "false" }
$code = $code.Replace("__NAME__", $Name).Replace("__SKIP__", $Skip).Replace("__UPDATE__", $upd)
$tmp = Join-Path $env:TEMP "ct_intake_place.cs"
[IO.File]::WriteAllText($tmp, $code, (New-Object Text.UTF8Encoding $false))
Invoke-UnityEval -File $tmp -Timeout 600
