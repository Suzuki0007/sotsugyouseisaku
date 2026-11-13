/*********************************************************************/
// * \file   EventActionCatalog.cs
// * \brief  イベントの部品の日本語名・分類と、中身の一言要約（エディタ用）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// イベントの部品（EventAction）の日本語名・分類と、Inspector の一覧に出す一言要約
/// </summary>
///
/// ・新しい部品を作ったら、下の表に1行足す（足さなくてもクラス名で出る）
/// ・要約は、Inspector で値が入っている項目を「項目名 値」で3つまで並べる（会話・待つは専用の要約）
public static class EventActionCatalog
{
    //=====================================================================
    // 名前と分類
    //=====================================================================

    private static readonly Dictionary<string, (string category, string name)> table = new Dictionary<string, (string, string)>
    {
        { "DialogueEventAction",       ("会話・演出", "会話") },
        { "BarkEventAction",           ("会話・演出", "一言（吹き出し）") },
        { "SeEventAction",             ("会話・演出", "効果音") },
        { "EffectEventAction",         ("会話・演出", "エフェクト") },
        { "AnimEventAction",           ("会話・演出", "アニメ") },
        { "ShakeEventAction",          ("会話・演出", "画面を揺らす") },
        { "ScreenFadeEventAction",     ("会話・演出", "画面の暗転") },
        { "CameraLookEventAction",     ("会話・演出", "カメラを向ける") },
        { "DarknessEventAction",       ("会話・演出", "暗さ・視界を変える") },
        { "BgmFlagEventAction",        ("会話・演出", "BGM のフラグ") },
        { "BgmProgressEventAction",    ("会話・演出", "BGM の進み具合") },

        { "SetActiveEventAction",      ("物・キャラ", "表示・非表示") },
        { "SpriteChangeEventAction",   ("物・キャラ", "絵を差し替える") },
        { "MoveObjectEventAction",     ("物・キャラ", "物を動かす") },
        { "FaceDirectionEventAction",  ("物・キャラ", "キャラの向き") },
        { "PartyModeEventAction",      ("物・キャラ", "2人の状態") },
        { "PartyWarpEventAction",      ("物・キャラ", "キャラを移す") },

        { "WaitEventAction",           ("進行", "待つ") },
        { "SetFlagEventAction",        ("進行", "フラグ") },
        { "ItemGiveEventAction",       ("進行", "アイテムを渡す") },
        { "ItemTakeEventAction",       ("進行", "アイテムを取る") },
        { "NoteEventAction",           ("進行", "メモを渡す") },
        { "AutoSaveEventAction",       ("進行", "オートセーブ") },
        { "SceneChangeEventAction",    ("進行", "シーンを移る") },
        { "DeathEventAction",          ("進行", "即死") },

        { "GimmickEventAction",        ("仕掛け・敵", "仕掛けを操作") },
        { "PushBlockResetEventAction", ("仕掛け・敵", "押せる箱を戻す") },
        { "EnemyCommandEventAction",   ("仕掛け・敵", "敵に命令") },
        { "EnemyDamageEventAction",    ("仕掛け・敵", "敵にダメージ") },
        { "DamageAreaEventAction",     ("仕掛け・敵", "ダメージエリアを出す") },
        { "BattleControlEventAction",  ("仕掛け・敵", "戦闘エリアを操作") },
    };

    /// <summary> 部品の日本語名（表にないものはクラス名） </summary>
    public static string NameOf(Type type)
    {
        return table.TryGetValue(type.Name, out var v) ? v.name : type.Name;
    }

    /// <summary> 部品の分類（表にないものは「その他」） </summary>
    public static string CategoryOf(Type type)
    {
        return table.TryGetValue(type.Name, out var v) ? v.category : "その他";
    }

