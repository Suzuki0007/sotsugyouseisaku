// シグルの立ち絵（Sigle.asset）・タイトルの背景（UI_TitleImage・画面いっぱい）
var sb = new System.Text.StringBuilder();
// 1. Sigle portrait (same as Noah: expression Normal only)
var sigleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Images/UI/UI_siguru.png");
foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Images/UI/UI_siguru.png")) if (o is UnityEngine.Sprite s) sigleSprite = s;
var sigle = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterProfile>("Assets/Data/Characters/Sigle.asset");
{ var so = new UnityEditor.SerializedObject(sigle); var ex = so.FindProperty("expressions"); ex.arraySize = 1;
  var e0 = ex.GetArrayElementAtIndex(0); e0.FindPropertyRelative("expression").enumValueIndex = 0; e0.FindPropertyRelative("sprite").objectReferenceValue = sigleSprite;
  so.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(sigle); }
sb.Append("sigle sprite=" + sigleSprite.name + "\n");
// 2. Title image import: single sprite, full resolution
string tp = "Assets/Images/UI/UI_TitleImage.PNG";
var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(tp);
imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.maxTextureSize = 4096; imp.mipmapEnabled = false; imp.SaveAndReimport();
var titleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(tp);
UnityEditor.AssetDatabase.SaveAssets();
sb.Append("title sprite rect=" + titleSprite.rect + "\n");
// 3. Title scene background (cover the screen, keep aspect)
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Title.unity") { if (scene.isDirty) return "ABORT dirty " + scene.path; scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Title.unity"); }
UnityEngine.UI.Image bg = null;
foreach (var img in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(UnityEngine.FindObjectsInactive.Include)) if (img.name == "Background") bg = img;
bg.sprite = titleSprite; bg.preserveAspect = false; bg.color = UnityEngine.Color.white;
var fit = bg.GetComponent<UnityEngine.UI.AspectRatioFitter>(); if (fit == null) fit = bg.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = titleSprite.rect.width / titleSprite.rect.height;
UnityEditor.EditorUtility.SetDirty(bg); UnityEditor.EditorUtility.SetDirty(fit);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
sb.Append("bg set ratio=" + fit.aspectRatio + " sibling=" + bg.transform.GetSiblingIndex());
return sb.ToString();
