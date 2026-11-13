// Called by auditor.ps1 (placeholders __REPORT__ / __TSV__ / __TOP__ / __IGNORE__ are replaced before running).
// Loads a saved Project Auditor report and sorts the issues (items with a descriptor) into groups:
//   asset  = assets and project settings (the part scan.ps1 cannot see well)
//   code   = runtime code (Assembly-CSharp, not under Editor/)
//   reload = DomainReload (UDR*: statics not reset; this project disables domain reload)
//   editor = code under Editor/ (counted only)
//   ignored = descriptors listed in references/auditor-ignore.md
// Writes asset/code/reload issues to a TSV (id, severity, group, path, line, description) for diffing,
// and returns: first line key=value counts, then per group the descriptors with the most issues.
// Items without a descriptor are inventory rows (texture list, shader variants...) and are only counted.
// ASCII only. Not under Assets/ (would be imported).
string err;
var report = Unity.ProjectAuditor.Editor.Report.Load("__REPORT__", out err);
if (report == null) return "error=load failed " + err;
int top = __TOP__;
var ignore = new System.Collections.Generic.HashSet<string>("__IGNORE__".Split(new[]{','}, System.StringSplitOptions.RemoveEmptyEntries));
var all = report.GetAllIssues();
var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Unity.ProjectAuditor.Editor.ReportItem>>>();
foreach (var g in new[]{"asset","code","reload"}) groups[g] = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Unity.ProjectAuditor.Editor.ReportItem>>();
int inventory = 0, packages = 0, editor = 0, ignored = 0;
var count = new System.Collections.Generic.Dictionary<string, int> { {"asset",0}, {"code",0}, {"reload",0} };
var tsv = new System.Text.StringBuilder();
tsv.Append("id\tseverity\tgroup\tpath\tline\tdescription\n");
var compileErrors = new System.Text.StringBuilder();
int compileErrorCount = 0;
foreach (var it in all) {
  // Player build compile errors: the runtime code could not be analyzed (and a real build would fail too)
  if (it.Category == Unity.ProjectAuditor.Editor.IssueCategory.CodeCompilerMessage && it.LogLevel == Unity.ProjectAuditor.Editor.LogLevel.Error) {
    compileErrorCount++;
    compileErrors.Append("  - ").Append(it.RelativePath).Append(it.Line > 0 ? ":" + it.Line : "").Append("  ").Append(it.Description).Append("\n");
    continue;
  }
  if (!it.Id.IsValid()) { inventory++; continue; }
  string path = it.RelativePath ?? "";
  if (path.StartsWith("Packages/") || path.StartsWith("Library/")) { packages++; continue; }
  string key = it.Id.AsString();
  if (ignore.Contains(key)) { ignored++; continue; }
  string grp;
  if (it.Category == Unity.ProjectAuditor.Editor.IssueCategory.DomainReload) grp = "reload";
  else if (it.Category == Unity.ProjectAuditor.Editor.IssueCategory.Code || it.Category == Unity.ProjectAuditor.Editor.IssueCategory.CodeCompilerMessage || it.Category == Unity.ProjectAuditor.Editor.IssueCategory.ObsoleteAPI) grp = "code";
  else grp = "asset";
  if (path.Contains("/Editor/")) { editor++; continue; }
  count[grp]++;
  if (!groups[grp].ContainsKey(key)) groups[grp][key] = new System.Collections.Generic.List<Unity.ProjectAuditor.Editor.ReportItem>();
  groups[grp][key].Add(it);
  string desc = (it.Description ?? "").Replace("\t", " ").Replace("\n", " ").Replace("\r", "");
  tsv.Append(key).Append('\t').Append(it.Severity).Append('\t').Append(grp).Append('\t').Append(path).Append('\t').Append(it.Line).Append('\t').Append(desc).Append('\n');
}
System.IO.File.WriteAllText("__TSV__", tsv.ToString(), new System.Text.UTF8Encoding(false));
var sb = new System.Text.StringBuilder();
sb.Append("total=").Append(all.Count).Append(";assetIssues=").Append(count["asset"]).Append(";codeIssues=").Append(count["code"])
  .Append(";reloadIssues=").Append(count["reload"]).Append(";editorIssues=").Append(editor).Append(";ignored=").Append(ignored)
  .Append(";packageIssues=").Append(packages).Append(";inventory=").Append(inventory).Append(";playerCompileErrors=").Append(compileErrorCount).Append("\n");
if (compileErrorCount > 0) {
  sb.Append("\n## !! PLAYER BUILD COMPILE ERRORS (").Append(compileErrorCount).Append("): Assembly-CSharp does not compile for the player.\n")
    .Append("## A real build fails, and the runtime code (group B) could not be analyzed.\n").Append(compileErrors);
}
System.Func<string, int> Rank = s => s == "Critical" ? 0 : s == "Error" ? 1 : s == "Major" ? 2 : s == "Moderate" ? 3 : s == "Warning" ? 4 : s == "Minor" ? 5 : 6;
var titles = new System.Collections.Generic.Dictionary<string, string> {
  {"asset", "## A. assets and project settings"}, {"code", "## B. runtime code (not Editor/)"}, {"reload", "## C. domain reload (statics not reset; this project disables domain reload)"} };
foreach (var grp in new[]{"asset","code","reload"}) {
  var ordered = System.Linq.Enumerable.ToList(System.Linq.Enumerable.ThenByDescending(
    System.Linq.Enumerable.OrderBy(groups[grp], g => Rank(g.Value[0].Severity.ToString())), g => g.Value.Count));
  sb.Append("\n").Append(titles[grp]).Append(" (").Append(count[grp]).Append(" issues, ").Append(ordered.Count).Append(" kinds)\n");
  int shown = 0;
  foreach (var g in ordered) {
    if (shown++ >= top) { sb.Append("... ").Append(ordered.Count - top).Append(" more kinds (use -Top or the TSV)\n"); break; }
    var d = g.Value[0].Id.GetDescriptor();
    sb.Append("### ").Append(g.Key).Append(" [").Append(g.Value[0].Severity).Append("] ").Append(d != null ? d.Title : "?")
      .Append(" (").Append(d != null ? d.Areas.ToString() : "").Append(") - ").Append(g.Value.Count).Append("\n");
    if (d != null && !string.IsNullOrEmpty(d.Recommendation)) {
      var rec = System.Text.RegularExpressions.Regex.Replace(d.Recommendation, "<[^>]+>", "").Replace("\n", " ").Replace("\r", "");
      if (rec.Length > 200) rec = rec.Substring(0, 200) + "...";
      sb.Append("  fix: ").Append(rec).Append("\n");
    }
    int n = 0;
    foreach (var it in g.Value) {
      if (n++ >= 4) { sb.Append("  - ... ").Append(g.Value.Count - 4).Append(" more\n"); break; }
      var desc = (it.Description ?? "").Replace("\n", " ");
      if (desc.Length > 110) desc = desc.Substring(0, 110) + "...";
      sb.Append("  - ").Append(it.RelativePath).Append(it.Line > 0 ? ":" + it.Line : "").Append("  ").Append(desc).Append("\n");
    }
  }
}
return sb.ToString();
