# Import a dialogue CSV into DialogueData assets, and/or lint the generated dialogues.
# Usage:
#   import.ps1 -Csv Assets/Data/Dialogue/Scenario_F1.csv           import (same as Tools menu, no dialog) and print the log lines
#   import.ps1 -Lint Scenario_F1                                    lint Assets/Data/Dialogue/Scenario_F1/*.asset
#   import.ps1 -Csv Assets/Data/Dialogue/Scenario_F1.csv -Lint Scenario_F1
#   options: -MaxChars 40 (one line longer than this = warn), -MaxLines 10 (one dialogue longer than this = warn)
# Lint also lists dialogues that no scene/prefab references (by GUID), so they are never shown in game.
# ASCII only.
param(
    [string]$Csv,
    [string]$Lint,
    [int]$MaxChars = 40,
    [int]$MaxLines = 10
)

. (Join-Path $PSScriptRoot "..\..\unity-check\scripts\lib.ps1")

if ($Csv) {
    $p = $Csv.Replace("\", "/")
    $code = @'
var logs = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback cb = (msg, st, type) => { if (msg.Contains("CSV")) logs.Add(type + ": " + msg); };
UnityEngine.Application.logMessageReceived += cb;
try { DialogueCsv.Import("__CSV__", false); } finally { UnityEngine.Application.logMessageReceived -= cb; }
return string.Join("\n", logs);
'@
    Invoke-UnityEval -Code ($code.Replace("__CSV__", $p)) -Timeout 60
}

if ($Lint) {
    $folder = "Assets/Data/Dialogue/" + $Lint
    $code = @'
var sb = new System.Text.StringBuilder();
int n = 0;
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:DialogueData", new[]{ "__FOLDER__" })) {
  var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
  var d = UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueData>(path); if (d == null) continue; n++;
  var so = new UnityEditor.SerializedObject(d); var lines = so.FindProperty("lines");
  if (lines == null) { sb.Append("? no 'lines' field: " + d.name + "\n"); continue; }
  if (lines.arraySize > __MAXLINES__) sb.Append("LONG-TALK " + d.name + ": " + lines.arraySize + " lines\n");
  if (lines.arraySize == 0) sb.Append("EMPTY " + d.name + "\n");
  for (int i = 0; i < lines.arraySize; i++) {
    var l = lines.GetArrayElementAtIndex(i);
    string text = l.FindPropertyRelative("text").stringValue ?? "";
    var chara = l.FindPropertyRelative("character").objectReferenceValue;
    string name = l.FindPropertyRelative("speakerName").stringValue ?? "";
    foreach (var part in text.Split('\n')) if (part.Length > __MAXCHARS__) sb.Append("LONG-LINE " + d.name + " #" + (i + 1) + " (" + part.Length + "): " + part + "\n");
    if (chara == null && name.Trim().Length == 0) sb.Append("NO-SPEAKER " + d.name + " #" + (i + 1) + ": " + text + "\n");
  }
}
return n + " dialogue(s) in __FOLDER__\n" + sb.ToString();
'@
    $code = $code.Replace("__FOLDER__", $folder).Replace("__MAXLINES__", "$MaxLines").Replace("__MAXCHARS__", "$MaxChars")
    Invoke-UnityEval -Code $code -Timeout 60

    # Unused: no scene / prefab / asset (other than the dialogue itself) mentions the GUID
    $root = (Get-Location).Path
    $dir = Join-Path $root $folder
    if (Test-Path $dir) {
        $targets = @(Get-ChildItem (Join-Path $root "Assets") -Recurse -Include *.unity, *.prefab, *.asset -File | Where-Object { $_.DirectoryName -ne $dir })
        $unused = @()
        foreach ($meta in Get-ChildItem $dir -Filter *.asset.meta) {
            $g = (Select-String -Path $meta.FullName -Pattern "^guid: (\w+)").Matches[0].Groups[1].Value
            $hit = $targets | Select-String -Pattern $g -SimpleMatch -List | Select-Object -First 1
            if (-not $hit) { $unused += $meta.Name.Replace(".asset.meta", "") }
        }
        Write-Output ("UNUSED (not placed in any scene/prefab yet): " + $unused.Count)
        $unused | ForEach-Object { Write-Output ("  " + $_) }
    }
}

if (-not $Csv -and -not $Lint) { Write-Output "Specify -Csv <path> and/or -Lint <folder name under Assets/Data/Dialogue>" }
