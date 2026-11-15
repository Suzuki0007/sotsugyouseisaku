# asset-intake: run an assignment table (AssetMapApplier)
#   apply.ps1 -Table "Docs/<assign folder>/xxx.tsv"            dry run (changes nothing, shows the plan)
#   apply.ps1 -Table "Docs/<assign folder>/xxx.tsv" -Apply     really apply (saves prefabs / data / scenes)
#   -Changes : print only the lines that change something (hide "same" lines)
param(
    [Parameter(Mandatory = $true)][string]$Table,
    [switch]$Apply,
    [switch]$Changes
)
$ErrorActionPreference = "Continue"
. "$PSScriptRoot/../../unity-check/scripts/lib.ps1"
$dry = if ($Apply) { "false" } else { "true" }
$path = $Table.Replace('\', '/')
$code = "return AssetMapApplier.Run(@`"" + $path + "`", " + $dry + ");"
$tmp = Join-Path $env:TEMP "ct_intake_apply.cs"
[IO.File]::WriteAllText($tmp, $code, (New-Object Text.UTF8Encoding $false))
$out = Invoke-UnityEval -File $tmp -Timeout 900
if ($Changes) {
    $same = [string][char]0xFF08 + [char]0x540C + [char]0x3058 + [char]0xFF09
    ($out -split "`n") | Where-Object { $_ -notlike "*$same*" }
} else { $out }
