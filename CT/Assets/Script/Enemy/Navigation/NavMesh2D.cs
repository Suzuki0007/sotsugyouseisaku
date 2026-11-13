/*********************************************************************/
// * \file   NavMesh2D.cs
// * \brief  2D のマップ（XY 平面）で敵の経路を探す（Unity 標準の NavMesh を使う）
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵の大きさ（通れる道の幅が変わる）
/// </summary>
public enum EnemySize
{
    [InspectorName("小（細い通路も通れる）")]
    Small = 0,

    [InspectorName("中")]
    Medium = 1,

    [InspectorName("大（広い所しか通れない）")]
    Large = 2,
}

/// <summary>
/// 2D のナビメッシュ（敵の経路探索）
/// </summary>
///
/// ・シーンに置かなくてよい。敵が経路を聞いたときに自動で作られ、シーンを出ると消える
/// ・壁：当たり判定のある（Is Trigger でない）2D コライダーのうち、動かないもの（Rigidbody2D がないか Static）。
///   プレイヤー・敵・弾は含めない。扉などの表示が切り替わったら、0.5 秒以内に作り直す
/// ・歩ける範囲：MapArea（歩ける範囲 WalkArea の形 → マップの絵 MapBounds → Tilemap。プレイヤーが動ける範囲と同じ）。
///   WalkArea があるシーンでは、その形どおりの床（升目 0.2 マス）を作る。WalkArea の形が変わると作り直す
/// ・EnemyNavArea（敵の通行設定エリア）で「入れない」「通りにくい」場所を作れる
///
/// しくみ：Unity の NavMesh は 3D（XZ 平面）用なので、ナビメッシュを X 軸で -90 度回して XY 平面に重ねる。
/// 2D コライダーの形は Collider2D.CreateMesh で取り出し、床より少し手前（カメラ側）に置いて「通れない」にする。
/// 経路を聞くときは、そのまま XY のワールド座標で聞いてよい
/// NavMeshAgent は使わない（敵は Rigidbody2D で動かすので、経路だけをここで計算する）
[DefaultExecutionOrder(-100)]
public class NavMesh2D : MonoBehaviour
{
    //=====================================================================
    // 定数
    //=====================================================================

