/*********************************************************************/
// * \file   PerfMonitor.cs
// * \brief  性能の計測（画面右上に FPS・メモリを出す／決めた間の記録と要約）
// *
// * \author
/*********************************************************************/

using System;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 性能の計測（エディタと Development Build だけで自動で作られる。シーンをまたいで残る）
/// </summary>
///
/// ・画面右上に FPS・フレーム時間・メモリ・1フレームで確保したメモリ・描画の数を出す（F3 で表示／非表示、覚えておく）
///   目標（60fps）を満たしていれば緑、少し下回ると黄、大きく下回ると赤
/// ・BeginRecord ～ EndRecord の間、毎フレームの値を記録し、中央値・p95・最大・スパイクの数などを1行にまとめる
///   （Claude の計測スクリプト perf.ps1 が使う。数字は「前と比べて悪くなっていないか」を見る目安）
/// ・Snapshot：メモリの量と、読み込まれているテクスチャ・音などの数（シーンを行き来したときの漏れの確認に使う）
/// ・リリース版（Development Build でない）では作られない
public class PerfMonitor : MonoBehaviour
{
    //=====================================================================
    // 設定
    //=====================================================================

    /// <summary> 目標のフレームレート </summary>
    public const float TargetFps = 60.0f;

    private const float RefreshInterval = 0.5f;
    private const int MaxRecordFrames = 60 * 600;   // 60fps で 10 分
    private const string PrefsKey = "Debug_PerfOverlay";

    private static PerfMonitor instance;

    //=====================================================================
    // 計測の値
    //=====================================================================

    private ProfilerRecorder gcAllocRecorder;
    private ProfilerRecorder gcUsedRecorder;
    private ProfilerRecorder totalUsedRecorder;
    private ProfilerRecorder batchesRecorder;      // リリースに近い版だけ（エディタでは UnityStats を使う）
    private ProfilerRecorder playerLoopRecorder;   // ゲームの1フレームの処理（エディタの処理は入らない）
    private ProfilerRecorder waitRecorder;         // その中の「60fps に合わせて待つ」時間
    private ProfilerRecorder updateRecorder;       // スクリプトの Update
    private ProfilerRecorder lateUpdateRecorder;   // スクリプトの LateUpdate
    private ProfilerRecorder fixedUpdateRecorder;  // スクリプトの FixedUpdate

    // 表示用（RefreshInterval ごとにまとめる）
    private float windowTime;
    private int windowFrames;
    private float windowMaxMs;
    private long windowGcAlloc;

    // 記録
    private bool recording;
    private string recordLabel;
    private int recordCount;
    private float[] recFrameMs;
    private float[] recCpuMs;
    private float[] recScriptMs;
    private long[] recGcAlloc;
    private int[] recBatches;
    private int recordGcCountStart;
    private long recordTotalUsedStart;
    private float recordStartTime;

    // 表示
    private GameObject overlayRoot;
    private TextMeshProUGUI overlayText;
    private readonly StringBuilder sb = new StringBuilder(256);

    //=====================================================================
    // 外から使う
    //=====================================================================

    /// <summary> 今の計測の部品（エディタと Development Build だけ。それ以外は null） </summary>
    public static PerfMonitor Instance => instance;

    /// <summary> 記録しているか </summary>
    public static bool IsRecording => instance != null && instance.recording;

    /// <summary> 画面の表示を出しているか </summary>
    public static bool OverlayVisible
    {
        get => instance != null && instance.overlayRoot != null && instance.overlayRoot.activeSelf;
        set
        {
            if(instance == null || instance.overlayRoot == null) { return; }
            instance.overlayRoot.SetActive(value);
            PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
        }
    }

    /// <summary>
    /// 記録を始める（前の記録は捨てる）
    /// </summary>
    public static string BeginRecord(string label)
    {
        if(instance == null) { return "PerfMonitor がない（リリース版？）"; }
        instance.StartRecording(label);
        return "recording " + label;
    }

    /// <summary>
    /// 記録を止めて、要約を1行（key=value;...）で返す
    /// </summary>
    public static string EndRecord()
    {
        if(instance == null) { return "error=no PerfMonitor"; }
        if(!instance.recording) { return "error=not recording"; }
        instance.recording = false;
        return instance.Summarize();
    }

