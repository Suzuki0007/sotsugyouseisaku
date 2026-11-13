/*********************************************************************/
// * \file   SceneChecker.cs
// * \brief  シーンの設定ミスを探すチェック処理
// *
// * \author
/*********************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンの設定ミスを探すチェック処理（SceneCheckWindow から使う）
/// </summary>
///
/// チェックを増やすときは CheckScene / CheckProject に足す
public static class SceneChecker
{
    //=====================================================================
    // 型
    //=====================================================================

    public enum Level
    {
        Error,      // 動かない・セーブが壊れる
        Warning,    // 意図どおりに動かない可能性が高い
        Info,       // 問題ないこともある
    }

    public class Issue
    {
        public Level level;
        public string sceneName;    // プロジェクト全体の問題なら空
        public string scenePath;
        public string objectPath;   // ヒエラルキー上のパス
        public Object target;       // クリックで選択するもの（シーンを閉じると消える）
        public string message;
    }

    //=====================================================================
    // シーンのチェック
    //=====================================================================

    public static void CheckScene(Scene scene, List<Issue> issues)
    {
        var ctx = new Context(scene, issues);

        var allObjects = new List<GameObject>();
        foreach(GameObject root in scene.GetRootGameObjects())
        {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) { allObjects.Add(t.gameObject); }
        }

        // スクリプトが外れている
        foreach(GameObject go in allObjects)
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if(missing > 0) { ctx.Add(Level.Error, go, $"スクリプトが外れたコンポーネントが {missing} 個あります（Missing Script）"); }
        }

        CheckSceneChanges(ctx);
        CheckSpawnPoints(ctx);
        CheckInteractables(ctx);
        CheckTreasures(ctx);
        CheckPickups(ctx);
        CheckItemInteractions(ctx);
        CheckEventZones(ctx);
        CheckEventSequences(ctx);
        CheckActions(ctx);
        CheckSwitches(ctx);
        CheckDialogues(ctx);
        CheckEnemies(ctx);
        CheckGimmicks(ctx);
        CheckWalkAreaAndTimpani(ctx);
        CheckRooms(ctx);
    }

    //=====================================================================
    // 部屋（Room）・カメラの範囲
    //=====================================================================

    private static void CheckRooms(Context ctx)
    {
        foreach(Room room in ctx.Find<Room>())
        {
            foreach(RoomChecks.Result r in RoomChecks.Check(room))
            {
                if(r.level == RoomChecks.Level.Ok) { continue; }
                if(r.target is PlayerSpawnPoint) { continue; }   // 出現位置が範囲の外かは「歩ける範囲」の点検で出す（同じ指摘を2回出さない）
                Level level = r.level == RoomChecks.Level.Error ? Level.Error : r.level == RoomChecks.Level.Warning ? Level.Warning : Level.Info;
                ctx.Add(level, r.target, $"部屋「{room.name}」：{r.text}");
            }
        }

        // 部屋に入っていないカメラの範囲も、画面より小さければ知らせる
        Vector2 view = RoomChecks.ViewSize();
        if(view.x <= 0.0f) { return; }
        foreach(CameraArea area in ctx.Find<CameraArea>())
        {
            if(area.GetComponentInParent<Room>() != null) { continue; }
            BoxCollider2D box = area.GetComponent<BoxCollider2D>();
            if(box == null) { continue; }
            Vector2 size = Vector2.Scale(box.size, area.transform.lossyScale);
            if(size.x + 0.01f < view.x || size.y + 0.01f < view.y)
            {
                ctx.Add(Level.Info, area, $"カメラの範囲（{size.x:0.#}×{size.y:0.#}）が画面（{view.x:0.#}×{view.y:0.#}）より小さいので、その向きには動かず真ん中に止まります");
            }
        }
    }

    //=====================================================================
    // 歩ける範囲・ティンパニ
    //=====================================================================

    private static void CheckWalkAreaAndTimpani(Context ctx)
    {
        // 歩ける範囲：形があるか・出現位置や調べる物が範囲の中か
        List<WalkArea> areas = new List<WalkArea>(ctx.Find<WalkArea>());
        if(areas.Count > 1) { ctx.Add(Level.Warning, areas[1], "歩ける範囲（WalkArea）が2つ以上あります（使われるのは1つだけです）"); }

        WalkArea area = areas.Count > 0 ? areas[0] : null;
        if(area != null)
        {
            if(area.ShapeCount == 0)
            {
                ctx.Add(Level.Error, area, "歩ける範囲（WalkArea）に形がありません。子に Circle / Box / Polygon Collider 2D の付いた物を置いてください（このままだとどこへでも歩けます）");
            }
            else
            {
                foreach(PlayerSpawnPoint point in ctx.Find<PlayerSpawnPoint>())
                {
                    if(!area.ContainsPoint(point.transform.position))
                    {
                        ctx.Add(Level.Error, point, $"出現位置「{point.SpawnPointId}」が歩ける範囲の外にあります（出たとたんに範囲のふちへ動かされます）");
                    }
                }
                foreach(TimpaniPeg peg in ctx.Find<TimpaniPeg>())
                {
                    Collider2D col = peg.GetComponent<Collider2D>();
                    bool reachable = area.ContainsPoint(peg.transform.position) ||
                                     (col != null && area.ContainsPoint(col.bounds.ClosestPoint(peg.transform.position + Vector3.down * 10.0f)));
                    if(!reachable) { ctx.Add(Level.Warning, peg, "ペグが歩ける範囲の外にあります（近づけないかもしれません）"); }
                }
            }
        }

        // ティンパニ1台：真ん中の敵とペグがあるか
        foreach(TimpaniUnit unit in ctx.Find<TimpaniUnit>())
        {
            if(unit.GetComponentInChildren<Enemy>(true) == null && new SerializedObject(unit).FindProperty("creature").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, unit, "ティンパニ（TimpaniUnit）に真ん中の敵がありません（子に敵を置くか、「真ん中の敵」に入れてください）");
            }
            if(unit.GetComponentsInChildren<TimpaniPeg>(true).Length == 0 && new SerializedObject(unit).FindProperty("pegs").arraySize == 0)
            {
                ctx.Add(Level.Error, unit, "ティンパニ（TimpaniUnit）にペグがありません（子にペグのプレハブを置いてください）");
            }
        }

        // 部屋全体：クリアのフラグ・激しさのフラグ
        foreach(TimpaniRoom room in ctx.Find<TimpaniRoom>())
        {
            var so = new SerializedObject(room);
            if(so.FindProperty("clearedFlag").objectReferenceValue == null)
            {
                ctx.Add(Level.Warning, room, "ティンパニの部屋（TimpaniRoom）の「全部調律できたら立てるフラグ」が空です（戦闘の勝ち方・クリア後に戻ってきたときの姿に使います）");
            }

            SerializedProperty flags = so.FindProperty("escalationFlags");
            for(int i = 0; i < flags.arraySize; i++)
            {
                if(flags.GetArrayElementAtIndex(i).objectReferenceValue is GameFlag flag && flag.Saved)
                {
                    ctx.Add(Level.Info, room, $"激しさのフラグ「{flag.Id}」がセーブするフラグです（シーンが始まるたびに OFF に戻すので動きますが、セーブしないフラグにするのがおすすめです）");
                }
            }
        }
    }

    //=====================================================================
    // 仕掛け（謎解き）
    //=====================================================================

    private static void CheckGimmicks(Context ctx)
    {
        // 仕掛けの状態：セーブの名前が重なっていないか・どこからも見られていないか
        List<GimmickWiring.Edge> edges = null;
        var keys = new Dictionary<string, GimmickState>();
        foreach(GimmickState state in ctx.Find<GimmickState>())
        {
            var so = new SerializedObject(state);
            if(so.FindProperty("save").boolValue)
            {
                string key = GimmickState.MakeSaveKey(state.gameObject, so.FindProperty("saveId").stringValue);
                if(keys.TryGetValue(key, out GimmickState other)) { ctx.Add(Level.Error, state, $"仕掛けのセーブの名前が「{other.name}」と重なっています（同じ名前の物が同じ親の下にある）。セーブ用の ID を付けてください"); }
                else { keys[key] = state; }
            }

            if(so.FindProperty("initialValue").intValue >= Mathf.Max(2, so.FindProperty("stateCount").intValue))
            {
                ctx.Add(Level.Warning, state, "仕掛けの「最初の状態」が段階の数より大きいです（最後の段階になります）");
            }

            if(edges == null) { edges = GimmickWiring.Collect(); }
            bool used = false;
            foreach(GimmickWiring.Edge e in edges) { if(e.from == state) { used = true; break; } }
            if(!used) { ctx.Add(Level.Info, state, "この仕掛けの状態は、どの条件・答え合わせからも使われていません（扉などの「条件」に入れると動きます）"); }
        }

        foreach(GimmickReaction reaction in ctx.Find<GimmickReaction>())
        {
            if(reaction.Condition == null || reaction.Condition.IsEmpty)
            {
                ctx.Add(Level.Warning, reaction, "条件で動く物の「条件」が空です（ずっと満たさないので動きません）");
            }
            else if(reaction.Condition.gimmicks != null && reaction.Condition.gimmicks.Exists(g => g == null || g.target == null))
            {
                ctx.Add(Level.Error, reaction, "条件で動く物の「仕掛け」の条件に、仕掛けが入っていない行があります（満たさないので動きません）");
            }

            bool nothing = reaction.ShowWhenMet.Count == 0 && reaction.HideWhenMet.Count == 0 && reaction.OnMetSequence == null && reaction.Moves.Count == 0;
            if(nothing) { ctx.Add(Level.Warning, reaction, "条件で動く物に、表示を切り替える物もイベントも入っていません（満たしても何も起きません）"); }

            foreach(GameObject go in reaction.HideWhenMet)
            {
                if(go != null && reaction.transform.IsChildOf(go.transform))
                {
                    ctx.Add(Level.Error, reaction, $"条件で動く物が、自分（か親）の「{go.name}」を隠します。隠れると条件を見られなくなります（隠す物は子にしてください）");
                }
            }
        }

        foreach(PuzzleCheck puzzle in ctx.Find<PuzzleCheck>())
        {
            if(puzzle.Inputs.Count == 0 || ((List<GimmickState>)puzzle.Inputs).Exists(g => g == null))
            {
                ctx.Add(Level.Error, puzzle, "答え合わせの入力が空か、空の行があります");
            }
            if(puzzle.Mode == PuzzleMode.Pattern && puzzle.Answer.Count != puzzle.Inputs.Count)
            {
                ctx.Add(Level.Error, puzzle, $"答え合わせの「答え」の数（{puzzle.Answer.Count}）が、入力の数（{puzzle.Inputs.Count}）と合っていません");
            }
            if(puzzle.Mode == PuzzleMode.Order && puzzle.Inputs.Count < 2)
            {
                ctx.Add(Level.Warning, puzzle, "「決めた順番」の答え合わせに、入力が 1 つしかありません");
            }
        }

        foreach(GimmickSwitch sw in ctx.Find<GimmickSwitch>())
        {
            bool hasTrigger = false;
            foreach(Collider2D c in sw.GetComponents<Collider2D>()) { if(c.isTrigger) { hasTrigger = true; } }
            if(!hasTrigger) { ctx.Add(Level.Error, sw, "調べるスイッチに Is Trigger の Collider2D（調べる範囲）がありません"); }
        }

        foreach(GimmickEventAction action in ctx.Find<GimmickEventAction>())
        {
            if(action.Target == null) { ctx.Add(Level.Error, action, "「仕掛けを操作」の仕掛けが空です"); }
        }

        foreach(FloorSwitch floor in ctx.Find<FloorSwitch>())
        {
            bool hasTrigger = false;
            foreach(Collider2D c in floor.GetComponents<Collider2D>()) { if(c.isTrigger) { hasTrigger = true; } }
            if(!hasTrigger) { ctx.Add(Level.Error, floor, "床スイッチに Is Trigger の Collider2D（乗る範囲）がありません"); }
        }

        foreach(ItemSocket socket in ctx.Find<ItemSocket>())
        {
            if(socket.Item == null) { ctx.Add(Level.Error, socket, "置き台に「はめるアイテム」が入っていません"); }
            bool hasTrigger = false;
            foreach(Collider2D c in socket.GetComponents<Collider2D>()) { if(c.isTrigger) { hasTrigger = true; } }
            if(!hasTrigger) { ctx.Add(Level.Error, socket, "置き台に Is Trigger の Collider2D（調べる範囲）がありません"); }
        }

        foreach(GimmickHole hole in ctx.Find<GimmickHole>())
        {
            bool solid = false;
            foreach(Collider2D c in hole.GetComponents<Collider2D>()) { if(!c.isTrigger) { solid = true; } }
            if(!solid && new SerializedObject(hole).FindProperty("blocker").objectReferenceValue == null)
            {
                ctx.Add(Level.Warning, hole, "穴に当たり判定（Is Trigger オフの Collider2D）がありません（通れてしまい、箱も落ちません）");
            }
        }

        var blockKeys = new Dictionary<string, PushBlock>();
        foreach(PushBlock block in ctx.Find<PushBlock>())
        {
            BoxCollider2D col = block.GetComponent<BoxCollider2D>();
            if(col != null && col.isTrigger) { ctx.Add(Level.Error, block, "押せる箱の BoxCollider2D の Is Trigger がオンです（押せません）"); }

            Vector2 size = col != null ? Vector2.Scale(col.size, block.transform.lossyScale) : Vector2.zero;
            if(size.x > block.CellSize + 0.01f || size.y > block.CellSize + 0.01f)
            {
                ctx.Add(Level.Warning, block, "押せる箱の当たり判定が「マスの大きさ」より大きいです（1 マス先がいつもふさがって押せないことがあります）");
            }

            var so = new SerializedObject(block);
            if(so.FindProperty("save").boolValue)
            {
                string id = so.FindProperty("saveId").stringValue;
                string path = block.name;
                for(Transform p = block.transform.parent; p != null; p = p.parent) { path = p.name + "/" + path; }
                string key = block.gameObject.scene.name + "/" + (string.IsNullOrEmpty(id) ? path : id);
                if(blockKeys.TryGetValue(key, out PushBlock other)) { ctx.Add(Level.Error, block, $"押せる箱のセーブの名前が「{other.name}」と重なっています（同じ名前の箱が同じ親の下にある）。セーブ用の ID を付けてください"); }
                else { blockKeys[key] = block; }
            }
        }

        // 2人の仕掛け
        bool usesHints = false;
        foreach(CharacterBarrier barrier in ctx.Find<CharacterBarrier>())
        {
            usesHints = true;
            bool solid = false;
            foreach(Collider2D c in barrier.GetComponentsInChildren<Collider2D>()) { if(!c.isTrigger) { solid = true; } }
            if(!solid) { ctx.Add(Level.Error, barrier, "キャラによって通れる壁に、当たり判定（Is Trigger オフの Collider2D）がありません"); }
            if(barrier.Passable == CharacterRestriction.Both) { ctx.Add(Level.Warning, barrier, "キャラによって通れる壁の「通れるキャラ」が「両方」です（ただの通れる場所になります）"); }
        }

        foreach(BreakableWall wall in ctx.Find<BreakableWall>())
        {
            usesHints |= wall.Breaker != CharacterRestriction.Both;
            bool solid = false;
            foreach(Collider2D c in wall.GetComponents<Collider2D>()) { if(!c.isTrigger) { solid = true; } }
            if(!solid) { ctx.Add(Level.Warning, wall, "壊せる壁に当たり判定（Is Trigger オフの Collider2D）がありません（通れてしまいます）"); }
        }

        foreach(LiftPoint lift in ctx.Find<LiftPoint>())
        {
            if(lift.Destination == null) { ctx.Add(Level.Error, lift, "持ち上げる場所の「行き先」が空です"); }
            bool hasTrigger = false;
            foreach(Collider2D c in lift.GetComponents<Collider2D>()) { if(c.isTrigger) { hasTrigger = true; } }
            if(!hasTrigger) { ctx.Add(Level.Error, lift, "持ち上げる場所に Is Trigger の Collider2D（調べる範囲）がありません"); }
        }

        // 謎解き（段階5）
        foreach(CodeLock code in ctx.Find<CodeLock>())
        {
            if(string.IsNullOrEmpty(code.Answer)) { ctx.Add(Level.Error, code, "番号錠の「答え」が空です"); }
            else
            {
                foreach(char ch in code.Answer)
                {
                    if(code.Symbols.IndexOf(ch) < 0) { ctx.Add(Level.Error, code, $"番号錠の答えの「{ch}」が、使える文字（{code.Symbols}）にありません（正解できません）"); break; }
                }
            }
        }

        foreach(NotePickup note in ctx.Find<NotePickup>())
        {
            if(note.Note == null) { ctx.Add(Level.Error, note, "メモ（NotePickup）に NoteData が入っていません"); }
            else if(!AssetDatabase.GetAssetPath(note.Note).StartsWith("Assets/Resources/Notes/")) { ctx.Add(Level.Warning, note, $"メモ「{note.Note.name}」が Assets/Resources/Notes/ の外にあります（メニューの「メモ」に出ません）"); }
        }
        foreach(NoteEventAction action in ctx.Find<NoteEventAction>())
        {
            if(action.Note == null) { ctx.Add(Level.Error, action, "「メモを渡す」に NoteData が入っていません"); }
        }

        // 相棒の一言
        foreach(BarkArea area in ctx.Find<BarkArea>())
        {
            if(area.Data == null) { ctx.Add(Level.Error, area, "一言の範囲に一言のデータが入っていません"); }
            Collider2D c = area.GetComponent<Collider2D>();
            if(c != null && !c.isTrigger) { ctx.Add(Level.Error, area, "一言の範囲の Collider2D が Is Trigger になっていません（ぶつかる壁になります）"); }
        }
        foreach(BarkEventAction action in ctx.Find<BarkEventAction>())
        {
            if(!action.HasContent) { ctx.Add(Level.Error, action, "イベントの部品「一言」に、データも文も入っていません"); }
        }

        foreach(ExamineSequence exam in ctx.Find<ExamineSequence>())
        {
            if(exam.Sequences.Count == 0) { ctx.Add(Level.Warning, exam, "調べるたびに変わる に、イベントが入っていません"); }
        }

        bool hasRetryPoint = ctx.Find<RetryPoint>().Count > 0;
        foreach(DeathTrap trap in ctx.Find<DeathTrap>())
        {
            Collider2D c = trap.GetComponent<Collider2D>();
            if(c != null && !c.isTrigger) { ctx.Add(Level.Error, trap, "即死の罠の Collider2D の Is Trigger がオフです（壁になって入れません）"); }
            if(trap.Result == DeathResult.Retry && !hasRetryPoint) { ctx.Add(Level.Info, trap, "このシーンに「やり直す位置」がないので、即死したらセーブの再開する位置（シーンの入口・セーブポイント）からやり直します"); }
        }

        foreach(RetryPoint retry in ctx.Find<RetryPoint>())
        {
            Collider2D c = retry.GetComponent<Collider2D>();
            if(c != null && !c.isTrigger) { ctx.Add(Level.Error, retry, "やり直す位置の Collider2D の Is Trigger がオフです"); }
        }

        foreach(PartyRejoinArea rejoin in ctx.Find<PartyRejoinArea>())
        {
            Collider2D c = rejoin.GetComponent<Collider2D>();
            if(c != null && !c.isTrigger) { ctx.Add(Level.Error, rejoin, "合流の範囲の Collider2D の Is Trigger がオフです（キャラが入れません）"); }
        }

        var cameraAreas = ctx.Find<CameraArea>();
        foreach(CameraArea area in cameraAreas)
        {
            BoxCollider2D box = area.GetComponent<BoxCollider2D>();
            if(box != null && !box.isTrigger) { ctx.Add(Level.Warning, area, "カメラの範囲の BoxCollider2D の Is Trigger がオフです（再生すると自動でオンにしますが、編集中は壁になって見えます）"); }

            // 小さすぎる範囲（カメラに映る広さより小さいと、真ん中に固定される）
            Camera cam = Camera.main;
            if(cam != null && box != null && cam.orthographic)
            {
                float h = cam.orthographicSize * 2.0f;
                float w = h * cam.aspect;
                Vector2 size = Vector2.Scale(box.size, area.transform.lossyScale);
                if(size.x < w - 0.01f || size.y < h - 0.01f) { ctx.Add(Level.Info, area, $"カメラの範囲（{size.x:0.#}×{size.y:0.#}）が画面の広さ（{w:0.#}×{h:0.#}）より小さいので、中ではカメラが動きません（真ん中に固定）"); }
            }
        }

        foreach(ConditionalVisible visible in ctx.Find<ConditionalVisible>())
        {
            if(visible.Condition == null || visible.Condition.IsEmpty) { ctx.Add(Level.Warning, visible, "条件で見える物の「見える条件」が空です（いつも見えます）"); }
        }

        foreach(GimmickSwitch sw in ctx.Find<GimmickSwitch>()) { usesHints |= sw.User != CharacterRestriction.Both; }
        foreach(PushBlock block in ctx.Find<PushBlock>()) { usesHints |= block.User != CharacterRestriction.Both; }
        if(usesHints && AssetDatabase.LoadAssetAtPath<PartyHintSettings>("Assets/Resources/PartyHints.asset") == null)
        {
            ctx.Add(Level.Info, null, "使えるキャラが決まった仕掛けがありますが、相棒の目印の設定（Resources/PartyHints.asset）がないので目印が出ません（Tools > 仕掛け > 相棒の目印の設定を作る）");
        }

        foreach(GimmickVisual visual in ctx.Find<GimmickVisual>())
        {
            if(visual.State == null && visual.GetComponentInParent<GimmickState>() == null)
            {
                ctx.Add(Level.Error, visual, "状態で見た目を変える部品に、仕掛けの状態がありません（同じオブジェクトか親に GimmickState、または欄に入れる）");
            }
        }
    }

    private static void CheckEnemies(Context ctx)
    {
        int enemiesLayer = LayerMask.NameToLayer("Enemies");

        foreach(Enemy enemy in ctx.Find<Enemy>())
        {
            var so = new SerializedObject(enemy);
            if(so.FindProperty("data").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, enemy, "敵のデータ（EnemyData）が入っていません");
            }

            if(!enemy.CompareTag("Enemies") || enemy.gameObject.layer != enemiesLayer)
            {
                ctx.Add(Level.Warning, enemy, "タグかレイヤーが「Enemies」になっていません（ノアの範囲攻撃・シグルの弾が当たりません）");
            }

            if(enemy.GetComponentInChildren<Collider2D>(true) == null)
            {
                ctx.Add(Level.Error, enemy, "Collider2D がありません（攻撃が当たらず、壁もすり抜けます）");
            }

            // 行動パターン
            EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
            var bso = brain != null ? new SerializedObject(brain) : null;
            var overridePattern = bso != null ? bso.FindProperty("overridePattern").objectReferenceValue as EnemyPattern : null;
            EnemyPattern pattern = overridePattern != null ? overridePattern : (enemy.Data != null ? enemy.Data.Pattern : null);

            if(brain == null)
            {
                ctx.Add(Level.Info, enemy, "EnemyBrain がないので、行動パターンを実行しません（その場に立っているだけ）");
            }
            else if(pattern == null)
            {
                ctx.Add(Level.Info, enemy, "行動パターンがありません（その場に立っているだけ）");
            }
            else if(UsesPatrol(pattern) && brain.PatrolPointCount == 0)
            {
                ctx.Add(Level.Warning, enemy, $"行動パターン「{pattern.name}」に「巡回する」がありますが、EnemyBrain の巡回ポイントが空です");
            }
        }

        var enemyKeys = new Dictionary<string, Enemy>();
        foreach(Enemy enemy in ctx.Find<Enemy>())
        {
            var so = new SerializedObject(enemy);
            if(!so.FindProperty("saveDefeat").boolValue) { continue; }
            string id = so.FindProperty("saveId").stringValue;
            string key = string.IsNullOrEmpty(id) ? HierarchyPath(enemy.transform) : id;
            if(enemyKeys.ContainsKey(key)) { ctx.Add(Level.Error, enemy, $"倒したことをセーブする敵のセーブ用の名前「{key}」が重なっています（ID を付けてください）"); }
            else                           { enemyKeys.Add(key, enemy); }
        }
        foreach(Enemy enemy in ctx.Find<Enemy>())
        {
            var so = new SerializedObject(enemy);
            if(so.FindProperty("afterTalk").objectReferenceValue != null && enemy.Data != null && enemy.Data.DefeatStyle == DefeatStyle.Vanish)
            {
                ctx.Add(Level.Warning, enemy, "「倒した後に調べたとき」が入っていますが、敵のデータの「倒されたあと」が「消える」なので調べられません");
            }
        }

        var groupKeys = new Dictionary<string, EnemyGroup>();
        foreach(EnemyGroup group in ctx.Find<EnemyGroup>())
        {
            var so = new SerializedObject(group);
            SerializedProperty members = so.FindProperty("members");
            if(members.arraySize == 0) { ctx.Add(Level.Error, group, "敵のグループのメンバーが空です"); }
            else if(HasNullElement(members)) { ctx.Add(Level.Warning, group, "敵のグループのメンバーに空の欄があります（倒したことになります）"); }
            if(so.FindProperty("useOrder").boolValue && members.arraySize < 2) { ctx.Add(Level.Info, group, "「順番に倒す」がオンですが、メンバーが 1 体以下です"); }
            if(so.FindProperty("onDefeated").objectReferenceValue == null && so.FindProperty("flag").objectReferenceValue == null && !so.FindProperty("useOrder").boolValue)
            {
                ctx.Add(Level.Warning, group, "敵のグループに、実行するイベントも ON にするフラグもありません（倒しても何も起きません）");
            }
            string key = EnemyGroup.MakeSaveKey(group.gameObject, so.FindProperty("saveId").stringValue);
            if(groupKeys.ContainsKey(key)) { ctx.Add(Level.Error, group, $"敵のグループのセーブ用の名前「{key}」が重なっています（ID を付けてください）"); }
            else                           { groupKeys.Add(key, group); }
        }

        foreach(EnemyPart part in ctx.Find<EnemyPart>())
        {
            if(part.GetComponentInParent<Enemy>(true) == null)
            {
                ctx.Add(Level.Error, part, "部位（EnemyPart）の親に敵（Enemy）がありません（部位として働きません）");
            }
            var so = new SerializedObject(part);
            if(so.FindProperty("partHealth").intValue <= 0 &&
               (so.FindProperty("independent").objectReferenceValue != null || so.FindProperty("changePhase").intValue > 0 || so.FindProperty("disableAttacks").arraySize > 0 || so.FindProperty("onBreak").objectReferenceValue != null))
            {
                ctx.Add(Level.Warning, part, "部位の「壊れたとき」の設定がありますが、部位の体力が 0（壊れない）です");
            }
        }

        foreach(AttackEmitter emitter in ctx.Find<AttackEmitter>())
        {
            var so = new SerializedObject(emitter);
            if(so.FindProperty("emitterPattern").objectReferenceValue == null)
            {
                ctx.Add(Level.Warning, emitter, "攻撃の発生源に行動パターンが入っていません（何もしません）");
            }
            var area = so.FindProperty("condition").FindPropertyRelative("area").objectReferenceValue as Collider2D;
            if(area != null && !area.isTrigger)
            {
                ctx.Add(Level.Warning, emitter, "攻撃の発生源の「プレイヤーがいる範囲」の Collider2D の Is Trigger がオフです（プレイヤーが通れません）");
            }
        }

        foreach(EnemyDataSpawner spawner in ctx.Find<EnemyDataSpawner>())
        {
            var so = new SerializedObject(spawner);
            SerializedProperty list = so.FindProperty("enemies");
            bool any = false;
            for(int i = 0; i < list.arraySize; i++)
            {
                if(list.GetArrayElementAtIndex(i).FindPropertyRelative("enemy").objectReferenceValue != null) { any = true; }
            }
            if(!any) { ctx.Add(Level.Error, spawner, "敵のスポナーに、出す敵（敵のデータ）が入っていません"); }

            var area = so.FindProperty("condition").FindPropertyRelative("area").objectReferenceValue as Collider2D;
            if(area != null && !area.isTrigger)
            {
                ctx.Add(Level.Warning, spawner, "敵のスポナーの「プレイヤーがいる範囲」の Collider2D の Is Trigger がオフです（プレイヤーが通れません）");
            }
            if(so.FindProperty("totalCount").intValue == 0 && (so.FindProperty("onCleared").objectReferenceValue != null || so.FindProperty("clearedFlag").objectReferenceValue != null))
            {
                ctx.Add(Level.Warning, spawner, "敵のスポナーの「全部倒したら」が設定されていますが、「合計で出す数」が 0（何体でも出す）なので起きません");
            }
        }

        var switchKeys = new Dictionary<string, AttackSwitch>();
        foreach(AttackSwitch sw in ctx.Find<AttackSwitch>())
        {
            var so = new SerializedObject(sw);
            if(so.FindProperty("sequence").objectReferenceValue == null)
            {
                ctx.Add(Level.Warning, sw, "攻撃で作動するスイッチに「実行するイベント」が入っていません（当たっても何も起きません）");
            }
            string key = AttackSwitch.MakeSaveKey(sw.gameObject, so.FindProperty("saveId").stringValue);
            if(switchKeys.ContainsKey(key)) { ctx.Add(Level.Error, sw, $"攻撃で作動するスイッチのセーブ用の名前「{key}」が重なっています（ID を付けてください）"); }
            else                            { switchKeys.Add(key, sw); }
        }

        foreach(EnemyCommandEventAction action in ctx.Find<EnemyCommandEventAction>())
        {
            var so = new SerializedObject(action);
            if(so.FindProperty("enemies").arraySize == 0 && so.FindProperty("emitters").arraySize == 0)
            {
                ctx.Add(Level.Warning, action, "「敵に命令」の相手（敵・攻撃の発生源）が空です");
            }
            else if(HasNullElement(so.FindProperty("enemies")) || HasNullElement(so.FindProperty("emitters")))
            {
                ctx.Add(Level.Warning, action, "「敵に命令」の相手に空の欄があります");
            }
        }

        foreach(EnemyDamageEventAction action in ctx.Find<EnemyDamageEventAction>())
        {
            var so = new SerializedObject(action);
            if(so.FindProperty("enemies").arraySize == 0 && so.FindProperty("parts").arraySize == 0)
            {
                ctx.Add(Level.Warning, action, "「敵にダメージ」の相手（敵・部分）が空です");
            }
        }

        var battleKeys = new Dictionary<string, BattleArea>();
        foreach(BattleArea battle in ctx.Find<BattleArea>())
        {
            var so = new SerializedObject(battle);
            var startType = (BattleStart)so.FindProperty("start").enumValueIndex;
            var winType = (BattleWin)so.FindProperty("win").enumValueIndex;
            var loseType = (BattleLose)so.FindProperty("lose").enumValueIndex;
            int enemyCount = so.FindProperty("enemies").arraySize;

            Collider2D area = battle.GetComponent<Collider2D>();
            if(startType == BattleStart.EnterArea && area != null && !area.isTrigger)
            {
                ctx.Add(Level.Error, battle, "戦闘エリアの Collider2D の Is Trigger がオフです（入れないので始まりません）");
            }

            var goal = so.FindProperty("goal").objectReferenceValue as Collider2D;
            if(winType == BattleWin.ReachGoal && goal == null) { ctx.Add(Level.Error, battle, "勝ち方が「ゴールに着く」ですが、ゴールが空です"); }
            if(winType == BattleWin.ReachGoal && goal != null && !goal.isTrigger) { ctx.Add(Level.Warning, battle, "ゴールの Collider2D の Is Trigger がオフです（ゴールに入れません）"); }
            if(winType == BattleWin.Flag && so.FindProperty("winFlag").objectReferenceValue == null) { ctx.Add(Level.Error, battle, "勝ち方が「フラグが立つ」ですが、フラグが空です"); }
            int waveCount = so.FindProperty("waves").arraySize;
            if(winType == BattleWin.DefeatAll && enemyCount == 0 && waveCount == 0) { ctx.Add(Level.Error, battle, "勝ち方が「敵を全部倒す」ですが、参加する敵もウェーブも空です"); }
            for(int w = 0; w < waveCount; w++)
            {
                SerializedProperty wave = so.FindProperty("waves").GetArrayElementAtIndex(w);
                if(wave.FindPropertyRelative("enemies").arraySize == 0 && wave.FindPropertyRelative("spawns").arraySize == 0)
                {
                    ctx.Add(Level.Warning, battle, $"戦闘エリアのウェーブ {w + 1} に敵がいません（すぐ次のウェーブになります）");
                }
            }
            if(loseType == BattleLose.Caught && enemyCount == 0) { ctx.Add(Level.Warning, battle, "負け方が「捕まる」ですが、参加する敵が空です（捕まりません）"); }
            if(HasNullElement(so.FindProperty("enemies")) || HasNullElement(so.FindProperty("emitters"))) { ctx.Add(Level.Warning, battle, "戦闘エリアの参加する敵・攻撃の発生源に空の欄があります"); }
            if(winType == BattleWin.None && loseType == BattleLose.None) { ctx.Add(Level.Info, battle, "戦闘エリアの勝ち方・負け方がどちらも「なし」です（イベントの部品で終える前提）"); }

            string key = BattleArea.MakeSaveKey(battle.gameObject, so.FindProperty("saveId").stringValue);
            if(battleKeys.ContainsKey(key)) { ctx.Add(Level.Error, battle, $"戦闘エリアのセーブ用の名前「{key}」が重なっています（ID を付けてください）"); }
            else                            { battleKeys.Add(key, battle); }
        }

        foreach(BattleControlEventAction action in ctx.Find<BattleControlEventAction>())
        {
            if(new SerializedObject(action).FindProperty("battle").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, action, "「戦闘エリアを操作」に戦闘エリアが入っていません");
            }
        }

        foreach(DamageAreaEventAction action in ctx.Find<DamageAreaEventAction>())
        {
            var so = new SerializedObject(action);
            if(so.FindProperty("attack").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, action, "「ダメージエリアを出す」に攻撃が入っていません");
            }
        }

        foreach(EnemyNavArea area in ctx.Find<EnemyNavArea>())
        {
            Collider2D col = area.GetComponent<Collider2D>();
            if(col != null && !col.isTrigger)
            {
                ctx.Add(Level.Warning, area, "敵の通行設定エリアの Collider2D の Is Trigger がオフです（プレイヤーも通れなくなります）");
            }
        }
    }

    private static void CheckSceneChanges(Context ctx)
    {
        foreach(SceneChange change in ctx.Find<SceneChange>())
        {
            var so = new SerializedObject(change);
            string sceneName = so.FindProperty("sceneName").stringValue;
            string spawnId = so.FindProperty("spawnPointId").stringValue;

            if(string.IsNullOrEmpty(sceneName))
            {
                ctx.Add(Level.Info, change, "SceneChange の遷移先シーンが空です（ボタンなどから ChangeScene を呼ぶ場合は問題ありません）");
                continue;
            }

            if(!SceneLookup.IsInBuild(sceneName))
            {
                ctx.Add(Level.Error, change, $"遷移先シーン「{sceneName}」が Build Settings に入っていません（遷移できません）");
            }

            if(!string.IsNullOrEmpty(spawnId))
            {
                string path = SceneLookup.FindScenePath(sceneName);
                if(path != null && !SceneLookup.SpawnPointIds(path).Contains(spawnId))
                {
                    ctx.Add(Level.Error, change, $"出現位置「{spawnId}」が「{sceneName}」にありません（デフォルトの位置に出ます）");
                }
            }

            if(change.GetComponent<Collider2D>() == null)
            {
                ctx.Add(Level.Info, change, "SceneChange に Collider2D がありません（ぶつかっても遷移しません。ボタンなどから呼ぶ場合は問題ありません）");
            }
        }
    }

    private static void CheckSpawnPoints(Context ctx)
    {
        var seen = new Dictionary<string, PlayerSpawnPoint>();
        foreach(PlayerSpawnPoint point in ctx.Find<PlayerSpawnPoint>())
        {
            string id = point.SpawnPointId;
            if(string.IsNullOrEmpty(id))
            {
                ctx.Add(Level.Warning, point, "PlayerSpawnPoint の ID が空です（どこからも使えません）");
                continue;
            }

            if(seen.ContainsKey(id)) { ctx.Add(Level.Error, point, $"出現位置 ID「{id}」が重なっています（どちらに出るか分かりません）"); }
            else                     { seen.Add(id, point); }
        }
    }

    private static void CheckInteractables(Context ctx)
    {
        // イベントの部品「会話」（DialogueEventAction）から使われている Dialogue
        var usedByEvent = new HashSet<Object>();
        foreach(DialogueEventAction action in ctx.Find<DialogueEventAction>())
        {
            Object dialogue = new SerializedObject(action).FindProperty("dialogue").objectReferenceValue;
            if(dialogue != null) { usedByEvent.Add(dialogue); }
        }

        foreach(Intaractable target in ctx.Find<Intaractable>())
        {
            // DialogueEventAction から呼ぶための Dialogue は、調べる範囲がなくてよい
            if(target is Dialogue && (target.GetComponent<EventAction>() != null || usedByEvent.Contains(target))) { continue; }

            if(!HasTrigger(target.gameObject))
            {
                ctx.Add(Level.Error, target, $"{target.GetType().Name} に Is Trigger をオンにした Collider2D がありません（調べられません）");
            }
        }
    }

    private static void CheckTreasures(Context ctx)
    {
        var seen = new HashSet<string>();
        foreach(Treasure treasure in ctx.Find<Treasure>())
        {
            var so = new SerializedObject(treasure);
            string id = so.FindProperty("id").stringValue;

            if(string.IsNullOrEmpty(id))      { ctx.Add(Level.Error, treasure, "宝箱の Id が空です（他の宝箱と開いた記録が混ざります）"); }
            else if(!seen.Add(id))            { ctx.Add(Level.Error, treasure, $"宝箱の Id「{id}」が重なっています（片方を開けると両方開きます）"); }

            if(so.FindProperty("beforeRenderer").objectReferenceValue == null &&
               so.FindProperty("openAnimator").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, treasure, "宝箱の Before Renderer が空です（開けたときにエラーになります）");
            }
        }
    }

    private static void CheckPickups(Context ctx)
    {
        var seen = new HashSet<string>();
        foreach(ItemPickup pickup in ctx.Find<ItemPickup>())
        {
            var so = new SerializedObject(pickup);
            if(so.FindProperty("item").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, pickup, "ItemPickup の Item が空です");
            }

            string key = ItemPickup.MakeSaveKey(pickup.gameObject, so.FindProperty("pickupId").stringValue);
            if(!seen.Add(key))
            {
                ctx.Add(Level.Error, pickup, "同じ名前の ItemPickup があります（片方を拾うと両方消えます）。Pickup Id に別々の名前を入れてください");
            }
        }
    }

    private static void CheckItemInteractions(Context ctx)
    {
        var seen = new HashSet<string>();
        foreach(ItemInteraction interaction in ctx.Find<ItemInteraction>())
        {
            var so = new SerializedObject(interaction);

            if(so.FindProperty("saveCompleted").boolValue)
            {
                string id = so.FindProperty("saveId").stringValue;
                string key = string.IsNullOrEmpty(id) ? HierarchyPath(interaction.transform) : id;
                if(!seen.Add(key))
                {
                    ctx.Add(Level.Error, interaction, "同じ名前の ItemInteraction があります（完了状態が混ざります）。Save Id に別々の名前を入れてください");
                }
            }

            SerializedProperty rules = so.FindProperty("rules");
            if(rules.arraySize == 0 && so.FindProperty("otherwise").objectReferenceValue == null)
            {
                ctx.Add(Level.Warning, interaction, "ItemInteraction にルールも Otherwise もありません（何も起きません）");
            }

            for(int i = 0; i < rules.arraySize; i++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                string label = rule.FindPropertyRelative("label").stringValue;
                string name = $"ルール{i + 1}" + (string.IsNullOrEmpty(label) ? "" : $"「{label}」");

                if(rule.FindPropertyRelative("sequence").objectReferenceValue == null)
                {
                    ctx.Add(Level.Warning, interaction, $"{name} の Sequence が空です（条件を満たしても何も起きません）");
                }

                if(HasNullRef(rule.FindPropertyRelative("conditions"), "item"))
                {
                    ctx.Add(Level.Warning, interaction, $"{name} に Item が空の条件があります（常に満たす扱いになります）");
                }

                if(HasNullRef(rule.FindPropertyRelative("flagConditions"), "flag"))
                {
                    ctx.Add(Level.Warning, interaction, $"{name} に Flag が空の条件があります（常に満たす扱いになります）");
                }
            }
        }
    }

    private static void CheckEventZones(Context ctx)
    {
        foreach(EventZone zone in ctx.Find<EventZone>())
        {
            var so = new SerializedObject(zone);
            if(so.FindProperty("eventSequence").objectReferenceValue == null)
            {
                ctx.Add(Level.Error, zone, "EventZone の Event Sequence が空です（入っても何も起きません）");
            }

            if(!HasTrigger(zone.gameObject))
            {
                ctx.Add(Level.Error, zone, "EventZone に Is Trigger をオンにした Collider2D がありません");
            }
        }
    }

    private static void CheckEventSequences(Context ctx)
    {
        foreach(EventSequence sequence in ctx.Find<EventSequence>())
        {
            var so = new SerializedObject(sequence);
            SerializedProperty actions = so.FindProperty("actions");
            bool sequential = so.FindProperty("playMode").enumValueIndex == (int)EventSequence.PlayMode.Sequential;

            var registered = new List<EventAction>();
            bool hasNull = false;
            for(int i = 0; i < actions.arraySize; i++)
            {
                var action = actions.GetArrayElementAtIndex(i).objectReferenceValue as EventAction;
                if(action == null) { hasNull = true; } else { registered.Add(action); }
            }

            EventAction[] own = sequence.GetComponents<EventAction>();

            if(actions.arraySize == 0)
            {
                if(own.Length == 0) { ctx.Add(Level.Warning, sequence, "EventSequence に実行する部品がありません"); }
                else                { registered.AddRange(own); }
            }
            else
            {
                foreach(EventAction action in own)
                {
                    if(!registered.Contains(action))
                    {
                        ctx.Add(Level.Warning, sequence, $"{action.GetType().Name} が付いているのに Actions に入っていません（実行されません）");
                    }
                }
            }

            if(hasNull) { ctx.Add(Level.Warning, sequence, "EventSequence の Actions に空の欄があります"); }

            // 順番に実行中に、自分（か親）を非表示にすると途中で止まる
            if(!sequential) { continue; }

            for(int i = 0; i < registered.Count; i++)
            {
                if(!(registered[i] is SetActiveEventAction setActive)) { continue; }
                if(i == registered.Count - 1) { continue; }  // 最後なら後ろに止まるものがない

                var aso = new SerializedObject(setActive);
                SerializedProperty hide = aso.FindProperty("hide");
                for(int j = 0; j < hide.arraySize; j++)
                {
                    var go = hide.GetArrayElementAtIndex(j).objectReferenceValue as GameObject;
                    if(go != null && sequence.transform.IsChildOf(go.transform))
                    {
                        ctx.Add(Level.Error, sequence, $"SetActiveEventAction が EventSequence 自身（または親）の「{go.name}」を非表示にするため、後ろの部品が実行されません。最後に移すか、別のオブジェクトに分けてください");
                    }
                }
            }
        }
    }

    private static void CheckActions(Context ctx)
    {
        foreach(DialogueEventAction action in ctx.Find<DialogueEventAction>())
        {
            if(!action.HasDialogue)
            {
                ctx.Add(Level.Error, action, "DialogueEventAction の「会話データ」が空です（会話が出ません）");
            }
        }

        foreach(SetFlagEventAction action in ctx.Find<SetFlagEventAction>())
        {
            SerializedProperty flags = new SerializedObject(action).FindProperty("flags");
            if(flags.arraySize == 0)               { ctx.Add(Level.Warning, action, "SetFlagEventAction にフラグがありません"); }
            else if(HasNullRef(flags, "flag"))     { ctx.Add(Level.Warning, action, "SetFlagEventAction に Flag が空の欄があります"); }
        }
    }

    private static void CheckSwitches(Context ctx)
    {
        foreach(FlagSwitch sw in ctx.Find<FlagSwitch>())
        {
            var so = new SerializedObject(sw);
            CheckSwitchTargets(ctx, sw, so);
            if(HasNullRef(so.FindProperty("flagConditions"), "flag")) { ctx.Add(Level.Warning, sw, "FlagSwitch に Flag が空の条件があります"); }
            if(HasNullRef(so.FindProperty("itemConditions"), "item")) { ctx.Add(Level.Warning, sw, "FlagSwitch に Item が空の条件があります"); }
        }

        foreach(ItemPossessionSwitch sw in ctx.Find<ItemPossessionSwitch>())
        {
            var so = new SerializedObject(sw);
            CheckSwitchTargets(ctx, sw, so);
            if(HasNullRef(so.FindProperty("conditions"), "item")) { ctx.Add(Level.Warning, sw, "ItemPossessionSwitch に Item が空の条件があります"); }
        }
    }

    private static void CheckSwitchTargets(Context ctx, Component sw, SerializedObject so)
    {
        SerializedProperty met = so.FindProperty("activeWhenMet");
        SerializedProperty notMet = so.FindProperty("activeWhenNotMet");

        if(met.arraySize == 0 && notMet.arraySize == 0)
        {
            ctx.Add(Level.Warning, sw, $"{sw.GetType().Name} に切り替えるオブジェクトがありません");
        }

        if(ContainsRef(met, sw.gameObject) || ContainsRef(notMet, sw.gameObject))
        {
            ctx.Add(Level.Error, sw, $"{sw.GetType().Name} の切り替え対象に自分自身が入っています（非表示になると切り替えが止まります）");
        }
    }

    private static void CheckDialogues(Context ctx)
    {
        List<Dialogue> dialogues = ctx.Find<Dialogue>();
        foreach(Dialogue dialogue in dialogues)
        {
            var so = new SerializedObject(dialogue);
            if(so.FindProperty("dialogueData").objectReferenceValue == null &&
               string.IsNullOrEmpty(so.FindProperty("dialogueText").stringValue))
            {
                ctx.Add(Level.Warning, dialogue, "Dialogue に会話データも Dialogue Text もありません（何も表示されません）");
            }
        }

        if(dialogues.Count > 0 && ctx.Find<DialogueManager>().Count == 0)
        {
            ctx.Add(Level.Warning, null, "シーンに DialogueManager がないため、会話が旧方式（1文だけ・立ち絵なし・閉じるまで待てない）になります。Assets/Prefab/UI/DialogueSystem.prefab を置いてください");
        }
    }

    //=====================================================================
    // プロジェクト全体のチェック
    //=====================================================================

    private static bool UsesPatrol(EnemyPattern pattern)
    {
        foreach(EnemyPhase phase in pattern.phases)
        {
            if(phase == null || phase.actions == null) { continue; }
            foreach(EnemyAction action in phase.actions) { if(action is PatrolAction) { return true; } }
        }
        return false;
    }

    /// <summary>
    /// 敵の行動パターン・攻撃のデータのチェック
    /// </summary>
    private static void CheckEnemyAssets(List<Issue> issues)
    {
        foreach(string guid in AssetDatabase.FindAssets("t:EnemyPattern"))
        {
            var pattern = AssetDatabase.LoadAssetAtPath<EnemyPattern>(AssetDatabase.GUIDToAssetPath(guid));
            if(pattern == null) { continue; }

            for(int i = 0; i < pattern.phases.Count; i++)
            {
                EnemyPhase phase = pattern.phases[i];
                if(phase == null) { continue; }

                if(phase.actions == null || phase.actions.Count == 0)
                {
                    issues.Add(new Issue { level = Level.Warning, target = pattern, message = $"行動パターン「{pattern.name}」の段階 {i + 1}「{phase.name}」に行動がありません（その段階では立っているだけ）" });
                    continue;
                }

                foreach(EnemyAction action in phase.actions)
                {
                    if(action == null)
                    {
                        issues.Add(new Issue { level = Level.Error, target = pattern, message = $"行動パターン「{pattern.name}」の段階 {i + 1} に、中身が消えた行動があります（クラス名を変えた・消した）" });
                    }
                    else if(action is AttackAction attack && attack.attack == null)
                    {
                        issues.Add(new Issue { level = Level.Error, target = pattern, message = $"行動パターン「{pattern.name}」の段階 {i + 1} の「攻撃する」に、攻撃が入っていません" });
                    }
                    else if(action is SummonAction summon && summon.enemy == null)
                    {
                        issues.Add(new Issue { level = Level.Error, target = pattern, message = $"行動パターン「{pattern.name}」の段階 {i + 1} の「仲間を呼ぶ」に、呼ぶ敵が入っていません" });
                    }
                    else if(action is SummonAction summon2 && summon2.enemy != null && summon2.enemy.Pattern == pattern)
                    {
                        issues.Add(new Issue { level = Level.Warning, target = pattern, message = $"行動パターン「{pattern.name}」は、同じ行動パターンの敵を呼びます（呼ばれた敵もまた呼ぶので、「同時にいられる数」に気をつけてください）" });
                    }
                }
            }
        }

        foreach(string guid in AssetDatabase.FindAssets("t:AttackData"))
        {
            var attack = AssetDatabase.LoadAssetAtPath<AttackData>(AssetDatabase.GUIDToAssetPath(guid));
            if(attack == null) { continue; }

            if(attack.activation == AttackActivation.Instant && attack.motion == AttackMotion.Charge && attack.shape.type != AttackShapeType.Rectangle)
            {
                issues.Add(new Issue { level = Level.Warning, target = attack, message = $"攻撃「{attack.name}」は突進ですが、形が長方形ではありません（長方形の長さが走る距離になります）" });
            }
            if(attack.damage <= 0)
            {
                issues.Add(new Issue { level = Level.Info, target = attack, message = $"攻撃「{attack.name}」のダメージが 0 です（当たっても何も起きません）" });
            }
        }
    }

    /// <summary> 相棒の一言のデータ </summary>
    private static void CheckBarkAssets(List<Issue> issues)
    {
        var names = new Dictionary<string, string>();
        foreach(string guid in AssetDatabase.FindAssets("t:BarkData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BarkData d = AssetDatabase.LoadAssetAtPath<BarkData>(path);
            if(d == null) { continue; }

            if(!path.StartsWith("Assets/Resources/Barks/"))
            {
                issues.Add(new Issue { level = Level.Error, target = d, message = $"一言「{d.name}」が Assets/Resources/Barks/ の外にあります（きっかけで自動に出ません）" });
            }
            if(!d.HasLines)
            {
                issues.Add(new Issue { level = Level.Warning, target = d, message = $"一言「{d.name}」に文がありません" });
            }
            if(d.trigger == BarkTrigger.EnemyDefeated && d.enemy == null && d.cooldown <= 0.0f)
            {
                issues.Add(new Issue { level = Level.Info, target = d, message = $"一言「{d.name}」：どの敵を倒しても出て、次に出せるまでが 0 秒です（敵が多いとしゃべりすぎます）" });
            }
            if(d.trigger == BarkTrigger.Signal && d.signal == null)
            {
                issues.Add(new Issue { level = Level.Error, target = d, message = $"一言「{d.name}」：きっかけが「合図が来た」なのに「合図」が空です（出ません）" });
            }
            if(names.TryGetValue(d.name, out string other))
            {
                issues.Add(new Issue { level = Level.Warning, target = d, message = $"一言「{d.name}」と同じ名前が {other} にあります（「ゲームの中で1回だけ」の記録が混ざります）" });
            }
            else { names.Add(d.name, path); }
        }
    }

    /// <summary>
    /// 調べる物（Intaractable）の子に Update があると、土台の Update（E キーで調べる）が呼ばれなくなる
    /// </summary>
    private static void CheckInteractableUpdate(List<Issue> issues)
    {
        const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
        foreach(System.Type type in TypeCache.GetTypesDerivedFrom<Intaractable>())
        {
            if(type.GetMethod("Update", Flags, null, System.Type.EmptyTypes, null) == null) { continue; }
            issues.Add(new Issue { level = Level.Error, message = $"プログラム：{type.Name} に Update があるため、E キーで調べられません（Intaractable の Update が呼ばれない）。毎フレームの処理は LateUpdate に書いてください" });
        }
    }

    public static void CheckProject(List<Issue> issues)
    {
        CheckEnemyAssets(issues);
        CheckBarkAssets(issues);
        CheckInteractableUpdate(issues);

        // Build Settings
        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if(scene.enabled && !File.Exists(scene.path))
            {
                issues.Add(new Issue { level = Level.Error, message = $"Build Settings のシーン「{scene.path}」のファイルがありません" });
            }
        }

        // アイテム
        string folder = "Assets/Resources/" + ItemData.ResourcesFolder + "/";
        var itemIds = new Dictionary<string, string>();
        foreach(string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if(item == null) { continue; }

            if(!path.StartsWith(folder))
            {
                issues.Add(new Issue { level = Level.Error, target = item, message = $"ItemData「{item.name}」が {folder} の外にあります（セーブから復元できません）" });
            }

            if(itemIds.TryGetValue(item.Id, out string other))
            {
                issues.Add(new Issue { level = Level.Error, target = item, message = $"アイテム ID「{item.Id}」が {other} と重なっています" });
            }
            else { itemIds.Add(item.Id, path); }
        }

        // フラグ
        var flagIds = new Dictionary<string, string>();
        foreach(string guid in AssetDatabase.FindAssets("t:GameFlag"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameFlag flag = AssetDatabase.LoadAssetAtPath<GameFlag>(path);
            if(flag == null) { continue; }

            if(flagIds.TryGetValue(flag.Id, out string other))
            {
                issues.Add(new Issue { level = Level.Error, target = flag, message = $"フラグ ID「{flag.Id}」が {other} と重なっています（同じフラグとして扱われます）" });
            }
            else { flagIds.Add(flag.Id, path); }
        }

        // 会話
        foreach(string guid in AssetDatabase.FindAssets("t:DialogueData"))
        {
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid));
            if(data != null && (data.Lines == null || data.Lines.Length == 0))
            {
                issues.Add(new Issue { level = Level.Warning, target = data, message = $"会話データ「{data.name}」に文がありません" });
            }
        }
    }

    //=====================================================================
    // 補助
    //=====================================================================

    private class Context
    {
        private readonly Scene scene;
        private readonly List<Issue> issues;
        private readonly Dictionary<System.Type, object> cache = new Dictionary<System.Type, object>();

        public Context(Scene scene, List<Issue> issues)
        {
            this.scene = scene;
            this.issues = issues;
        }

        public List<T> Find<T>() where T : Component
        {
            if(cache.TryGetValue(typeof(T), out object found)) { return (List<T>)found; }

            var list = new List<T>();
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<T>(true));
            }
            cache[typeof(T)] = list;
            return list;
        }

        public void Add(Level level, Object target, string message)
        {
            GameObject go = target as GameObject;
            if(go == null && target is Component c) { go = c.gameObject; }

            issues.Add(new Issue
            {
                level = level,
                sceneName = scene.name,
                scenePath = scene.path,
                objectPath = go != null ? HierarchyPath(go.transform) : "",
                target = go,
                message = message,
            });
        }
    }

    private static bool HasTrigger(GameObject go)
    {
        foreach(Collider2D col in go.GetComponents<Collider2D>())
        {
            if(col.isTrigger) { return true; }
        }
        return false;
    }

    /// <summary>
    /// 配列の要素の relative の参照が空のものがあるか
    /// </summary>
    private static bool HasNullRef(SerializedProperty array, string relative)
    {
        if(array == null || !array.isArray) { return false; }

        for(int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty p = array.GetArrayElementAtIndex(i).FindPropertyRelative(relative);
            if(p != null && p.objectReferenceValue == null) { return true; }
        }
        return false;
    }

    private static bool HasNullElement(SerializedProperty array)
    {
        for(int i = 0; i < array.arraySize; i++)
        {
            if(array.GetArrayElementAtIndex(i).objectReferenceValue == null) { return true; }
        }
        return false;
    }

    private static bool ContainsRef(SerializedProperty array, Object target)
    {
        for(int i = 0; i < array.arraySize; i++)
        {
            if(array.GetArrayElementAtIndex(i).objectReferenceValue == target) { return true; }
        }
        return false;
    }

    public static string HierarchyPath(Transform target)
    {
        string path = target.name;
        for(Transform parent = target.parent; parent != null; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }
}
