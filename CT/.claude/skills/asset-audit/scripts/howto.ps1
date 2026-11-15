# Rewrite only the first sheet ("how to use") of the asset checklist from data/howto.tsv.
# The other sheets (the list the team edits, summary, spec) are not touched.
# Excel must not have the workbook open (it is saved in place).
param([string]$Dir = (Join-Path $PSScriptRoot "..\data"))
$ErrorActionPreference = "Stop"
$cfg = Get-Content (Join-Path $Dir "config.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$lines = Get-Content (Join-Path $Dir "howto.tsv") -Encoding UTF8
$rows = @(); foreach ($l in $lines) { $rows += ,($l -split "`t") }

$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
try {
    $wb = $xl.Workbooks.Open($cfg.output)
    if ($wb.ReadOnly) { throw "workbook is read-only (close it in Excel first): $($cfg.output)" }
    $ws = $wb.Worksheets.Item($cfg.sheetHowto)
    $ws.Range("A1:B200").ClearContents() | Out-Null
    $ws.Range("A1:B200").Font.Bold = $false
    $ws.Range("A1:B200").Interior.ColorIndex = -4142
    $arr = New-Object 'object[,]' $rows.Count, 2
    for ($r = 0; $r -lt $rows.Count; $r++) { for ($c = 0; $c -lt 2; $c++) { $arr[$r, $c] = if ($c -lt $rows[$r].Count) { $rows[$r][$c] } else { "" } } }
    $ws.Range("A1:B$($rows.Count)").Value2 = $arr
    $ws.Cells.Item(1, 1).Font.Bold = $true
    $ws.Cells.Item(1, 1).Font.Size = 14
    for ($r = 1; $r -le $rows.Count; $r++) {
        if ($rows[$r - 1][0].StartsWith([string][char]0x25A0)) { $ws.Cells.Item($r, 1).Font.Bold = $true; $ws.Cells.Item($r, 1).Interior.Color = 0xF2E6DB }
    }
    $ws.Range("B1:B$($rows.Count)").WrapText = $true
    $wb.Save()
    Write-Output "howto sheet rewritten: $($rows.Count) rows"
}
finally {
    if ($wb) { $wb.Close($false) | Out-Null }
    $xl.Quit() | Out-Null
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($xl) | Out-Null
}
