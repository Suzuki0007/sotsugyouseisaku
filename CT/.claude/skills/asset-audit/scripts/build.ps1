# Build the asset checklist workbook from scratch (data/list.tsv etc.) with Excel COM. All Japanese text comes from UTF-8 data files.
# WARNING: overwrites the output file (team edits are lost). Use audit.ps1 to update an existing workbook.
param([string]$Dir = (Join-Path $PSScriptRoot "..\data"))
$ErrorActionPreference = "Stop"
$cfg = Get-Content (Join-Path $Dir "config.json") -Raw -Encoding UTF8 | ConvertFrom-Json

function ReadTsv($name) {
    $lines = Get-Content (Join-Path $Dir $name) -Encoding UTF8 | Where-Object { $_ -ne $null }
    $rows = @()
    foreach ($l in $lines) { $rows += ,($l -split "`t") }
    return ,$rows
}
function ToArray($rows, $cols) {
    $arr = New-Object 'object[,]' $rows.Count, $cols
    for ($r = 0; $r -lt $rows.Count; $r++) {
        for ($c = 0; $c -lt $cols; $c++) {
            $v = if ($c -lt $rows[$r].Count) { $rows[$r][$c] } else { "" }
            $arr[$r, $c] = $v
        }
    }
    return ,$arr
}
function ColName($n) { $s = ""; while ($n -gt 0) { $m = ($n - 1) % 26; $s = [char](65 + $m) + $s; $n = [math]::Floor(($n - 1) / 26) }; return $s }

$list = ReadTsv "list.tsv"
$howto = ReadTsv "howto.tsv"
$spec = ReadTsv "spec.tsv"

# header + extra columns
$header = @($list[0]) + @($cfg.extraHeaders)
$data = @()
for ($i = 1; $i -lt $list.Count; $i++) {
    if ($list[$i].Count -lt 5) { continue }
    $data += ,(@($list[$i]) + @("", "", $cfg.defaultProgress, ""))
}
$cols = $header.Count
$nData = $data.Count

