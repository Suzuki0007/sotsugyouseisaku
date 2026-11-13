# Apply the default sort and colors to the checklist sheet (idempotent; keeps all values).
#   - sort: category (in the order of the category dropdown) -> priority (A, B, C) -> ID
#   - ID and category cells: one pastel color per category (same color for both cells)
#   - from the priority column to the last column: alternating white / light gray rows (follows filters)
#   - priority A: bold; progress "done": gray text
# Usage: format.ps1            (opens the workbook from data/config.json, applies, saves)
#        called by build.ps1 at the end; audit.ps1 keeps these colors (it changes only font colors)
# ASCII only. Japanese strings come from data/config.json.
param([string]$Path = "")
$ErrorActionPreference = "Continue"
$skill = Split-Path -Parent $PSScriptRoot
$cfg = Get-Content (Join-Path $skill "data\config.json") -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $Path) { $Path = $cfg.output }

function Bgr([string]$hex) { $h = $hex.TrimStart("#"); $r = [Convert]::ToInt32($h.Substring(0, 2), 16); $g = [Convert]::ToInt32($h.Substring(2, 2), 16); $b = [Convert]::ToInt32($h.Substring(4, 2), 16); return $r + ($g * 256) + ($b * 65536) }
function ColName($n) { $s = ""; while ($n -gt 0) { $m = ($n - 1) % 26; $s = [char](65 + $m) + $s; $n = [math]::Floor(($n - 1) / 26) }; return $s }

