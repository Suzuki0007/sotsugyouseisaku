---
paths:
  - "Assets/Script/Player/**"
  - "Assets/Script/Attack/**"
  - "Assets/Script/Bullet/**"
  - "Assets/Script/Pool/**"
  - "Assets/Script/CharaBase/**"
  - "Assets/Script/StateMachine/**"
  - "Assets/Script/UI/AttackArcGauge/**"
  - "Assets/Prefab/Players/**"
  - "Assets/Prefab/Bullet/**"
---

# プレイヤーの操作・攻撃・弾（これらを触るとき）

ファイルごとの役割は `Docs/code-map.md` の「3. > プレイヤー・キャラクター」「攻撃・弾・敵」。2人の仕掛け・相棒の一言は `rules/gimmick-party.md`、敵とダメージは `rules/enemy.md`。
**ほとんどが成田さんのコード**（`rules/team-code.md`）。

## 操作（キー）
- 移動：WASD／矢印。C：操作キャラの切り替え（`PlayerSwitchManager`）。F：相棒を待たせる・呼び戻す。
- ノア：Space＝周りに範囲攻撃（`AreaAttackController`）、R＝ゲージ → Space で判定 → 倍率つき（`TimingGaugeAttackController`。判定待ちの間は `TimingGaugeUI.IsJudging`）、Q＝弾消しエリア（`NoahBulletDeleteAreaController`）。
- シグル：マウスの左クリックで射撃（`PlayerInput` が Update で直接読む → `SigleShooter`）。
- E：調べる（`Intaractable`）。会話の送りは Space・Enter（会話中は攻撃が出ない）。
- 攻撃は「操作しているキャラの分だけ」出る（`AttackController` の基底）。会話・イベント中は入力停止（`Common/InputBlock`）。

## 決めてあること・落とし穴
- **LateUpdate の順番**：PlayerFollower（0）→ PlayerMapLimit（100）→ PlayerHistory（200）→ CharacterAnimLink（300）。`DefaultExecutionOrder` で決めてある。崩すと、ついてくる相棒が端で震える。
- `PlayerMovement` は FixedUpdate で毎回速さを 0 にしてから動かす（押されて滑らない）。操作できないときは向きと速さを消す。
- マップの範囲に収めるのは `PlayerMapLimit.ClampToMap`（範囲は `Map/MapArea`。`rules/map-room.md`）。
- 追従の距離は「何フレーム前の位置か」なので fps で変わる（既知の問題、code-map）。
- 弾は `UnityEngine.Pool`（初期 300・最大 1000）。弾を `Destroy` しない・`Instantiate` しない（プールに返す）。1発だけ変えるのは `BulletBase.Configure`、敵の弾の当たり方は `EnemyBullet.SetRules`（取り出すたびに戻す）。
- 同じ攻撃で同じ Health には1回だけ当たる（本体と部位の二重ヒット防止）。ダメージは `DamageUtility.Apply` を通す。
- `StateMachine/*`・`ComponentBase` は未使用（`StateMachine.Update` に括弧の誤り）。新しいコードで使わない。
- **ノア・シグルのプレハブに FootstepSe がない**（足音が鳴らない。`rules/audio.md`）。

## 確かめ方
- `unity-playtest` の `press.ps1`（キー）・`event.ps1 -State`（2人の状態）。攻撃や弾を変えたら `perf-review` の `scan.ps1`（毎フレームの確保）も。
