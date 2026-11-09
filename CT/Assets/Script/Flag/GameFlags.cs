/*********************************************************************/
// * \file   GameFlags.cs
// * \brief  ゲーム進行フラグの保存先
// *
// * \author
/*********************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム進行フラグの値を管理するクラス
/// </summary>
///
/// ・セーブするフラグ：Save に仮置きで記録する（チェックポイント・シーン遷移で確定。アイテムと同じ）
/// ・セーブしないフラグ：メモリにだけ持つ（起動し直すと OFF）
///   BgmFlags の名前付きフラグもここに入るので、
///   「セーブしない」GameFlag の ID を BgmFlags の名前と同じにすると同じフラグとして扱える
public static class GameFlags
{
    //=====================================================================
    // 定数
    //=====================================================================

    /// <summary>
    /// セーブに使う名前の頭につける文字
    /// </summary>
    public const string SavePrefix = "Flag/";

    //=====================================================================
    // 変数
    //=====================================================================

    // セーブしないフラグ（ON のものだけ入れる）
    private static readonly HashSet<string> temporary = new HashSet<string>();

    //=====================================================================
    // イベント
    //=====================================================================

    /// <summary>
    /// どれかのフラグが変わったときに呼ばれる
    /// </summary>
    public static event Action OnChanged;

    //=====================================================================
    // GameFlag アセットで使う
    //=====================================================================

    /// <summary>
    /// フラグが ON か（null なら false）
    /// </summary>
    public static bool Get(GameFlag flag)
    {
        if(flag == null) { return false; }

        return flag.Saved ? Save.Get(SavePrefix + flag.Id, false) : temporary.Contains(flag.Id);
    }

    /// <summary>
    /// フラグを設定する
    /// </summary>
    public static void Set(GameFlag flag, bool value)
    {
        if(flag == null) { return; }
        if(Get(flag) == value) { return; }

        if(flag.Saved)
        {
            Save.SetPending(SavePrefix + flag.Id, value);
        }
        else if(value) { temporary.Add(flag.Id); }
        else           { temporary.Remove(flag.Id); }

        Debug.Log($"[フラグ] {flag.Id} = {(value ? "ON" : "OFF")}", flag);
        OnChanged?.Invoke();
    }

    //=====================================================================
    // 名前で使う（セーブしないフラグ。BgmFlags 互換）
    //=====================================================================

    public static bool GetTemporary(string name)
    {
        return !string.IsNullOrEmpty(name) && temporary.Contains(name);
    }

    public static void SetTemporary(string name, bool value)
    {
        if(string.IsNullOrEmpty(name)) { return; }

        bool changed = value ? temporary.Add(name) : temporary.Remove(name);
        if(changed) { OnChanged?.Invoke(); }
    }

    /// <summary>
    /// セーブしないフラグの今の状態を写し取る（戦闘エリアのやり直しで、始まったときの状態に戻すため）
    /// </summary>
    public static HashSet<string> CaptureTemporary()
    {
        return new HashSet<string>(temporary);
    }

    /// <summary>
    /// 写し取った状態に戻す
    /// </summary>
    public static void RestoreTemporary(HashSet<string> snapshot)
    {
        if(snapshot == null) { return; }
        if(temporary.SetEquals(snapshot)) { return; }

        temporary.Clear();
        temporary.UnionWith(snapshot);
        OnChanged?.Invoke();
    }

    /// <summary>
    /// セーブしないフラグをすべて OFF にする
    /// </summary>
    public static void ClearTemporary()
    {
        if(temporary.Count == 0) { return; }

        temporary.Clear();
        OnChanged?.Invoke();
    }

    //=====================================================================
    // 初期化
    //=====================================================================

    // 再生開始時に空にする
    // エディタでドメインリロードを無効にしていると、前回の再生の値や登録が残るため
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        temporary.Clear();
        OnChanged = null;
    }
}
