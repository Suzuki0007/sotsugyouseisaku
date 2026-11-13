// Runs SceneChecker on every scene currently open in the Editor (does not open or save scenes)
// plus the project-wide checks, and returns a text report.
// Used by check.ps1 -SceneCheck via: unity command script eval --file <this>
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

var issues = new List<SceneChecker.Issue>();
var names = new List<string>();
for (int i = 0; i < SceneManager.sceneCount; i++)
{
    var s = SceneManager.GetSceneAt(i);
    if (s.isLoaded) { SceneChecker.CheckScene(s, issues); names.Add(string.IsNullOrEmpty(s.name) ? "(untitled)" : s.name); }
}
SceneChecker.CheckProject(issues);

var sb = new StringBuilder();
int e = 0, w = 0, n = 0;
foreach (var it in issues)
{
    if (it.level == SceneChecker.Level.Error) e++;
    else if (it.level == SceneChecker.Level.Warning) w++;
    else n++;
}
sb.AppendLine("Scenes: " + string.Join(", ", names));
sb.AppendLine($"Error {e} / Warning {w} / Info {n}");
foreach (var it in issues)
{
    if (it.level == SceneChecker.Level.Info) continue;
    string where = string.IsNullOrEmpty(it.sceneName) ? "(project)" : it.sceneName;
    sb.AppendLine($"[{it.level}] {where} {it.objectPath} : {it.message}");
}
sb.ToString()
