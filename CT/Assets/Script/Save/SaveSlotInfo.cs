/*********************************************************************/
// * \file   SaveSlotInfo.cs
// * \brief  セーブスロットの概要（ロード画面の表示・再開する場所）
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// セーブスロット1つ分の概要
/// </summary>
///
/// セーブデータの中の「Meta/...」のキーから作る
public class SaveSlotInfo
{
    //=====================================================================
    // セーブのキー
    //=====================================================================

    public const string SceneKey      = "Meta/Scene";        // 再開するシーン
    public const string SpawnPointKey = "Meta/SpawnPoint";   // 再開する出現位置ID（空なら既定の位置）
    public const string HasPosKey     = "Meta/HasPos";       // 位置で再開するか（チェックポイント）
    public const string PosXKey       = "Meta/PosX";
    public const string PosYKey       = "Meta/PosY";
    public const string PlayTimeKey   = "Meta/PlayTime";     // プレイ時間（秒）
    public const string SavedAtKey    = "Meta/SavedAt";      // 保存日時（ISO 8601）

    //=====================================================================
    // 値
    //=====================================================================

    public int Slot;
    public bool Exists;
    public string SceneName = "";
    public string SpawnPointId = "";
    public bool HasPosition;
    public Vector2 Position;
    public float PlayTime;
    public DateTime? SavedAt;

    //=====================================================================
    // 作成
    //=====================================================================

    /// <summary>
    /// スロットのファイルを読んで概要を作る（ファイルがなければ Exists = false）
    /// </summary>
    public static SaveSlotInfo Load(int slot)
    {
        var info = new SaveSlotInfo { Slot = slot };

        Dictionary<string, string> values = Save.PeekSlot(slot);
        if(values == null) { return info; }

        info.Exists = true;
        info.SceneName    = Get(values, SceneKey, "");
        info.SpawnPointId = Get(values, SpawnPointKey, "");
        info.HasPosition  = Get(values, HasPosKey, "False") == "True";
        info.Position     = new Vector2(ParseFloat(Get(values, PosXKey, "0")), ParseFloat(Get(values, PosYKey, "0")));
        info.PlayTime     = ParseFloat(Get(values, PlayTimeKey, "0"));

        if(DateTime.TryParse(Get(values, SavedAtKey, ""), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime saved))
        {
            info.SavedAt = saved;
        }

        return info;
    }

    /// <summary>
    /// 今読み込んでいるデータ（Save）から、再開する場所を作る
    /// </summary>
    public static SaveSlotInfo FromCurrent()
    {
        return new SaveSlotInfo
        {
            Slot         = Save.CurrentSlot,
            Exists       = true,
            SceneName    = Save.Get(SceneKey, ""),
            SpawnPointId = Save.Get(SpawnPointKey, ""),
            HasPosition  = Save.Get(HasPosKey, false),
            Position     = new Vector2(Save.Get(PosXKey, 0f), Save.Get(PosYKey, 0f)),
            PlayTime     = Save.Get(PlayTimeKey, 0f),
        };
    }

    //=====================================================================
    // 表示用
    //=====================================================================

    /// <summary>
    /// プレイ時間を「時間:分:秒」にする
    /// </summary>
    public static string FormatPlayTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}";
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private static string Get(Dictionary<string, string> values, string key, string def)
    {
        return values.TryGetValue(key, out string value) ? value : def;
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
    }
}