    /// <summary>
    /// 今のメモリの量と、読み込まれている物の数を1行（key=value;...）で返す
    /// （正確に見たいときは、先に CollectNow を呼んで少し待つ）
    /// </summary>
    public static string Snapshot()
    {
        var s = new StringBuilder();
        s.Append("scene=").Append(SceneManager.GetActiveScene().name);
        s.Append(";totalUsedMB=").Append(ToMB(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()).ToString("F1"));
        s.Append(";gcUsedMB=").Append(ToMB(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()).ToString("F1"));
        s.Append(";gcHeapMB=").Append(ToMB(UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong()).ToString("F1"));
        s.Append(";textures=").Append(Resources.FindObjectsOfTypeAll<Texture>().Length);
        s.Append(";audio=").Append(Resources.FindObjectsOfTypeAll<AudioClip>().Length);
        s.Append(";materials=").Append(Resources.FindObjectsOfTypeAll<Material>().Length);
        s.Append(";gameObjects=").Append(FindObjectsByType<GameObject>().Length);
        return s.ToString();
    }

    /// <summary>
    /// GC と使っていないアセットの解放を今すぐ行う（Snapshot の前に。解放は次のフレーム以降に終わる）
    /// </summary>
    public static string CollectNow()
    {
        GC.Collect();
        Resources.UnloadUnusedAssets();
        return "collect requested";
    }

    //=====================================================================
    // 自動で作る
    //=====================================================================

    // ドメインリロード無効のエディタで、前の再生の値を持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if(instance != null) { return; }
        if(!Debug.isDebugBuild) { return; }