    // ナビメッシュの向き（XZ → XY）
    private static readonly Quaternion Rotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);

    // 敵の大きさごとの半径
    private static readonly float[] Radii = { 0.2f, 0.4f, 0.8f };

    private const float AgentHeight = 1.0f;
    private const float AgentClimb = 0.1f;

    // 壁を床から浮かせる量（登れる高さより上・通れる高さより下にして、真下の床を通れなくする）
    private const float ObstacleLift = 0.5f;

    private const float FloorThickness = 0.1f;

    // 壁が変わったかを確かめる間隔（秒）
    private const float CheckInterval = 0.5f;

    /// <summary> 「通りにくい」場所のエリア番号（0 = 歩ける、1 = 歩けない、2 = ジャンプ は Unity が使う） </summary>
    public const int AvoidArea = 3;

    // 「通りにくい」場所の重み（普通の床の何倍遠く感じるか）
    private const float AvoidCost = 5.0f;

    private const int NotWalkableArea = 1;

    //=====================================================================
    // static
    //=====================================================================

    private static NavMesh2D instance;

    /// <summary> 歩ける範囲と、敵の経路を画面に表示する（デバッグウィンドウから切り替える） </summary>
    public static bool ShowDebug { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        instance = null;
        ShowDebug = false;
    }

    /// <summary>
    /// 今のシーンのナビメッシュ（なければ作る。再生中だけ）
    /// </summary>
    public static NavMesh2D Instance
    {
        get
        {
            if(instance == null && Application.isPlaying)
            {
                var go = new GameObject("NavMesh2D（自動で作られる）");
                instance = go.AddComponent<NavMesh2D>();
            }
            return instance;
        }
    }

    /// <summary> 今のシーンにナビメッシュがあるか（作らずに確かめる） </summary>
    public static bool Exists => instance != null;

    //=====================================================================
    // 変数
    //=====================================================================

    private class Layer
    {
        public NavMeshBuildSettings settings;
        public NavMeshData data;
        public NavMeshDataInstance handle;
        public AsyncOperation pending;
        public bool dirty;
    }

    private readonly Layer[] layers = new Layer[3];

    // 作り直し中に中身を変えないよう、集め直すたびに新しいリストにする
    private List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();

    private readonly Dictionary<Collider2D, CachedMesh> meshes = new Dictionary<Collider2D, CachedMesh>();

    private struct CachedMesh
    {
        public Mesh mesh;
        public int hash;
    }

    private Bounds localBounds;
    private Rect worldRect;
    private int signature;
    private float nextCheckTime;
    private int buildVersion;

    // NavMeshPath はコンストラクタ（フィールドの初期化）で作れないので、Awake で作る
    private NavMeshPath path;
    private readonly Vector3[] cornerBuffer = new Vector3[128];

    //=====================================================================
    // プロパティ
    //=====================================================================

    /// <summary> 歩ける範囲（ワールド座標の四角） </summary>
    public Rect WorldRect => worldRect;

    /// <summary> 作り直すたびに増える（表示の更新に使う） </summary>
    public int BuildVersion => buildVersion;

    //=====================================================================
    // 初期化・片づけ
    //=====================================================================

    private void Awake()
    {
        if(instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        path = new NavMeshPath();
        NavMesh.SetAreaCost(AvoidArea, AvoidCost);

        signature = ComputeSignature();
        Collect();
    }

    private void OnDestroy()
    {
        // ナビメッシュはシーンをまたいで残るので、必ず消す
        foreach(Layer layer in layers)
        {
            if(layer == null) { continue; }
            if(layer.handle.valid) { layer.handle.Remove(); }
            NavMesh.RemoveSettings(layer.settings.agentTypeID);
        }

        foreach(CachedMesh cached in meshes.Values)
        {
            if(cached.mesh != null) { Destroy(cached.mesh); }
        }
        meshes.Clear();

        if(debugMesh != null) { Destroy(debugMesh); }

        if(instance == this) { instance = null; }
    }

    //=====================================================================
    // 更新（壁が変わったら作り直す）
    //=====================================================================

    private void Update()
    {
        UpdateDebugView();

        if(Time.unscaledTime < nextCheckTime) { return; }
        nextCheckTime = Time.unscaledTime + CheckInterval;

        // 作り直しの途中は、材料（メッシュ・リスト）を変えない
        if(IsBuilding()) { return; }

        int newSignature = ComputeSignature();
        if(newSignature != signature)
        {
            signature = newSignature;
            Collect();
            foreach(Layer layer in layers) { if(layer != null) { layer.dirty = true; } }
        }

        foreach(Layer layer in layers)
        {
            if(layer == null || !layer.dirty) { continue; }

            layer.dirty = false;
            layer.pending = NavMeshBuilder.UpdateNavMeshDataAsync(layer.data, layer.settings, sources, localBounds);
            layer.pending.completed += _ => buildVersion++;
        }
    }

    private bool IsBuilding()
    {
        foreach(Layer layer in layers)
        {
            if(layer != null && layer.pending != null && !layer.pending.isDone) { return true; }
        }
        return false;
    }

    //=====================================================================
    // 経路
    //=====================================================================

    /// <summary>
    /// from から to までの経路を探す
    /// </summary>
    /// <param name="corners"> 曲がり角の位置（最初は from の近く、最後は to の近く） </param>
    /// <param name="complete"> to まで行けるか（false なら、行ける所まで） </param>
    /// <returns> 経路が見つかったか（ナビメッシュの外にいる・まったく行けないときは false） </returns>
    public bool CalculatePath(Vector2 from, Vector2 to, EnemySize size, List<Vector2> corners, out bool complete)
    {
        complete = false;
        corners.Clear();

        Layer layer = GetLayer(size);
        if(layer == null) { return false; }

        NavMeshQueryFilter filter = Filter(layer);

        if(!NavMesh.SamplePosition(ToWorld(from), out NavMeshHit start, 1.5f, filter)) { return false; }
        if(!NavMesh.SamplePosition(ToWorld(to), out NavMeshHit end, 3.0f, filter)) { return false; }

        if(!NavMesh.CalculatePath(start.position, end.position, filter, path)) { return false; }
        if(path.status == NavMeshPathStatus.PathInvalid) { return false; }

        int count = path.GetCornersNonAlloc(cornerBuffer);
        for(int i = 0; i < count; i++) { corners.Add(cornerBuffer[i]); }

        complete = path.status == NavMeshPathStatus.PathComplete;
        return corners.Count > 0;
    }

    /// <summary>
    /// position のいちばん近くの歩ける場所
    /// </summary>
    public bool SamplePosition(Vector2 position, float maxDistance, EnemySize size, out Vector2 result)
    {
        result = position;

        Layer layer = GetLayer(size);
        if(layer == null) { return false; }

        if(!NavMesh.SamplePosition(ToWorld(position), out NavMeshHit hit, maxDistance, Filter(layer))) { return false; }

        result = hit.position;
        return true;
    }

    /// <summary>
    /// from から to まで歩いて行けるか
    /// </summary>
    public bool IsReachable(Vector2 from, Vector2 to, EnemySize size)
    {
        var corners = new List<Vector2>();
        return CalculatePath(from, to, size, corners, out bool complete) && complete;
    }

    private static Vector3 ToWorld(Vector2 position) => new Vector3(position.x, position.y, 0.0f);

    private static NavMeshQueryFilter Filter(Layer layer)
    {
        return new NavMeshQueryFilter { agentTypeID = layer.settings.agentTypeID, areaMask = NavMesh.AllAreas };
    }

    /// <summary>
    /// 大きさごとのナビメッシュ（初めて使うときに作る）
    /// </summary>
    private Layer GetLayer(EnemySize size)
    {
        int index = Mathf.Clamp((int)size, 0, layers.Length - 1);
        if(layers[index] != null) { return layers[index]; }

        NavMeshBuildSettings settings = NavMesh.CreateSettings();
        settings.agentRadius = Radii[index];
        settings.agentHeight = AgentHeight;
        settings.agentClimb = AgentClimb;
        settings.agentSlope = 45.0f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = Mathf.Max(0.05f, Radii[index] / 3.0f);
        settings.minRegionArea = 0.1f;

        NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, localBounds, Vector3.zero, Rotation);
        if(data == null)
        {
            NavMesh.RemoveSettings(settings.agentTypeID);
            Debug.LogError("NavMesh2D: ナビメッシュを作れませんでした", this);
            return null;
        }

        var layer = new Layer
        {
            settings = settings,
            data = data,
            handle = NavMesh.AddNavMeshData(data),
        };
        layers[index] = layer;
        buildVersion++;
        return layer;
    }

    //=====================================================================
    // 材料を集める
    //=====================================================================

    /// <summary>
    /// 壁になるコライダーか
    /// </summary>
    public static bool IsObstacle(Collider2D collider)
    {
        if(collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) { return false; }

        // 敵の通行設定エリアは別に扱う
        if(collider.GetComponent<EnemyNavArea>() != null) { return false; }

        if(collider.isTrigger) { return false; }

        // まとめ役（CompositeCollider2D）に含まれるものは、まとめ役の側で扱う
        if(collider.compositeOperation != Collider2D.CompositeOperation.None) { return false; }

        // 動くもの（プレイヤー・敵・弾など）は壁にしない
        Rigidbody2D body = collider.attachedRigidbody;
        if(body != null && body.bodyType != RigidbodyType2D.Static) { return false; }

        int layer = collider.gameObject.layer;
        if(layer == LayerMask.NameToLayer("Bullet") || layer == LayerMask.NameToLayer("Enemies")) { return false; }

        return true;
    }

    /// <summary>
    /// 壁の状態の目印（変わったら作り直す）
    /// </summary>
    private int ComputeSignature()
    {
        unchecked
        {
            int hash = 17;
            foreach(Collider2D collider in FindObjectsByType<Collider2D>())
            {
                bool obstacle = IsObstacle(collider);
                EnemyNavArea area = obstacle ? null : collider.GetComponent<EnemyNavArea>();
                if(!obstacle && (area == null || !area.isActiveAndEnabled || !collider.enabled)) { continue; }

                hash = hash * 31 + collider.GetEntityId().GetHashCode();
                hash = hash * 31 + ShapeHash(collider);
                if(area != null) { hash = hash * 31 + (int)area.Mode; }
            }

            Rect rect = ComputeWorldRect(null);
            hash = hash * 31 + rect.GetHashCode();
            hash = hash * 31 + WalkArea.Signature;
            return hash;
        }
    }

    private static int ShapeHash(Collider2D collider)
    {
        Bounds b = collider.bounds;
        unchecked
        {
            int hash = Mathf.RoundToInt(b.center.x * 20.0f);
            hash = hash * 31 + Mathf.RoundToInt(b.center.y * 20.0f);
            hash = hash * 31 + Mathf.RoundToInt(b.size.x * 20.0f);
            hash = hash * 31 + Mathf.RoundToInt(b.size.y * 20.0f);
            hash = hash * 31 + Mathf.RoundToInt(collider.transform.eulerAngles.z * 10.0f);
            return hash;
        }
    }

    /// <summary>
    /// ナビメッシュの材料を集め直す
    /// </summary>
    private void Collect()
    {
        var newSources = new List<NavMeshBuildSource>();
        var used = new HashSet<Collider2D>();
        var obstacleBounds = new List<Bounds>();

        foreach(Collider2D collider in FindObjectsByType<Collider2D>())
        {
            EnemyNavArea area = collider.GetComponent<EnemyNavArea>();
            bool obstacle = IsObstacle(collider);
            bool blockArea = area != null && area.isActiveAndEnabled && collider.enabled && area.Mode == EnemyNavArea.AreaMode.Block;
            bool avoidArea = area != null && area.isActiveAndEnabled && collider.enabled && area.Mode == EnemyNavArea.AreaMode.Avoid;

            if(obstacle || blockArea)
            {
                Mesh mesh = GetMesh(collider);
                if(mesh == null) { continue; }
                used.Add(collider);
                obstacleBounds.Add(collider.bounds);

                // 床より手前（カメラ側 = ナビメッシュの上側）に浮かせて、真下の床を通れなくする
                newSources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh,
                    transform = Matrix4x4.Translate(new Vector3(0.0f, 0.0f, -ObstacleLift)),
                    area = NotWalkableArea,
                });
            }
            else if(avoidArea)
            {
                // 「通りにくい」は四角（コライダーの外枠）で効く
                Bounds b = collider.bounds;
                newSources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.ModifierBox,
                    transform = Matrix4x4.TRS(new Vector3(b.center.x, b.center.y, 0.0f), Quaternion.identity, Vector3.one),
                    size = new Vector3(b.size.x, b.size.y, 2.0f),
                    area = AvoidArea,
                });
            }
        }

        // 使わなくなったメッシュを片づける
        var removed = new List<Collider2D>();
        foreach(var pair in meshes) { if(!used.Contains(pair.Key)) { removed.Add(pair.Key); } }
        foreach(Collider2D key in removed)
        {
            if(meshes[key].mesh != null) { Destroy(meshes[key].mesh); }
            meshes.Remove(key);
        }

        // 床（歩ける範囲）
        worldRect = ComputeWorldRect(obstacleBounds);
        if(MapArea.HasWalkShape)
        {
            // 歩ける範囲（WalkArea）の形どおりの床：細かい升目で塗り、横に続く所を1本の細長い箱にまとめる
            AddShapedFloor(newSources, worldRect);
        }
        else
        {
            newSources.Add(FloorBox(worldRect.center, worldRect.size));
        }

        // ナビメッシュの中での範囲（回す前の XZ 平面で考える：ワールドの X → X、Y → Z）
        localBounds = new Bounds(
            new Vector3(worldRect.center.x, 0.0f, worldRect.center.y),
            new Vector3(worldRect.width + 4.0f, 4.0f, worldRect.height + 4.0f));

        sources = newSources;
    }

    /// <summary>
    /// コライダーの形のメッシュ（形が変わったときだけ作り直す）
    /// </summary>
    private Mesh GetMesh(Collider2D collider)
    {
        int hash = ShapeHash(collider);
        if(meshes.TryGetValue(collider, out CachedMesh cached) && cached.hash == hash && cached.mesh != null) { return cached.mesh; }

        if(cached.mesh != null) { Destroy(cached.mesh); }

        Mesh mesh = collider.CreateMesh(true, true);
        if(mesh == null) { meshes.Remove(collider); return null; }

        meshes[collider] = new CachedMesh { mesh = mesh, hash = hash };
        return mesh;
    }

    // 形どおりの床を作るときの升目の大きさ（マス）
    private const float FloorCell = 0.2f;

    private static NavMeshBuildSource FloorBox(Vector2 center, Vector2 size)
    {
        return new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            transform = Matrix4x4.TRS(new Vector3(center.x, center.y, FloorThickness * 0.5f), Quaternion.identity, Vector3.one),
            size = new Vector3(size.x, size.y, FloorThickness),
            area = 0,
        };
    }

    /// <summary>
    /// 歩ける範囲の形どおりの床（升目の中心が範囲の中なら床。横に続く升目は1つの箱にまとめる）
    /// </summary>
    private static void AddShapedFloor(List<NavMeshBuildSource> sources, Rect rect)
    {
        int cols = Mathf.CeilToInt(rect.width / FloorCell);
        int rows = Mathf.CeilToInt(rect.height / FloorCell);
        for(int r = 0; r < rows; r++)
        {
            float y = rect.yMin + (r + 0.5f) * FloorCell;
            int start = -1;
            for(int c = 0; c <= cols; c++)
            {
                bool inside = c < cols && WalkArea.Contains(new Vector2(rect.xMin + (c + 0.5f) * FloorCell, y));
                if(inside && start < 0) { start = c; }
                if(!inside && start >= 0)
                {
                    float x0 = rect.xMin + start * FloorCell;
                    float x1 = rect.xMin + c * FloorCell;
                    sources.Add(FloorBox(new Vector2((x0 + x1) * 0.5f, y), new Vector2(x1 - x0, FloorCell)));
                    start = -1;
                }
            }
        }
    }

    /// <summary>
    /// 歩ける範囲を囲む四角（MapArea：歩ける範囲 → マップの絵 → Tilemap。なければ壁の範囲を少し広げたもの）
    /// </summary>
    private static Rect ComputeWorldRect(List<Bounds> obstacleBounds)
    {
        if(MapArea.TryGetWalkRect(out Rect walk)) { return walk; }

        if(obstacleBounds != null && obstacleBounds.Count > 0)
        {
            Bounds all = obstacleBounds[0];
            foreach(Bounds b in obstacleBounds) { all.Encapsulate(b); }
            all.Expand(20.0f);
            return Rect.MinMaxRect(all.min.x, all.min.y, all.max.x, all.max.y);
        }

        return new Rect(-50.0f, -50.0f, 100.0f, 100.0f);
    }

    //=====================================================================
    // 表示（デバッグ）
    //=====================================================================

    private GameObject debugObject;
    private Mesh debugMesh;
    private int debugVersion = -1;
    private readonly List<Vector3> debugNavVertices = new List<Vector3>();
    private readonly List<Vector3> lineVertices = new List<Vector3>();
    private readonly List<Color> lineColors = new List<Color>();
    private readonly List<int> lineIndices = new List<int>();

    private static readonly Color NavColor = new Color(0.2f, 0.8f, 1.0f, 0.35f);
    private static readonly Color PathColor = new Color(1.0f, 0.85f, 0.1f, 0.95f);

    /// <summary>
    /// 歩ける範囲（水色の線）と敵の経路（黄色の線）を、ゲーム画面に線で描く（Gizmos をオンにしなくても見える）
    /// </summary>
    private void UpdateDebugView()
    {
        if(!ShowDebug)
        {
            if(debugObject != null && debugObject.activeSelf) { debugObject.SetActive(false); }
            return;
        }

        if(debugObject == null) { CreateDebugObject(); }
        if(!debugObject.activeSelf) { debugObject.SetActive(true); }

        // 歩ける範囲は作り直したときだけ取り直す
        if(debugVersion != buildVersion)
        {
            debugVersion = buildVersion;
            debugNavVertices.Clear();
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            for(int i = 0; i + 2 < tri.indices.Length; i += 3)
            {
                Vector3 a = tri.vertices[tri.indices[i]];
                Vector3 b = tri.vertices[tri.indices[i + 1]];
                Vector3 c = tri.vertices[tri.indices[i + 2]];
                debugNavVertices.Add(a); debugNavVertices.Add(b);
                debugNavVertices.Add(b); debugNavVertices.Add(c);
                debugNavVertices.Add(c); debugNavVertices.Add(a);
            }
        }

        lineVertices.Clear();
        lineColors.Clear();
        lineIndices.Clear();

        foreach(Vector3 v in debugNavVertices) { AddPoint(v, NavColor); }

        foreach(Enemy enemy in Enemy.All)
        {
            IReadOnlyList<Vector2> corners = enemy.PathCorners;
            if(corners == null || corners.Count == 0) { continue; }

            Vector2 prev = enemy.transform.position;
            for(int i = enemy.PathIndex; i < corners.Count; i++)
            {
                AddPoint(prev, PathColor);
                AddPoint(corners[i], PathColor);
                prev = corners[i];
            }
        }

        debugMesh.Clear();
        debugMesh.SetVertices(lineVertices);
        debugMesh.SetColors(lineColors);
        debugMesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
        debugMesh.RecalculateBounds();
    }

    private void AddPoint(Vector3 position, Color color)
    {
        position.z = -0.1f;
        lineIndices.Add(lineVertices.Count);
        lineVertices.Add(position);
        lineColors.Add(color);
    }

    private void CreateDebugObject()
    {
        debugObject = new GameObject("経路の表示（デバッグ）");
        debugObject.transform.SetParent(transform, false);

        debugMesh = new Mesh { name = "NavMesh2D Debug" };
        debugMesh.MarkDynamic();
        debugObject.AddComponent<MeshFilter>().sharedMesh = debugMesh;

        var renderer = debugObject.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if(shader == null) { shader = Shader.Find("Sprites/Default"); }
        renderer.sharedMaterial = new Material(shader);
        renderer.sortingOrder = 1000;
    }
}
