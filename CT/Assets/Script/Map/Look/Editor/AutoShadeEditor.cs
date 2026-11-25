/*********************************************************************/
// * \file   AutoShadeEditor.cs
// * \brief  隅の影（AutoShade）の Inspector：「影を塗り直す」ボタンと、塗る処理
// *
// * \author
/*********************************************************************/

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 隅の影の Inspector と塗る処理（レシピからは AutoShadeEditor.Rebuild）
/// </summary>
[CustomEditor(typeof(AutoShade))]
public class AutoShadeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if(GUILayout.Button("影を塗り直す"))
        {
            var shade = (AutoShade)target;
            int n = Rebuild(shade, true);
            Debug.Log($"{shade.name}：隅の影を {n} マス塗りました", shade);
        }
        EditorGUILayout.HelpBox("床・壁を塗り直したら押してください。この Tilemap の今の中身は消えます。", MessageType.Info);
    }

    /// <summary>
    /// 影の Tilemap を消して、床と壁から塗り直す。塗ったマスの数を返す
    /// </summary>
    public static int Rebuild(AutoShade shade, bool undo)
    {
        Tilemap target = shade.GetComponent<Tilemap>();
        if(undo) { Undo.RegisterCompleteObjectUndo(target, "隅の影を塗り直す"); }
        target.ClearAllTiles();
        if(shade.lightShade == null && shade.darkShade == null) { return 0; }

        int count = 0;
        foreach(Tilemap floor in shade.floors)
        {
            if(floor == null) { continue; }
            floor.CompressBounds();
            foreach(Vector3Int c in floor.cellBounds.allPositionsWithin)
            {
                if(!floor.HasTile(c)) { continue; }
                // 床のマスの位置をワールド経由で影の Tilemap のマスに直す（Grid が違っても合う）
                Vector3 w = floor.GetCellCenterWorld(c);
                bool up    = IsWall(shade, w + Vector3.up);
                bool left  = IsWall(shade, w + Vector3.left);
                bool right = IsWall(shade, w + Vector3.right);
                bool self  = IsWall(shade, w);
                bool side  = shade.shadeSides && (left || right || self);

                TileBase t = null;
                if(up && side)       { t = shade.darkShade != null ? shade.darkShade : shade.lightShade; }
                else if(up || side)  { t = shade.lightShade != null ? shade.lightShade : shade.darkShade; }
                if(t == null) { continue; }

                Vector3Int tc = target.WorldToCell(w);
                if(target.HasTile(tc)) { continue; }
                target.SetTile(tc, t);
                count++;
            }
        }
        target.CompressBounds();
        EditorUtility.SetDirty(target);
        return count;
    }

    private static bool IsWall(AutoShade shade, Vector3 world)
    {
        foreach(Tilemap wall in shade.walls)
        {
            if(wall != null && wall.HasTile(wall.WorldToCell(world))) { return true; }
        }
        return false;
    }
}