        var go = new GameObject("性能の計測（自動で作られる）");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PerfMonitor>();
        instance.BuildOverlay();
        instance.overlayRoot.SetActive(PlayerPrefs.GetInt(PrefsKey, 1) == 1);
    }

    private void OnEnable()
    {
        gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        gcUsedRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
        totalUsedRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        playerLoopRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop");
        waitRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "WaitForTargetFPS");
        updateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Update.ScriptRunBehaviourUpdate");
        lateUpdateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PreLateUpdate.ScriptRunBehaviourLateUpdate");
        fixedUpdateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedUpdate.ScriptRunBehaviourFixedUpdate");
    }

    private void OnDisable()
    {
        gcAllocRecorder.Dispose();
        gcUsedRecorder.Dispose();
        totalUsedRecorder.Dispose();
        batchesRecorder.Dispose();
        playerLoopRecorder.Dispose();
        waitRecorder.Dispose();
        updateRecorder.Dispose();
        lateUpdateRecorder.Dispose();
        fixedUpdateRecorder.Dispose();
    }

    /// <summary> ゲームの1フレームの処理時間（待ち時間を除く、ミリ秒） </summary>
    private float WorkMs()
    {
        if(!playerLoopRecorder.Valid) { return 0.0f; }
        long wait = waitRecorder.Valid ? waitRecorder.LastValue : 0;
        return Mathf.Max(0, playerLoopRecorder.LastValue - wait) * 1e-6f;
    }

    /// <summary> スクリプトの Update・LateUpdate・FixedUpdate の合計（ミリ秒） </summary>
    private float ScriptMs()
    {
        long sum = 0;
        if(updateRecorder.Valid) { sum += updateRecorder.LastValue; }
        if(lateUpdateRecorder.Valid) { sum += lateUpdateRecorder.LastValue; }
        if(fixedUpdateRecorder.Valid) { sum += fixedUpdateRecorder.LastValue; }
        return sum * 1e-6f;
    }

    /// <summary> 描画の数（ドローコール。取れなければ -1） </summary>
    private int DrawCalls()
    {
#if UNITY_EDITOR
        return UnityEditor.UnityStats.drawCalls;
#else
        return batchesRecorder.Valid ? (int)batchesRecorder.LastValue : -1;
#endif
    }

    private void OnDestroy()
    {
        if(instance == this) { instance = null; }
    }

    private void BuildOverlay()
    {
        overlayRoot = new GameObject("表示");
        overlayRoot.transform.SetParent(transform, false);

        var canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 1.0f;

        var bg = new GameObject("背景");
        bg.transform.SetParent(overlayRoot.transform, false);
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.0f, 0.0f, 0.0f, 0.55f);
        bgImage.raycastTarget = false;
        var bgRect = bgImage.rectTransform;
        bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = new Vector2(1.0f, 1.0f);
        bgRect.anchoredPosition = new Vector2(-8.0f, -8.0f);
        bgRect.sizeDelta = new Vector2(360.0f, 138.0f);

        var textGo = new GameObject("文字");
        textGo.transform.SetParent(bg.transform, false);
        overlayText = textGo.AddComponent<TextMeshProUGUI>();
        overlayText.fontSize = 20.0f;
        overlayText.lineSpacing = -10.0f;
        overlayText.raycastTarget = false;
        overlayText.richText = true;
        overlayText.alignment = TextAlignmentOptions.TopLeft;
        var textRect = overlayText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10.0f, 6.0f);
        textRect.offsetMax = new Vector2(-10.0f, -6.0f);
    }

    //=====================================================================
    // 更新
    //=====================================================================

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if(kb != null && kb.f3Key.wasPressedThisFrame) { OverlayVisible = !OverlayVisible; }

        float frameMs = Time.unscaledDeltaTime * 1000.0f;
        long gcAlloc = gcAllocRecorder.Valid ? gcAllocRecorder.LastValue : 0;

        if(recording && recordCount < MaxRecordFrames)
        {
            recFrameMs[recordCount] = frameMs;
            recCpuMs[recordCount] = WorkMs();
            recScriptMs[recordCount] = ScriptMs();
            recGcAlloc[recordCount] = gcAlloc;
            recBatches[recordCount] = DrawCalls();
            recordCount++;
        }

        windowTime += Time.unscaledDeltaTime;
        windowFrames++;
        if(frameMs > windowMaxMs) { windowMaxMs = frameMs; }
        windowGcAlloc += gcAlloc;

        if(windowTime >= RefreshInterval)
        {
            if(overlayRoot != null && overlayRoot.activeSelf) { RefreshOverlay(); }
            windowTime = 0.0f;
            windowFrames = 0;
            windowMaxMs = 0.0f;
            windowGcAlloc = 0;
        }
    }

    private void RefreshOverlay()
    {
        float fps = windowFrames / Mathf.Max(windowTime, 0.0001f);
        float avgMs = windowTime * 1000.0f / Mathf.Max(windowFrames, 1);
        string color = fps >= TargetFps * 0.92f ? "#7CFF7C" : (fps >= TargetFps * 0.75f ? "#FFE070" : "#FF6A6A");

        sb.Clear();
        sb.Append("<color=").Append(color).Append('>');
        sb.Append("FPS ");
        AppendInt(sb, Mathf.RoundToInt(fps));
        sb.Append("  ");
        AppendFixed1(sb, avgMs);
        sb.Append("ms  最大 ");
        AppendFixed1(sb, windowMaxMs);
        sb.Append("ms</color>\n");

        sb.Append("処理 ");
        AppendFixed1(sb, WorkMs());
        sb.Append("ms  スクリプト ");
        AppendFixed1(sb, ScriptMs());
        sb.Append("ms\n");

        sb.Append("メモリ ");
        AppendInt(sb, (int)ToMB(totalUsedRecorder.Valid ? totalUsedRecorder.LastValue : 0));
        sb.Append("MB  GC ");
        AppendFixed1(sb, ToMB(gcUsedRecorder.Valid ? gcUsedRecorder.LastValue : 0));
        sb.Append("MB\n");

        // エディタでは、エディタ自身の確保も入る（ゲームの分だけは perf.ps1 がプロファイラで測る）
        sb.Append(Application.isEditor ? "確保(エディタ込) " : "確保 ");
        AppendFixed1(sb, windowGcAlloc / 1024.0f / Mathf.Max(windowFrames, 1));
        sb.Append("KB/フレーム  描画 ");
        int draw = DrawCalls();
        if(draw >= 0) { AppendInt(sb, draw); } else { sb.Append('-'); }
        if(recording) { sb.Append("  <color=#FF6A6A>●記録中</color>"); }

        overlayText.SetText(sb);
    }

    //=====================================================================
    // 記録
    //=====================================================================

    private void StartRecording(string label)
    {
        if(recFrameMs == null)
        {
            recFrameMs = new float[MaxRecordFrames];
            recCpuMs = new float[MaxRecordFrames];
            recScriptMs = new float[MaxRecordFrames];
            recGcAlloc = new long[MaxRecordFrames];
            recBatches = new int[MaxRecordFrames];
        }
        recordLabel = string.IsNullOrEmpty(label) ? "-" : label;
        recordCount = 0;
        recordGcCountStart = GC.CollectionCount(0);
        recordTotalUsedStart = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        recordStartTime = Time.unscaledTime;
        recording = true;
    }

    private string Summarize()
    {
        int n = recordCount;
        var s = new StringBuilder();
        s.Append("label=").Append(recordLabel);
        s.Append(";scene=").Append(SceneManager.GetActiveScene().name);
        s.Append(";seconds=").Append((Time.unscaledTime - recordStartTime).ToString("F1"));
        s.Append(";frames=").Append(n);
        if(n == 0) { return s.ToString(); }

        // 最初の数フレームはシーンの読み込み・記録の開始の影響を受けやすいので、多ければ除く
        int skip = n > 60 ? 5 : 0;
        int m = n - skip;

        var frame = new float[m];
        var cpu = new float[m];
        var script = new float[m];
        double sumMs = 0.0;
        long sumGc = 0;
        long maxGc = 0;
        int allocFrames = 0;
        long sumBatches = 0;
        int maxBatches = -1;
        int spikes = 0;
        float spikeMs = 2000.0f / TargetFps;   // 目標の2倍以上かかったフレーム
        for(int i = 0; i < m; i++)
        {
            int k = i + skip;
            frame[i] = recFrameMs[k];
            cpu[i] = recCpuMs[k];
            script[i] = recScriptMs[k];
            sumMs += recFrameMs[k];
            sumGc += recGcAlloc[k];
            if(recGcAlloc[k] > maxGc) { maxGc = recGcAlloc[k]; }
            if(recGcAlloc[k] > 0) { allocFrames++; }
            sumBatches += recBatches[k];
            if(recBatches[k] > maxBatches) { maxBatches = recBatches[k]; }
            if(recFrameMs[k] >= spikeMs) { spikes++; }
        }
        Array.Sort(frame);
        Array.Sort(cpu);
        Array.Sort(script);

        s.Append(";avgFps=").Append((1000.0 / (sumMs / m)).ToString("F1"));
        s.Append(";p50ms=").Append(Percentile(frame, 0.50f).ToString("F2"));
        s.Append(";p95ms=").Append(Percentile(frame, 0.95f).ToString("F2"));
        s.Append(";p99ms=").Append(Percentile(frame, 0.99f).ToString("F2"));
        s.Append(";maxms=").Append(frame[m - 1].ToString("F2"));
        s.Append(";spikes=").Append(spikes);
        s.Append(";workP50ms=").Append(Percentile(cpu, 0.50f).ToString("F2"));
        s.Append(";workP95ms=").Append(Percentile(cpu, 0.95f).ToString("F2"));
        s.Append(";workMaxms=").Append(cpu[m - 1].ToString("F2"));
        s.Append(";scriptP50ms=").Append(Percentile(script, 0.50f).ToString("F2"));
        s.Append(";scriptP95ms=").Append(Percentile(script, 0.95f).ToString("F2"));
        s.Append(";gcKBPerFrame=").Append((sumGc / 1024.0 / m).ToString("F2"));
        s.Append(";gcMaxKB=").Append((maxGc / 1024.0).ToString("F1"));
        s.Append(";allocFramePct=").Append((100.0 * allocFrames / m).ToString("F0"));
        s.Append(";gcCollections=").Append(GC.CollectionCount(0) - recordGcCountStart);
        s.Append(";drawCallsAvg=").Append(maxBatches < 0 ? "-" : (sumBatches / (double)m).ToString("F0"));
        s.Append(";drawCallsMax=").Append(maxBatches < 0 ? "-" : maxBatches.ToString());
        s.Append(";memStartMB=").Append(ToMB(recordTotalUsedStart).ToString("F1"));
        s.Append(";memEndMB=").Append(ToMB(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()).ToString("F1"));
        s.Append(";gcUsedMB=").Append(ToMB(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()).ToString("F1"));
        return s.ToString();
    }

    //=====================================================================
    // 小さな道具
    //=====================================================================

    private static float Percentile(float[] sorted, float p)
    {
        int i = Mathf.Clamp(Mathf.CeilToInt(p * sorted.Length) - 1, 0, sorted.Length - 1);
        return sorted[i];
    }

    private static float ToMB(long bytes)
    {
        return bytes / (1024.0f * 1024.0f);
    }

    // StringBuilder.Append(int) は文字列を作るので、表示の更新では数字を1文字ずつ足す
    private static void AppendInt(StringBuilder s, int value)
    {
        if(value < 0) { s.Append('-'); value = -value; }
        if(value >= 10) { AppendInt(s, value / 10); }
        s.Append((char)('0' + value % 10));
    }

    private static void AppendFixed1(StringBuilder s, float value)
    {
        int tenths = Mathf.RoundToInt(value * 10.0f);
        if(tenths < 0) { s.Append('-'); tenths = -tenths; }
        AppendInt(s, tenths / 10);
        s.Append('.');
        s.Append((char)('0' + tenths % 10));
    }
}