    /// <summary> 追加できる部品の型（分類・名前の順） </summary>
    public static List<Type> AllTypes()
    {
        var list = new List<Type>();
        foreach(Type t in TypeCache.GetTypesDerivedFrom<EventAction>())
        {
            if(t.IsAbstract || t.IsGenericType) { continue; }
            list.Add(t);
        }
        list.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(CategoryOf(a), CategoryOf(b));
            return c != 0 ? c : string.CompareOrdinal(NameOf(a), NameOf(b));
        });
        return list;
    }

    //=====================================================================
    // 要約
    //=====================================================================

    /// <summary>
    /// 部品の中身の一言要約
    /// </summary>
    public static string Summary(EventAction action)
    {
        if(action == null) { return "（空の欄）"; }

        if(action is DialogueEventAction talk) { return DialogueSummary(talk); }

        var so = new SerializedObject(action);
        if(action is WaitEventAction)
        {
            SerializedProperty s = so.FindProperty("seconds");
            if(s != null) { return s.floatValue.ToString("0.##") + " 秒"; }
        }

        if(action is SeEventAction)
        {
            UnityEngine.Object enter = so.FindProperty("seOnEnter")?.objectReferenceValue;
            UnityEngine.Object exit = so.FindProperty("seOnExit")?.objectReferenceValue;
            if(enter == null && exit == null) { return "⚠ 音が空欄（鳴らない）"; }
            return (enter != null ? enter.name : "") + (exit != null ? "（出たとき " + exit.name + "）" : "");
        }

        return GenericSummary(so, action.GetType());
    }

    private static string DialogueSummary(DialogueEventAction talk)
    {
        DialogueData data = talk.DirectData;
        string how = "";
        if(data == null && talk.LegacyDialogue != null)
        {
            data = new SerializedObject(talk.LegacyDialogue).FindProperty("dialogueData").objectReferenceValue as DialogueData;
            how = "（子の会話）";
        }
        if(data == null) { return talk.LegacyDialogue != null ? "子の会話 " + talk.LegacyDialogue.name : "会話データが空"; }

        var lines = data.Lines;
        if(lines == null || lines.Length == 0) { return data.name + how + "：文なし"; }

        DialogueData.Line first = lines[0];
        string text = (first.text ?? "").Replace("\n", " ");
        if(text.Length > 22) { text = text.Substring(0, 22) + "…"; }
        return $"{data.name}{how}（{lines.Length} 文）{first.DisplayName}「{text}」";
    }

    private static string GenericSummary(SerializedObject so, Type type)
    {
        var parts = new List<string>();
        SerializedProperty p = so.GetIterator();
        bool enter = true;
        while(p.NextVisible(enter))
        {
            enter = false;
            if(p.name == "m_Script") { continue; }

            string value = ValueText(p);
            if(string.IsNullOrEmpty(value)) { continue; }

            parts.Add(LabelOf(type, p) + " " + value);
            if(parts.Count >= 3) { break; }
        }

        if(parts.Count == 0) { return "（設定なし）"; }
        string s = string.Join("・", parts);
        return s.Length > 70 ? s.Substring(0, 70) + "…" : s;
    }

    private static string ValueText(SerializedProperty p)
    {
        switch(p.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return p.objectReferenceValue != null ? p.objectReferenceValue.name : null;
            case SerializedPropertyType.Boolean:
                return p.boolValue ? "オン" : null;
            case SerializedPropertyType.Integer:
                return p.intValue != 0 ? p.intValue.ToString() : null;
            case SerializedPropertyType.Float:
                return Mathf.Abs(p.floatValue) > 0.0001f ? p.floatValue.ToString("0.##") : null;
            case SerializedPropertyType.String:
                if(string.IsNullOrEmpty(p.stringValue)) { return null; }
                return "「" + (p.stringValue.Length > 16 ? p.stringValue.Substring(0, 16) + "…" : p.stringValue) + "」";
            case SerializedPropertyType.Enum:
                return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length ? p.enumDisplayNames[p.enumValueIndex] : null;
            case SerializedPropertyType.Vector2:
                return p.vector2Value != Vector2.zero ? p.vector2Value.ToString("0.#") : null;
            case SerializedPropertyType.Vector3:
                return p.vector3Value != Vector3.zero ? p.vector3Value.ToString("0.#") : null;
            case SerializedPropertyType.Generic:
                if(p.isArray && p.arraySize > 0)
                {
                    SerializedProperty first = p.GetArrayElementAtIndex(0);
                    string firstText = first.propertyType == SerializedPropertyType.ObjectReference && first.objectReferenceValue != null
                        ? first.objectReferenceValue.name
                        : FirstChildText(first);
                    return (firstText ?? "") + (p.arraySize > 1 ? $" ほか{p.arraySize - 1}" : "");
                }
                return null;
            default:
                return null;
        }
    }

    // 構造体の要素（フラグと ON/OFF など）は、中の最初の値を出す
    private static string FirstChildText(SerializedProperty element)
    {
        SerializedProperty copy = element.Copy();
        SerializedProperty end = element.GetEndProperty();
        bool enter = true;
        while(copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
        {
            enter = false;
            string v = ValueText(copy);
            if(!string.IsNullOrEmpty(v)) { return v; }
        }
        return null;
    }

    private static string LabelOf(Type type, SerializedProperty p)
    {
        for(Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
        {
            FieldInfo f = t.GetField(p.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if(f == null) { continue; }
            var label = f.GetCustomAttribute<LabelAttribute>();
            return label != null ? label.label : p.displayName;
        }
        return p.displayName;
    }
}
