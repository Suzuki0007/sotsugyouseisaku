// Called by perf.ps1 (-Profile / -Measure) after the profiler has recorded a few seconds of play mode.
// Stops the profiler and reads the captured frames: GC allocation and time of the game only (PlayerLoop),
// and the top scripts by GC allocation and by self time. First line is key=value for the CSV.
// ASCII only. Not under Assets/ (would be imported).
UnityEditorInternal.ProfilerDriver.enabled = false;
int first = UnityEditorInternal.ProfilerDriver.firstFrameIndex, last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
var gcBy = new System.Collections.Generic.Dictionary<string, double>();
var timeBy = new System.Collections.Generic.Dictionary<string, double>();
double plGc = 0, elGc = 0, plTime = 0, maxGc = 0; int frames = 0, allocFrames = 0;
var kids = new System.Collections.Generic.List<int>();
var colGc = UnityEditor.Profiling.HierarchyFrameDataView.columnGcMemory;
var colSelf = UnityEditor.Profiling.HierarchyFrameDataView.columnSelfTime;
var colTotal = UnityEditor.Profiling.HierarchyFrameDataView.columnTotalTime;
System.Func<string, string> Short = s => { int i = s.IndexOf("!::"); if (i >= 0) s = s.Substring(i + 3); return s.Replace(" [Invoke]", ""); };
System.Action<UnityEditor.Profiling.HierarchyFrameDataView, int, string> walk = null;
walk = (v, id, parent) => {
  string name = v.GetItemName(id);
  if (name == "GC.Alloc") { double g = v.GetItemColumnDataAsDouble(id, colGc); gcBy[parent] = (gcBy.ContainsKey(parent) ? gcBy[parent] : 0) + g; return; }
  double self = v.GetItemColumnDataAsDouble(id, colSelf);
  timeBy[name] = (timeBy.ContainsKey(name) ? timeBy[name] : 0) + self;
  var c = new System.Collections.Generic.List<int>(); v.GetItemChildren(id, c);
  foreach (var k in c) walk(v, k, name);
};
// skip the first frames (profiler start)
for (int f = first + 3; f <= last; f++) {
  using (var v = UnityEditorInternal.ProfilerDriver.GetHierarchyFrameDataView(f, 0, UnityEditor.Profiling.HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, colGc, false)) {
    if (v == null || !v.valid) continue;
    kids.Clear(); v.GetItemChildren(v.GetRootItemID(), kids);
    bool counted = false;
    foreach (var id in kids) {
      string n = v.GetItemName(id);
      double g = v.GetItemColumnDataAsDouble(id, colGc);
      if (n == "PlayerLoop") {
        counted = true;
        plGc += g; if (g > maxGc) maxGc = g; if (g > 0) allocFrames++;
        plTime += v.GetItemColumnDataAsDouble(id, colTotal);
        walk(v, id, "PlayerLoop");
      }
      else if (n == "EditorLoop") elGc += g;
    }
    if (counted) frames++;
  }
}
UnityEditorInternal.ProfilerDriver.ClearAllFrames();
if (frames == 0) return "error=no profiler frames (not playing?)";
var sb = new System.Text.StringBuilder();
sb.Append("gameGcKBPerFrame=" + (plGc / frames / 1024).ToString("F2") + ";gameGcMaxKB=" + (maxGc / 1024).ToString("F1")
  + ";gameAllocFramePct=" + (100.0 * allocFrames / frames).ToString("F0") + ";profiledFrames=" + frames
  + ";playerLoopMs=" + (plTime / frames).ToString("F2") + ";editorGcKBPerFrame=" + (elGc / frames / 1024).ToString("F2") + "\n");
sb.Append("top GC alloc in the game (KB/frame):\n");
foreach (var kv in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(gcBy, x => x.Value), 8))
  sb.Append("    " + (kv.Value / frames / 1024).ToString("F2").PadLeft(7) + "  " + Short(kv.Key) + "\n");
sb.Append("top self time in the game (ms/frame):\n");
foreach (var kv in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(timeBy, x => x.Value), 10))
  sb.Append("    " + (kv.Value / frames).ToString("F3").PadLeft(7) + "  " + Short(kv.Key) + "\n");
return sb.ToString();