$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false
try {
    $wb = $xl.Workbooks.Open($Path)
    if ($wb.ReadOnly) { Write-Output "[ERROR] the workbook is open somewhere (read-only). Close it first."; $wb.Close($false); exit 3 }
    $ws = $wb.Worksheets.Item($cfg.sheetList)

    # "done" check column at the very left (inserted once)
    if ([string]$ws.Cells.Item(1, 1).Value2 -ne $cfg.format.checkHeader) {
        [void]$ws.Columns.Item(1).Insert()
        $hc = $ws.Cells.Item(1, 1)
        $hc.Value2 = $cfg.format.checkHeader
        [void]$hc.AddComment([string]$cfg.format.checkNote); $hc.Comment.Shape.Width = 300; $hc.Comment.Shape.Height = 80
        Write-Output "inserted the check column"
    }
    $hc = $ws.Cells.Item(1, 1)
    $hc.Interior.Color = 0x66E6FF; $hc.Font.Color = 0x000000; $hc.Font.Bold = $true; $hc.HorizontalAlignment = -4108
    $ws.Columns.Item(1).ColumnWidth = 5
    $chk = $ws.Range("A2:A500")
    $chk.Validation.Delete()
    [void]$chk.Validation.Add(3, 1, 1, [string]$cfg.format.checkMark)
    $chk.Validation.IgnoreBlank = $true
    $chk.Validation.InCellDropdown = $true
    $chk.HorizontalAlignment = -4108
    $chk.Font.Size = 12
    $chk.Font.Bold = $true

    # howto sheet: one line about the check column (once)
    $how = $wb.Worksheets.Item($cfg.sheetHowto)
    $hl = $cfg.format.howtoLine
    if (-not $how.UsedRange.Find($hl[0])) {
        $hr = $how.UsedRange.Rows.Count + $how.UsedRange.Row
        $how.Cells.Item($hr, 1).Value2 = $hl[0]; $how.Cells.Item($hr, 2).Value2 = $hl[1]
        $how.Cells.Item($hr, 1).Font.Bold = $true; $how.Cells.Item($hr, 1).Interior.Color = 0xDBE6F2; $how.Cells.Item($hr, 2).WrapText = $true
    }

    # columns by header
    $lastCol = $ws.UsedRange.Columns.Count + $ws.UsedRange.Column - 1
    $col = @{}
    for ($c = 1; $c -le $lastCol; $c++) { $h = [string]$ws.Cells.Item(1, $c).Value2; if ($h) { $col[$h] = $c; $lastHeader = $c } }
    $lastCol = $lastHeader
    $cId = $col[$cfg.audit.idHeader]; $cCat = $col[$cfg.format.categoryHeader]; $cPri = $col[$cfg.format.priorityHeader]; $cProg = $col[$cfg.audit.progressHeader]
    if (-not ($cId -and $cCat -and $cPri -and $cProg)) { Write-Output "[ERROR] header not found"; exit 4 }
    $lastRow = $ws.Cells.Item($ws.Rows.Count, $cId).End(-4162).Row
    $L = ColName $lastCol
    $cats = $cfg.listCategory.Split(",")

    # ---- filter over every column (includes the check column) ----
    if ($ws.AutoFilterMode) { $ws.AutoFilterMode = $false }
    $data = $ws.Range("A1:$L$lastRow")
    [void]$data.AutoFilter()

    # ---- sort ----
    $ws.Sort.SortFields.Clear()
    [void]$ws.Sort.SortFields.Add($ws.Range((ColName $cCat) + "2:" + (ColName $cCat) + "$lastRow"), 0, 1, ($cats -join ","), 0)
    [void]$ws.Sort.SortFields.Add($ws.Range((ColName $cPri) + "2:" + (ColName $cPri) + "$lastRow"), 0, 1, [Type]::Missing, 0)
    [void]$ws.Sort.SortFields.Add($ws.Range((ColName $cId) + "2:" + (ColName $cId) + "$lastRow"), 0, 1, [Type]::Missing, 0)
    $ws.Sort.SetRange($data)
    $ws.Sort.Header = 1
    $ws.Sort.MatchCase = $false
    $ws.Sort.Orientation = 1
    $ws.Sort.Apply()

    # ---- colors: clear old fills and conditional formats on the data area ----
    $area = $ws.Range("A2:" + $L + "500")
    $area.FormatConditions.Delete()
    $area.Interior.Pattern = -4142   # none

    $idL = ColName $cId; $catL = ColName $cCat; $priL = ColName $cPri; $progL = ColName $cProg

    # band: the column after priority .. last column, every other visible row (SUBTOTAL keeps the stripes right when filtered)
    # (the priority column itself keeps one color per priority, below)
    $band = $ws.Range((ColName ($cPri + 1)) + "2:" + $L + "500")
    $fb = $band.FormatConditions.Add(2, 0, "=AND(`$$idL" + "2<>"""",MOD(SUBTOTAL(3,`$$idL`$2:`$$idL" + "2),2)=0)")
    $fb.Interior.Color = (Bgr $cfg.format.bandColor)
    $fb.StopIfTrue = $false

    # ID + category: one color per category
    $idCat = $ws.Range($idL + "2:" + $idL + "500," + $catL + "2:" + $catL + "500")
    for ($i = 0; $i -lt $cats.Count; $i++) {
        $color = $cfg.format.categoryColors[$i % $cfg.format.categoryColors.Count]
        $f = $idCat.FormatConditions.Add(2, 0, "=`$$catL" + "2=""" + $cats[$i] + """")
        $f.Interior.Color = (Bgr $color)
        $f.StopIfTrue = $false
    }

    # priority: one fill per priority (A light red + bold, B light yellow, C none); no stripes in this column
    $pri = $cfg.listPriority.Split(",")
    $fa = $ws.Range($priL + "2:" + $priL + "500").FormatConditions.Add(1, 3, "=""" + $pri[0] + """")
    $fa.Interior.Color = (Bgr $cfg.format.priorityColors[0]); $fa.Font.Bold = $true
    $fbp = $ws.Range($priL + "2:" + $priL + "500").FormatConditions.Add(1, 3, "=""" + $pri[1] + """")
    $fbp.Interior.Color = (Bgr $cfg.format.priorityColors[1])
    # done rows gray text (no fill, so the colors stay)
    $done = $cfg.listProgress.Split(",")[3]
    $fd = $area.FormatConditions.Add(2, 0, "=`$$progL" + "2=""$done""")
    $fd.Font.Color = (Bgr "#909090")
    $fd.StopIfTrue = $false

    # checked rows: gray text; the check cell itself green
    $mark = [string]$cfg.format.checkMark
    $fcRow = $ws.Range("B2:" + $L + "500").FormatConditions.Add(2, 0, "=`$A2=""$mark""")
    $fcRow.Font.Color = (Bgr "#909090")
    $fcRow.StopIfTrue = $false
    $fcChk = $ws.Range("A2:A500").FormatConditions.Add(1, 3, "=""$mark""")
    $fcChk.Interior.Color = (Bgr "#C6EFCE"); $fcChk.Font.Color = (Bgr "#006100")

    # headers of the automatic columns: same dark blue as the other fixed headers
    foreach ($h in @($cfg.audit.keyHeader, $cfg.audit.resultHeader)) {
        if ($col.ContainsKey($h)) { $hc = $ws.Cells.Item(1, $col[$h]); $hc.Interior.Color = 0x5A3A1F; $hc.Font.Color = 0xFFFFFF; $hc.Font.Bold = $true }
    }

    # ID / priority centered, borders light
    $ws.Range($idL + "2:" + $idL + "500").HorizontalAlignment = -4108
    $ws.Range($priL + "2:" + $priL + "500").HorizontalAlignment = -4108
    $ws.Range("A2:$L$lastRow").Borders.LineStyle = 1
    $ws.Range("A2:$L$lastRow").Borders.Color = (Bgr "#D9D9D9")

    # freeze: header row and the columns up to the material name
    $ws.Activate()
    $xl.ActiveWindow.FreezePanes = $false
    $xl.ActiveWindow.SplitRow = 1
    $xl.ActiveWindow.SplitColumn = $(if ($col.ContainsKey($cfg.format.nameHeader)) { $col[$cfg.format.nameHeader] } else { 6 })
    $xl.ActiveWindow.FreezePanes = $true
    $ws.Range("A2").Select()
    $wb.Save()
    $wb.Close($false)
    Write-Output "formatted rows=$($lastRow - 1) cols=$lastCol"
}
finally {
    $xl.Quit()
    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($xl)
}