$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
try {
    $wb = $xl.Workbooks.Add()
    while ($wb.Worksheets.Count -lt 4) { [void]$wb.Worksheets.Add([Type]::Missing, $wb.Worksheets.Item($wb.Worksheets.Count)) }
    $wsHow = $wb.Worksheets.Item(1); $wsHow.Name = $cfg.sheetHowto
    $wsList = $wb.Worksheets.Item(2); $wsList.Name = $cfg.sheetList
    $wsSum = $wb.Worksheets.Item(3); $wsSum.Name = $cfg.sheetSummary
    $wsSpec = $wb.Worksheets.Item(4); $wsSpec.Name = $cfg.sheetSpec

    $font = "Meiryo UI"
    foreach ($ws in @($wsHow, $wsList, $wsSum, $wsSpec)) { $ws.Cells.Font.Name = $font; $ws.Cells.Font.Size = 10 }

    # ---- list sheet ----
    $all = @(,$header) + $data
    $last = ColName $cols
    $rng = $wsList.Range("A1:$last$($nData + 1)")
    $rng.Value2 = (ToArray $all $cols)
    $rng.WrapText = $true
    $rng.VerticalAlignment = -4160   # top
    $rng.Borders.LineStyle = 1
    $rng.Borders.Color = 0xBFBFBF
    $hdr = $wsList.Range("A1:$last" + "1")
    $hdr.Font.Bold = $true
    $hdr.Interior.Color = 0x5A3A1F    # dark blue (BGR)
    $hdr.Font.Color = 0xFFFFFF
    $hdr.VerticalAlignment = -4108
    # editable headers: K (how), L-O
    foreach ($c in @(11, 12, 13, 14, 15)) { $cell = $wsList.Cells.Item(1, $c); $cell.Interior.Color = 0x66E6FF; $cell.Font.Color = 0x000000 }
    $widths = @(8, 12, 16, 6, 26, 52, 30, 40, 30, 30, 14, 10, 10, 10, 30)
    for ($c = 1; $c -le $cols; $c++) { $wsList.Columns.Item($c).ColumnWidth = $widths[$c - 1] }
    $wsList.Rows.Item(1).RowHeight = 30

    # dropdowns (to row 500 so new rows get them too)
    function AddList($col, $values) {
        $r = $wsList.Range("$col" + "2:$col" + "500")
        $r.Validation.Delete()
        [void]$r.Validation.Add(3, 1, 1, $values)
        $r.Validation.IgnoreBlank = $true
        $r.Validation.InCellDropdown = $true
    }
    AddList "B" $cfg.listCategory
    AddList "C" $cfg.listState
    AddList "D" $cfg.listPriority
    AddList "K" $cfg.listHow
    AddList "N" $cfg.listProgress
    $wsList.Range("M2:M500").NumberFormat = "yyyy/mm/dd"
    $wsList.Range("A2:A500").HorizontalAlignment = -4108
    $wsList.Range("D2:D500").HorizontalAlignment = -4108

    # conditional formats: done = gray row, priority A = light red, B = light yellow
    $done = $cfg.listProgress.Split(",")[3]
    $fc = $wsList.Range("A2:O500").FormatConditions.Add(2, 0, "=`$N2=""$done""")
    $fc.Interior.Color = 0xD9D9D9; $fc.Font.Color = 0x808080
    $fcA = $wsList.Range("D2:D500").FormatConditions.Add(1, 3, "=""A""")
    $fcA.Interior.Color = 0xC7CEFF; $fcA.Font.Bold = $true
    $fcB = $wsList.Range("D2:D500").FormatConditions.Add(1, 3, "=""B""")
    $fcB.Interior.Color = 0x9CEBFF

    [void]$wsList.Range("A1:$last$($nData + 1)").AutoFilter()
    $wsList.Activate()
    $xl.ActiveWindow.SplitRow = 1
    $xl.ActiveWindow.SplitColumn = 5
    $xl.ActiveWindow.FreezePanes = $true

    # ---- summary sheet ----
    $cats = $cfg.listCategory.Split(",")
    $prog = $cfg.listProgress.Split(",")
    $states = $cfg.listState.Split(",")
    $pri = $cfg.listPriority.Split(",")
    $L = "'" + $cfg.sheetList + "'!"
    $wsSum.Cells.Item(1, 1).Value2 = $cfg.summaryTitle1
    $wsSum.Cells.Item(1, 1).Font.Bold = $true; $wsSum.Cells.Item(1, 1).Font.Size = 12
    for ($j = 0; $j -lt $prog.Count; $j++) { $wsSum.Cells.Item(2, 2 + $j).Value2 = $prog[$j] }
    $wsSum.Cells.Item(2, 2 + $prog.Count).Value2 = $cfg.summaryTotal
    $wsSum.Cells.Item(2, 3 + $prog.Count).Value2 = $cfg.summaryDoneRate
    for ($i = 0; $i -lt $cats.Count; $i++) {
        $row = 3 + $i
        $wsSum.Cells.Item($row, 1).Value2 = $cats[$i]
        for ($j = 0; $j -lt $prog.Count; $j++) {
            $colL = ColName (2 + $j)
            $wsSum.Cells.Item($row, 2 + $j).Formula = "=COUNTIFS($($L)`$B:`$B,`$A$row,$($L)`$N:`$N,$colL`$2)"
        }
        $totL = ColName (2 + $prog.Count)
        $wsSum.Cells.Item($row, 2 + $prog.Count).Formula = "=COUNTIF($($L)`$B:`$B,`$A$row)"
        $wsSum.Cells.Item($row, 3 + $prog.Count).Formula = "=IF($totL$row=0,0,E$row/$totL$row)"
    }
    $tr = 3 + $cats.Count
    $wsSum.Cells.Item($tr, 1).Value2 = $cfg.summaryTotal
    for ($j = 0; $j -le $prog.Count; $j++) {
        $colL = ColName (2 + $j)
        $wsSum.Cells.Item($tr, 2 + $j).Formula = "=SUM($colL" + "3:$colL$($tr - 1))"
    }
    $totL = ColName (2 + $prog.Count)
    $wsSum.Cells.Item($tr, 3 + $prog.Count).Formula = "=IF($totL$tr=0,0,E$tr/$totL$tr)"
    $wsSum.Range((ColName (3 + $prog.Count)) + "3:" + (ColName (3 + $prog.Count)) + "$tr").NumberFormat = "0%"
    $t1 = $wsSum.Range("A2:" + (ColName (3 + $prog.Count)) + "$tr")
    $t1.Borders.LineStyle = 1
    $wsSum.Range("A2:" + (ColName (3 + $prog.Count)) + "2").Interior.Color = 0xF2E6DB
    $wsSum.Range("A2:" + (ColName (3 + $prog.Count)) + "2").Font.Bold = $true
    $wsSum.Range("A$($tr):" + (ColName (3 + $prog.Count)) + "$tr").Font.Bold = $true

    $s2 = $tr + 3
    $wsSum.Cells.Item($s2, 1).Value2 = $cfg.summaryTitle2
    $wsSum.Cells.Item($s2, 1).Font.Bold = $true; $wsSum.Cells.Item($s2, 1).Font.Size = 12
    for ($j = 0; $j -lt $states.Count; $j++) { $wsSum.Cells.Item($s2 + 1, 2 + $j).Value2 = $states[$j] }
    $wsSum.Cells.Item($s2 + 1, 2 + $states.Count).Value2 = $cfg.summaryTotal
    for ($i = 0; $i -lt $pri.Count; $i++) {
        $row = $s2 + 2 + $i
        $wsSum.Cells.Item($row, 1).Value2 = $pri[$i]
        for ($j = 0; $j -lt $states.Count; $j++) {
            $colL = ColName (2 + $j)
            $wsSum.Cells.Item($row, 2 + $j).Formula = "=COUNTIFS($($L)`$D:`$D,`$A$row,$($L)`$C:`$C,$colL`$$($s2 + 1))"
        }
        $wsSum.Cells.Item($row, 2 + $states.Count).Formula = "=COUNTIF($($L)`$D:`$D,`$A$row)"
    }
    $t2 = $wsSum.Range("A$($s2 + 1):" + (ColName (2 + $states.Count)) + "$($s2 + 1 + $pri.Count)")
    $t2.Borders.LineStyle = 1
    $wsSum.Range("A$($s2 + 1):" + (ColName (2 + $states.Count)) + "$($s2 + 1)").Interior.Color = 0xF2E6DB
    $wsSum.Range("A$($s2 + 1):" + (ColName (2 + $states.Count)) + "$($s2 + 1)").Font.Bold = $true
    $wsSum.Range("A$($s2 + 1):" + (ColName (2 + $states.Count)) + "$($s2 + 1)").WrapText = $true
    $wsSum.Cells.Item($s2 + 2 + $pri.Count + 1, 1).Value2 = $cfg.summaryNote
    $wsSum.Columns.Item(1).ColumnWidth = 22
    for ($c = 2; $c -le 8; $c++) { $wsSum.Columns.Item($c).ColumnWidth = 14 }

    # ---- howto sheet ----
    $hr = $wsHow.Range("A1:B$($howto.Count)")
    $hr.Value2 = (ToArray $howto 2)
    $wsHow.Columns.Item(1).ColumnWidth = 26
    $wsHow.Columns.Item(2).ColumnWidth = 110
    $hr.WrapText = $true
    $hr.VerticalAlignment = -4160
    $wsHow.Cells.Item(1, 1).Font.Size = 14; $wsHow.Cells.Item(1, 1).Font.Bold = $true
    for ($r = 1; $r -le $howto.Count; $r++) { if ($howto[$r - 1][0].StartsWith([string][char]0x25A0)) { $wsHow.Cells.Item($r, 1).Font.Bold = $true; $wsHow.Cells.Item($r, 1).Interior.Color = 0xF2E6DB } }

    # ---- spec sheet ----
    $sr = $wsSpec.Range("A1:C$($spec.Count)")
    $sr.Value2 = (ToArray $spec 3)
    $sr.WrapText = $true; $sr.VerticalAlignment = -4160; $sr.Borders.LineStyle = 1
    $wsSpec.Range("A1:C1").Font.Bold = $true; $wsSpec.Range("A1:C1").Interior.Color = 0x5A3A1F; $wsSpec.Range("A1:C1").Font.Color = 0xFFFFFF
    $wsSpec.Columns.Item(1).ColumnWidth = 18; $wsSpec.Columns.Item(2).ColumnWidth = 60; $wsSpec.Columns.Item(3).ColumnWidth = 60

    $wsList.Activate()
    $wsList.Range("A2").Select()
    if (Test-Path $cfg.output) { Remove-Item $cfg.output -Force }
    $wb.SaveAs($cfg.output, 51)
    $wb.Close($false)
    Write-Output "saved rows=$nData cols=$cols"
}
finally {
    $xl.Quit()
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($xl)
}

# default sort and colors (category -> priority -> ID, category colors, row stripes)
& (Join-Path $PSScriptRoot "format.ps1")
