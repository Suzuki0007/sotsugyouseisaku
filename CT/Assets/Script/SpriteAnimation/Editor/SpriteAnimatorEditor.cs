/*********************************************************************/
// * \file   SpriteAnimatorEditor.cs
// * \brief  SpriteAnimator の Inspector 表示 (動きの選択・試し再生・設定ミスの警告)
// *
// * \author
/*********************************************************************/

using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SpriteAnimator の Inspector
///   ・「最初に流す動き」をデータの動きから選べる
///   ・データを入れると、シーン上の見た目を最初のコマにする
///   ・Playモード中は動きごとのボタンで試し再生できる
///   ・Animator が一緒に付いていると警告する (画像を上書きしてしまうため)
/// </summary>
[CustomEditor(typeof(SpriteAnimator))]
[CanEditMultipleObjects]
public class SpriteAnimatorEditor : Editor
{
    private SerializedProperty data, playOnEnable, startClip, startFacing, speed, ignoreTimeScale;

    private void OnEnable()
    {
        data            = serializedObject.FindProperty("data");
        playOnEnable    = serializedObject.FindProperty("playOnEnable");
        startClip       = serializedObject.FindProperty("startClip");
        startFacing     = serializedObject.FindProperty("startFacing");
        speed           = serializedObject.FindProperty("speed");
        ignoreTimeScale = serializedObject.FindProperty("ignoreTimeScale");
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(data, new GUIContent("アニメーションデータ"));
        bool dataChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.PropertyField(playOnEnable, new GUIContent("自動で再生する"));

        var animData = data.objectReferenceValue as SpriteAnimData;
        if(animData != null && !data.hasMultipleDifferentValues && animData.Clips.Count > 0)
        {
            string[] names = new[] { "(データの「最初に流す動き」)" }.Concat(animData.Clips.Select(c => c?.name ?? "")).ToArray();
            int current = System.Array.FindIndex(names, 1, names.Length - 1,
                n => string.Equals(n, startClip.stringValue, System.StringComparison.OrdinalIgnoreCase));
            int picked = EditorGUILayout.Popup("最初に流す動き", Mathf.Max(0, current), names);
            if(picked != Mathf.Max(0, current)) { startClip.stringValue = picked == 0 ? "" : names[picked]; }
        }
        else
        {
            EditorGUILayout.PropertyField(startClip, new GUIContent("最初に流す動き"));
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(startFacing, new GUIContent("最初の向き"));
        bool facingChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.PropertyField(speed, new GUIContent("再生速度の倍率"));
        EditorGUILayout.PropertyField(ignoreTimeScale, new GUIContent("ポーズ中も動かす"));

        serializedObject.ApplyModifiedProperties();

        // データや向きを変えたら、シーン上の見た目を最初のコマにする
        if((dataChanged || facingChanged) && !Application.isPlaying) { ShowFirstFrame(); }

        DrawWarnings(animData);
        DrawTestButtons(animData);
    }

    //=====================================================================
    // 見た目の更新 (エディタ)
    //=====================================================================

    private void ShowFirstFrame()
    {
        foreach(Object t in targets)
        {
            var anim = (SpriteAnimator)t;
            if(anim.Data == null) { continue; }

            var so = new SerializedObject(anim);
            string start = so.FindProperty("startClip").stringValue;
            var facing = (SpriteAnimFacing)so.FindProperty("startFacing").enumValueIndex;

            SpriteAnimClip clip = anim.Data.Find(start) ?? anim.Data.DefaultClip;
            if(clip == null) { continue; }

            Sprite[] frames = clip.GetFrames(facing, facing == SpriteAnimFacing.Left, out bool flip);
            if(!SpriteAnimClip.Has(frames) || frames[0] == null) { continue; }

            var sr = anim.GetComponent<SpriteRenderer>();
            Undo.RecordObject(sr, "アニメーションの最初のコマを表示");
            sr.sprite = frames[0];
            sr.flipX = flip;
        }
    }

    //=====================================================================
    // 警告
    //=====================================================================

    private void DrawWarnings(SpriteAnimData animData)
    {
        if(serializedObject.isEditingMultipleObjects) { return; }
        var anim = (SpriteAnimator)target;

        EditorGUILayout.Space(6);

        if(animData == null)
        {
            EditorGUILayout.HelpBox("アニメーションデータが入っていません。\n" +
                                    "画像を選んで右クリック → Create → Animation → SpriteAnimData (選択した画像から作成) で作れます。", MessageType.Warning);
        }

        Animator animator = anim.GetComponent<Animator>();
        if(animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            EditorGUILayout.HelpBox("Animator コンポーネントも付いています。Animator が画像を上書きするため、SpriteAnimator の動きが表示されません。", MessageType.Warning);
            if(GUILayout.Button("Animator を外す"))
            {
                Undo.DestroyObjectImmediate(animator);
                GUIUtility.ExitGUI();
            }
        }

        if(anim.GetComponent<CharacterAnimLink>() == null && animData != null && animData.Has("Walk"))
        {
            EditorGUILayout.HelpBox("「Walk」の動きがあります。キャラクターなら CharacterAnimLink も付けると、移動に合わせて Idle / Walk が自動で切り替わります。", MessageType.Info);
            if(GUILayout.Button("CharacterAnimLink を付ける"))
            {
                Undo.AddComponent<CharacterAnimLink>(anim.gameObject);
            }
        }
    }

    //=====================================================================
    // 試し再生
    //=====================================================================

    private void DrawTestButtons(SpriteAnimData animData)
    {
        if(serializedObject.isEditingMultipleObjects || animData == null) { return; }

        EditorGUILayout.Space(6);
        if(!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Playモード中は、ここから動きを試し再生できます。", MessageType.None);
            return;
        }

        var anim = (SpriteAnimator)target;
        EditorGUILayout.LabelField("今の動き", $"{anim.CurrentClipName}  ({SpriteAnimImporter.FacingLabel(anim.Facing)}向き){(anim.IsFinished ? "  再生終了" : "")}");

        if(anim.GetComponent<CharacterAnimLink>() != null)
        {
            EditorGUILayout.HelpBox("CharacterAnimLink が付いているため、Idle / Walk はすぐに移動に合わせた動きに戻ります。", MessageType.None);
        }

        int perRow = 3;
        var clipList = animData.Clips.Where(c => c != null).ToList();
        for(int i = 0; i < clipList.Count; i += perRow)
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                for(int j = i; j < Mathf.Min(i + perRow, clipList.Count); j++)
                {
                    if(GUILayout.Button("▶ " + clipList[j].name)) { anim.Play(clipList[j].name, true); }
                }
            }
        }

        using(new EditorGUILayout.HorizontalScope())
        {
            for(int f = 0; f < 4; f++)
            {
                var facing = (SpriteAnimFacing)f;
                if(GUILayout.Button(SpriteAnimImporter.FacingLabel(facing) + "向き")) { anim.SetFacing(facing); }
            }
        }
    }
}
